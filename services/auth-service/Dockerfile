FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY AuthService.sln .
COPY AuthService.API/*.csproj AuthService.API/
COPY AuthService.Application/*.csproj AuthService.Application/
COPY AuthService.Domain/*.csproj AuthService.Domain/
COPY AuthService.Infrastructure/*.csproj AuthService.Infrastructure/
COPY AuthService.Tests/*.csproj AuthService.Tests/

# Restore dependencies
RUN dotnet restore AuthService.sln

# Copy source code
COPY . .

# Build and publish
WORKDIR /src/AuthService.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "AuthService.API.dll"]