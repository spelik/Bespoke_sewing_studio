using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudioName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    SiteTagline = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PublicEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    PublicPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WhatsAppPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ContactButtonLabel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactIntroText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    NotificationEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    NotificationPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmailNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    WhatsAppNotificationsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    FacebookUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    InstagramUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    TikTokUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    PinterestUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    FooterText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ServiceAreaText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BusinessLegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                    table.CheckConstraint("CK_SiteSettings_Singleton", "\"Id\" = '7e7a43ab-bd37-4e9f-8e62-d384e8663180'::uuid");
                });

            migrationBuilder.InsertData(
                table: "SiteSettings",
                columns: new[] { "Id", "BusinessLegalName", "ContactButtonLabel", "ContactIntroText", "EmailNotificationsEnabled", "FacebookUrl", "FooterText", "InstagramUrl", "NotificationEmail", "NotificationPhone", "PinterestUrl", "PublicEmail", "PublicPhone", "ServiceAreaText", "SiteTagline", "StudioName", "TikTokUrl", "UpdatedAt", "WhatsAppNotificationsEnabled", "WhatsAppPhone" },
                values: new object[] { new Guid("7e7a43ab-bd37-4e9f-8e62-d384e8663180"), null, "Send Enquiry", "Consultations and orders are arranged individually.", false, null, "Bespoke Sewing Studio. All rights reserved.", null, null, null, null, null, "074 6734 7194", "Appointments arranged individually.", "Bespoke sewing, tailoring, dressmaking, alterations and memory bears.", "Bespoke Sewing Studio", null, new DateTimeOffset(new DateTime(2026, 6, 27, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SiteSettings");
        }
    }
}
