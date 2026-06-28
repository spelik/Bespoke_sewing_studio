using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class ServicePriceOptionConfiguration : IEntityTypeConfiguration<ServicePriceOption>
{
    public void Configure(EntityTypeBuilder<ServicePriceOption> builder)
    {
        builder.ToTable("ServicePriceOptions");
        builder.HasKey(option => option.Id);
        builder.Property(option => option.Id).ValueGeneratedNever();
        builder.Property(option => option.Label).HasMaxLength(150).IsRequired();
        builder.Property(option => option.Description).HasMaxLength(500);
        builder.Property(option => option.PriceText).HasMaxLength(100).IsRequired();

        builder.HasOne<ServiceOffering>()
            .WithMany(service => service.PriceOptions)
            .HasForeignKey(option => option.ServiceOfferingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(option => new { option.ServiceOfferingId, option.DisplayOrder });
        builder.HasIndex(option => new { option.ServiceOfferingId, option.IsActive });
    }
}
