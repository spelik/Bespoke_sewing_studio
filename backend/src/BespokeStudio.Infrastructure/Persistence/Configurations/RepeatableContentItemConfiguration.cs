using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class RepeatableContentItemConfiguration : IEntityTypeConfiguration<RepeatableContentItem>
{
    public void Configure(EntityTypeBuilder<RepeatableContentItem> builder)
    {
        builder.ToTable("RepeatableContentItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.GroupKey).HasMaxLength(80).IsRequired();
        builder.Property(item => item.ItemKey).HasMaxLength(100).IsRequired();
        builder.Property(item => item.Title).HasMaxLength(500);
        builder.Property(item => item.Subtitle).HasMaxLength(1000);
        builder.Property(item => item.Body).HasMaxLength(12000);
        builder.Property(item => item.Label).HasMaxLength(100);
        builder.Property(item => item.Value).HasMaxLength(500);
        builder.Property(item => item.IconKey).HasMaxLength(50);
        builder.Property(item => item.Url).HasMaxLength(2048);
        builder.Property(item => item.Location).HasMaxLength(250);
        builder.Property(item => item.Service).HasMaxLength(250);
        builder.HasIndex(item => new { item.GroupKey, item.ItemKey }).IsUnique().HasFilter("\"ArchivedAt\" IS NULL");
        builder.HasIndex(item => new { item.GroupKey, item.IsActive, item.DisplayOrder });
        builder.HasIndex(item => item.ArchivedAt);
    }
}
