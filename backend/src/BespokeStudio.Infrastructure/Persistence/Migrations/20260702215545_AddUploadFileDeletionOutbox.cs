using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BespokeStudio.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadFileDeletionOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UploadFileDeletionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    OriginalFileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    RelatedEntityType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    NextAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SucceededAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadFileDeletionJobs", x => x.Id);
                    table.CheckConstraint("CK_UploadFileDeletionJobs_Attempts", "\"Attempts\" >= 0");
                    table.CheckConstraint("CK_UploadFileDeletionJobs_FileSizeBytes", "\"FileSizeBytes\" IS NULL OR \"FileSizeBytes\" >= 0");
                    table.CheckConstraint("CK_UploadFileDeletionJobs_MaxAttempts", "\"MaxAttempts\" > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_UploadFileDeletionJobs_CreatedAt",
                table: "UploadFileDeletionJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UploadFileDeletionJobs_Status_NextAttemptAt",
                table: "UploadFileDeletionJobs",
                columns: new[] { "Status", "NextAttemptAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UploadFileDeletionJobs_StorageKey",
                table: "UploadFileDeletionJobs",
                column: "StorageKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UploadFileDeletionJobs");
        }
    }
}
