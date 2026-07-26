using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.InStock;
using BespokeStudio.Application.Contracts.Orders;
using BespokeStudio.Application.Contracts.Storage;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BespokeStudio.Tests.InStock;

public sealed class InStockServiceTests
{
    [Fact]
    public async Task PublicList_ReturnsOnlyPublishedNonArchived_OrderedByDisplayOrderThenCreatedAt()
    {
        await using var db = CreateDb();
        var older = await SeedItemAsync(db, "alpha", displayOrder: 1, published: true, createdOffsetMinutes: -30);
        var first = await SeedItemAsync(db, "beta", displayOrder: 0, published: true, createdOffsetMinutes: -10);
        await SeedItemAsync(db, "hidden", displayOrder: 0, published: false);
        await SeedItemAsync(db, "archived", displayOrder: 0, published: true, archived: true);
        var service = CreateService(db);

        var items = await service.GetPublicItemsAsync();

        Assert.Equal([first.Id, older.Id], items.Select(item => item.Id).ToArray());
        Assert.All(items, item => Assert.StartsWith("/api/in-stock/images/", item.Images.First().ImageUrl));
    }

    [Theory]
    [InlineData(InStockItemStatus.Available)]
    [InlineData(InStockItemStatus.Reserved)]
    [InlineData(InStockItemStatus.Sold)]
    public async Task PublicDetail_ReturnsPublishedStatusesIncludingReservedAndSold(InStockItemStatus status)
    {
        await using var db = CreateDb();
        await SeedItemAsync(db, "silk-blouse", published: true, status: status);
        var service = CreateService(db);

        var item = await service.GetPublicItemBySlugAsync("silk-blouse");

        Assert.NotNull(item);
        Assert.Equal(status, item.Status);
        Assert.Equal("GBP", item.Currency);
    }

    [Fact]
    public async Task PublicDetail_ReturnsNullForMissingHiddenOrArchived()
    {
        await using var db = CreateDb();
        await SeedItemAsync(db, "draft", published: false);
        await SeedItemAsync(db, "gone", published: true, archived: true);
        var service = CreateService(db);

        Assert.Null(await service.GetPublicItemBySlugAsync("missing"));
        Assert.Null(await service.GetPublicItemBySlugAsync("draft"));
        Assert.Null(await service.GetPublicItemBySlugAsync("gone"));
    }

    [Fact]
    public async Task AdminCrud_CreateUpdateArchiveRestore_WorksWithoutImages()
    {
        await using var db = CreateDb();
        var service = CreateService(db);

        var created = await service.CreateItemAsync(CreateSaveRequest("Coat", "wool-coat", 250m, published: false));
        Assert.Empty(created.Images);
        Assert.False(created.IsPublished);

        var updated = await service.UpdateItemAsync(
            created.Id,
            CreateSaveRequest("Winter coat", "wool-coat", 275m, published: true, status: InStockItemStatus.Reserved));
        Assert.NotNull(updated);
        Assert.Equal("Winter coat", updated.Title);
        Assert.Equal(InStockItemStatus.Reserved, updated.Status);

        var archived = await service.ArchiveItemAsync(created.Id);
        Assert.NotNull(archived);
        Assert.True(archived.Archived);
        Assert.Null(await service.GetPublicItemBySlugAsync("wool-coat"));

        var adminAfterArchive = await service.GetAdminItemByIdAsync(created.Id);
        Assert.NotNull(adminAfterArchive?.ArchivedAt);

        var restored = await service.RestoreItemAsync(created.Id);
        Assert.NotNull(restored);
        Assert.True(restored.Restored);
        var adminAfterRestore = await service.GetAdminItemByIdAsync(created.Id);
        Assert.NotNull(adminAfterRestore);
        Assert.Null(adminAfterRestore.ArchivedAt);
        Assert.False(adminAfterRestore.IsPublished);
    }

    [Fact]
    public async Task Create_RejectsDuplicateSlugAmongNonArchived()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        await service.CreateItemAsync(CreateSaveRequest("One", "shared-slug", 10m));

