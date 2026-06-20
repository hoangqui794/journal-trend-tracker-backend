# Bước 1: Môi trường Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AdminService.csproj", "./"]
RUN dotnet restore "./AdminService.csproj"

COPY . .
WORKDIR "/src/"

RUN dotnet build "AdminService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdminService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Bước 2: Môi trường Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "AdminService.dll"]
