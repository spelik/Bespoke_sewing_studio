using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteContentCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PageContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PageKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SectionKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Body = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    CtaLabel = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    CtaUrl = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ImageFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    ImageAltText = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageContents_UploadedFiles_ImageFileId",
                        column: x => x.ImageFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PageContents_ArchivedAt",
                table: "PageContents",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PageContents_ImageFileId",
                table: "PageContents",
                column: "ImageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_PageContents_PageKey_IsActive_DisplayOrder",
                table: "PageContents",
                columns: new[] { "PageKey", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_PageContents_PageKey_SectionKey",
                table: "PageContents",
                columns: new[] { "PageKey", "SectionKey" },
                unique: true,
                filter: "\"ArchivedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageContents");
        }
    }
}
