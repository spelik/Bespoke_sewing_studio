using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Services;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Services;

public sealed partial class ServiceOfferingService(BespokeStudioDbContext dbContext)
    : IServiceOfferingService
{
    public async Task<IReadOnlyList<PublicServiceOfferingResponse>> GetPublicServicesAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        var services = await dbContext.ServiceOfferings
            .AsNoTracking()
            .Include(service => service.PriceOptions)
            .Where(service => service.IsActive && service.ArchivedAt == null)
            .OrderBy(service => service.DisplayOrder)
            .ThenBy(service => service.Name)
            .ToListAsync(cancellationToken);

        return services.Select(ToPublicResponse).ToArray();
    }

    public async Task<IReadOnlyList<AdminServiceOfferingResponse>> GetAdminServicesAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureDefaultsAsync(cancellationToken);

        var services = await dbContext.ServiceOfferings
            .AsNoTracking()
            .Include(service => service.PriceOptions)
            .OrderBy(service => service.ArchivedAt != null)
            .ThenBy(service => service.DisplayOrder)
            .ThenBy(service => service.Name)
            .ToListAsync(cancellationToken);
        var usageCounts = await GetUsageCountsAsync(cancellationToken);

        return services.Select(service =>
            ToAdminResponse(service, usageCounts.GetValueOrDefault(service.Id))).ToArray();
    }

    public async Task<AdminServiceOfferingResponse?> GetByIdAsync(
        Guid serviceOfferingId,
        CancellationToken cancellationToken = default)
    {
        var service = await dbContext.ServiceOfferings
            .AsNoTracking()
            .Include(candidate => candidate.PriceOptions)
            .SingleOrDefaultAsync(candidate => candidate.Id == serviceOfferingId, cancellationToken);

        if (service is null)
        {
            return null;
        }

        var usageCount = await dbContext.Orders
            .AsNoTracking()
            .CountAsync(order => order.ServiceOfferingId == serviceOfferingId, cancellationToken);
        return ToAdminResponse(service, usageCount);
    }

    public async Task<AdminServiceOfferingResponse> CreateAsync(
        CreateServiceOfferingRequest request,
        CancellationToken cancellationToken = default)
    {
        var slug = CreateSlug(request.Slug, request.Name);
        await EnsureSlugAvailableAsync(slug, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var service = new ServiceOffering
        {
            Slug = slug,
            Name = request.Name.Trim(),
            ShortDescription = request.ShortDescription.Trim(),
            Description = Normalize(request.Description),
            Category = Normalize(request.Category),
            ImageUrl = Normalize(request.ImageUrl),
            IsActive = request.IsActive,
            IsFeatured = request.IsFeatured,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
        AddPriceOptions(service, request.PriceOptions);

        dbContext.ServiceOfferings.Add(service);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminResponse(service, 0);
    }

    public async Task<AdminServiceOfferingResponse?> UpdateAsync(
        Guid serviceOfferingId,
        UpdateServiceOfferingRequest request,
        CancellationToken cancellationToken = default)
    {
        var service = await dbContext.ServiceOfferings
            .Include(candidate => candidate.PriceOptions)
            .SingleOrDefaultAsync(candidate => candidate.Id == serviceOfferingId, cancellationToken);
        if (service is null)
        {
            return null;
        }

        var slug = CreateSlug(request.Slug, request.Name);
        await EnsureSlugAvailableAsync(slug, serviceOfferingId, cancellationToken);

        service.Slug = slug;
        service.Name = request.Name.Trim();
        service.ShortDescription = request.ShortDescription.Trim();
        service.Description = Normalize(request.Description);
        service.Category = Normalize(request.Category);
        service.ImageUrl = Normalize(request.ImageUrl);
        service.IsActive = request.IsActive && service.ArchivedAt is null;
        service.IsFeatured = request.IsFeatured;
        service.DisplayOrder = request.DisplayOrder;
        service.UpdatedAt = DateTimeOffset.UtcNow;

        dbContext.ServicePriceOptions.RemoveRange(service.PriceOptions);
        service.PriceOptions.Clear();
        AddPriceOptions(service, request.PriceOptions);

        await dbContext.SaveChangesAsync(cancellationToken);
        var usageCount = await dbContext.Orders
            .AsNoTracking()
            .CountAsync(order => order.ServiceOfferingId == serviceOfferingId, cancellationToken);
        return ToAdminResponse(service, usageCount);
    }

    public async Task<DeleteServiceOfferingResponse?> DeleteOrArchiveAsync(
        Guid serviceOfferingId,
        CancellationToken cancellationToken = default)
    {
        var service = await dbContext.ServiceOfferings
            .SingleOrDefaultAsync(candidate => candidate.Id == serviceOfferingId, cancellationToken);
        if (service is null)
        {
            return null;
        }

        var usageCount = await dbContext.Orders
            .AsNoTracking()
            .CountAsync(order => order.ServiceOfferingId == serviceOfferingId, cancellationToken);

        if (usageCount > 0)
        {
            service.IsActive = false;
            service.ArchivedAt ??= DateTimeOffset.UtcNow;
            service.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return new DeleteServiceOfferingResponse(
                service.Id,
                Deleted: false,
                Archived: true,
                Message: "This service has existing orders and was archived instead of deleted.");
        }

        dbContext.ServiceOfferings.Remove(service);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeleteServiceOfferingResponse(
            service.Id,
            Deleted: true,
            Archived: false,
            Message: "Service deleted.");
    }

    private async Task EnsureDefaultsAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.ServiceOfferings.AsNoTracking().AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.ServiceOfferings.AddRange(CreateDefaultServices());
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            if (!await dbContext.ServiceOfferings.AsNoTracking().AnyAsync(cancellationToken))
            {
                throw;
            }
        }
    }

    private async Task EnsureSlugAvailableAsync(
        string slug,
        Guid? exceptId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ServiceOfferings.AsNoTracking().AnyAsync(
            service =>
                service.ArchivedAt == null &&
                service.Slug == slug &&
                (!exceptId.HasValue || service.Id != exceptId.Value),
            cancellationToken);
        if (exists)
        {
            throw new ServiceOfferingConflictException(
                "Slug",
                "A non-archived service with this slug already exists.");
        }
    }

    private async Task<Dictionary<Guid, int>> GetUsageCountsAsync(CancellationToken cancellationToken) =>
        await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.ServiceOfferingId != null)
            .GroupBy(order => order.ServiceOfferingId!.Value)
            .Select(group => new { Id = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Id, item => item.Count, cancellationToken);

    private static void AddPriceOptions(
        ServiceOffering service,
        IReadOnlyCollection<ServicePriceOptionRequest>? priceOptions)
    {
        foreach (var option in priceOptions ?? [])
        {
            service.PriceOptions.Add(new ServicePriceOption
            {
                ServiceOfferingId = service.Id,
                Label = option.Label.Trim(),
                Description = Normalize(option.Description),
                PriceText = option.PriceText.Trim(),
                DisplayOrder = option.DisplayOrder,
                IsActive = option.IsActive
            });
        }
    }

    private static PublicServiceOfferingResponse ToPublicResponse(ServiceOffering service) =>
        new(
            service.Id,
            service.Slug,
            service.Name,
            service.ShortDescription,
            service.Description,
            service.Category,
            service.IsFeatured,
            service.DisplayOrder,
            service.PriceOptions
                .Where(option => option.IsActive)
                .OrderBy(option => option.DisplayOrder)
                .ThenBy(option => option.Label)
                .Select(ToPriceResponse)
                .ToArray(),
            service.ImageUrl);

    private static AdminServiceOfferingResponse ToAdminResponse(
        ServiceOffering service,
        int usageCount) =>
        new(
            service.Id,
            service.Slug,
            service.Name,
            service.ShortDescription,
            service.Description,
            service.Category,
            service.IsActive,
            service.IsFeatured,
            service.DisplayOrder,
            service.PriceOptions
                .OrderBy(option => option.DisplayOrder)
                .ThenBy(option => option.Label)
                .Select(ToPriceResponse)
                .ToArray(),
            service.ImageUrl,
            service.CreatedAt,
            service.UpdatedAt,
            service.ArchivedAt,
            usageCount,
            CanDelete: usageCount == 0);

    private static ServicePriceOptionResponse ToPriceResponse(ServicePriceOption option) =>
        new(
            option.Id,
            option.Label,
            option.Description,
            option.PriceText,
            option.DisplayOrder,
            option.IsActive);

    private static string CreateSlug(string? suppliedSlug, string name)
    {
        var slug = string.IsNullOrWhiteSpace(suppliedSlug)
            ? GenerateSlug(name)
            : suppliedSlug.Trim();
        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ServiceOfferingConflictException(
                "Slug",
                "Slug could not be generated from the service name. Enter a lowercase kebab-case slug.");
        }

        return slug;
    }

    private static string GenerateSlug(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = InvalidSlugCharacters().Replace(builder.ToString(), "-").Trim('-');
        return RepeatedHyphens().Replace(slug, "-");
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static IReadOnlyCollection<ServiceOffering> CreateDefaultServices()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            CreateDefaultService(
                "tailoring",
                "Tailoring",
                "Bespoke tailoring crafted to your exact measurements and personal aesthetic.",
                "Every piece begins with a detailed consultation and pattern drafting to achieve a precise fit.",
                0,
                now,
                [new ServicePriceOptionRequest("Bespoke tailoring", null, "Quote on request", 0, true)]),
            CreateDefaultService(
                "dressmaking",
                "Dressmaking",
                "Custom dressmaking for everyday elegance or special occasions.",
                "Custom garments are developed around your preferred silhouette, fabric and occasion.",
                1,
                now,
                [new ServicePriceOptionRequest("Custom dressmaking", null, "From consultation", 0, true)]),
            CreateDefaultService(
                "alterations",
                "Alterations",
                "Expert alterations to ensure your garments fit perfectly, including bridal and formal wear.",
                "Taking in, letting out, hemming and delicate finishing work are handled with care.",
                2,
                now,
                [new ServicePriceOptionRequest("Garment alterations", null, "Quote on request", 0, true)]),
            CreateDefaultService(
                "memory-bears",
                "Memory Bears",
                "Handmade keepsakes created from meaningful clothing.",
                "Memory bears are individually made from clothing supplied by the client.",
                3,
                now,
                [
                    new ServicePriceOptionRequest("Small (25 cm)", null, "from £45", 0, true),
                    new ServicePriceOptionRequest("Medium (30–35 cm)", null, "from £65", 1, true),
                    new ServicePriceOptionRequest("Large (40 cm+)", null, "from £95", 2, true),
                    new ServicePriceOptionRequest("Personalised embroidery", "Name or date", "+£15", 3, true)
                ])
        ];
    }

    private static ServiceOffering CreateDefaultService(
        string slug,
        string name,
        string shortDescription,
        string description,
        int displayOrder,
        DateTimeOffset now,
        IReadOnlyCollection<ServicePriceOptionRequest> priceOptions)
    {
        var service = new ServiceOffering
        {
            Slug = slug,
            Name = name,
            ShortDescription = shortDescription,
            Description = description,
            Category = name,
            IsActive = true,
            IsFeatured = true,
            DisplayOrder = displayOrder,
            CreatedAt = now,
            UpdatedAt = now
        };
        AddPriceOptions(service, priceOptions);
        return service;
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidSlugCharacters();

    [GeneratedRegex("-{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex RepeatedHyphens();
}
