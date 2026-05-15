using AdminService.Models.Entities;

namespace AdminService.Repositories.Interfaces;

public interface IAdminRepository
{
    Task<List<ApiSource>> GetApiSourcesAsync();
    Task<ApiSource?> GetApiSourceByIdAsync(Guid id);
    Task<List<SystemSetting>> GetSystemSettingsAsync();
    Task<SystemSetting?> GetSystemSettingByKeyAsync(string key);
    Task<List<AuditLog>> GetAuditLogsAsync(int limit);
    void AddSystemSetting(SystemSetting setting);
    void AddAuditLog(AuditLog log);
    Task SaveChangesAsync();
}
