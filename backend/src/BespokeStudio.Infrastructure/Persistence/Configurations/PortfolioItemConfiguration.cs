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
        builder.Property(item => item.Slug).HasMaxLength(220);
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.ShortDescription).HasMaxLength(500);
        builder.Property(item => item.Description).HasMaxLength(4000);
        builder.Property(item => item.AltText).HasMaxLength(250).IsRequired();
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();

        builder.HasOne(item => item.Category)
            .WithMany(category => category.Items)
            .HasForeignKey(item => item.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(item => item.CoverImageFile)
            .WithMany()
            .HasForeignKey(item => item.CoverImageFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(item => item.Slug)
            .IsUnique()
            .HasFilter("\"Slug\" IS NOT NULL AND \"ArchivedAt\" IS NULL");
        builder.HasIndex(item => new { item.IsActive, item.DisplayOrder });
        builder.HasIndex(item => new { item.IsFeatured, item.DisplayOrder });
        builder.HasIndex(item => new { item.CategoryId, item.DisplayOrder });
        builder.HasIndex(item => item.ArchivedAt);
    }
}
