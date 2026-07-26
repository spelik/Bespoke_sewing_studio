using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.InStock;
using BespokeStudio.Application.Contracts.Storage;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace BespokeStudio.Infrastructure.Services;

public sealed partial class InStockService(
    BespokeStudioDbContext dbContext,
    IUploadService uploadService,
    IUploadFileDeletionScheduler fileDeletionScheduler,
    IDbContextTransactionFactory transactionFactory,
    ILogger<InStockService> logger) : IInStockService
{
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromSeconds(30);
    private const string ImageRoute = "/api/in-stock/images/";

    public async Task<IReadOnlyList<PublicInStockItemResponse>> GetPublicItemsAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.InStockItems.AsNoTracking()
            .Include(item => item.Images)
            .ThenInclude(image => image.UploadedFile)
            .Where(item => item.IsPublished && item.ArchivedAt == null)
            .OrderBy(item => item.DisplayOrder)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(ToPublicItem).ToArray();
    }

    public async Task<PublicInStockItemResponse?> GetPublicItemBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var normalized = slug.Trim();
        var item = await dbContext.InStockItems.AsNoTracking()
            .Include(candidate => candidate.Images)
            .ThenInclude(image => image.UploadedFile)
            .SingleOrDefaultAsync(
                candidate => candidate.Slug == normalized &&
                    candidate.IsPublished &&
                    candidate.ArchivedAt == null,
                cancellationToken);

        return item is null ? null : ToPublicItem(item);
    }

    public async Task<IReadOnlyList<AdminInStockItemResponse>> GetAdminItemsAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await dbContext.InStockItems.AsNoTracking()
            .Include(item => item.Images)
            .ThenInclude(image => image.UploadedFile)
            .OrderBy(item => item.ArchivedAt != null)
            .ThenBy(item => item.DisplayOrder)
            .ThenBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(ToAdminItem).ToArray();
    }

    public async Task<AdminInStockItemResponse?> GetAdminItemByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.InStockItems.AsNoTracking()
            .Include(candidate => candidate.Images)
            .ThenInclude(image => image.UploadedFile)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return item is null ? null : ToAdminItem(item);
    }

    public async Task<AdminInStockItemResponse> CreateItemAsync(
        SaveInStockItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var slug = CreateSlug(request.Slug, request.Title);
        await EnsureSlugAvailableAsync(slug, null, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var item = new InStockItem
        {
            Slug = slug,
            Title = request.Title.Trim(),
            ShortDescription = Normalize(request.ShortDescription),
            Description = Normalize(request.Description),
            Price = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero),
            Currency = InStockItem.DefaultCurrency,
            Status = request.Status,
            IsPublished = request.IsPublished,
            DisplayOrder = request.DisplayOrder,
            Sizes = Normalize(request.Sizes),
            Materials = Normalize(request.Materials),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.InStockItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminItem(item);
    }

    public async Task<AdminInStockItemResponse?> UpdateItemAsync(
        Guid id,
        SaveInStockItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.InStockItems
            .Include(candidate => candidate.Images)
            .ThenInclude(image => image.UploadedFile)
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var slug = CreateSlug(request.Slug, request.Title);
        await EnsureSlugAvailableAsync(slug, id, cancellationToken);

        item.Slug = slug;
        item.Title = request.Title.Trim();
        item.ShortDescription = Normalize(request.ShortDescription);
        item.Description = Normalize(request.Description);
        item.Price = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero);
        item.Currency = InStockItem.DefaultCurrency;
        item.Status = request.Status;
        item.IsPublished = request.IsPublished && item.ArchivedAt is null;
        item.DisplayOrder = request.DisplayOrder;
        item.Sizes = Normalize(request.Sizes);
        item.Materials = Normalize(request.Materials);
        item.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminItem(item);
    }

    public async Task<ArchiveInStockItemResponse?> ArchiveItemAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.InStockItems.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        item.IsPublished = false;
        item.ArchivedAt ??= DateTimeOffset.UtcNow;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ArchiveInStockItemResponse(
            id,
            Archived: true,
            Restored: false,
            "IN STOCK item archived. Images were retained.");
    }

    public async Task<ArchiveInStockItemResponse?> RestoreItemAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.InStockItems.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        if (item.ArchivedAt is null)
        {
            return new ArchiveInStockItemResponse(
                id,
                Archived: false,
                Restored: false,
                "IN STOCK item is not archived.");
        }

        await EnsureSlugAvailableAsync(item.Slug, id, cancellationToken);

        item.ArchivedAt = null;
        item.IsPublished = false;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ArchiveInStockItemResponse(
            id,
            Archived: false,
            Restored: true,
            "IN STOCK item restored as unpublished. Republish when ready.");
    }

    public async Task<AdminInStockImageResponse?> AddImageAsync(
        Guid itemId,
        UploadFileRequest file,
        string? altText,
        int? displayOrder,
        CancellationToken cancellationToken = default)
    {
        // Resolve the owner before promote so missing items never leave promoted orphans.
        var item = await dbContext.InStockItems
            .Include(candidate => candidate.Images)
            .SingleOrDefaultAsync(candidate => candidate.Id == itemId, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var prepared = await uploadService.PrepareInStockImageAsync(file, cancellationToken);
        var uploadedFile = prepared.Metadata;

        IDbContextTransaction? transaction = null;
        var commitAttempted = false;

        try
        {
            // Everything after successful promotion must be covered so orphans are compensated.
            var nextOrder = displayOrder ?? (item.Images.Count == 0
                ? 0
                : item.Images.Max(image => image.DisplayOrder) + 1);

            var image = new InStockItemImage
            {
                InStockItemId = item.Id,
                UploadedFileId = uploadedFile.Id,
                UploadedFile = uploadedFile,
                AltText = Normalize(altText),
                DisplayOrder = nextOrder,
                CreatedAt = DateTimeOffset.UtcNow
            };

            transaction = await transactionFactory.BeginTransactionAsync(cancellationToken);

            // UploadedFile + InStockItemImage + UpdatedAt commit together.
            dbContext.UploadedFiles.Add(uploadedFile);
            item.Images.Add(image);
            item.UpdatedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                commitAttempted = true;
                await transaction.CommitAsync(cancellationToken);
            }

            return ToAdminImage(image);
        }
        catch (Exception exception)
        {
            using var cleanupCts = new CancellationTokenSource(CleanupTimeout);
            var cleanupToken = cleanupCts.Token;

            if (transaction is not null && !commitAttempted)
            {
                try
                {
                    await transaction.RollbackAsync(cleanupToken);
                }
                catch (Exception rollbackException)
                {
                    logger.LogWarning(
                        rollbackException,
                        "IN STOCK image link rollback failed after a linking error. Continuing with compensation.");
                }
            }

            if (!commitAttempted)
            {
                try
                {
                    await uploadService.CompensateOrphanedPromotedFileAsync(
                        prepared.FinalStorageKey,
                        uploadedFile.OriginalFileName,
                        uploadedFile.SizeBytes,
                        cleanupToken);
                }
                catch (Exception compensationException)
                {
                    logger.LogError(
                        compensationException,
                        "IN STOCK image compensation failed for storage key {StorageKey}. Storage maintenance may collect the orphan later.",
                        prepared.FinalStorageKey);
                }
            }
            else
            {
                // CommitAsync was invoked; the server may have committed before the client saw failure.
                // Do not delete the promoted file immediately — that could break a persisted DB link.
                logger.LogError(
                    exception,
                    "IN STOCK image link commit outcome is ambiguous for storage key {StorageKey}. Leaving the promoted file for storage maintenance reconciliation.",
                    prepared.FinalStorageKey);
            }

            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                try
                {
                    await transaction.DisposeAsync();
                }
                catch (Exception disposeException)
                {
                    // Dispose must not replace the original exception or fail a successful commit,
                    // and must not trigger compensation (which only runs in the catch above).
                    logger.LogWarning(
                        disposeException,
                        "IN STOCK image link transaction dispose failed. Operation outcome is unchanged.");
                }
            }
        }
    }

    public async Task<AdminInStockImageResponse?> UpdateImageAsync(
        Guid itemId,
        Guid imageId,
        UpdateInStockImageRequest request,
        CancellationToken cancellationToken = default)
    {
        var image = await dbContext.InStockItemImages
            .Include(candidate => candidate.UploadedFile)
            .Include(candidate => candidate.Item)
            .SingleOrDefaultAsync(
                candidate => candidate.Id == imageId && candidate.InStockItemId == itemId,
                cancellationToken);
        if (image is null)
        {
            return null;
        }

        image.AltText = Normalize(request.AltText);
        image.DisplayOrder = request.DisplayOrder;
        if (image.Item is not null)
        {
            image.Item.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminImage(image);
    }

    public async Task<bool> DeleteImageAsync(
        Guid itemId,
        Guid imageId,
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            transaction = await transactionFactory.BeginTransactionAsync(cancellationToken);

            var image = await dbContext.InStockItemImages
                .Include(candidate => candidate.UploadedFile)
                .Include(candidate => candidate.Item)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == imageId && candidate.InStockItemId == itemId,
                    cancellationToken);
            if (image?.UploadedFile is null)
            {
                return false;
            }

            var file = image.UploadedFile;

            // Schedule deletion job in the same DbContext; physical delete happens after commit.
            await fileDeletionScheduler.ScheduleAsync(
                new ScheduleUploadFileDeletionRequest(
                    file.StorageKey,
                    file.OriginalFileName,
                    file.SizeBytes,
                    "InStockItemImage",
                    image.Id,
                    "in_stock_image.deleted"),
                cancellationToken);

            if (image.Item is not null)
            {
                image.Item.UpdatedAt = DateTimeOffset.UtcNow;
            }

            dbContext.InStockItemImages.Remove(image);
            dbContext.UploadedFiles.Remove(file);
            await dbContext.SaveChangesAsync(cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return true;
        }
        catch
        {
            if (transaction is not null)
            {
                using var cleanupCts = new CancellationTokenSource(CleanupTimeout);
                try
                {
                    await transaction.RollbackAsync(cleanupCts.Token);
                }
                catch (Exception rollbackException)
                {
                    logger.LogWarning(
                        rollbackException,
                        "IN STOCK image delete rollback failed. Relation may need manual inspection.");
                }
            }

            // Relation and metadata remain; no physical delete was started.
            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    private async Task EnsureSlugAvailableAsync(string slug, Guid? exceptId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.InStockItems.AsNoTracking().AnyAsync(
            item => item.ArchivedAt == null &&
                item.Slug == slug &&
                (!exceptId.HasValue || item.Id != exceptId),
            cancellationToken);
        if (exists)
        {
            throw new InStockConflictException(
                "Slug",
                "A non-archived IN STOCK item with this slug already exists.");
        }
    }

    private static PublicInStockItemResponse ToPublicItem(InStockItem item) =>
        new(
            item.Id,
            item.Slug,
            item.Title,
            item.ShortDescription,
            item.Description,
            item.Price,
            item.Currency,
            item.Status,
            item.Sizes,
            item.Materials,
            item.Images
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.CreatedAt)
                .Select(ToPublicImage)
                .ToArray());

    private static AdminInStockItemResponse ToAdminItem(InStockItem item) =>
        new(
            item.Id,
            item.Slug,
            item.Title,
            item.ShortDescription,
            item.Description,
            item.Price,
            item.Currency,
            item.Status,
            item.IsPublished,
            item.DisplayOrder,
            item.Sizes,
            item.Materials,
            item.CreatedAt,
            item.UpdatedAt,
            item.ArchivedAt,
            item.Images
                .OrderBy(image => image.DisplayOrder)
                .ThenBy(image => image.CreatedAt)
                .Select(ToAdminImage)
                .ToArray());

    private static PublicInStockImageResponse ToPublicImage(InStockItemImage image) =>
        new(image.Id, $"{ImageRoute}{image.Id}", image.AltText, image.DisplayOrder);

    private static AdminInStockImageResponse ToAdminImage(InStockItemImage image) =>
        new(
            image.Id,
            image.UploadedFileId,
            $"{ImageRoute}{image.Id}",
            image.AltText,
            image.DisplayOrder,
            image.UploadedFile?.OriginalFileName ?? string.Empty,
            image.UploadedFile?.ContentType ?? "application/octet-stream",
            image.UploadedFile?.SizeBytes ?? 0,
            image.CreatedAt);

    private static string CreateSlug(string? supplied, string source)
    {
        if (!string.IsNullOrWhiteSpace(supplied))
        {
            return supplied.Trim();
        }

        var normalized = source.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        var slug = InvalidSlugCharacters().Replace(builder.ToString(), "-").Trim('-');
        slug = RepeatedHyphens().Replace(slug, "-");
        if (slug.Length == 0)
        {
            throw new InStockConflictException("Slug", "Enter a lowercase kebab-case slug.");
        }

        return slug;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidSlugCharacters();

    [GeneratedRegex("-{2,}", RegexOptions.CultureInvariant)]
    private static partial Regex RepeatedHyphens();
}
