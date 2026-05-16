using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace UserService.Configurations
{
    /// <summary>
    /// Cấu hình Swagger để hiện ô nhập X-User-Id header khi test API.
    /// </summary>
    public static class SwaggerConfig
    {
        public static void Configure(SwaggerGenOptions c)
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "UserService API",
                Version = "v1",
                Description = "API quản lý User Profile, Bookmarks, Follows và Notifications"
            });

            // Định nghĩa X-User-Id như một API Key header (Authorize)
            c.AddSecurityDefinition("X-User-Id", new OpenApiSecurityScheme
            {
                Name = "X-User-Id",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Description = "Nhập UUID của user để test. Ví dụ: 00000000-0000-0000-0000-000000000001"
            });

            // Áp dụng cho tất cả các endpoint
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "X-User-Id"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        }
    }
}
