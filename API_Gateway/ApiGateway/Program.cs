var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.MapReverseProxy();
app.Run();
