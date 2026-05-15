using AdminService.Models.Dtos;
using AdminService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AdminService.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(IAdminManagementService adminManagementService) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var response = await adminManagementService.GetUsersAsync();
        return ToContentResult(response);
    }

    [HttpPut("users/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleUserStatus(Guid id)
    {
        var response = await adminManagementService.ToggleUserStatusAsync(
            id,
            GetAdminUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return ToContentResult(response);
    }

    [HttpGet("api-sources")]
    public async Task<IActionResult> GetApiSources()
    {
        var data = await adminManagementService.GetApiSourcesAsync();
        return Ok(data);
    }

    [HttpPut("api-sources/{id:guid}/toggle")]
    public async Task<IActionResult> ToggleApiSource(Guid id)
    {
        var source = await adminManagementService.ToggleApiSourceAsync(
            id,
            GetAdminUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return source is null
            ? NotFound(new { message = "Api source not found." })
            : Ok(source);
    }

    [HttpGet("sync-jobs")]
    public async Task<IActionResult> GetSyncJobs()
    {
        var response = await adminManagementService.GetSyncJobsAsync();
        return ToContentResult(response);
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var data = await adminManagementService.GetSettingsAsync();
        return Ok(data);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] IEnumerable<UpdateSystemSettingRequest> requests)
    {
        await adminManagementService.UpdateSettingsAsync(
            requests,
            GetAdminUserId(),
            HttpContext.Connection.RemoteIpAddress?.ToString());

        return NoContent();
    }

    [HttpGet("logs")]
    public async Task<IActionResult> GetLogs([FromQuery] int limit = 100)
    {
        var logs = await adminManagementService.GetLogsAsync(limit);
        return Ok(logs);
    }

    private Guid GetAdminUserId()
    {
        var value = Request.Headers["X-Admin-User-Id"].FirstOrDefault();
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    private IActionResult ToContentResult(ProxyResponse response)
    {
        return new ContentResult
        {
            Content = response.Content,
            ContentType = response.ContentType,
            StatusCode = (int)response.StatusCode
        };
    }
}
