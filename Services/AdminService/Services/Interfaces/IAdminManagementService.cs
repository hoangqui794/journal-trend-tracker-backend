using AdminService.Models.Dtos;
using AdminService.Models.Entities;

namespace AdminService.Services.Interfaces;

public interface IAdminManagementService
{
    Task<ProxyResponse> GetUsersAsync();
    Task<ProxyResponse> ToggleUserStatusAsync(Guid userId, Guid adminUserId, string? ipAddress);
    Task<ProxyResponse> DeactivateUserAsync(Guid userId, Guid adminUserId, string? ipAddress);
    Task<IReadOnlyList<ApiSource>> GetApiSourcesAsync();
    Task<ApiSource?> ToggleApiSourceAsync(Guid id, Guid adminUserId, string? ipAddress);
    Task<ProxyResponse> GetSyncJobsAsync();
    Task<IReadOnlyList<SystemSetting>> GetSettingsAsync();
    Task UpdateSettingsAsync(IEnumerable<UpdateSystemSettingRequest> requests, Guid adminUserId, string? ipAddress);
    Task<IReadOnlyList<AuditLog>> GetLogsAsync(int limit);
    Task<ProxyResponse> TriggerSyncAsync(Guid adminUserId, string? ipAddress);
    Task<ProxyResponse> WipeMockDataAsync(Guid adminUserId, string? ipAddress);
}
