using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandSeoSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AboutLabel",
                table: "SiteSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BrandDisplayName",
                table: "SiteSettings",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactLabel",
                table: "SiteSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultMetaDescription",
                table: "SiteSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultMetaTitle",
                table: "SiteSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DefaultOgDescription",
                table: "SiteSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultOgImageFileId",
                table: "SiteSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultOgTitle",
                table: "SiteSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FaviconFileId",
                table: "SiteSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeaderCtaLabel",
                table: "SiteSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HeaderCtaUrl",
                table: "SiteSettings",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LogoAltText",
                table: "SiteSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "LogoFileId",
                table: "SiteSettings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderLabel",
                table: "SiteSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PortfolioLabel",
                table: "SiteSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServicesLabel",
                table: "SiteSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ShowAboutLink",
                table: "SiteSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowContactLink",
                table: "SiteSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOrderLink",
                table: "SiteSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowPortfolioLink",
                table: "SiteSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ShowServicesLink",
                table: "SiteSettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: new Guid("7e7a43ab-bd37-4e9f-8e62-d384e8663180"),
                columns: new[] { "AboutLabel", "BrandDisplayName", "ContactLabel", "DefaultMetaDescription", "DefaultMetaTitle", "DefaultOgDescription", "DefaultOgImageFileId", "DefaultOgTitle", "FaviconFileId", "HeaderCtaLabel", "HeaderCtaUrl", "LogoAltText", "LogoFileId", "OrderLabel", "PortfolioLabel", "ServicesLabel", "ShowAboutLink", "ShowContactLink", "ShowOrderLink", "ShowPortfolioLink", "ShowServicesLink" },
                values: new object[] { "About", "Bespoke Sewing Studio", "Contact", "Bespoke sewing, tailoring, dressmaking, alterations and memory bears.", "Bespoke Sewing Studio", null, null, null, null, "Book Now", "/order", "Bespoke Sewing Studio logo", null, "Order", "Portfolio", "Services", true, true, true, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_DefaultOgImageFileId",
                table: "SiteSettings",
                column: "DefaultOgImageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_FaviconFileId",
                table: "SiteSettings",
                column: "FaviconFileId");

            migrationBuilder.CreateIndex(
                name: "IX_SiteSettings_LogoFileId",
                table: "SiteSettings",
                column: "LogoFileId");

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSettings_UploadedFiles_DefaultOgImageFileId",
                table: "SiteSettings",
                column: "DefaultOgImageFileId",
                principalTable: "UploadedFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSettings_UploadedFiles_FaviconFileId",
                table: "SiteSettings",
                column: "FaviconFileId",
                principalTable: "UploadedFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SiteSettings_UploadedFiles_LogoFileId",
                table: "SiteSettings",
                column: "LogoFileId",
                principalTable: "UploadedFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SiteSettings_UploadedFiles_DefaultOgImageFileId",
                table: "SiteSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteSettings_UploadedFiles_FaviconFileId",
                table: "SiteSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_SiteSettings_UploadedFiles_LogoFileId",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_DefaultOgImageFileId",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_FaviconFileId",
                table: "SiteSettings");

            migrationBuilder.DropIndex(
                name: "IX_SiteSettings_LogoFileId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "AboutLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "BrandDisplayName",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ContactLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DefaultMetaDescription",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DefaultMetaTitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DefaultOgDescription",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DefaultOgImageFileId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "DefaultOgTitle",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "FaviconFileId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeaderCtaLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "HeaderCtaUrl",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LogoAltText",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "LogoFileId",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "OrderLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "PortfolioLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ServicesLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShowAboutLink",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShowContactLink",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShowOrderLink",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShowPortfolioLink",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShowServicesLink",
                table: "SiteSettings");
        }
    }
}
