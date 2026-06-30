using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailDeliveryLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailDeliveryLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Subject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Provider = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SentExternally = table.Column<bool>(type: "boolean", nullable: false),
                    ResultMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RelatedEntityLabel = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailDeliveryLogEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryLogEntries_CreatedAt",
                table: "EmailDeliveryLogEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryLogEntries_MessageType_CreatedAt",
                table: "EmailDeliveryLogEntries",
                columns: new[] { "MessageType", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryLogEntries_Provider_CreatedAt",
                table: "EmailDeliveryLogEntries",
                columns: new[] { "Provider", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryLogEntries_RecipientEmail_CreatedAt",
                table: "EmailDeliveryLogEntries",
                columns: new[] { "RecipientEmail", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryLogEntries_RelatedEntityType_RelatedEntityId",
                table: "EmailDeliveryLogEntries",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailDeliveryLogEntries_Status_CreatedAt",
                table: "EmailDeliveryLogEntries",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailDeliveryLogEntries");
        }
    }
}
