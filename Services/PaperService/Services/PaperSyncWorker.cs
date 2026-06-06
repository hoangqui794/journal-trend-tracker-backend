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
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Generic;

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

            // Run a sync immediately on startup after a short delay (5 seconds)
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                _logger.LogInformation("Running initial startup synchronization...");
                await DoSyncWorkAsync(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Background Sync Worker is stopping due to cancellation request.");
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during initial startup sync.");
            }

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
                var trendServiceClient = scope.ServiceProvider.GetRequiredService<ITrendServiceClient>();

                // Create a new ApiSyncJob record in the database
                var job = new ApiSyncJob
                {
                    Id = Guid.NewGuid(),
                    SourceName = "OpenAlex & Semantic Scholar",
                    SourceBaseUrl = "https://api.openalex.org",
                    QueryParams = "{\"filter\": \"default_search:Computer Science\", \"per_page\": 10}",
                    ScheduledAt = DateTime.UtcNow,
                    StartedAt = DateTime.UtcNow,
                    Status = SyncStatus.Running,
                    CreatedAt = DateTime.UtcNow
                };

                context.ApiSyncJobs.Add(job);
                await context.SaveChangesAsync(stoppingToken);

                int fetched = 0;
                int inserted = 0;
                int updated = 0;

                // Create a dedicated HTTP client with polite UA for OpenAlex
                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "mailto:admin@jts.com");
                client.Timeout = TimeSpan.FromSeconds(30);

                var keywordsUpdated = new HashSet<Keyword>();

                try
                {
                    // 1. --- OPENALEX SYNC ---
                    _logger.LogInformation("Starting OpenAlex Sync...");
                    var openAlexCursor = await context.SyncCursors.FirstOrDefaultAsync(c => c.SourceName == "OpenAlex", stoppingToken);
                    string cursorValue = openAlexCursor?.LastCursor ?? "*";

                    var openAlexUrl = $"https://api.openalex.org/works?filter=default_search:\"Computer Science\",publication_year:{DateTime.UtcNow.Year}&per_page=10&cursor={Uri.EscapeDataString(cursorValue)}";
                    
                    try
                    {
                        var openAlexResponse = await client.GetAsync(openAlexUrl, stoppingToken);
                        if (openAlexResponse.IsSuccessStatusCode)
                        {
                            var openAlexJson = await openAlexResponse.Content.ReadAsStringAsync(stoppingToken);
                            using var doc = JsonDocument.Parse(openAlexJson);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var work in results.EnumerateArray())
                                {
                                    fetched++;
                                    try
                                    {
                                        var isInserted = await ProcessOpenAlexWorkAsync(context, work, keywordsUpdated, userServiceClient, stoppingToken);
                                        if (isInserted) inserted++;
                                        else updated++;
                                    }
                                    catch (Exception workEx)
                                    {
                                        _logger.LogError(workEx, "Error parsing OpenAlex work");
                                        context.SyncErrors.Add(new SyncError
                                        {
                                            Id = Guid.NewGuid(),
                                            JobId = job.Id,
                                            ExternalId = work.TryGetProperty("id", out var idProp) ? idProp.GetString() : "unknown",
                                            ErrorType = "OpenAlexWorkParsing",
                                            ErrorDetail = workEx.ToString(),
                                            OccurredAt = DateTime.UtcNow
                                        });
                                        await context.SaveChangesAsync(stoppingToken);
                                    }
                                }
                            }

                            // Save next cursor
                            if (root.TryGetProperty("meta", out var meta) && meta.TryGetProperty("next_cursor", out var nextCursorProp))
                            {
                                var nextCursor = nextCursorProp.GetString();
                                if (openAlexCursor == null)
                                {
                                    openAlexCursor = new SyncCursor { SourceName = "OpenAlex" };
                                    context.SyncCursors.Add(openAlexCursor);
                                }
                                openAlexCursor.LastCursor = nextCursor;
                                openAlexCursor.LastSyncedAt = DateTime.UtcNow;
                                openAlexCursor.UpdatedAt = DateTime.UtcNow;
                                await context.SaveChangesAsync(stoppingToken);
                            }
                        }
                        else
                        {
                            throw new HttpRequestException($"OpenAlex API returned status code {openAlexResponse.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred during OpenAlex sync block.");
                        context.SyncErrors.Add(new SyncError
                        {
                            Id = Guid.NewGuid(),
                            JobId = job.Id,
                            ErrorType = "OpenAlexSyncBlock",
                            ErrorDetail = ex.ToString(),
                            OccurredAt = DateTime.UtcNow
                        });
                        await context.SaveChangesAsync(stoppingToken);
                    }

                    // 2. --- SEMANTIC SCHOLAR SYNC ---
                    _logger.LogInformation("Starting Semantic Scholar Sync...");
                    var sScholarCursor = await context.SyncCursors.FirstOrDefaultAsync(c => c.SourceName == "SemanticScholar", stoppingToken);
                    string offsetValue = sScholarCursor?.LastCursor ?? "0";
                    if (!int.TryParse(offsetValue, out int offset)) offset = 0;

                    var sScholarUrl = $"https://api.semanticscholar.org/graph/v1/paper/search?query=computer+science&limit=10&offset={offset}&fields=paperId,title,abstract,year,externalIds,url,citationCount,referenceCount,authors,venue,s2FieldsOfStudy";

                    try
                    {
                        var sScholarResponse = await client.GetAsync(sScholarUrl, stoppingToken);
                        if (sScholarResponse.IsSuccessStatusCode)
                        {
                            var sScholarJson = await sScholarResponse.Content.ReadAsStringAsync(stoppingToken);
                            using var doc = JsonDocument.Parse(sScholarJson);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("data", out var s2Data) && s2Data.ValueKind == JsonValueKind.Array)
                            {
                                foreach (var paperVal in s2Data.EnumerateArray())
                                {
                                    fetched++;
                                    try
                                    {
                                        var isInserted = await ProcessSemanticScholarPaperAsync(context, paperVal, keywordsUpdated, userServiceClient, stoppingToken);
                                        if (isInserted) inserted++;
                                        else updated++;
                                    }
                                    catch (Exception paperEx)
                                    {
                                        _logger.LogError(paperEx, "Error parsing Semantic Scholar paper");
                                        context.SyncErrors.Add(new SyncError
                                        {
                                            Id = Guid.NewGuid(),
                                            JobId = job.Id,
                                            ExternalId = paperVal.TryGetProperty("paperId", out var idProp) ? idProp.GetString() : "unknown",
                                            ErrorType = "SemanticScholarPaperParsing",
                                            ErrorDetail = paperEx.ToString(),
                                            OccurredAt = DateTime.UtcNow
                                        });
                                        await context.SaveChangesAsync(stoppingToken);
                                    }
                                }
                            }

                            // Save next offset
                            int nextOffset = offset + 10;
                            if (sScholarCursor == null)
                            {
                                sScholarCursor = new SyncCursor { SourceName = "SemanticScholar" };
                                context.SyncCursors.Add(sScholarCursor);
                            }
                            sScholarCursor.LastCursor = nextOffset.ToString();
                            sScholarCursor.LastSyncedAt = DateTime.UtcNow;
                            sScholarCursor.UpdatedAt = DateTime.UtcNow;
                            await context.SaveChangesAsync(stoppingToken);
                        }
                        else
                        {
                            throw new HttpRequestException($"Semantic Scholar API returned status code {sScholarResponse.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred during Semantic Scholar sync block.");
                        context.SyncErrors.Add(new SyncError
                        {
                            Id = Guid.NewGuid(),
                            JobId = job.Id,
                            ErrorType = "SemanticScholarSyncBlock",
                            ErrorDetail = ex.ToString(),
                            OccurredAt = DateTime.UtcNow
                        });
                        await context.SaveChangesAsync(stoppingToken);
                    }

                    // 3. --- RECALCULATE SNAPSHOTS IN TREND SERVICE ---
                    _logger.LogInformation($"Recalculating trend snapshots for {keywordsUpdated.Count} updated keywords...");
                    foreach (var kw in keywordsUpdated)
                    {
                        try
                        {
                            // Fetch all papers and citations for this keyword in our DB grouped by year
                            var stats = await context.PaperKeywords
                                .Where(pk => pk.KeywordId == kw.Id && pk.Paper != null && pk.Paper.PublicationYear.HasValue)
                                .Select(pk => new { Year = pk.Paper!.PublicationYear!.Value, Citation = pk.Paper.CitationCount })
                                .ToListAsync(stoppingToken);

                            var statsByYear = stats
                                .GroupBy(s => s.Year)
                                .Select(g => new {
                                    Year = g.Key,
                                    Count = g.Count(),
                                    Citations = g.Sum(s => s.Citation)
                                })
                                .ToList();

                            foreach (var stat in statsByYear)
                            {
                                var recDto = new RecalculateSnapshotDto
                                {
                                    KeywordId = kw.Id,
                                    KeywordTerm = kw.Term,
                                    Year = stat.Year,
                                    PaperCount = stat.Count,
                                    CitationSum = stat.Citations
                                };
                                await trendServiceClient.RecalculateSnapshotAsync(recDto);
                            }
                        }
                        catch (Exception recEx)
                        {
                            _logger.LogError(recEx, $"Error recalculating trend snapshot for keyword: {kw.Term}");
                        }
                    }

                    // Update the sync job status to Success
                    job.Status = SyncStatus.Success;
                    job.FinishedAt = DateTime.UtcNow;
                    job.PapersFetched = fetched;
                    job.PapersInserted = inserted;
                    job.PapersUpdated = updated;
                    context.ApiSyncJobs.Update(job);
                    await context.SaveChangesAsync(stoppingToken);

                    _logger.LogInformation($"Background sync job completed successfully. Fetched: {fetched}, Inserted: {inserted}, Updated: {updated}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fatal error occurred during sync job execution.");

                    // Update the sync job status to Failed
                    job.Status = SyncStatus.Failed;
                    job.FinishedAt = DateTime.UtcNow;
                    job.ErrorMessage = ex.Message;
                    context.ApiSyncJobs.Update(job);
                    await context.SaveChangesAsync(CancellationToken.None);
                }
            }
        }

        private async Task<bool> ProcessOpenAlexWorkAsync(PaperDbContext context, JsonElement work, HashSet<Keyword> keywordsUpdated, IUserServiceClient userServiceClient, CancellationToken stoppingToken)
        {
            var externalId = work.GetProperty("id").GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(externalId)) return false;

            var title = work.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
            
            // Reconstruct abstract
            string? paperAbstract = null;
            if (work.TryGetProperty("abstract_inverted_index", out var abstractProp) && abstractProp.ValueKind == JsonValueKind.Object)
            {
                paperAbstract = ReconstructAbstract(abstractProp);
            }

            short? publicationYear = null;
            if (work.TryGetProperty("publication_year", out var pyProp) && pyProp.TryGetInt16(out short py))
            {
                publicationYear = py;
            }

            var doi = work.TryGetProperty("doi", out var doiProp) ? doiProp.GetString() : null;
            var url = doi;
            if (work.TryGetProperty("primary_location", out var primLoc) && primLoc.ValueKind == JsonValueKind.Object)
            {
                if (primLoc.TryGetProperty("landing_page_url", out var urlProp)) url = urlProp.GetString() ?? url;
            }

            int citationCount = 0;
            if (work.TryGetProperty("cited_by_count", out var citeProp) && citeProp.TryGetInt32(out int cite))
            {
                citationCount = cite;
            }

            int referenceCount = 0;
            if (work.TryGetProperty("referenced_works", out var refProp) && refProp.ValueKind == JsonValueKind.Array)
            {
                referenceCount = refProp.GetArrayLength();
            }

            // Journal Resolution
            Journal? journal = null;
            string? journalExternalId = null;
            string? journalName = null;
            string? journalIssn = null;
            string? journalPublisher = null;

            if (work.TryGetProperty("primary_location", out var primLocObj) && primLocObj.ValueKind == JsonValueKind.Object)
            {
                if (primLocObj.TryGetProperty("source", out var sourceProp) && sourceProp.ValueKind == JsonValueKind.Object)
                {
                    if (sourceProp.TryGetProperty("id", out var idProp)) journalExternalId = idProp.GetString();
                    if (sourceProp.TryGetProperty("display_name", out var nameProp)) journalName = nameProp.GetString();
                    if (sourceProp.TryGetProperty("issn", out var issnProp) && issnProp.ValueKind == JsonValueKind.Array && issnProp.GetArrayLength() > 0)
                    {
                        journalIssn = issnProp[0].GetString();
                    }
                    if (sourceProp.TryGetProperty("publisher", out var pubProp)) journalPublisher = pubProp.GetString();
                }
            }

            if (!string.IsNullOrWhiteSpace(journalName))
            {
                var normalizedJournalName = journalName.ToLowerInvariant().Trim();
                journal = await context.Journals.FirstOrDefaultAsync(j => j.Name.ToLower() == normalizedJournalName, stoppingToken);
                if (journal == null)
                {
                    journal = new Journal
                    {
                        Id = Guid.NewGuid(),
                        ExternalId = journalExternalId ?? Guid.NewGuid().ToString(),
                        Name = journalName,
                        Issn = journalIssn,
                        Publisher = journalPublisher,
                        Field = "Computer Science",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.Journals.Add(journal);
                    await context.SaveChangesAsync(stoppingToken);
                }
            }

            // Extract Fields of study from topics
            var fieldsOfStudy = new List<string>();
            if (work.TryGetProperty("topics", out var topicsProp) && topicsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var topic in topicsProp.EnumerateArray())
                {
                    if (topic.TryGetProperty("field", out var fieldProp) && fieldProp.ValueKind == JsonValueKind.Object)
                    {
                        if (fieldProp.TryGetProperty("display_name", out var fieldNameProp))
                        {
                            var fn = fieldNameProp.GetString();
                            if (!string.IsNullOrWhiteSpace(fn) && !fieldsOfStudy.Contains(fn))
                            {
                                fieldsOfStudy.Add(fn);
                            }
                        }
                    }
                }
            }

            var existingPaper = await context.Papers.FirstOrDefaultAsync(p => p.ExternalId == externalId && p.Source == PaperSource.OpenAlex, stoppingToken);
            if (existingPaper != null)
            {
                existingPaper.CitationCount = citationCount;
                existingPaper.ReferenceCount = referenceCount;
                existingPaper.UpdatedAt = DateTime.UtcNow;
                existingPaper.SyncedAt = DateTime.UtcNow;
                context.Papers.Update(existingPaper);
                await context.SaveChangesAsync(stoppingToken);
                return false; // updated
            }

            // Insert new paper
            var paper = new Paper
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Source = PaperSource.OpenAlex,
                Title = title,
                Abstract = paperAbstract,
                PublicationYear = publicationYear,
                Doi = doi,
                Url = url,
                CitationCount = citationCount,
                ReferenceCount = referenceCount,
                FieldsOfStudy = fieldsOfStudy,
                JournalId = journal?.Id,
                RawData = JsonSerializer.Serialize(work),
                SyncedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Papers.Add(paper);
            await context.SaveChangesAsync(stoppingToken);

            // Authors Resolution
            var authorLinks = new List<PaperAuthor>();
            if (work.TryGetProperty("authorships", out var authorshipsProp) && authorshipsProp.ValueKind == JsonValueKind.Array)
            {
                short order = 0;
                foreach (var authorship in authorshipsProp.EnumerateArray())
                {
                    if ( authorship.TryGetProperty("author", out var authorProp) && authorProp.ValueKind == JsonValueKind.Object)
                    {
                        var aName = authorProp.TryGetProperty("display_name", out var nameProp) ? nameProp.GetString() : null;
                        var aExtId = authorProp.TryGetProperty("id", out var extIdProp) ? extIdProp.GetString() : null;
                        var aOrcid = authorProp.TryGetProperty("orcid", out var orcProp) ? orcProp.GetString() : null;
                        string? aAff = null;

                        if (authorship.TryGetProperty("institutions", out var insts) && insts.ValueKind == JsonValueKind.Array && insts.GetArrayLength() > 0)
                        {
                            var firstInst = insts[0];
                            if (firstInst.TryGetProperty("display_name", out var instName))
                            {
                                aAff = instName.GetString();
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(aName))
                        {
                            var author = await context.Authors.FirstOrDefaultAsync(a => a.Name.ToLower() == aName.ToLower(), stoppingToken);
                            if (author == null)
                            {
                                author = new Author
                                {
                                    Id = Guid.NewGuid(),
                                    ExternalId = aExtId ?? Guid.NewGuid().ToString(),
                                    Name = aName,
                                    Affiliation = aAff,
                                    Orcid = aOrcid,
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };
                                context.Authors.Add(author);
                                await context.SaveChangesAsync(stoppingToken);
                            }

                            authorLinks.Add(new PaperAuthor
                            {
                                PaperId = paper.Id,
                                AuthorId = author.Id,
                                AuthorOrder = order++
                            });
                        }
                    }
                }
            }

            if (authorLinks.Any())
            {
                context.PaperAuthors.AddRange(authorLinks);
                await context.SaveChangesAsync(stoppingToken);
            }

            // Keywords Resolution (from concepts & topics)
            var extractedKeywords = new List<(string Term, double Score)>();
            if (work.TryGetProperty("concepts", out var conceptsProp) && conceptsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var conceptVal in conceptsProp.EnumerateArray())
                {
                    var term = conceptVal.TryGetProperty("display_name", out var dn) ? dn.GetString() : null;
                    double score = 0.5;
                    if (conceptVal.TryGetProperty("score", out var scoreProp))
                    {
                        scoreProp.TryGetDouble(out score);
                    }
                    if (!string.IsNullOrWhiteSpace(term) && score >= 0.3)
                    {
                        extractedKeywords.Add((term, score));
                    }
                }
            }

            if (work.TryGetProperty("topics", out var topicsProperty) && topicsProperty.ValueKind == JsonValueKind.Array)
            {
                foreach (var topicVal in topicsProperty.EnumerateArray())
                {
                    var term = topicVal.TryGetProperty("display_name", out var dn) ? dn.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(term) && !extractedKeywords.Any(k => k.Term.Equals(term, StringComparison.OrdinalIgnoreCase)))
                    {
                        extractedKeywords.Add((term, 0.8));
                    }
                }
            }

            foreach (var kwItem in extractedKeywords)
            {
                var normalized = kwItem.Term.ToLowerInvariant().Trim();
                var keyword = await context.Keywords.FirstOrDefaultAsync(k => k.NormalizedTerm == normalized, stoppingToken);
                if (keyword == null)
                {
                    keyword = new Keyword
                    {
                        Id = Guid.NewGuid(),
                        Term = kwItem.Term,
                        NormalizedTerm = normalized,
                        Source = KeywordSource.Api,
                        UsageCount = 1,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Keywords.Add(keyword);
                    await context.SaveChangesAsync(stoppingToken);
                }
                else
                {
                    keyword.UsageCount++;
                    context.Keywords.Update(keyword);
                    await context.SaveChangesAsync(stoppingToken);
                }

                keywordsUpdated.Add(keyword);

                var pk = new PaperKeyword
                {
                    PaperId = paper.Id,
                    KeywordId = keyword.Id,
                    RelevanceScore = kwItem.Score
                };
                context.PaperKeywords.Add(pk);
                await context.SaveChangesAsync(stoppingToken);

                // Trigger Notification to UserService
                var notificationDto = new NotificationTriggerDto
                {
                    Keyword = keyword.Term,
                    PaperId = paper.Id,
                    PaperTitle = paper.Title
                };
                await userServiceClient.TriggerNotificationAsync(notificationDto);
            }

            return true; // inserted
        }

        private async Task<bool> ProcessSemanticScholarPaperAsync(PaperDbContext context, JsonElement paperVal, HashSet<Keyword> keywordsUpdated, IUserServiceClient userServiceClient, CancellationToken stoppingToken)
        {
            var externalId = paperVal.GetProperty("paperId").GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(externalId)) return false;

            var title = paperVal.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "" : "";
            var paperAbstract = paperVal.TryGetProperty("abstract", out var abstractProp) ? abstractProp.GetString() : null;

            short? publicationYear = null;
            if (paperVal.TryGetProperty("year", out var yearProp) && yearProp.TryGetInt16(out short year))
            {
                publicationYear = year;
            }

            string? doi = null;
            if (paperVal.TryGetProperty("externalIds", out var extIds) && extIds.ValueKind == JsonValueKind.Object)
            {
                if (extIds.TryGetProperty("DOI", out var doiProp)) doi = doiProp.GetString();
            }

            var url = paperVal.TryGetProperty("url", out var urlProp) ? urlProp.GetString() : null;

            int citationCount = 0;
            if (paperVal.TryGetProperty("citationCount", out var citeProp) && citeProp.TryGetInt32(out int cite))
            {
                citationCount = cite;
            }

            int referenceCount = 0;
            if (paperVal.TryGetProperty("referenceCount", out var refProp) && refProp.TryGetInt32(out int refCount))
            {
                referenceCount = refCount;
            }

            // Journal Resolution
            Journal? journal = null;
            string? journalName = null;
            if (paperVal.TryGetProperty("venue", out var venueProp) && venueProp.ValueKind == JsonValueKind.String)
            {
                journalName = venueProp.GetString();
            }

            if (!string.IsNullOrWhiteSpace(journalName))
            {
                var normalizedJournalName = journalName.ToLowerInvariant().Trim();
                journal = await context.Journals.FirstOrDefaultAsync(j => j.Name.ToLower() == normalizedJournalName, stoppingToken);
                if (journal == null)
                {
                    journal = new Journal
                    {
                        Id = Guid.NewGuid(),
                        ExternalId = journalName,
                        Name = journalName,
                        Field = "Computer Science",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.Journals.Add(journal);
                    await context.SaveChangesAsync(stoppingToken);
                }
            }

            // Fields of Study
            var fieldsOfStudy = new List<string>();
            var extractedKeywords = new List<(string Term, double Score)>();
            if (paperVal.TryGetProperty("s2FieldsOfStudy", out var fieldsProp) && fieldsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var fVal in fieldsProp.EnumerateArray())
                {
                    if (fVal.TryGetProperty("category", out var catProp))
                    {
                        var cat = catProp.GetString();
                        if (!string.IsNullOrWhiteSpace(cat))
                        {
                            fieldsOfStudy.Add(cat);
                            extractedKeywords.Add((cat, 0.9));
                        }
                    }
                }
            }

            var existingPaper = await context.Papers.FirstOrDefaultAsync(p => p.ExternalId == externalId && p.Source == PaperSource.SemanticScholar, stoppingToken);
            if (existingPaper != null)
            {
                existingPaper.CitationCount = citationCount;
                existingPaper.ReferenceCount = referenceCount;
                existingPaper.UpdatedAt = DateTime.UtcNow;
                existingPaper.SyncedAt = DateTime.UtcNow;
                context.Papers.Update(existingPaper);
                await context.SaveChangesAsync(stoppingToken);
                return false; // updated
            }

            // Insert new paper
            var paper = new Paper
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Source = PaperSource.SemanticScholar,
                Title = title,
                Abstract = paperAbstract,
                PublicationYear = publicationYear,
                Doi = doi,
                Url = url,
                CitationCount = citationCount,
                ReferenceCount = referenceCount,
                FieldsOfStudy = fieldsOfStudy,
                JournalId = journal?.Id,
                RawData = JsonSerializer.Serialize(paperVal),
                SyncedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Papers.Add(paper);
            await context.SaveChangesAsync(stoppingToken);

            // Authors Resolution
            var authorLinks = new List<PaperAuthor>();
            if (paperVal.TryGetProperty("authors", out var s2Authors) && s2Authors.ValueKind == JsonValueKind.Array)
            {
                short order = 0;
                foreach (var authorVal in s2Authors.EnumerateArray())
                {
                    var aName = authorVal.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
                    var aExtId = authorVal.TryGetProperty("authorId", out var extIdProp) ? extIdProp.GetString() : null;

                    if (!string.IsNullOrWhiteSpace(aName))
                    {
                        var author = await context.Authors.FirstOrDefaultAsync(a => a.Name.ToLower() == aName.ToLower(), stoppingToken);
                        if (author == null)
                        {
                            author = new Author
                            {
                                Id = Guid.NewGuid(),
                                ExternalId = aExtId ?? Guid.NewGuid().ToString(),
                                Name = aName,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            context.Authors.Add(author);
                            await context.SaveChangesAsync(stoppingToken);
                        }

                        authorLinks.Add(new PaperAuthor
                        {
                            PaperId = paper.Id,
                            AuthorId = author.Id,
                            AuthorOrder = order++
                        });
                    }
                }
            }

            if (authorLinks.Any())
            {
                context.PaperAuthors.AddRange(authorLinks);
                await context.SaveChangesAsync(stoppingToken);
            }

            // Keywords Resolution
            foreach (var kwItem in extractedKeywords)
            {
                var normalized = kwItem.Term.ToLowerInvariant().Trim();
                var keyword = await context.Keywords.FirstOrDefaultAsync(k => k.NormalizedTerm == normalized, stoppingToken);
                if (keyword == null)
                {
                    keyword = new Keyword
                    {
                        Id = Guid.NewGuid(),
                        Term = kwItem.Term,
                        NormalizedTerm = normalized,
                        Source = KeywordSource.Api,
                        UsageCount = 1,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Keywords.Add(keyword);
                    await context.SaveChangesAsync(stoppingToken);
                }
                else
                {
                    keyword.UsageCount++;
                    context.Keywords.Update(keyword);
                    await context.SaveChangesAsync(stoppingToken);
                }

                keywordsUpdated.Add(keyword);

                var pk = new PaperKeyword
                {
                    PaperId = paper.Id,
                    KeywordId = keyword.Id,
                    RelevanceScore = kwItem.Score
                };
                context.PaperKeywords.Add(pk);
                await context.SaveChangesAsync(stoppingToken);

                // Trigger Notification to UserService
                var notificationDto = new NotificationTriggerDto
                {
                    Keyword = keyword.Term,
                    PaperId = paper.Id,
                    PaperTitle = paper.Title
                };
                await userServiceClient.TriggerNotificationAsync(notificationDto);
            }

            return true; // inserted
        }

        private static string ReconstructAbstract(JsonElement? invertedIndexElement)
        {
            if (invertedIndexElement == null || invertedIndexElement.Value.ValueKind != JsonValueKind.Object)
                return string.Empty;

            var dict = new SortedDictionary<int, string>();
            foreach (var property in invertedIndexElement.Value.EnumerateObject())
            {
                var word = property.Name;
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var indexElement in property.Value.EnumerateArray())
                    {
                        if (indexElement.TryGetInt32(out int pos))
                        {
                            dict[pos] = word;
                        }
                    }
                }
            }
            return string.Join(" ", dict.Values);
        }
    }
}
