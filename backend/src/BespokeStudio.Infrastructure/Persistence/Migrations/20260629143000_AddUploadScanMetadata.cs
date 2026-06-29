using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadScanMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ScannedAt",
                table: "UploadedFiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanMessage",
                table: "UploadedFiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanProvider",
                table: "UploadedFiles",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanStatus",
                table: "UploadedFiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Skipped");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScannedAt",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "ScanMessage",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "ScanProvider",
                table: "UploadedFiles");

            migrationBuilder.DropColumn(
                name: "ScanStatus",
                table: "UploadedFiles");
        }
    }
}
