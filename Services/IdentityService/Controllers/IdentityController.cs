using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/identity")]
    public class IdentityController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public IdentityController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var result = await _authService.RegisterAsync(model.FullName, model.Email, model.Password, "student");
            if (!result) return BadRequest("Email already exists");
            return Ok("User registered successfully");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var result = await _authService.LoginAsync(model.Email, model.Password);
            if (result == null) return Unauthorized("Invalid credentials or account locked");
            return Ok(new { AccessToken = result.Value.AccessToken, RefreshToken = result.Value.RefreshToken });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshModel model)
        {
            var result = await _authService.RefreshTokenAsync(model.AccessToken, model.RefreshToken);
            if (result == null) return BadRequest("Invalid token or refresh token");
            return Ok(new { AccessToken = result.Value.AccessToken, RefreshToken = result.Value.RefreshToken });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutModel model)
        {
            var result = await _authService.LogoutAsync(model.RefreshToken);
            if (!result) return BadRequest("Invalid refresh token");
            return Ok("Logged out successfully");
        }

        [HttpGet("auth/google")]
        public IActionResult GoogleLogin()
        {
            var clientId = _configuration["Google:ClientId"] ?? "YOUR_GOOGLE_CLIENT_ID";
            var redirectUrl = _configuration["Google:RedirectUrl"] ?? "http://localhost:5251/api/identity/auth/google/callback";
            var url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={Uri.EscapeDataString(redirectUrl)}&response_type=code&scope=email%20profile";
            return Redirect(url);
        }

        [HttpGet("auth/google/callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code)
        {
            if (string.IsNullOrEmpty(code)) return BadRequest("Authorization code is missing");

            var clientId = _configuration["Google:ClientId"] ?? "YOUR_GOOGLE_CLIENT_ID";
            var clientSecret = _configuration["Google:ClientSecret"] ?? "YOUR_GOOGLE_CLIENT_SECRET";
            var redirectUrl = _configuration["Google:RedirectUrl"] ?? "http://localhost:5251/api/identity/auth/google/callback";

            using var httpClient = new HttpClient();
            var tokenRequestParams = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", clientId },
                { "client_secret", clientSecret },
                { "redirect_uri", redirectUrl },
                { "grant_type", "authorization_code" }
            };

            var requestContent = new FormUrlEncodedContent(tokenRequestParams);
            var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", requestContent);
            if (!response.IsSuccessStatusCode)
            {
                var errorResponse = await response.Content.ReadAsStringAsync();
                return BadRequest($"Failed to exchange code for token: {errorResponse}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var doc = System.Text.Json.JsonDocument.Parse(responseContent);
            if (!doc.RootElement.TryGetProperty("id_token", out var idTokenProp))
            {
                return BadRequest("ID Token not found in response");
            }

            var idToken = idTokenProp.GetString();
            if (string.IsNullOrEmpty(idToken)) return BadRequest("ID Token is empty");

            var result = await _authService.GoogleLoginAsync(idToken);
            if (result == null) return Unauthorized("Google authentication failed");

            return Ok(new { AccessToken = result.Value.AccessToken, RefreshToken = result.Value.RefreshToken });
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _authService.GetAllUsersAsync();
                var json = System.Text.Json.JsonSerializer.Serialize(users);
                return Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stackTrace = ex.StackTrace, innerException = ex.InnerException?.Message });
            }
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(Guid id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPut("users/{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(Guid id, [FromBody] UpdateStatusModel model)
        {
            var result = await _authService.UpdateUserStatusAsync(id, model.Status);
            if (!result) return NotFound();
            return Ok("User status updated");
        }
    }

    public class RegisterModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginModel
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshModel
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutModel
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UpdateStatusModel
    {
        public string Status { get; set; } = string.Empty;
    }
}
