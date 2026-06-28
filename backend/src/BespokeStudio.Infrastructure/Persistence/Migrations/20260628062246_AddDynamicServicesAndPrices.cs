using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDynamicServicesAndPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceOfferings_ServiceType",
                table: "ServiceOfferings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ServiceOfferings_StartingPrice",
                table: "ServiceOfferings");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "ServiceOfferings");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "ServiceOfferings");

            migrationBuilder.DropColumn(
                name: "StartingPrice",
                table: "ServiceOfferings");

            migrationBuilder.RenameColumn(
                name: "DetailedDescription",
                table: "ServiceOfferings",
                newName: "Description");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                table: "ServiceOfferings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "ServiceOfferings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "ServiceOfferings",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "ServiceOfferings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "ServiceOfferings",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServiceNameSnapshot",
                table: "Orders",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceOfferingId",
                table: "Orders",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH generated AS (
                    SELECT
                        "Id",
                        COALESCE(
                            NULLIF(TRIM(BOTH '-' FROM REGEXP_REPLACE(LOWER("Name"), '[^a-z0-9]+', '-', 'g')), ''),
                            'service-' || SUBSTRING("Id"::text, 1, 8)) AS base_slug
                    FROM "ServiceOfferings"
                ),
                ranked AS (
                    SELECT
                        "Id",
                        base_slug,
                        ROW_NUMBER() OVER (PARTITION BY base_slug ORDER BY "Id") AS slug_rank
                    FROM generated
                )
                UPDATE "ServiceOfferings" AS service
                SET "Slug" = CASE
                    WHEN ranked.slug_rank = 1 THEN ranked.base_slug
                    ELSE ranked.base_slug || '-' || SUBSTRING(service."Id"::text, 1, 8)
                END
                FROM ranked
                WHERE service."Id" = ranked."Id";

                UPDATE "Orders"
                SET "ServiceNameSnapshot" = CASE "ServiceType"
                    WHEN 'MemoryBear' THEN 'Memory Bears'
                    ELSE "ServiceType"
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "ServiceOfferings",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldDefaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ServiceNameSnapshot",
                table: "Orders",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldDefaultValue: "");

            migrationBuilder.CreateTable(
                name: "ServicePriceOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceOfferingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PriceText = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePriceOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServicePriceOptions_ServiceOfferings_ServiceOfferingId",
                        column: x => x.ServiceOfferingId,
                        principalTable: "ServiceOfferings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOfferings_ArchivedAt",
                table: "ServiceOfferings",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOfferings_Slug",
                table: "ServiceOfferings",
                column: "Slug",
                unique: true,
                filter: "\"ArchivedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ServiceOfferingId",
                table: "Orders",
                column: "ServiceOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePriceOptions_ServiceOfferingId_DisplayOrder",
                table: "ServicePriceOptions",
                columns: new[] { "ServiceOfferingId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ServicePriceOptions_ServiceOfferingId_IsActive",
                table: "ServicePriceOptions",
                columns: new[] { "ServiceOfferingId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ServiceOfferings_ServiceOfferingId",
                table: "Orders",
                column: "ServiceOfferingId",
                principalTable: "ServiceOfferings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ServiceOfferings_ServiceOfferingId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "ServicePriceOptions");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOfferings_ArchivedAt",
                table: "ServiceOfferings");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOfferings_Slug",
                table: "ServiceOfferings");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ServiceOfferingId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                table: "ServiceOfferings");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "ServiceOfferings");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "ServiceOfferings");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "ServiceOfferings");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "ServiceOfferings");

            migrationBuilder.DropColumn(
                name: "ServiceNameSnapshot",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ServiceOfferingId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ServiceOfferings",
                newName: "DetailedDescription");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "ServiceOfferings",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ServiceType",
                table: "ServiceOfferings",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "StartingPrice",
                table: "ServiceOfferings",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOfferings_ServiceType",
                table: "ServiceOfferings",
                column: "ServiceType",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ServiceOfferings_StartingPrice",
                table: "ServiceOfferings",
                sql: "\"StartingPrice\" IS NULL OR \"StartingPrice\" >= 0");
        }
    }
}
