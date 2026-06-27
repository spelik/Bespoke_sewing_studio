using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeSiteSettingsContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotificationEmail",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "NotificationPhone",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "WhatsAppPhone",
                table: "SiteSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NotificationEmail",
                table: "SiteSettings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NotificationPhone",
                table: "SiteSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WhatsAppPhone",
                table: "SiteSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: new Guid("7e7a43ab-bd37-4e9f-8e62-d384e8663180"),
                columns: new[] { "NotificationEmail", "NotificationPhone", "WhatsAppPhone" },
                values: new object[] { null, null, null });
        }
    }
}
