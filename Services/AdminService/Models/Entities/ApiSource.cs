namespace AdminService.Models.Entities;

public sealed class ApiSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKeyEncrypted { get; set; }
    public int RateLimitPerSec { get; set; }
    public bool IsActive { get; set; }
    public int SyncIntervalHours { get; set; }
    public string[] SupportedFields { get; set; } = [];
    public DateTimeOffset? LastSyncedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
