using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using DotNetEnv;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// Authentication
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
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.PermitLimit = 5;
        opt.QueueLimit = 2;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
    });
});

builder.Services.AddHealthChecks();

// Thêm dịch vụ tự động gọi các service khác để tránh sleep trên Render
builder.Services.AddHttpClient();
builder.Services.AddHostedService<ApiGateway.KeepAliveService>();
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Gateway (Main)");
    
    // Gom 5 anh em siêu nhân về đây
    c.SwaggerEndpoint("/identity-api/swagger/v1/swagger.json", "1. Identity Service");
    c.SwaggerEndpoint("/paper-api/swagger/v1/swagger.json", "2. Paper Service");
    c.SwaggerEndpoint("/trend-api/swagger/v1/swagger.json", "3. Trend Service");
    c.SwaggerEndpoint("/user-api/swagger/v1/swagger.json", "4. User Service");
    c.SwaggerEndpoint("/admin-api/swagger/v1/swagger.json", "5. Admin Service");

    c.RoutePrefix = "swagger";
});

// app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.MapHealthChecks("/health");

app.Run();
