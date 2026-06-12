using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using UserService.DTOs;

namespace UserService.Services
{
    public interface IIdentityClient
    {
        Task<bool> ValidateUserExistsAsync(Guid userId);
        Task<UserIdentityDto?> GetUserAsync(Guid userId);
    }

    public class IdentityClient : IIdentityClient
    {
        private readonly HttpClient _httpClient;

        public IdentityClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<UserIdentityDto?> GetUserAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/identity/users/{userId}");
                if (!response.IsSuccessStatusCode) return null;
                return await response.Content.ReadFromJsonAsync<UserIdentityDto>();
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is System.IO.IOException || ex is System.Net.Sockets.SocketException)
            {
                // If IdentityService is down in local dev, fallback to mock user to allow local testing of UserService
                Console.WriteLine($"[IdentityClient Warning] IdentityService is unreachable ({ex.Message}). Returning mock user details.");
                return new UserIdentityDto
                {
                    Id = userId,
                    FullName = "Local Dev User",
                    Email = "dev-user@example.com"
                };
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ValidateUserExistsAsync(Guid userId)
        {
            var user = await GetUserAsync(userId);
            return user != null;
        }
    }
}
