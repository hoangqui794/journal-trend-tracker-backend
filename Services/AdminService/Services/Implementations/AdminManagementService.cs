using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AdminService.Models.Dtos;
using AdminService.Models.Entities;
using AdminService.Repositories.Interfaces;
using AdminService.Services.Interfaces;

namespace AdminService.Services.Implementations;

public sealed class AdminManagementService(
    IAdminRepository repository,
    IHttpClientFactory httpClientFactory) : IAdminManagementService
{
    public async Task<ProxyResponse> GetUsersAsync()
    {
        try
        {
            var response = await httpClientFactory.CreateClient("identity").GetAsync("/api/identity/users");
            if (!response.IsSuccessStatusCode)
            {
                return await ProxyResponse.FromHttpResponseAsync(response);
            }

            var content = await response.Content.ReadAsStringAsync();
            var deserializeOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var users = JsonSerializer.Deserialize<List<IdentityUserDto>>(content, deserializeOptions);

            var serializeOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var camelCaseJson = JsonSerializer.Serialize(users, serializeOptions);

            return new ProxyResponse(response.StatusCode, camelCaseJson, "application/json");
        }
        catch (HttpRequestException ex)
        {
            return ServiceUnavailable("IdentityService", ex);
        }
    }

    public async Task<ProxyResponse> ToggleUserStatusAsync(Guid userId, Guid adminUserId, string? ipAddress)
    {
        try
        {
            var identityClient = httpClientFactory.CreateClient("identity");

            var userResponse = await identityClient.GetAsync($"/api/identity/users/{userId}");
            if (!userResponse.IsSuccessStatusCode)
            {
                return await ProxyResponse.FromHttpResponseAsync(userResponse);
            }

            var userJson = await userResponse.Content.ReadAsStringAsync();
            var currentStatus = ExtractStatus(userJson);
            var nextStatus = string.Equals(currentStatus, "active", StringComparison.OrdinalIgnoreCase)
                ? "locked"
                : "active";

            var updateResponse = await identityClient.PutAsJsonAsync(
                $"/api/identity/users/{userId}/status",
                new { status = nextStatus });

            if (!updateResponse.IsSuccessStatusCode)
            {
                return await ProxyResponse.FromHttpResponseAsync(updateResponse);
            }

            repository.AddAuditLog(new AuditLog
            {
                AdminUserId = adminUserId,
                Action = "TOGGLE_USER_STATUS",
                EntityType = "User",
                EntityId = userId,
                OldValue = JsonSerializer.SerializeToDocument(new { status = currentStatus }),
                NewValue = JsonSerializer.SerializeToDocument(new { status = nextStatus }),
                IpAddress = ipAddress,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await repository.SaveChangesAsync();

            return await ProxyResponse.FromHttpResponseAsync(updateResponse);
        }
        catch (HttpRequestException ex)
        {
            return ServiceUnavailable("IdentityService", ex);
        }
    }

    public async Task<IReadOnlyList<ApiSource>> GetApiSourcesAsync()
    {
        return await repository.GetApiSourcesAsync();
    }

    public async Task<ApiSource?> ToggleApiSourceAsync(Guid id, Guid adminUserId, string? ipAddress)
    {
        var source = await repository.GetApiSourceByIdAsync(id);
        if (source is null)
        {
            return null;
        }

        var oldActive = source.IsActive;
        source.IsActive = !source.IsActive;
        source.UpdatedAt = DateTimeOffset.UtcNow;

        repository.AddAuditLog(new AuditLog
        {
            AdminUserId = adminUserId,
            Action = "TOGGLE_API_SOURCE",
            EntityType = "ApiSource",
            EntityId = source.Id,
            OldValue = JsonSerializer.SerializeToDocument(new { isActive = oldActive }),
            NewValue = JsonSerializer.SerializeToDocument(new { isActive = source.IsActive }),
            IpAddress = ipAddress,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await repository.SaveChangesAsync();
        return source;
    }

    public async Task<ProxyResponse> GetSyncJobsAsync()
    {
        try
        {
            var response = await httpClientFactory.CreateClient("paper").GetAsync("/api/papers/sync-jobs");
            return await ProxyResponse.FromHttpResponseAsync(response);
        }
        catch (HttpRequestException ex)
        {
            return ServiceUnavailable("PaperService", ex);
        }
    }

    public async Task<ProxyResponse> TriggerSyncAsync(Guid adminUserId, string? ipAddress)
    {
        try
        {
            var response = await httpClientFactory.CreateClient("paper").PostAsync("/api/papers/sync/trigger", null);
            repository.AddAuditLog(new AuditLog
            {
                AdminUserId = adminUserId,
                Action = "TRIGGER_MANUAL_SYNC",
                EntityType = "SyncJob",
                IpAddress = ipAddress,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await repository.SaveChangesAsync();
            return await ProxyResponse.FromHttpResponseAsync(response);
        }
        catch (HttpRequestException ex)
        {
            return ServiceUnavailable("PaperService", ex);
        }
    }

    public async Task<ProxyResponse> WipeMockDataAsync(Guid adminUserId, string? ipAddress)
    {
        try
        {
            var response = await httpClientFactory.CreateClient("paper").DeleteAsync("/api/papers/sync/wipe");
            repository.AddAuditLog(new AuditLog
            {
                AdminUserId = adminUserId,
                Action = "WIPE_MOCK_DATA",
                EntityType = "Database",
                IpAddress = ipAddress,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await repository.SaveChangesAsync();
            return await ProxyResponse.FromHttpResponseAsync(response);
        }
        catch (HttpRequestException ex)
        {
            return ServiceUnavailable("PaperService", ex);
        }
    }

    public async Task<IReadOnlyList<SystemSetting>> GetSettingsAsync()
    {
        return await repository.GetSystemSettingsAsync();
    }

    public async Task UpdateSettingsAsync(IEnumerable<UpdateSystemSettingRequest> requests, Guid adminUserId, string? ipAddress)
    {
        foreach (var request in requests)
        {
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                continue;
            }

            var existing = await repository.GetSystemSettingByKeyAsync(request.Key);
            if (existing is null)
            {
                var newSetting = new SystemSetting
                {
                    Key = request.Key,
                    Value = request.Value,
                    Description = request.Description,
                    UpdatedBy = adminUserId == Guid.Empty ? null : adminUserId,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                repository.AddSystemSetting(newSetting);
                repository.AddAuditLog(new AuditLog
                {
                    AdminUserId = adminUserId,
                    Action = "CREATE_SYSTEM_SETTING",
                    EntityType = "SystemSetting",
                    EntityId = null,
                    OldValue = null,
                    NewValue = JsonSerializer.SerializeToDocument(request),
                    IpAddress = ipAddress,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                continue;
            }

            var oldValue = JsonSerializer.SerializeToDocument(new
            {
                key = existing.Key,
                value = existing.Value,
                description = existing.Description
            });

            existing.Value = request.Value;
            existing.Description = request.Description;
            existing.UpdatedBy = adminUserId == Guid.Empty ? null : adminUserId;
            existing.UpdatedAt = DateTimeOffset.UtcNow;

            repository.AddAuditLog(new AuditLog
            {
                AdminUserId = adminUserId,
                Action = "UPDATE_SYSTEM_SETTING",
                EntityType = "SystemSetting",
                EntityId = null,
                OldValue = oldValue,
                NewValue = JsonSerializer.SerializeToDocument(request),
                IpAddress = ipAddress,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await repository.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AuditLog>> GetLogsAsync(int limit)
    {
        var clampedLimit = Math.Clamp(limit, 1, 500);
        return await repository.GetAuditLogsAsync(clampedLimit);
    }

    private static string ExtractStatus(string userJson)
    {
        if (string.IsNullOrWhiteSpace(userJson))
        {
            return "active";
        }

        try
        {
            using var doc = JsonDocument.Parse(userJson);
            if (doc.RootElement.TryGetProperty("status", out var statusValue) &&
                statusValue.ValueKind == JsonValueKind.String)
            {
                return statusValue.GetString() ?? "active";
            }
            if (doc.RootElement.TryGetProperty("Status", out var statusValuePascal) &&
                statusValuePascal.ValueKind == JsonValueKind.String)
            {
                return statusValuePascal.GetString() ?? "active";
            }
        }
        catch
        {
            return "active";
        }

        return "active";
    }

    private static ProxyResponse ServiceUnavailable(string serviceName, HttpRequestException ex)
    {
        var payload = JsonSerializer.Serialize(new
        {
            message = $"{serviceName} is unavailable.",
            detail = ex.Message
        });
        return new ProxyResponse(HttpStatusCode.ServiceUnavailable, payload);
    }

    public async Task<ProxyResponse> DeactivateUserAsync(Guid userId, Guid adminUserId, string? ipAddress)
    {
        try
        {
            var identityClient = httpClientFactory.CreateClient("identity");

            var response = await identityClient.PutAsJsonAsync(
                $"/api/identity/users/{userId}/status",
                new { status = "locked" });

            if (response.IsSuccessStatusCode)
            {
                repository.AddAuditLog(new AuditLog
                {
                    AdminUserId = adminUserId,
                    Action = "DEACTIVATE_USER",
                    EntityType = "User",
                    EntityId = userId,
                    OldValue = JsonSerializer.SerializeToDocument(new { status = "active" }),
                    NewValue = JsonSerializer.SerializeToDocument(new { status = "locked" }),
                    IpAddress = ipAddress,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await repository.SaveChangesAsync();
            }

            return await ProxyResponse.FromHttpResponseAsync(response);
        }
        catch (HttpRequestException ex)
        {
            return ServiceUnavailable("IdentityService", ex);
        }
    }

    private sealed class IdentityUserDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int Provider { get; set; }
        public int Role { get; set; }
        public int Status { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
