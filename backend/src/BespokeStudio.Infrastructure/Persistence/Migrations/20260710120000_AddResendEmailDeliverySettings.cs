using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    [Migration("20260710120000_AddResendEmailDeliverySettings")]
    public partial class AddResendEmailDeliverySettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailDeliveryReplyToEmail",
                table: "SiteSettings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailDeliveryResendApiKeyProtected",
                table: "SiteSettings",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailDeliveryResendFromEmail",
                table: "SiteSettings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: new Guid("7e7a43ab-bd37-4e9f-8e62-d384e8663180"),
                columns: new[] { "EmailDeliveryReplyToEmail", "EmailDeliveryResendFromEmail" },
                values: new object[] { "contact@oksanalogosha.com", "noreply@oksanalogosha.com" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailDeliveryReplyToEmail",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailDeliveryResendApiKeyProtected",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailDeliveryResendFromEmail",
                table: "SiteSettings");
        }
    }
}
