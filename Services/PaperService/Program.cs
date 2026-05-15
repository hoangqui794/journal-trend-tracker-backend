using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaperService.Data;
using PaperService.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

// Configure Npgsql DataSource to map enums
var dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.Configuration.GetConnectionString("DefaultConnection"));
dataSourceBuilder.MapEnum<PaperSource>("paper_source");
dataSourceBuilder.MapEnum<KeywordSource>("keyword_source");
dataSourceBuilder.MapEnum<SyncStatus>("sync_status");
var dataSource = dataSourceBuilder.Build();

// Add DbContext
builder.Services.AddDbContext<PaperDbContext>(options =>
    options.UseNpgsql(dataSource));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
