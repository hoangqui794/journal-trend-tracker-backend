using AIChatService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure Database (Dynamic: Npgsql for Docker/Production, InMemory for Local Test)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(connectionString) && connectionString != "Host=localhost;Database=aichat_db;Username=THQ;Password=a")
{
    builder.Services.AddDbContext<AIChatDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    builder.Services.AddDbContext<AIChatDbContext>(options =>
        options.UseInMemoryDatabase("ChatDbTest"));
}

// Add HttpClient for AI API calls
builder.Services.AddHttpClient();
builder.Services.AddScoped<AIChatService.Services.IGeminiService, AIChatService.Services.GeminiService>();
builder.Services.AddScoped<AIChatService.Repositories.IChatRepository, AIChatService.Repositories.ChatRepository>();
builder.Services.AddScoped<AIChatService.Services.IChatService, AIChatService.Services.ChatService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
