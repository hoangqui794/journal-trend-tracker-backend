# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY ["Services/AIChatService/AIChatService.csproj", "Services/AIChatService/"]
RUN dotnet restore "Services/AIChatService/AIChatService.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/app/Services/AIChatService"
RUN dotnet build "AIChatService.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "AIChatService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AIChatService.dll"]
