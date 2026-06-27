using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class PortfolioItemConfiguration : IEntityTypeConfiguration<PortfolioItem>
{
    public void Configure(EntityTypeBuilder<PortfolioItem> builder)
    {
        builder.ToTable("PortfolioItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(4000);
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();

        builder.HasOne<PortfolioCategory>()
            .WithMany()
            .HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<UploadedFileMetadata>()
            .WithMany()
            .HasForeignKey(item => item.CoverImageFileId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(item => new { item.Status, item.DisplayOrder });
    }
}
