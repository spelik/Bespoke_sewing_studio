using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Portfolio;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Services;

public sealed partial class PortfolioService(BespokeStudioDbContext dbContext) : IPortfolioService
{
    private const string ImageRoute = "/api/portfolio/images/";

    public async Task<IReadOnlyList<PublicPortfolioItemResponse>> GetPublicPortfolioAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultCategoriesAsync(cancellationToken);
        var items = await dbContext.PortfolioItems.AsNoTracking()
            .Include(item => item.Category)
            .Where(item => item.IsActive && item.ArchivedAt == null && item.CoverImageFileId != null &&
                item.Category != null && item.Category.IsActive && item.Category.ArchivedAt == null)
            .OrderBy(item => item.DisplayOrder).ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);
        return items.Select(ToPublicItem).ToArray();
    }

    public async Task<IReadOnlyList<PublicPortfolioCategoryResponse>> GetPublicCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultCategoriesAsync(cancellationToken);
        return await dbContext.PortfolioCategories.AsNoTracking()
            .Where(category => category.IsActive && category.ArchivedAt == null)
            .OrderBy(category => category.DisplayOrder).ThenBy(category => category.Name)
            .Select(category => new PublicPortfolioCategoryResponse(category.Id, category.Slug, category.Name, category.Description, category.DisplayOrder))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AdminPortfolioItemResponse>> GetAdminItemsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultCategoriesAsync(cancellationToken);
        var items = await dbContext.PortfolioItems.AsNoTracking().Include(item => item.Category)
            .OrderBy(item => item.ArchivedAt != null).ThenBy(item => item.DisplayOrder).ThenBy(item => item.Title)
            .ToListAsync(cancellationToken);
        return items.Select(ToAdminItem).ToArray();
    }

    public async Task<AdminPortfolioItemResponse?> GetAdminItemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.PortfolioItems.AsNoTracking().Include(candidate => candidate.Category)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        return item is null ? null : ToAdminItem(item);
    }

    public async Task<AdminPortfolioItemResponse> CreateItemAsync(SavePortfolioItemRequest request, CancellationToken cancellationToken = default)
    {
        var category = await GetValidCategoryAsync(request.CategoryId, request.IsActive, cancellationToken);
        await EnsurePortfolioImageAsync(request.ImageFileId, request.IsActive, cancellationToken);
        var slug = CreateSlug(request.Slug, request.Title);
        await EnsureItemSlugAvailableAsync(slug, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var item = new PortfolioItem
        {
            CategoryId = category.Id,
            Slug = slug,
            Title = request.Title.Trim(),
            ShortDescription = Normalize(request.ShortDescription),
            Description = Normalize(request.Description),
            CoverImageFileId = request.ImageFileId,
            AltText = Normalize(request.AltText) ?? request.Title.Trim(),
            IsActive = request.IsActive,
            IsFeatured = request.IsFeatured,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = now,
            UpdatedAt = now,
            Category = category
        };
        dbContext.PortfolioItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminItem(item);
    }

    public async Task<AdminPortfolioItemResponse?> UpdateItemAsync(Guid id, SavePortfolioItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.PortfolioItems.Include(candidate => candidate.Category)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null) return null;
        var isActive = request.IsActive && item.ArchivedAt is null;
        var category = await GetValidCategoryAsync(request.CategoryId, isActive, cancellationToken);
        await EnsurePortfolioImageAsync(request.ImageFileId, isActive, cancellationToken);
        var slug = CreateSlug(request.Slug, request.Title);
        await EnsureItemSlugAvailableAsync(slug, id, cancellationToken);
        item.CategoryId = category.Id;
        item.Category = category;
        item.Slug = slug;
        item.Title = request.Title.Trim();
        item.ShortDescription = Normalize(request.ShortDescription);
        item.Description = Normalize(request.Description);
        item.CoverImageFileId = request.ImageFileId;
        item.AltText = Normalize(request.AltText) ?? request.Title.Trim();
        item.IsActive = isActive;
        item.IsFeatured = request.IsFeatured;
        item.DisplayOrder = request.DisplayOrder;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminItem(item);
    }

    public async Task<DeletePortfolioResponse?> DeleteOrArchiveItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await dbContext.PortfolioItems.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null) return null;
        item.IsActive = false;
        item.ArchivedAt ??= DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeletePortfolioResponse(id, false, true, "Portfolio item archived. Its image file was retained.");
    }

    public async Task<IReadOnlyList<AdminPortfolioCategoryResponse>> GetAdminCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureDefaultCategoriesAsync(cancellationToken);
        return await dbContext.PortfolioCategories.AsNoTracking()
            .OrderBy(category => category.ArchivedAt != null).ThenBy(category => category.DisplayOrder).ThenBy(category => category.Name)
            .Select(category => new AdminPortfolioCategoryResponse(category.Id, category.Slug, category.Name, category.Description,
                category.IsActive, category.DisplayOrder, category.CreatedAt, category.UpdatedAt, category.ArchivedAt, category.Items.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminPortfolioCategoryResponse> CreateCategoryAsync(SavePortfolioCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var slug = CreateSlug(request.Slug, request.Name);
        await EnsureCategorySlugAvailableAsync(slug, null, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var category = new PortfolioCategory { Slug = slug, Name = request.Name.Trim(), Description = Normalize(request.Description),
            IsActive = request.IsActive, DisplayOrder = request.DisplayOrder, CreatedAt = now, UpdatedAt = now };
        dbContext.PortfolioCategories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminCategory(category, 0);
    }

    public async Task<AdminPortfolioCategoryResponse?> UpdateCategoryAsync(Guid id, SavePortfolioCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.PortfolioCategories.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (category is null) return null;
        var slug = CreateSlug(request.Slug, request.Name);
        await EnsureCategorySlugAvailableAsync(slug, id, cancellationToken);
        category.Slug = slug;
        category.Name = request.Name.Trim();
        category.Description = Normalize(request.Description);
        category.IsActive = request.IsActive && category.ArchivedAt is null;
        category.DisplayOrder = request.DisplayOrder;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await dbContext.PortfolioItems.CountAsync(item => item.CategoryId == id, cancellationToken);
        return ToAdminCategory(category, count);
    }

    public async Task<DeletePortfolioResponse?> DeleteOrArchiveCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.PortfolioCategories.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (category is null) return null;
        var itemCount = await dbContext.PortfolioItems.CountAsync(item => item.CategoryId == id, cancellationToken);
        if (itemCount == 0)
        {
            dbContext.PortfolioCategories.Remove(category);
            await dbContext.SaveChangesAsync(cancellationToken);
            return new DeletePortfolioResponse(id, true, false, "Portfolio category deleted.");
        }
        category.IsActive = false;
        category.ArchivedAt ??= DateTimeOffset.UtcNow;
        category.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return new DeletePortfolioResponse(id, false, true, "Category has portfolio items and was archived instead of deleted.");
    }

    private async Task<PortfolioCategory> GetValidCategoryAsync(Guid id, bool itemWillBeActive, CancellationToken cancellationToken)
    {
        var category = await dbContext.PortfolioCategories.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken)
            ?? throw new PortfolioConflictException("CategoryId", "The selected portfolio category does not exist.");
        if (category.ArchivedAt is not null || (itemWillBeActive && !category.IsActive))
            throw new PortfolioConflictException("CategoryId", "An active item requires an active, non-archived category.");
        return category;
    }

    private async Task EnsurePortfolioImageAsync(Guid? id, bool required, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            if (required) throw new PortfolioConflictException("ImageFileId", "An image is required before the portfolio item can be active.");
            return;
        }
        var valid = await dbContext.UploadedFiles.AsNoTracking().AnyAsync(file => file.Id == id && file.Purpose == UploadPurpose.PortfolioImage, cancellationToken);
        if (!valid) throw new PortfolioConflictException("ImageFileId", "Select a valid uploaded portfolio image.");
    }

    private async Task EnsureDefaultCategoriesAsync(CancellationToken cancellationToken)
    {
        if (await dbContext.PortfolioCategories.AsNoTracking().AnyAsync(cancellationToken)) return;
        var now = DateTimeOffset.UtcNow;
        dbContext.PortfolioCategories.AddRange(
            new PortfolioCategory { Slug = "dressmaking", Name = "Dressmaking", DisplayOrder = 0, CreatedAt = now, UpdatedAt = now },
            new PortfolioCategory { Slug = "alterations", Name = "Alterations", DisplayOrder = 1, CreatedAt = now, UpdatedAt = now },
            new PortfolioCategory { Slug = "memory-bears", Name = "Memory Bears", DisplayOrder = 2, CreatedAt = now, UpdatedAt = now });
        try { await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            if (!await dbContext.PortfolioCategories.AsNoTracking().AnyAsync(cancellationToken)) throw;
        }
    }

    private async Task EnsureItemSlugAvailableAsync(string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await dbContext.PortfolioItems.AsNoTracking().AnyAsync(item => item.ArchivedAt == null && item.Slug == slug && (!exceptId.HasValue || item.Id != exceptId), cancellationToken))
            throw new PortfolioConflictException("Slug", "A non-archived portfolio item with this slug already exists.");
    }

    private async Task EnsureCategorySlugAvailableAsync(string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        if (await dbContext.PortfolioCategories.AsNoTracking().AnyAsync(category => category.ArchivedAt == null && category.Slug == slug && (!exceptId.HasValue || category.Id != exceptId), cancellationToken))
            throw new PortfolioConflictException("Slug", "A non-archived portfolio category with this slug already exists.");
    }

    private static PublicPortfolioItemResponse ToPublicItem(PortfolioItem item) => new(item.Id, item.Slug, item.Title,
        item.ShortDescription, item.Description, new PublicPortfolioCategoryResponse(item.Category!.Id, item.Category.Slug,
            item.Category.Name, item.Category.Description, item.Category.DisplayOrder), $"{ImageRoute}{item.CoverImageFileId}",
        item.AltText, item.IsFeatured, item.DisplayOrder);

    private static AdminPortfolioItemResponse ToAdminItem(PortfolioItem item) => new(item.Id, item.Slug, item.Title,
        item.ShortDescription, item.Description, item.CategoryId, item.Category?.Name ?? "Unknown", item.CoverImageFileId,
        item.CoverImageFileId is null ? null : $"{ImageRoute}{item.CoverImageFileId}", item.AltText, item.IsActive,
        item.IsFeatured, item.DisplayOrder, item.CreatedAt, item.UpdatedAt, item.ArchivedAt);

    private static AdminPortfolioCategoryResponse ToAdminCategory(PortfolioCategory category, int itemCount) => new(category.Id,
        category.Slug, category.Name, category.Description, category.IsActive, category.DisplayOrder, category.CreatedAt,
        category.UpdatedAt, category.ArchivedAt, itemCount);

    private static string CreateSlug(string? supplied, string source)
    {
        if (!string.IsNullOrWhiteSpace(supplied)) return supplied.Trim();
        var normalized = source.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark) builder.Append(character);
        var slug = InvalidSlugCharacters().Replace(builder.ToString(), "-").Trim('-');
        slug = RepeatedHyphens().Replace(slug, "-");
        if (slug.Length == 0) throw new PortfolioConflictException("Slug", "Enter a lowercase kebab-case slug.");
        return slug;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)] private static partial Regex InvalidSlugCharacters();
    [GeneratedRegex("-{2,}", RegexOptions.CultureInvariant)] private static partial Regex RepeatedHyphens();
}
