using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioGalleryCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioItems_UploadedFiles_CoverImageFileId",
                table: "PortfolioItems");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioItems_CategoryId",
                table: "PortfolioItems");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioItems_Status_DisplayOrder",
                table: "PortfolioItems");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioCategories_Slug",
                table: "PortfolioCategories");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "PortfolioItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AltText",
                table: "PortfolioItems",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "PortfolioItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "PortfolioItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "PortfolioItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "PortfolioItems",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "PortfolioCategories",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "PortfolioItems"
                SET "AltText" = "Title",
                    "IsActive" = CASE WHEN "Status" = 'Published' THEN TRUE ELSE FALSE END;
                """);

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PortfolioItems");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "PortfolioItems");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioItems_ArchivedAt",
                table: "PortfolioItems",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioItems_CategoryId_DisplayOrder",
                table: "PortfolioItems",
                columns: new[] { "CategoryId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioItems_IsActive_DisplayOrder",
                table: "PortfolioItems",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioItems_IsFeatured_DisplayOrder",
                table: "PortfolioItems",
                columns: new[] { "IsFeatured", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioItems_Slug",
                table: "PortfolioItems",
                column: "Slug",
                unique: true,
                filter: "\"Slug\" IS NOT NULL AND \"ArchivedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioCategories_ArchivedAt",
                table: "PortfolioCategories",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioCategories_Slug",
                table: "PortfolioCategories",
                column: "Slug",
                unique: true,
                filter: "\"ArchivedAt\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioItems_UploadedFiles_CoverImageFileId",
                table: "PortfolioItems",
                column: "CoverImageFileId",
                principalTable: "UploadedFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PortfolioItems_UploadedFiles_CoverImageFileId",
                table: "PortfolioItems");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioItems_ArchivedAt",
                table: "PortfolioItems");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioItems_CategoryId_DisplayOrder",
                table: "PortfolioItems");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioItems_IsActive_DisplayOrder",
                table: "PortfolioItems");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioItems_IsFeatured_DisplayOrder",
                table: "PortfolioItems");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioItems_Slug",
                table: "PortfolioItems");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioCategories_ArchivedAt",
                table: "PortfolioCategories");

            migrationBuilder.DropIndex(
                name: "IX_PortfolioCategories_Slug",
                table: "PortfolioCategories");

            migrationBuilder.DropColumn(
                name: "AltText",
                table: "PortfolioItems");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "PortfolioItems");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "PortfolioItems");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "PortfolioItems");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "PortfolioItems");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "PortfolioCategories");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAt",
                table: "PortfolioItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "PortfolioItems",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioItems_CategoryId",
                table: "PortfolioItems",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioItems_Status_DisplayOrder",
                table: "PortfolioItems",
                columns: new[] { "Status", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioCategories_Slug",
                table: "PortfolioCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PortfolioItems_UploadedFiles_CoverImageFileId",
                table: "PortfolioItems",
                column: "CoverImageFileId",
                principalTable: "UploadedFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
