using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 🔐 Rules Applied: Kept completely empty to trick EF Core into syncing metadata models.
            // All columns and the OutboxMessages table are already alive and running inside your Neon database.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 🔐 Kept completely empty to allow for safe structural parity checks.
        }
    }
}
