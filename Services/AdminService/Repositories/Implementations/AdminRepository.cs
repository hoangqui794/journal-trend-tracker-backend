using AdminService.Data;
using AdminService.Models.Entities;
using AdminService.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Repositories.Implementations;

public sealed class AdminRepository(AdminDbContext dbContext) : IAdminRepository
{
    public Task<List<ApiSource>> GetApiSourcesAsync()
    {
        return dbContext.ApiSources
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public Task<ApiSource?> GetApiSourceByIdAsync(Guid id)
    {
        return dbContext.ApiSources.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<SystemSetting>> GetSystemSettingsAsync()
    {
        return dbContext.SystemSettings
            .OrderBy(x => x.Key)
            .ToListAsync();
    }

    public Task<SystemSetting?> GetSystemSettingByKeyAsync(string key)
    {
        return dbContext.SystemSettings.FirstOrDefaultAsync(x => x.Key == key);
    }

    public Task<List<AuditLog>> GetAuditLogsAsync(int limit)
    {
        return dbContext.AuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public void AddSystemSetting(SystemSetting setting)
    {
        dbContext.SystemSettings.Add(setting);
    }

    public void AddAuditLog(AuditLog log)
    {
        dbContext.AuditLogs.Add(log);
    }

    public Task SaveChangesAsync()
    {
        return dbContext.SaveChangesAsync();
    }
}
