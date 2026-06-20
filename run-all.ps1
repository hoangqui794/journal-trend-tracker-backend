$services = @(
    "API_Gateway\ApiGateway",
    "Services\IdentityService",
    "Services\AdminService",
    "Services\PaperService",
    "Services\TrendService",
    "Services\UserService"
)

foreach ($service in $services) {
    Write-Host "Starting $service..."
    Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd '$service'; dotnet run"
}

Write-Host "All services have been started in separate windows."
