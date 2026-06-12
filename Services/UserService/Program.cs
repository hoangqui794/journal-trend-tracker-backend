// using DocumentService.Data;
// using DocumentService.Services;
using UserService.Data;
using UserService.Repositories;
using UserService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure Entity Framework Core with PostgreSQL
var userConnectionString = builder.Configuration.GetConnectionString("UserConnection");

// Register PostgreSQL ENUM types at the DataSource level (required by Npgsql)
var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(userConnectionString);
dataSourceBuilder.MapEnum<UserService.Models.BookmarkEntity>("bookmark_entity");
dataSourceBuilder.MapEnum<UserService.Models.FollowType>("follow_type");
dataSourceBuilder.MapEnum<UserService.Models.NotificationType>("notification_type");
dataSourceBuilder.MapEnum<UserService.Models.DeliveryStatus>("delivery_status");
var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(dataSource));

// Register Services and Repositories
// builder.Services.AddScoped<ISubjectService, SubjectService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserAppService, UserAppService>();
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddHttpClient<IIdentityClient, IdentityClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:IdentityService"] ?? "http://identity-service");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(UserService.Configurations.SwaggerConfig.Configure);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirect root → Swagger UI tự động
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
