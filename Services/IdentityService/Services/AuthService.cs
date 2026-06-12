using BCrypt.Net;
using Google.Apis.Auth;
using IdentityService.Models;
using IdentityService.Repositories;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityService.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;

        public AuthService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, ITokenService tokenService)
        {
            _userRepository = userRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
        }

        public async Task<bool> RegisterAsync(string fullName, string email, string password, string role)
        {
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null) return false;

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                PasswordHash = passwordHash,
                Role = Enum.Parse<UserRole>(role, true),
                Status = UserStatus.active
            };

            await _userRepository.CreateAsync(user);
            return true;
        }

        public async Task<(string AccessToken, string RefreshToken)?> LoginAsync(string email, string password)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;

            if (user.Status == UserStatus.locked) return null;

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshTokenValue = _tokenService.GenerateRefreshToken();

            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshTokenValue,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _refreshTokenRepository.CreateAsync(refreshToken);

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            return (accessToken, refreshTokenValue);
        }

        public async Task<(string AccessToken, string RefreshToken)?> RefreshTokenAsync(string token, string refreshToken)
        {
            var principal = _tokenService.GetPrincipalFromExpiredToken(token);
            if (principal == null) return null;

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null) return null;

            var userId = Guid.Parse(userIdClaim.Value);
            var savedRefreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);

            if (savedRefreshToken == null || savedRefreshToken.UserId != userId || !savedRefreshToken.IsActive)
                return null;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || user.Status == UserStatus.locked) return null;

            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var newRefreshTokenValue = _tokenService.GenerateRefreshToken();

            savedRefreshToken.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(savedRefreshToken);

            var newRefreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = newRefreshTokenValue,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            await _refreshTokenRepository.CreateAsync(newRefreshToken);

            return (newAccessToken, newRefreshTokenValue);
        }

        public async Task<bool> LogoutAsync(string refreshToken)
        {
            var savedRefreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            if (savedRefreshToken == null) return false;

            savedRefreshToken.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(savedRefreshToken);
            return true;
        }

        public async Task<(string AccessToken, string RefreshToken)?> GoogleLoginAsync(string idToken)
        {
            try
            {
                var validationSettings = new GoogleJsonWebSignature.ValidationSettings
                {
                    IssuedAtClockTolerance = TimeSpan.FromMinutes(5),
                    ExpirationTimeClockTolerance = TimeSpan.FromMinutes(5)
                };
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);
                var email = payload.Email;

                var user = await _userRepository.GetByEmailAsync(email);
                if (user == null)
                {
                    user = new User
                    {
                        Id = Guid.NewGuid(),
                        FullName = payload.Name ?? email,
                        Email = email,
                        Provider = AuthProvider.google,
                        ProviderId = payload.Subject,
                        Role = UserRole.student,
                        Status = UserStatus.active,
                        AvatarUrl = payload.Picture
                    };
                    await _userRepository.CreateAsync(user);
                }

                if (user.Status == UserStatus.locked) return null;

                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshTokenValue = _tokenService.GenerateRefreshToken();

                var refreshToken = new RefreshToken
                {
                    Id = Guid.NewGuid(),
                    Token = refreshTokenValue,
                    UserId = user.Id,
                    ExpiresAt = DateTime.UtcNow.AddDays(7)
                };

                await _refreshTokenRepository.CreateAsync(refreshToken);

                user.LastLoginAt = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                return (accessToken, refreshTokenValue);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoogleLogin] Verification failed: {ex}");
                return null;
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<bool> UpdateUserStatusAsync(Guid id, string status)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return false;

            user.Status = Enum.Parse<UserStatus>(status, true);
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return true;
        }
    }
}
