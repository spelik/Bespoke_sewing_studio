using BespokeStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Persistence;

public sealed class BespokeStudioDbContext(DbContextOptions<BespokeStudioDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderAttachment> OrderAttachments => Set<OrderAttachment>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    public DbSet<PortfolioCategory> PortfolioCategories => Set<PortfolioCategory>();
    public DbSet<PortfolioItem> PortfolioItems => Set<PortfolioItem>();
    public DbSet<ServiceOffering> ServiceOfferings => Set<ServiceOffering>();
    public DbSet<UploadedFileMetadata> UploadedFiles => Set<UploadedFileMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BespokeStudioDbContext).Assembly);
    }
}
