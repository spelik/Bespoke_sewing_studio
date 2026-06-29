using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminEmailDeliverySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailDeliveryAppPasswordProtected",
                table: "SiteSettings",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailDeliveryGmailAddress",
                table: "SiteSettings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailDeliveryProvider",
                table: "SiteSettings",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "Configuration");

            migrationBuilder.AddColumn<string>(
                name: "EmailDeliverySenderName",
                table: "SiteSettings",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "Bespoke Sewing Studio");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EmailDeliveryUpdatedAt",
                table: "SiteSettings",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailDeliveryAppPasswordProtected",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailDeliveryGmailAddress",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailDeliveryProvider",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailDeliverySenderName",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "EmailDeliveryUpdatedAt",
                table: "SiteSettings");
        }
    }
}
