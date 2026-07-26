using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInStockNavigationSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InStockLabel",
                table: "SiteSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "IN STOCK");

            migrationBuilder.AddColumn<bool>(
                name: "ShowInStockLink",
                table: "SiteSettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.UpdateData(
                table: "SiteSettings",
                keyColumn: "Id",
                keyValue: new Guid("7e7a43ab-bd37-4e9f-8e62-d384e8663180"),
                columns: new[] { "InStockLabel", "ShowInStockLink" },
                values: new object[] { "IN STOCK", true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InStockLabel",
                table: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "ShowInStockLink",
                table: "SiteSettings");
        }
    }
}
