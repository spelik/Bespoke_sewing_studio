using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class EmailDeliveryLogEntryConfiguration : IEntityTypeConfiguration<EmailDeliveryLogEntry>
{
    public void Configure(EntityTypeBuilder<EmailDeliveryLogEntry> builder)
    {
        builder.ToTable("EmailDeliveryLogEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();
        builder.Property(entry => entry.MessageType).HasMaxLength(120).IsRequired();
        builder.Property(entry => entry.RecipientEmail).HasMaxLength(320).IsRequired();
        builder.Property(entry => entry.Subject).HasMaxLength(320).IsRequired();
        builder.Property(entry => entry.Provider).HasMaxLength(80).IsRequired();
        builder.Property(entry => entry.Status).HasMaxLength(32).IsRequired();
        builder.Property(entry => entry.ResultMessage).HasMaxLength(1000).IsRequired();
        builder.Property(entry => entry.ErrorMessage).HasMaxLength(1000);
        builder.Property(entry => entry.RelatedEntityType).HasMaxLength(120);
        builder.Property(entry => entry.RelatedEntityId).HasMaxLength(120);
        builder.Property(entry => entry.RelatedEntityLabel).HasMaxLength(320);
        builder.Property(entry => entry.CreatedAt).IsRequired();
        builder.Property(entry => entry.CompletedAt);

        builder.HasIndex(entry => entry.CreatedAt);
        builder.HasIndex(entry => new { entry.Status, entry.CreatedAt });
        builder.HasIndex(entry => new { entry.MessageType, entry.CreatedAt });
        builder.HasIndex(entry => new { entry.RecipientEmail, entry.CreatedAt });
        builder.HasIndex(entry => new { entry.Provider, entry.CreatedAt });
        builder.HasIndex(entry => new { entry.RelatedEntityType, entry.RelatedEntityId });
    }
}
