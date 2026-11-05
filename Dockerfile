# Build và chạy Tripory.API (.NET 10). Dùng cho Render, Railway, Azure Container Apps...
# Build từ thư mục gốc repo:  docker build -t tripory-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Services/Tripory/Tripory.API/Tripory.API.csproj
RUN dotnet publish src/Services/Tripory/Tripory.API/Tripory.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Nền tảng PaaS truyền cổng qua biến PORT (Render mặc định 10000); không có thì dùng 8080.
# Cố ý KHÔNG đặt bí mật trong image: ConnectionStrings__DefaultConnection, JwtOptions__SecretKey,
# AuthRateLimitOptions__BffSharedSecret, Cors__AllowedOrigins__0 đặt bằng biến môi trường của nền tảng.
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Tripory.API.dll"]
