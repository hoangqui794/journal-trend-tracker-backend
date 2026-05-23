using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using PaperService.Data;
using PaperService.Entities;
using PaperService.Clients;
using PaperService.DTOs;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace PaperService.Services
{
    public class PaperSyncWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaperSyncWorker> _logger;

        public PaperSyncWorker(IServiceProvider serviceProvider, ILogger<PaperSyncWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background Sync Worker is starting...");

            while (!stoppingToken.IsCancellationRequested)
            {
                // Calculate time until next midnight (00:00)
                var now = DateTime.UtcNow;
                var nextRun = now.Date.AddDays(1); // 00:00 tomorrow
                var delay = nextRun - now;

                _logger.LogInformation($"Next sync scheduled at: {nextRun} UTC (in {delay.TotalHours:F2} hours)");

                try
                {
                    // Sleep until midnight, or until cancelled
                    await Task.Delay(delay, stoppingToken);

                    // Perform the sync
                    await DoSyncWorkAsync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("Background Sync Worker is stopping due to cancellation request.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during scheduled background sync cycle.");
                    // Sleep for an hour before retrying in case of an error to prevent endless spam loops
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }

        public async Task DoSyncWorkAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Background sync job started.");

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<PaperDbContext>();
                var userServiceClient = scope.ServiceProvider.GetRequiredService<IUserServiceClient>();

                // 1. Create a new ApiSyncJob record in the database
                var job = new ApiSyncJob
                {
                    Id = Guid.NewGuid(),
                    SourceName = "OpenAlex & Semantic Scholar",
                    SourceBaseUrl = "https://api.openalex.org",
                    QueryParams = "{\"filter\": \"has_abstract:true\", \"per_page\": 10}",
                    ScheduledAt = DateTime.UtcNow,
                    StartedAt = DateTime.UtcNow,
                    Status = SyncStatus.Running,
                    CreatedAt = DateTime.UtcNow
                };

                context.ApiSyncJobs.Add(job);
                await context.SaveChangesAsync(stoppingToken);

                try
                {
                    // 2. [Mock] Fetch data and save to DB
                    // Simulating API response delay
                    await Task.Delay(2000, stoppingToken);

                    int fetched = 2;
                    int inserted = 0;
                    int updated = 0;

                    var mockPapers = new[]
                    {
                        new { ExtId = "https://openalex.org/W123456", Title = "Deep Learning Trends in 2026", JournalName = "AI Journal", Keyword = "Deep Learning" },
                        new { ExtId = "https://openalex.org/W789012", Title = "Microservices Architectures in .NET 8", JournalName = "Software Engineering Review", Keyword = "Microservices" }
                    };

                    foreach (var mock in mockPapers)
                    {
                        var existingPaper = await context.Papers.FirstOrDefaultAsync(p => p.ExternalId == mock.ExtId, stoppingToken);
                        if (existingPaper == null)
                        {
                            // Ensure journal exists
                            var journal = await context.Journals.FirstOrDefaultAsync(j => j.Name == mock.JournalName, stoppingToken);
                            if (journal == null)
                            {
                                journal = new Journal
                                {
                                    Id = Guid.NewGuid(),
                                    ExternalId = Guid.NewGuid().ToString(),
                                    Name = mock.JournalName,
                                    CreatedAt = DateTime.UtcNow
                                };
                                context.Journals.Add(journal);
                                await context.SaveChangesAsync(stoppingToken);
                            }

                            // Ensure keyword exists
                            var keyword = await context.Keywords.FirstOrDefaultAsync(k => k.Term == mock.Keyword, stoppingToken);
                            if (keyword == null)
                            {
                                keyword = new Keyword
                                {
                                    Id = Guid.NewGuid(),
                                    Term = mock.Keyword,
                                    NormalizedTerm = mock.Keyword.ToLowerInvariant(),
                                    UsageCount = 1,
                                    CreatedAt = DateTime.UtcNow
                                };
                                context.Keywords.Add(keyword);
                                await context.SaveChangesAsync(stoppingToken);
                            }

                            var paper = new Paper
                            {
                                Id = Guid.NewGuid(),
                                ExternalId = mock.ExtId,
                                Title = mock.Title,
                                Abstract = "An in-depth analysis of " + mock.Title.ToLower() + ".",
                                PublicationYear = (short?)DateTime.UtcNow.Year,
                                Source = PaperSource.OpenAlex,
                                JournalId = journal.Id,
                                CitationCount = 0,
                                CreatedAt = DateTime.UtcNow
                            };

                            context.Papers.Add(paper);
                            await context.SaveChangesAsync(stoppingToken);

                            // Associate keyword
                            var paperKeyword = new PaperKeyword
                            {
                                PaperId = paper.Id,
                                KeywordId = keyword.Id,
                                RelevanceScore = 0.95
                            };
                            context.PaperKeywords.Add(paperKeyword);
                            await context.SaveChangesAsync(stoppingToken);

                            inserted++;

                            // 3. Trigger Notification to UserService
                            var notificationDto = new NotificationTriggerDto
                            {
                                Keyword = mock.Keyword,
                                PaperId = paper.Id,
                                PaperTitle = paper.Title
                            };
                            await userServiceClient.TriggerNotificationAsync(notificationDto);
                        }
                        else
                        {
                            existingPaper.CitationCount += 1;
                            context.Papers.Update(existingPaper);
                            await context.SaveChangesAsync(stoppingToken);
                            updated++;
                        }
                    }

                    // 4. Update the sync job status to Success
                    job.Status = SyncStatus.Success;
                    job.FinishedAt = DateTime.UtcNow;
                    job.PapersFetched = fetched;
                    job.PapersInserted = inserted;
                    job.PapersUpdated = updated;
                    context.ApiSyncJobs.Update(job);
                    await context.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation($"Background sync job completed successfully. Inserted: {inserted}, Updated: {updated}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during sync job execution.");

                    // Update the sync job status to Failed
                    job.Status = SyncStatus.Failed;
                    job.FinishedAt = DateTime.UtcNow;
                    job.ErrorMessage = ex.Message;
                    context.ApiSyncJobs.Update(job);
                    await context.SaveChangesAsync(CancellationToken.None);
                }
            }
        }
    }
}
