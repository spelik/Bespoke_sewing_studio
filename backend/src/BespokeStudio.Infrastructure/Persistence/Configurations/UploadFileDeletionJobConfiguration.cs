using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class UploadFileDeletionJobConfiguration : IEntityTypeConfiguration<UploadFileDeletionJob>
{
    public void Configure(EntityTypeBuilder<UploadFileDeletionJob> builder)
    {
        builder.ToTable("UploadFileDeletionJobs", table =>
        {
            table.HasCheckConstraint("CK_UploadFileDeletionJobs_Attempts", "\"Attempts\" >= 0");
            table.HasCheckConstraint("CK_UploadFileDeletionJobs_MaxAttempts", "\"MaxAttempts\" > 0");
            table.HasCheckConstraint("CK_UploadFileDeletionJobs_FileSizeBytes", "\"FileSizeBytes\" IS NULL OR \"FileSizeBytes\" >= 0");
        });

        builder.HasKey(job => job.Id);
        builder.Property(job => job.Id).ValueGeneratedNever();
        builder.Property(job => job.StorageKey).HasMaxLength(1024).IsRequired();
        builder.Property(job => job.OriginalFileName).HasMaxLength(255);
        builder.Property(job => job.RelatedEntityType).HasMaxLength(120).IsRequired();
        builder.Property(job => job.Reason).HasMaxLength(120).IsRequired();
        builder.Property(job => job.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(job => job.LastError).HasMaxLength(500);
        builder.Property(job => job.CreatedAt).IsRequired();
        builder.Property(job => job.UpdatedAt).IsRequired();

        builder.HasIndex(job => job.StorageKey);
        builder.HasIndex(job => new { job.Status, job.NextAttemptAt });
        builder.HasIndex(job => job.CreatedAt);
    }
}
