using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.FullName).HasMaxLength(200).IsRequired();
        builder.Property(message => message.Email).HasMaxLength(320).IsRequired();
        builder.Property(message => message.Phone).HasMaxLength(50);
        builder.Property(message => message.Subject).HasMaxLength(250);
        builder.Property(message => message.Message).HasMaxLength(4000).IsRequired();
        builder.Property(message => message.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();
        builder.HasIndex(message => message.Status);
        builder.HasIndex(message => message.CreatedAt);
        builder.HasIndex(message => message.Email);
    }
}
