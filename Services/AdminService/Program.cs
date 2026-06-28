using AdminService.Data;
using AdminService.Repositories.Implementations;
using AdminService.Repositories.Interfaces;
using AdminService.Services.Implementations;
using AdminService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger configuration with JWT Security ───────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AdminService API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AdminConnection")));

builder.Services.AddScoped<IAdminRepository, AdminRepository>();
builder.Services.AddScoped<IAdminManagementService, AdminManagementService>();

builder.Services.AddHttpClient("identity", client =>
{
    var baseUrl = builder.Configuration["Services:IdentityBaseUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient("paper", client =>
{
    var baseUrl = builder.Configuration["Services:PaperBaseUrl"] ?? "http://localhost:5002";
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHealthChecks();

// ── Authentication ──────────────────────────────────────────────
var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? "default_secret_key_that_is_long_enough_32_bytes");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

var app = builder.Build();

// Enable Swagger in production
app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        var scheme = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpReq.Scheme;
        var host = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpReq.Host.Value;
        swagger.Servers = new List<Microsoft.OpenApi.Models.OpenApiServer>
        {
            new Microsoft.OpenApi.Models.OpenApiServer { Url = $"{scheme}://{host}" },
            new Microsoft.OpenApi.Models.OpenApiServer { Url = "/admin-api" }
        };
    });
});
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/api/admin/health");

app.Run();
