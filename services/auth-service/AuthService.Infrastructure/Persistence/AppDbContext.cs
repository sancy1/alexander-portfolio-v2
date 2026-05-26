// File: AuthService.Infrastructure/Persistence/AppDbContext.cs
// Purpose: Entity Framework Core database context for auth service
// Layer: Infrastructure

using Microsoft.EntityFrameworkCore;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Admin> Admins { get; set; }
    public DbSet<SocialUser> SocialUsers { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Admin Configuration
        modelBuilder.Entity<Admin>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Role)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.PasswordResetToken).HasMaxLength(255);
        });

        // SocialUser Configuration
        modelBuilder.Entity<SocialUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProviderId).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            
            entity.Property(e => e.ProviderId)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);
            
            entity.Property(e => e.Provider)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.DisplayName)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.AvatarUrl).HasMaxLength(500);
            
            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>();
            
            entity.Property(e => e.CreatedAt).IsRequired();
        });

        // OutboxMessage Configuration (SEPARATE - not inside Admin)
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100);
            entity.Property(e => e.RoutingKey).HasMaxLength(100);
            entity.Property(e => e.Broker).HasMaxLength(50);
            entity.Property(e => e.IsProcessed).HasComputedColumnSql("(\"ProcessedAt\" IS NOT NULL)");
        });
    }
}