        await Assert.ThrowsAsync<InStockConflictException>(() =>
            service.CreateItemAsync(CreateSaveRequest("Two", "shared-slug", 20m)));
    }

    [Fact]
    public async Task DeleteImage_SchedulerFailure_LeavesRelationIntact()
    {
        await using var db = CreateDb();
        var service = CreateService(db, new FakeUploadService(db), new ThrowingDeletionScheduler());
        var item = await service.CreateItemAsync(CreateSaveRequest("Coat", "coat", 40m));
        var image = await CreateService(db, new FakeUploadService(db), new RecordingDeletionScheduler())
            .AddImageAsync(item.Id, CreateUpload("coat.jpg"), "Front", 0, CancellationToken.None);
        Assert.NotNull(image);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteImageAsync(item.Id, image.Id));

        Assert.Equal(1, await db.InStockItemImages.CountAsync());
        Assert.Equal(1, await db.UploadedFiles.CountAsync());
        Assert.Empty(await db.UploadFileDeletionJobs.ToListAsync());
    }

    [Fact]
    public async Task Images_CanUploadReorderUpdateAltAndDeleteWithDeletionOutbox()
    {
        await using var db = CreateDb();
        var upload = new FakeUploadService(db);
        var deletion = new RecordingDeletionScheduler();
        var service = CreateService(db, upload, deletion);
        var item = await service.CreateItemAsync(CreateSaveRequest("Dress", "dress", 90m));

        var first = await service.AddImageAsync(
            item.Id,
            CreateUpload("a.jpg"),
            "Front",
            5,
            CancellationToken.None);
        var second = await service.AddImageAsync(
            item.Id,
            CreateUpload("b.jpg"),
            "Back",
            1,
            CancellationToken.None);
        Assert.NotNull(first);
        Assert.NotNull(second);

        var updated = await service.UpdateImageAsync(
            item.Id,
            first.Id,
            new UpdateInStockImageRequest("Front detail", 0));
        Assert.NotNull(updated);
        Assert.Equal("Front detail", updated.AltText);
        Assert.Equal(0, updated.DisplayOrder);

        var publicItem = await service.GetPublicItemBySlugAsync("dress");
        Assert.Null(publicItem);

        await service.UpdateItemAsync(item.Id, CreateSaveRequest("Dress", "dress", 90m, published: true));
        publicItem = await service.GetPublicItemBySlugAsync("dress");
        Assert.NotNull(publicItem);
        Assert.Equal([0, 1], publicItem.Images.Select(image => image.DisplayOrder).ToArray());
        Assert.Equal($"/api/in-stock/images/{updated.Id}", publicItem.Images[0].ImageUrl);

        var deleted = await service.DeleteImageAsync(item.Id, second.Id);
        Assert.True(deleted);
        Assert.Single(deletion.Requests);
        Assert.Equal("in_stock_image.deleted", deletion.Requests[0].Reason);
        Assert.Equal(1, await db.InStockItemImages.CountAsync());
    }

    [Fact]
    public async Task AdminList_IncludesArchivedAndAllStatuses()
    {
        await using var db = CreateDb();
        await SeedItemAsync(db, "a", status: InStockItemStatus.Available);
        await SeedItemAsync(db, "b", status: InStockItemStatus.Reserved, archived: true);
        await SeedItemAsync(db, "c", status: InStockItemStatus.Sold, published: false);
        var service = CreateService(db);

        var items = await service.GetAdminItemsAsync();

        Assert.Equal(3, items.Count);
        Assert.Contains(items, item => item.ArchivedAt is not null);
        Assert.Contains(items, item => item.Status == InStockItemStatus.Sold && !item.IsPublished);
    }

    private static InStockService CreateService(BespokeStudioDbContext db) =>
        CreateService(db, new FakeUploadService(db), new RecordingDeletionScheduler());

    private static InStockService CreateService(
        BespokeStudioDbContext db,
        IUploadService upload,
        IUploadFileDeletionScheduler deletion) =>
        new(
            db,
            upload,
            deletion,
            new BespokeStudioDbContextTransactionFactory(db),
            NullLogger<InStockService>.Instance);

    private static BespokeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BespokeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new BespokeStudioDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static async Task<InStockItem> SeedItemAsync(
        BespokeStudioDbContext db,
        string slug,
        int displayOrder = 0,
        bool published = true,
        bool archived = false,
        InStockItemStatus status = InStockItemStatus.Available,
        int createdOffsetMinutes = 0)
    {
        var now = DateTimeOffset.UtcNow.AddMinutes(createdOffsetMinutes);
        var item = new InStockItem
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = slug,
            Price = 100m,
            Currency = InStockItem.DefaultCurrency,
            Status = status,
            IsPublished = published,
            DisplayOrder = displayOrder,
            CreatedAt = now,
            UpdatedAt = now,
            ArchivedAt = archived ? now : null
        };

        var file = new UploadedFileMetadata
        {
            Id = Guid.NewGuid(),
            Purpose = UploadPurpose.InStockImage,
            OriginalFileName = $"{slug}.jpg",
            StoredFileName = $"{slug}.jpg",
            StorageKey = $"in-stock-images/2026/07/{slug}.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 128,
            ScanStatus = UploadScanStatus.Clean,
            CreatedAt = now,
            UpdatedAt = now
        };
        var image = new InStockItemImage
        {
            Id = Guid.NewGuid(),
            InStockItemId = item.Id,
            UploadedFileId = file.Id,
            UploadedFile = file,
            AltText = slug,
            DisplayOrder = 0,
            CreatedAt = now
        };
        item.Images.Add(image);
        db.UploadedFiles.Add(file);
        db.InStockItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static SaveInStockItemRequest CreateSaveRequest(
        string title,
        string slug,
        decimal price,
        bool published = false,
        InStockItemStatus status = InStockItemStatus.Available) =>
        new(slug, title, null, null, price, "GBP", status, published, 0, null, null);

    private static UploadFileRequest CreateUpload(string fileName) =>
        new(fileName, "image/jpeg", 4, new MemoryStream([0xFF, 0xD8, 0xFF, 0xD9]));

    private sealed class FakeUploadService : IUploadService
    {
        public FakeUploadService(BespokeStudioDbContext _)
        {
        }

        public Task<IReadOnlyList<UploadedFileResponse>> UploadOrderAttachmentsAsync(
            IReadOnlyCollection<UploadFileRequest> files,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadedFileResponse> UploadPortfolioImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public List<string> CompensatedKeys { get; } = [];

        public Task<PreparedUploadFile> PrepareInStockImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var metadata = new UploadedFileMetadata
            {
                Id = Guid.NewGuid(),
                Purpose = UploadPurpose.InStockImage,
                OriginalFileName = file.OriginalFileName,
                StoredFileName = file.OriginalFileName,
                StorageKey = $"in-stock-images/2026/07/{Guid.NewGuid():N}.jpg",
                ContentType = file.ContentType,
                SizeBytes = file.SizeBytes,
                ScanStatus = UploadScanStatus.Clean,
                ScanProvider = "Disabled",
                ScannedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            return Task.FromResult(new PreparedUploadFile(metadata, metadata.StorageKey));
        }

        public Task CompensateOrphanedPromotedFileAsync(
            string storageKey,
            string? originalFileName,
            long? fileSizeBytes,
            CancellationToken cancellationToken = default)
        {
            CompensatedKeys.Add(storageKey);
            return Task.CompletedTask;
        }

        public Task<UploadDownloadResponse?> OpenPublicInStockImageAsync(
            Guid imageId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UploadDownloadResponse?>(null);

        public Task<UploadDownloadResponse?> OpenInStockImageForAdminAsync(
            Guid imageId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UploadDownloadResponse?>(null);

        public Task<UploadedFileResponse> UploadContentImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenPublicContentImageAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenContentImageForAdminAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadedFileResponse> UploadBrandImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenPublicBrandImageAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenBrandImageForAdminAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenPublicPortfolioImageAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenPortfolioImageForAdminAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadDownloadResponse?> OpenOrderAttachmentAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DeleteOrderAttachmentResult?> DeleteOrderAttachmentAsync(
            Guid orderId,
            Guid attachmentId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadMetadataResponse?> GetMetadataAsync(
            Guid uploadedFileId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UploadedFileResponse>> GetAllAsync(
            UploadPurpose? purpose = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingDeletionScheduler : IUploadFileDeletionScheduler
    {
        public List<ScheduleUploadFileDeletionRequest> Requests { get; } = [];

        public Task ScheduleAsync(
            ScheduleUploadFileDeletionRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingDeletionScheduler : IUploadFileDeletionScheduler
    {
        public Task ScheduleAsync(
            ScheduleUploadFileDeletionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Deletion scheduler failed.");
    }
}
