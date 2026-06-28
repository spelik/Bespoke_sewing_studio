using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRepeatableContentCms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RepeatableContentItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GroupKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ItemKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subtitle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Body = table.Column<string>(type: "character varying(12000)", maxLength: 12000, nullable: true),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IconKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    Rating = table.Column<int>(type: "integer", nullable: true),
                    Location = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    Service = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ArchivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RepeatableContentItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RepeatableContentItems_ArchivedAt",
                table: "RepeatableContentItems",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RepeatableContentItems_GroupKey_IsActive_DisplayOrder",
                table: "RepeatableContentItems",
                columns: new[] { "GroupKey", "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RepeatableContentItems_GroupKey_ItemKey",
                table: "RepeatableContentItems",
                columns: new[] { "GroupKey", "ItemKey" },
                unique: true,
                filter: "\"ArchivedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RepeatableContentItems");
        }
    }
}
