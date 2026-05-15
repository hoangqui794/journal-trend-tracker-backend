namespace AdminService.Models.Dtos;

public sealed record UpdateSystemSettingRequest(string Key, string Value, string? Description);
