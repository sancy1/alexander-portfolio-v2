FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY AuthService.sln .
COPY AuthService.API/*.csproj AuthService.API/
COPY AuthService.Application/*.csproj AuthService.Application/
COPY AuthService.Domain/*.csproj AuthService.Domain/
COPY AuthService.Infrastructure/*.csproj AuthService.Infrastructure/
COPY AuthService.Tests/*.csproj AuthService.Tests/

# Clear NuGet cache completely and restore
RUN dotnet nuget locals all --clear
RUN dotnet restore AuthService.sln --force

# Copy source code
COPY . .

# Build and publish with explicit restore
WORKDIR /src/AuthService.API
RUN dotnet restore
RUN dotnet build -c Release --no-restore
RUN dotnet publish -c Release -o /app/publish --no-build

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

# Install curl for health checks (optional but recommended)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy the published application assets from the build container
COPY --from=build /app/publish .

# 🔐 Rules Applied: Embed the downloaded Aiven CA Certificate into the final runtime container
# This copies ca.pem directly from your build context into the root of /app
COPY ca.pem /app/ca.pem

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/api/v1/health/ping || exit 1

ENTRYPOINT ["dotnet", "AuthService.API.dll"]
