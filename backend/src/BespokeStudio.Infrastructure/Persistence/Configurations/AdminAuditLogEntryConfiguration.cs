using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class AdminAuditLogEntryConfiguration : IEntityTypeConfiguration<AdminAuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AdminAuditLogEntry> builder)
    {
        builder.ToTable("AdminAuditLogEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();
        builder.Property(entry => entry.ActorEmail).HasMaxLength(320).IsRequired();
        builder.Property(entry => entry.Action).HasMaxLength(120).IsRequired();
        builder.Property(entry => entry.EntityType).HasMaxLength(120).IsRequired();
        builder.Property(entry => entry.EntityId).HasMaxLength(120);
        builder.Property(entry => entry.EntityLabel).HasMaxLength(320);
        builder.Property(entry => entry.Summary).HasMaxLength(1000).IsRequired();
        builder.Property(entry => entry.MetadataJson).HasColumnType("jsonb");
        builder.HasIndex(entry => entry.CreatedAt);
        builder.HasIndex(entry => new { entry.ActorEmail, entry.CreatedAt });
        builder.HasIndex(entry => new { entry.Action, entry.CreatedAt });
        builder.HasIndex(entry => new { entry.EntityType, entry.EntityId });
    }
}
