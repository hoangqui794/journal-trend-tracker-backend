using Microsoft.EntityFrameworkCore;
using TrendService.DBContext;
using TrendService.Repositories;
using TrendService.Services;
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

var app = builder.Build();

// ── Middleware Pipeline ───────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowGateway");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
