using AdminService.Data;
using AdminService.Repositories.Implementations;
using AdminService.Repositories.Interfaces;
using AdminService.Services.Implementations;
using AdminService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using DotNetEnv;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
