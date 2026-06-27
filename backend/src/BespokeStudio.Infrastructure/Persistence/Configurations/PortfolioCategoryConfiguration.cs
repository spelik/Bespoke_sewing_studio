using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class PortfolioCategoryConfiguration : IEntityTypeConfiguration<PortfolioCategory>
{
    public void Configure(EntityTypeBuilder<PortfolioCategory> builder)
    {
        builder.ToTable("PortfolioCategories");
        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.Name).HasMaxLength(100).IsRequired();
        builder.Property(category => category.Slug).HasMaxLength(120).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(1000);
        builder.Property(category => category.CreatedAt).IsRequired();
        builder.Property(category => category.UpdatedAt).IsRequired();

        builder.HasIndex(category => category.Slug).IsUnique();
        builder.HasIndex(category => new { category.IsActive, category.DisplayOrder });
    }
}
