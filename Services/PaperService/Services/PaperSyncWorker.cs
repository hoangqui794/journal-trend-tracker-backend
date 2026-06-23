using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

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
                using (var scope = _serviceProvider.CreateScope())
                {
                    var syncJobService = scope.ServiceProvider.GetRequiredService<ISyncJobService>();
                    await syncJobService.DoSyncWorkAsync(stoppingToken);
                }
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
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var syncJobService = scope.ServiceProvider.GetRequiredService<ISyncJobService>();
                        await syncJobService.DoSyncWorkAsync(stoppingToken);
                    }
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
    }
}
