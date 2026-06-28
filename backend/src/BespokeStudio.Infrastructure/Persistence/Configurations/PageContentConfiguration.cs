using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class PageContentConfiguration : IEntityTypeConfiguration<PageContent>
{
    public void Configure(EntityTypeBuilder<PageContent> builder)
    {
        builder.ToTable("PageContents");
        builder.HasKey(content => content.Id);
        builder.Property(content => content.Id).ValueGeneratedNever();
        builder.Property(content => content.PageKey).HasMaxLength(50).IsRequired();
        builder.Property(content => content.SectionKey).HasMaxLength(80).IsRequired();
        builder.Property(content => content.Title).HasMaxLength(500);
        builder.Property(content => content.Subtitle).HasMaxLength(1000);
        builder.Property(content => content.Body).HasMaxLength(12000);
        builder.Property(content => content.CtaLabel).HasMaxLength(150);
        builder.Property(content => content.CtaUrl).HasMaxLength(2048);
        builder.Property(content => content.ImageAltText).HasMaxLength(250);
        builder.HasOne(content => content.ImageFile).WithMany().HasForeignKey(content => content.ImageFileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(content => new { content.PageKey, content.SectionKey }).IsUnique().HasFilter("\"ArchivedAt\" IS NULL");
        builder.HasIndex(content => new { content.PageKey, content.IsActive, content.DisplayOrder });
        builder.HasIndex(content => content.ArchivedAt);
    }
}
