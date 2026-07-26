using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class InStockItemConfiguration : IEntityTypeConfiguration<InStockItem>
{
    public void Configure(EntityTypeBuilder<InStockItem> builder)
    {
        builder.ToTable("InStockItems", table =>
            table.HasCheckConstraint("CK_InStockItems_Price", "\"Price\" >= 0"));

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Slug).HasMaxLength(220).IsRequired();
        builder.Property(item => item.Title).HasMaxLength(200).IsRequired();
        builder.Property(item => item.ShortDescription).HasMaxLength(500);
        builder.Property(item => item.Description).HasMaxLength(4000);
        builder.Property(item => item.Price).HasPrecision(12, 2).IsRequired();
        builder.Property(item => item.Currency).HasMaxLength(3).IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(item => item.Sizes).HasMaxLength(500);
        builder.Property(item => item.Materials).HasMaxLength(1000);
        builder.Property(item => item.CreatedAt).IsRequired();
        builder.Property(item => item.UpdatedAt).IsRequired();

        builder.HasMany(item => item.Images)
            .WithOne(image => image.Item)
            .HasForeignKey(image => image.InStockItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique among non-archived rows (same filtered-unique pattern as Portfolio/Services).
        builder.HasIndex(item => item.Slug)
            .IsUnique()
            .HasFilter("\"ArchivedAt\" IS NULL");
        builder.HasIndex(item => new { item.IsPublished, item.DisplayOrder, item.CreatedAt });
        builder.HasIndex(item => item.ArchivedAt);
        builder.HasIndex(item => item.Status);
    }
}
