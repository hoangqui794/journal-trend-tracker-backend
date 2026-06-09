# Bước 1: Môi trường Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PaperService.csproj", "./"]
RUN dotnet restore "./PaperService.csproj"

COPY . .
WORKDIR "/src/"

RUN dotnet build "PaperService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PaperService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Bước 2: Môi trường Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PaperService.dll"]
