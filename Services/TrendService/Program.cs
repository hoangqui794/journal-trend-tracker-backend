using Microsoft.EntityFrameworkCore;
using TrendService.DBContext;
using TrendService.Repositories;
using TrendService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("TrendConnection")
    ?? throw new InvalidOperationException("Connection string 'TrendConnection' not found.");

builder.Services.AddDbContext<TrendDbContext>(options =>
    options.UseNpgsql(connectionString));

// ── HTTP Client → PaperService ────────────────────────────────
builder.Services.AddHttpClient<IPaperServiceClient, PaperServiceClient>(client =>
{
    var paperServiceUrl = builder.Configuration["ServiceUrls:PaperService"]
        ?? "http://localhost:5002";
    client.BaseAddress = new Uri(paperServiceUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// ── Dependency Injection ──────────────────────────────────────
builder.Services.AddScoped<ITrendRepository, TrendRepository>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IExportService, ExportService>();

// ── Swagger + Controllers ─────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "TrendService API", Version = "v1" });
});

// ── CORS ──────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowGateway", policy =>
        policy.WithOrigins(
                builder.Configuration["ServiceUrls:Gateway"] ?? "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod());
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

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────
// Enable Swagger in production
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowGateway");
// app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
