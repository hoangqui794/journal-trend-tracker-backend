using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaperService.Data;
using PaperService.Entities;
using DotNetEnv;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
builder.Services.AddControllers();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});

builder.Services.AddScoped<PaperService.Services.IPaperService, PaperService.Services.PaperServiceImpl>();
builder.Services.AddScoped<PaperService.Services.ISyncJobService, PaperService.Services.SyncJobServiceImpl>();
builder.Services.AddHostedService<PaperService.Services.PaperSyncWorker>();

// Register HTTP Clients
builder.Services.AddHttpClient<PaperService.Clients.ITrendServiceClient, PaperService.Clients.TrendServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:TrendService"] ?? "http://localhost:5003");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<PaperService.Clients.IUserServiceClient, PaperService.Clients.UserServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:UserService"] ?? "http://localhost:5004");
    client.Timeout = TimeSpan.FromSeconds(5);
});

builder.Services.AddHttpClient<PaperService.Clients.IAdminServiceClient, PaperService.Clients.AdminServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ServiceUrls:AdminService"] ?? "http://localhost:5005");
    client.Timeout = TimeSpan.FromSeconds(5);
});

// Configure Npgsql DataSource to map enums
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("PaperConnection"));
dataSourceBuilder.MapEnum<PaperSource>("paper_source");
dataSourceBuilder.MapEnum<KeywordSource>("keyword_source");
dataSourceBuilder.MapEnum<SyncStatus>("sync_status");
var dataSource = dataSourceBuilder.Build();

// Add DbContext
builder.Services.AddDbContext<PaperDbContext>(options =>
    options.UseNpgsql(dataSource));

builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
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
            new Microsoft.OpenApi.Models.OpenApiServer { Url = "/paper-api" }
        };
    });
});
app.UseSwaggerUI();

// app.UseHttpsRedirection();

app.UseRouting();
app.UseCors("AllowAll");

app.MapControllers();

app.MapHealthChecks("/health");
app.MapHealthChecks("/api/papers/health");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PaperDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration failed: {ex.Message}");
    }
}

app.Run();
