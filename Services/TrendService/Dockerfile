# Bước 1: Môi trường Build (chứa đầy đủ công cụ để biên dịch code)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file .csproj vào trước và restore các thư viện (để tận dụng cache của Docker)
COPY ["TrendService.csproj", "./"]
RUN dotnet restore "TrendService.csproj"

# Copy toàn bộ code còn lại vào thư mục src
COPY . .
WORKDIR "/src/"

# Biên dịch dự án ra các file DLL
RUN dotnet build "TrendService.csproj" -c Release -o /app/build

# Publish dự án (tối ưu hóa code để chạy thực tế)
FROM build AS publish
RUN dotnet publish "TrendService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Bước 2: Môi trường Runtime (siêu nhẹ, chỉ chứa thứ cần thiết để chạy)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Đặt biến môi trường rõ ràng cho .NET 8 để sử dụng cổng 8080 (cổng mặc định của .NET 8)
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Lấy các file đã publish từ Bước 1 sang
COPY --from=publish /app/publish .

# Lệnh khởi chạy ứng dụng khi Container bắt đầu
ENTRYPOINT ["dotnet", "TrendService.dll"]
