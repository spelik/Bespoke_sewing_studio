using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class EmailOutboxMessageConfiguration : IEntityTypeConfiguration<EmailOutboxMessage>
{
    public void Configure(EntityTypeBuilder<EmailOutboxMessage> builder)
    {
        builder.ToTable("EmailOutboxMessages", table =>
        {
            table.HasCheckConstraint("CK_EmailOutboxMessages_Attempts", "\"Attempts\" >= 0");
            table.HasCheckConstraint("CK_EmailOutboxMessages_MaxAttempts", "\"MaxAttempts\" > 0");
            table.HasCheckConstraint(
                "CK_EmailOutboxMessages_Body",
                "\"HtmlBody\" IS NOT NULL OR \"TextBody\" IS NOT NULL");
        });

        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.MessageType).HasMaxLength(120).IsRequired();
        builder.Property(message => message.RecipientEmail).HasMaxLength(320).IsRequired();
        builder.Property(message => message.RecipientName).HasMaxLength(200);
        builder.Property(message => message.Subject).HasMaxLength(320).IsRequired();
        builder.Property(message => message.HtmlBody).HasColumnType("text");
        builder.Property(message => message.TextBody).HasColumnType("text");
        builder.Property(message => message.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(1000);
        builder.Property(message => message.RelatedEntityType).HasMaxLength(120);
        builder.Property(message => message.RelatedEntityId).HasMaxLength(120);
        builder.Property(message => message.RelatedEntityLabel).HasMaxLength(320);
        builder.Property(message => message.CorrelationId).HasMaxLength(120);
        builder.Property(message => message.CreatedAt).IsRequired();
        builder.Property(message => message.UpdatedAt).IsRequired();

        builder.HasOne<EmailDeliveryLogEntry>()
            .WithMany()
            .HasForeignKey(message => message.EmailDeliveryLogEntryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(message => new { message.Status, message.NextAttemptAt, message.CreatedAt });
        builder.HasIndex(message => new { message.RelatedEntityType, message.RelatedEntityId });
        builder.HasIndex(message => message.CreatedAt);
        builder.HasIndex(message => message.EmailDeliveryLogEntryId);
    }
}
