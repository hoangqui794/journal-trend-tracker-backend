using IdentityService.Models;
using System;
using System.Threading.Tasks;

namespace IdentityService.Services
{
    public interface IAuthService
    {
        Task<bool> RegisterAsync(string fullName, string email, string password, string role);
        Task<(string AccessToken, string RefreshToken)?> LoginAsync(string email, string password);
        Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string token, string refreshToken);
        Task<bool> LogoutAsync(string refreshToken);
        Task<(string AccessToken, string RefreshToken)?> GoogleLoginAsync(string idToken);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<bool> UpdateUserStatusAsync(Guid id, string status);
<<<<<<< Updated upstream
=======
        Task<bool> UpdateUserDetailsAsync(Guid id, string fullName, string email);
        Task<string?> ForgotPasswordAsync(string email);
        Task<bool> VerifyResetTokenAsync(string email, string token);
        Task<bool> ResetPasswordAsync(string email, string token, string newPassword);
>>>>>>> Stashed changes
    }
}
