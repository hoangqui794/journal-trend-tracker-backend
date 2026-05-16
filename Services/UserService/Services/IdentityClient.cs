using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace UserService.Services
{
    public interface IIdentityClient
    {
        Task<bool> ValidateUserExistsAsync(Guid userId);
    }

    public class IdentityClient : IIdentityClient
    {
        private readonly HttpClient _httpClient;

        public IdentityClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> ValidateUserExistsAsync(Guid userId)
        {
            try
            {
                // In a real microservice, this would be the actual URL of the IdentityService
                var response = await _httpClient.GetAsync($"/api/identity/users/{userId}");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
