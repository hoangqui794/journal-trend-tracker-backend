using System.Text;
using System.Text.Json;
using PaperService.DTOs;

namespace PaperService.Clients
{
    public class TrendServiceClient : ITrendServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TrendServiceClient> _logger;

        public TrendServiceClient(HttpClient httpClient, ILogger<TrendServiceClient> logger, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            var secret = configuration["InternalSecret"] ?? "default_internal_secret_key_123";
            if (!_httpClient.DefaultRequestHeaders.Contains("X-Internal-Secret"))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Internal-Secret", secret);
            }
        }

        public async Task LogSearchHistoryAsync(SearchHistoryLogDto dto)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/trends/search-history", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to log search history to TrendService. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                // Fire and forget, do not throw
                _logger.LogError(ex, "Exception occurred while logging search history to TrendService");
            }
        }

        public async Task RecalculateSnapshotAsync(RecalculateSnapshotDto dto)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/trends/snapshots/recalculate", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to recalculate snapshot to TrendService. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while recalculating snapshot to TrendService");
            }
        }
        public async Task RecalculateJournalSnapshotAsync(RecalculateJournalSnapshotDto dto)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/trends/snapshots/journals/recalculate", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to recalculate journal snapshot to TrendService. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while recalculating journal snapshot to TrendService");
            }
        }

        public async Task RecalculateTopicSnapshotAsync(RecalculateTopicSnapshotDto dto)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/trends/snapshots/topics/recalculate", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to recalculate topic snapshot to TrendService. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while recalculating topic snapshot to TrendService");
            }
        }

        public async Task RecalculateAuthorSnapshotAsync(RecalculateAuthorSnapshotDto dto)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(dto), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("/api/trends/snapshots/authors/recalculate", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"Failed to recalculate author snapshot to TrendService. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while recalculating author snapshot to TrendService");
            }
        }
    }
}
