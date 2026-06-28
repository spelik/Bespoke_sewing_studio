using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
            table.HasCheckConstraint("CK_Orders_QuotedAmount", "\"QuotedAmount\" IS NULL OR \"QuotedAmount\" >= 0"));

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.ServiceType).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(order => order.ServiceNameSnapshot).HasMaxLength(150).IsRequired();
        builder.Property(order => order.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(order => order.Description).HasMaxLength(4000).IsRequired();
        builder.Property(order => order.Currency).HasMaxLength(3).IsRequired();
        builder.Property(order => order.QuotedAmount).HasPrecision(12, 2);
        builder.Property(order => order.CreatedAt).IsRequired();
        builder.Property(order => order.UpdatedAt).IsRequired();

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(order => order.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(order => order.ServiceOffering)
            .WithMany()
            .HasForeignKey(order => order.ServiceOfferingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(order => new { order.Status, order.CreatedAt });
        builder.HasIndex(order => order.ServiceOfferingId);
    }
}
