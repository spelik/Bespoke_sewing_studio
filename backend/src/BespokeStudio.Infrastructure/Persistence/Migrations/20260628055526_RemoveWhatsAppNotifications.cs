using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWhatsAppNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WhatsAppNotificationsEnabled",
                table: "SiteSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "WhatsAppNotificationsEnabled",
                table: "SiteSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: new Guid("7e7a43ab-bd37-4e9f-8e62-d384e8663180"),
                column: "WhatsAppNotificationsEnabled",
                value: false);
        }
    }
}
