using System.Net.Http.Json;
using TrendService.DTOs;

namespace TrendService.Services;

public class PaperServiceClient : IPaperServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PaperServiceClient> _logger;

    public PaperServiceClient(HttpClient http, ILogger<PaperServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<int> GetTotalAuthorsAsync()
    {
        try
        {
            var result = await _http.GetFromJsonAsync<TotalAuthorsResponse>("/api/papers/authors/count");
            return result?.Total ?? 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lấy total authors từ PaperService");
            return 0;
        }
    }

    public async Task<List<TopAuthorDto>> GetTopAuthorsAsync(int top)
    {
        try
        {
            var result = await _http.GetFromJsonAsync<List<TopAuthorDto>>($"/api/papers/authors/top?top={top}");
            return result ?? new List<TopAuthorDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Không thể lấy top authors từ PaperService");
            return new List<TopAuthorDto>();
        }
    }

    private class TotalAuthorsResponse
    {
        public int Total { get; set; }
    }
}
