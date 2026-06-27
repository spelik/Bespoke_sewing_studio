using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class UploadedFileMetadataConfiguration : IEntityTypeConfiguration<UploadedFileMetadata>
{
    public void Configure(EntityTypeBuilder<UploadedFileMetadata> builder)
    {
        builder.ToTable("UploadedFiles", table =>
            table.HasCheckConstraint("CK_UploadedFiles_SizeBytes", "\"SizeBytes\" >= 0"));

        builder.HasKey(file => file.Id);

        builder.Property(file => file.Id).ValueGeneratedNever();
        builder.Property(file => file.Purpose).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(file => file.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(file => file.StoredFileName).HasMaxLength(255).IsRequired();
        builder.Property(file => file.StorageKey).HasMaxLength(1024).IsRequired();
        builder.Property(file => file.ContentType).HasMaxLength(255).IsRequired();
        builder.Property(file => file.CreatedAt).IsRequired();
        builder.Property(file => file.UpdatedAt).IsRequired();

        builder.HasIndex(file => file.StorageKey).IsUnique();
        builder.HasIndex(file => new { file.Purpose, file.CreatedAt });
    }
}
