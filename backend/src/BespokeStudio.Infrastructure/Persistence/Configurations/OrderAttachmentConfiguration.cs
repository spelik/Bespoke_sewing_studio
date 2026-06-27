using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class OrderAttachmentConfiguration : IEntityTypeConfiguration<OrderAttachment>
{
    public void Configure(EntityTypeBuilder<OrderAttachment> builder)
    {
        builder.ToTable("OrderAttachments");
        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id).ValueGeneratedNever();
        builder.Property(attachment => attachment.Caption).HasMaxLength(500);
        builder.Property(attachment => attachment.CreatedAt).IsRequired();

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(attachment => attachment.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<UploadedFileMetadata>()
            .WithMany()
            .HasForeignKey(attachment => attachment.UploadedFileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(attachment => new { attachment.OrderId, attachment.DisplayOrder });
        builder.HasIndex(attachment => attachment.UploadedFileId).IsUnique();
    }
}
