using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailOutboxMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RecipientEmail = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Subject = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    HtmlBody = table.Column<string>(type: "text", nullable: true),
                    TextBody = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RelatedEntityId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RelatedEntityLabel = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    EmailDeliveryLogEntryId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailOutboxMessages", x => x.Id);
                    table.CheckConstraint("CK_EmailOutboxMessages_Attempts", "\"Attempts\" >= 0");
                    table.CheckConstraint("CK_EmailOutboxMessages_Body", "\"HtmlBody\" IS NOT NULL OR \"TextBody\" IS NOT NULL");
                    table.CheckConstraint("CK_EmailOutboxMessages_MaxAttempts", "\"MaxAttempts\" > 0");
                    table.ForeignKey(
                        name: "FK_EmailOutboxMessages_EmailDeliveryLogEntries_EmailDeliveryLo~",
                        column: x => x.EmailDeliveryLogEntryId,
                        principalTable: "EmailDeliveryLogEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutboxMessages_CreatedAt",
                table: "EmailOutboxMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutboxMessages_EmailDeliveryLogEntryId",
                table: "EmailOutboxMessages",
                column: "EmailDeliveryLogEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutboxMessages_RelatedEntityType_RelatedEntityId",
                table: "EmailOutboxMessages",
                columns: new[] { "RelatedEntityType", "RelatedEntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutboxMessages_Status_NextAttemptAt_CreatedAt",
                table: "EmailOutboxMessages",
                columns: new[] { "Status", "NextAttemptAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailOutboxMessages");
        }
    }
}
