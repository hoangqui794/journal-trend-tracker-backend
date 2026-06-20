using Microsoft.OpenApi.Any;
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

            // Load XML documentation comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Định nghĩa Bearer Token (JWT)
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                Description = "Nhập AccessToken (JWT) thu được từ Identity Service."
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
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Hỗ trợ Nullable Reference Types và set giá trị Example cho các thuộc tính nullable thành null
            c.SupportNonNullableReferenceTypes();
            c.SchemaFilter<NullableExampleSchemaFilter>();
        }
    }

    /// <summary>
    /// Schema Filter để tự động chuyển giá trị Example của các thuộc tính nullable thành null trong Swagger UI
    /// </summary>
    public class NullableExampleSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (schema.Properties == null) return;

            foreach (var property in schema.Properties)
            {
                if (property.Value.Nullable)
                {
                    property.Value.Example = new OpenApiNull();
                }
            }
        }
    }
}
