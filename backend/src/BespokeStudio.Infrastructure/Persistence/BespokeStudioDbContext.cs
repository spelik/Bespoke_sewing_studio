using BespokeStudio.Domain.Entities;
using BespokeStudio.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Persistence;

public sealed class BespokeStudioDbContext(DbContextOptions<BespokeStudioDbContext> options)
    : IdentityDbContext<AdminUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderAttachment> OrderAttachments => Set<OrderAttachment>();
    public DbSet<OrderNote> OrderNotes => Set<OrderNote>();
    public DbSet<PageContent> PageContents => Set<PageContent>();
    public DbSet<PortfolioCategory> PortfolioCategories => Set<PortfolioCategory>();
    public DbSet<PortfolioItem> PortfolioItems => Set<PortfolioItem>();
    public DbSet<ServiceOffering> ServiceOfferings => Set<ServiceOffering>();
    public DbSet<ServicePriceOption> ServicePriceOptions => Set<ServicePriceOption>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<UploadedFileMetadata> UploadedFiles => Set<UploadedFileMetadata>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BespokeStudioDbContext).Assembly);
    }
}
