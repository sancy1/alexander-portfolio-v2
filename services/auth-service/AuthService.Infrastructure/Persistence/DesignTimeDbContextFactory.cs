
// File: AuthService.Infrastructure/Persistence/DesignTimeDbContextFactory.cs
// Purpose: Factory for design-time DbContext creation (migrations)
// Layer: Infrastructure

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthService.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        
        // Get connection string from environment variable or use a default
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        
        if (string.IsNullOrEmpty(databaseUrl))
        {
            // Fallback for migrations (use Neon connection)
            databaseUrl = "Host=ep-fragrant-bread-aqz5rywh-pooler.c-8.us-east-1.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=npg_6JuFfUcH1YIp;SslMode=Require;";
        }
        
        optionsBuilder.UseNpgsql(databaseUrl);
        
        return new AppDbContext(optionsBuilder.Options);
    }
}
