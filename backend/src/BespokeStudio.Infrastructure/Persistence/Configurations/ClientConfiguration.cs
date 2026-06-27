using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BespokeStudio.Infrastructure.Persistence.Configurations;

public sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("Clients");
        builder.HasKey(client => client.Id);

        builder.Property(client => client.Id).ValueGeneratedNever();
        builder.Property(client => client.FullName).HasMaxLength(200).IsRequired();
        builder.Property(client => client.Email).HasMaxLength(320);
        builder.Property(client => client.Phone).HasMaxLength(50);
        builder.Property(client => client.CreatedAt).IsRequired();
        builder.Property(client => client.UpdatedAt).IsRequired();

        builder.HasIndex(client => client.Email);
    }
}
