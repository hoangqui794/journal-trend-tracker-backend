using System.Text.Json;
using PaperService.DTOs;

namespace PaperService.Clients
{
    public class AdminServiceClient : IAdminServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AdminServiceClient> _logger;

        public AdminServiceClient(HttpClient httpClient, ILogger<AdminServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<IEnumerable<ApiSourceDto>> GetApiSourcesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/admin/api-sources");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<IEnumerable<ApiSourceDto>>(content, options) ?? Array.Empty<ApiSourceDto>();
                }
                
                _logger.LogWarning($"Failed to get API sources from AdminService. Status: {response.StatusCode}");
                return Array.Empty<ApiSourceDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while fetching API sources from AdminService");
                return Array.Empty<ApiSourceDto>();
            }
        }
    }
}
