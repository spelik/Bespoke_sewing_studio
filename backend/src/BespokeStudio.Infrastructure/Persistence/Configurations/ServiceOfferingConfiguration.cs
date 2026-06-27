using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class ServiceOfferingConfiguration : IEntityTypeConfiguration<ServiceOffering>
{
    public void Configure(EntityTypeBuilder<ServiceOffering> builder)
    {
        builder.ToTable("ServiceOfferings", table =>
            table.HasCheckConstraint("CK_ServiceOfferings_StartingPrice", "\"StartingPrice\" IS NULL OR \"StartingPrice\" >= 0"));

        builder.HasKey(service => service.Id);

        builder.Property(service => service.Id).ValueGeneratedNever();
        builder.Property(service => service.ServiceType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(service => service.Name).HasMaxLength(150).IsRequired();
        builder.Property(service => service.ShortDescription).HasMaxLength(500).IsRequired();
        builder.Property(service => service.DetailedDescription).HasMaxLength(4000);
        builder.Property(service => service.StartingPrice).HasPrecision(12, 2);
        builder.Property(service => service.Currency).HasMaxLength(3).IsRequired();
        builder.Property(service => service.CreatedAt).IsRequired();
        builder.Property(service => service.UpdatedAt).IsRequired();

        builder.HasIndex(service => service.ServiceType).IsUnique();
        builder.HasIndex(service => new { service.IsActive, service.DisplayOrder });
    }
}
