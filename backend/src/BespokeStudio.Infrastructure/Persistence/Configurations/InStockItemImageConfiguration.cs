using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class InStockItemImageConfiguration : IEntityTypeConfiguration<InStockItemImage>
{
    public void Configure(EntityTypeBuilder<InStockItemImage> builder)
    {
        builder.ToTable("InStockItemImages");
        builder.HasKey(image => image.Id);

        builder.Property(image => image.Id).ValueGeneratedNever();
        builder.Property(image => image.AltText).HasMaxLength(250);
        builder.Property(image => image.CreatedAt).IsRequired();

        builder.HasOne(image => image.UploadedFile)
            .WithMany()
            .HasForeignKey(image => image.UploadedFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(image => new { image.InStockItemId, image.DisplayOrder });
        builder.HasIndex(image => image.UploadedFileId).IsUnique();
    }
}
