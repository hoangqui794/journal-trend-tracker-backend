using IdentityService.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace IdentityService.Controllers
{
    [ApiController]
    [Route("api/identity")]
    public class IdentityController : ControllerBase
    {
        private readonly IAuthService _authService;

        public IdentityController(IAuthService authService)
        {
            _authService = authService;
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
            var clientId = "YOUR_GOOGLE_CLIENT_ID";
            var redirectUrl = "http://localhost:5000/api/identity/auth/google/callback";
            var url = $"https://accounts.google.com/o/oauth2/v2/auth?client_id={clientId}&redirect_uri={redirectUrl}&response_type=code&scope=email%20profile";
            return Redirect(url);
        }

        [HttpGet("auth/google/callback")]
        public async Task<IActionResult> GoogleCallback([FromQuery] string code)
        {
            return Ok(new { Message = "Google callback received. Code: " + code, Note = "Implement code exchange for ID Token and call GoogleLoginAsync." });
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
