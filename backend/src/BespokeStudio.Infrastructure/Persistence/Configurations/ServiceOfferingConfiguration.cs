using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class ServiceOfferingConfiguration : IEntityTypeConfiguration<ServiceOffering>
{
    public void Configure(EntityTypeBuilder<ServiceOffering> builder)
    {
        builder.ToTable("ServiceOfferings");

        builder.HasKey(service => service.Id);

        builder.Property(service => service.Id).ValueGeneratedNever();
        builder.Property(service => service.Slug).HasMaxLength(160).IsRequired();
        builder.Property(service => service.Name).HasMaxLength(150).IsRequired();
        builder.Property(service => service.ShortDescription).HasMaxLength(500).IsRequired();
        builder.Property(service => service.Description).HasMaxLength(4000);
        builder.Property(service => service.Category).HasMaxLength(100);
        builder.Property(service => service.ImageUrl).HasMaxLength(2048);
        builder.Property(service => service.CreatedAt).IsRequired();
        builder.Property(service => service.UpdatedAt).IsRequired();

        builder.HasIndex(service => service.Slug)
            .IsUnique()
            .HasFilter("\"ArchivedAt\" IS NULL");
        builder.HasIndex(service => new { service.IsActive, service.DisplayOrder });
        builder.HasIndex(service => service.ArchivedAt);
    }
}
