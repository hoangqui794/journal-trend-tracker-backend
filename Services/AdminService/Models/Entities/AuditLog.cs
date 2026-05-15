using System.Text.Json;

namespace AdminService.Models.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }
    public Guid AdminUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public JsonDocument? OldValue { get; set; }
    public JsonDocument? NewValue { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
