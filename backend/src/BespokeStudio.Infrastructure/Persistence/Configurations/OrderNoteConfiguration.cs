using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class OrderNoteConfiguration : IEntityTypeConfiguration<OrderNote>
{
    public void Configure(EntityTypeBuilder<OrderNote> builder)
    {
        builder.ToTable("OrderNotes");
        builder.HasKey(note => note.Id);

        builder.Property(note => note.Id).ValueGeneratedNever();
        builder.Property(note => note.Text).HasMaxLength(2000).IsRequired();
        builder.Property(note => note.CreatedAt).IsRequired();

        builder.HasOne<Order>()
            .WithMany()
            .HasForeignKey(note => note.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(note => new { note.OrderId, note.CreatedAt });
    }
}
