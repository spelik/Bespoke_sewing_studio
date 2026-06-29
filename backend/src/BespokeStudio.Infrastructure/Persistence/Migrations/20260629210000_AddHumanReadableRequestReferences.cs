using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHumanReadableRequestReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE SEQUENCE "OrderReferenceSequence" AS bigint START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
                CREATE SEQUENCE "ContactMessageReferenceSequence" AS bigint START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
                """);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "Orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "ContactMessages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Orders" AS target
                SET "ReferenceNumber" = source."ReferenceNumber"
                FROM (
                    SELECT
                        "Id",
                        'BSS-ORD-' || EXTRACT(YEAR FROM "CreatedAt")::int || '-' || LPAD(ROW_NUMBER() OVER (ORDER BY "CreatedAt", "Id")::text, 6, '0') AS "ReferenceNumber"
                    FROM "Orders"
                ) AS source
                WHERE target."Id" = source."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE "ContactMessages" AS target
                SET "ReferenceNumber" = source."ReferenceNumber"
                FROM (
                    SELECT
                        "Id",
                        'BSS-CON-' || EXTRACT(YEAR FROM "CreatedAt")::int || '-' || LPAD(ROW_NUMBER() OVER (ORDER BY "CreatedAt", "Id")::text, 6, '0') AS "ReferenceNumber"
                    FROM "ContactMessages"
                ) AS source
                WHERE target."Id" = source."Id";
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "Orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ReferenceNumber",
                table: "ContactMessages",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.Sql("""
                SELECT setval('"OrderReferenceSequence"', GREATEST((SELECT COUNT(*) FROM "Orders"), 1), (SELECT COUNT(*) FROM "Orders") > 0);
                SELECT setval('"ContactMessageReferenceSequence"', GREATEST((SELECT COUNT(*) FROM "ContactMessages"), 1), (SELECT COUNT(*) FROM "ContactMessages") > 0);
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ReferenceNumber",
                table: "Orders",
                column: "ReferenceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ContactMessages_ReferenceNumber",
                table: "ContactMessages",
                column: "ReferenceNumber",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_ReferenceNumber",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_ContactMessages_ReferenceNumber",
                table: "ContactMessages");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "ContactMessages");

            migrationBuilder.Sql("""
                DROP SEQUENCE IF EXISTS "OrderReferenceSequence";
                DROP SEQUENCE IF EXISTS "ContactMessageReferenceSequence";
                """);
        }
    }
}
