using AdminService.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Data;

public sealed class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<ApiSource> ApiSources => Set<ApiSource>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiSource>(entity =>
        {
            entity.ToTable("api_sources");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Name).HasColumnName("name");
            entity.Property(x => x.BaseUrl).HasColumnName("base_url");
            entity.Property(x => x.ApiKeyEncrypted).HasColumnName("api_key_encrypted");
            entity.Property(x => x.RateLimitPerSec).HasColumnName("rate_limit_per_sec");
            entity.Property(x => x.IsActive).HasColumnName("is_active");
            entity.Property(x => x.SyncIntervalHours).HasColumnName("sync_interval_hours");
            entity.Property(x => x.SupportedFields).HasColumnName("supported_fields");
            entity.Property(x => x.LastSyncedAt).HasColumnName("last_synced_at");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.ToTable("system_settings");
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasColumnName("key");
            entity.Property(x => x.Value).HasColumnName("value");
            entity.Property(x => x.Description).HasColumnName("description");
            entity.Property(x => x.UpdatedBy).HasColumnName("updated_by");
            entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("audit_logs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.AdminUserId).HasColumnName("admin_user_id");
            entity.Property(x => x.Action).HasColumnName("action");
            entity.Property(x => x.EntityType).HasColumnName("entity_type");
            entity.Property(x => x.EntityId).HasColumnName("entity_id");
            entity.Property(x => x.OldValue).HasColumnName("old_value").HasColumnType("jsonb");
            entity.Property(x => x.NewValue).HasColumnName("new_value").HasColumnType("jsonb");
            entity.Property(x => x.IpAddress).HasColumnName("ip_address");
            entity.Property(x => x.CreatedAt).HasColumnName("created_at");
        });
    }
}
