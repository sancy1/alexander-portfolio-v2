using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminBlockColumnsToSocialUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminBlockReason",
                table: "SocialUsers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdminBlockedAt",
                table: "SocialUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdminBlocked",
                table: "SocialUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminBlockReason",
                table: "SocialUsers");

            migrationBuilder.DropColumn(
                name: "AdminBlockedAt",
                table: "SocialUsers");

            migrationBuilder.DropColumn(
                name: "IsAdminBlocked",
                table: "SocialUsers");
        }
    }
}
