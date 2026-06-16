using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiGateway;

public class KeepAliveService : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<KeepAliveService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public KeepAliveService(IConfiguration configuration, ILogger<KeepAliveService> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KeepAliveService started.");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var clusters = _configuration.GetSection("ReverseProxy:Clusters").GetChildren();
                var urlsToPing = new List<string>();

                foreach (var cluster in clusters)
                {
                    var destinations = cluster.GetSection("Destinations").GetChildren();
                    foreach (var destination in destinations)
                    {
                        var address = destination.GetValue<string>("Address");
                        if (!string.IsNullOrEmpty(address))
                        {
                            // Try to ping the health endpoint if it exists, otherwise just the base address
                            urlsToPing.Add($"{address.TrimEnd('/')}/health");
                        }
                    }
                }

                if (urlsToPing.Any())
                {
                    _logger.LogInformation("Pinging {Count} downstream services to wake them up/keep them alive...", urlsToPing.Count);
                    using var client = _httpClientFactory.CreateClient();
                    client.Timeout = TimeSpan.FromSeconds(10); // Short timeout, we just want to trigger a wake-up

                    var tasks = urlsToPing.Select(async url =>
                    {
                        try
                        {
                            _logger.LogInformation("Pinging {Url}", url);
                            var response = await client.GetAsync(url, stoppingToken);
                            _logger.LogInformation("Pinged {Url} - Status: {StatusCode}", url, response.StatusCode);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning("Failed to ping {Url}: {Message}", url, ex.Message);
                        }
                    });

                    await Task.WhenAll(tasks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in KeepAliveService");
            }

            // Wait 14 minutes before pinging again (Render sleeps after 15 mins of inactivity)
            await Task.Delay(TimeSpan.FromMinutes(14), stoppingToken);
        }
    }
}
