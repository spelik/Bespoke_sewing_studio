using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerConfirmationEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CustomerConfirmationEmailsEnabled",
                table: "SiteSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CustomerOrderConfirmationSubject",
                table: "SiteSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "We received your order request");

            migrationBuilder.AddColumn<string>(
                name: "CustomerOrderConfirmationBody",
                table: "SiteSettings",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "Hello {{customerName}},\n\nThank you for your order request to {{studioName}}.\nWe have received your details and will review them shortly.\n\nKind regards,\n{{studioName}}");

            migrationBuilder.AddColumn<string>(
                name: "CustomerContactConfirmationSubject",
                table: "SiteSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "We received your message");

            migrationBuilder.AddColumn<string>(
                name: "CustomerContactConfirmationBody",
                table: "SiteSettings",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "Hello {{customerName}},\n\nThank you for contacting {{studioName}}.\nWe have received your message and will review it shortly.\n\nKind regards,\n{{studioName}}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerConfirmationEmailsEnabled",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CustomerOrderConfirmationSubject",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CustomerOrderConfirmationBody",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CustomerContactConfirmationSubject",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "CustomerContactConfirmationBody",
                table: "SiteSettings");
        }
    }
}
