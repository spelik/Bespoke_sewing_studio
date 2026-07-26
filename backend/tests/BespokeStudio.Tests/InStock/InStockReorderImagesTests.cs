using System.Data.Common;
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
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace BespokeStudio.Tests.InStock;

public sealed class InStockReorderImagesTests
{
    [Fact]
    public async Task ReorderImages_UpdatesDisplayOrderAtomically()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var item = await service.CreateItemAsync(CreateSaveRequest());
        var first = await service.AddImageAsync(item.Id, CreateUpload("a.jpg"), null, 0);
        var second = await service.AddImageAsync(item.Id, CreateUpload("b.jpg"), null, 1);
        var third = await service.AddImageAsync(item.Id, CreateUpload("c.jpg"), null, 2);
        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotNull(third);

        var reordered = await service.ReorderImagesAsync(
            item.Id,
            new ReorderInStockImagesRequest([third.Id, first.Id, second.Id]));

        Assert.NotNull(reordered);
        Assert.Equal([third.Id, first.Id, second.Id], reordered.Select(image => image.Id).ToArray());
        Assert.Equal([0, 1, 2], reordered.Select(image => image.DisplayOrder).ToArray());

        db.ChangeTracker.Clear();
        var persisted = await db.InStockItemImages
            .Where(image => image.InStockItemId == item.Id)
            .OrderBy(image => image.DisplayOrder)
            .Select(image => image.Id)
            .ToArrayAsync();
        Assert.Equal([third.Id, first.Id, second.Id], persisted);
    }

    [Fact]
    public async Task ReorderImages_RejectsUnknownImageId()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var item = await service.CreateItemAsync(CreateSaveRequest());
        var first = await service.AddImageAsync(item.Id, CreateUpload("a.jpg"), null, 0);
        Assert.NotNull(first);

        var exception = await Assert.ThrowsAsync<InStockConflictException>(() =>
            service.ReorderImagesAsync(
                item.Id,
                new ReorderInStockImagesRequest([first.Id, Guid.NewGuid()])));

        Assert.Equal("ImageIds", exception.Field);
        db.ChangeTracker.Clear();
        Assert.Equal(
            0,
            await db.InStockItemImages
                .Where(image => image.Id == first.Id)
                .Select(image => image.DisplayOrder)
                .SingleAsync());
    }

    [Fact]
    public async Task ReorderImages_RejectsImageFromAnotherItem()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var left = await service.CreateItemAsync(CreateSaveRequest("Left", "left"));
        var right = await service.CreateItemAsync(CreateSaveRequest("Right", "right"));
        var leftImage = await service.AddImageAsync(left.Id, CreateUpload("a.jpg"), null, 0);
        var rightImage = await service.AddImageAsync(right.Id, CreateUpload("b.jpg"), null, 0);
        Assert.NotNull(leftImage);
        Assert.NotNull(rightImage);

        await Assert.ThrowsAsync<InStockConflictException>(() =>
            service.ReorderImagesAsync(
                left.Id,
                new ReorderInStockImagesRequest([rightImage.Id])));
    }

    [Fact]
    public async Task ReorderImages_RejectsDuplicateIds()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var item = await service.CreateItemAsync(CreateSaveRequest());
        var first = await service.AddImageAsync(item.Id, CreateUpload("a.jpg"), null, 0);
        var second = await service.AddImageAsync(item.Id, CreateUpload("b.jpg"), null, 1);
        Assert.NotNull(first);
        Assert.NotNull(second);

        await Assert.ThrowsAsync<InStockConflictException>(() =>
            service.ReorderImagesAsync(
                item.Id,
                new ReorderInStockImagesRequest([first.Id, first.Id])));
    }

    [Fact]
    public async Task ReorderImages_RollsBackDisplayOrdersWhenSaveFails()
    {
        var interceptor = new FailSaveChangesInterceptor("reorder save failed");
        await using var db = CreateFailingDb(interceptor);
        var service = CreateService(db);
        var item = await service.CreateItemAsync(CreateSaveRequest());
        var first = await service.AddImageAsync(item.Id, CreateUpload("a.jpg"), null, 0);
        var second = await service.AddImageAsync(item.Id, CreateUpload("b.jpg"), null, 1);
        Assert.NotNull(first);
        Assert.NotNull(second);

        interceptor.Enabled = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReorderImagesAsync(
                item.Id,
                new ReorderInStockImagesRequest([second.Id, first.Id])));

        interceptor.Enabled = false;
        db.ChangeTracker.Clear();
        var orders = await db.InStockItemImages
            .Where(image => image.InStockItemId == item.Id)
            .Select(image => new { image.Id, image.DisplayOrder })
            .ToListAsync();
        Assert.Equal(0, orders.Single(image => image.Id == first.Id).DisplayOrder);
        Assert.Equal(1, orders.Single(image => image.Id == second.Id).DisplayOrder);
    }

    [Fact]
    public async Task ReorderImages_ReturnsNullForMissingItem()
    {
        await using var db = CreateDb();
        var service = CreateService(db);
        var result = await service.ReorderImagesAsync(
            Guid.NewGuid(),
            new ReorderInStockImagesRequest([Guid.NewGuid()]));
        Assert.Null(result);
    }

    [Fact]
    public async Task DisposeFailure_AfterSuccessfulCommit_DoesNotFailReorder()
    {
        await using var db = CreateDb();
        var seed = CreateService(db);
        var item = await seed.CreateItemAsync(CreateSaveRequest());
        var first = await seed.AddImageAsync(item.Id, CreateUpload("a.jpg"), null, 0);
        var second = await seed.AddImageAsync(item.Id, CreateUpload("b.jpg"), null, 1);
        Assert.NotNull(first);
        Assert.NotNull(second);

        var transaction = new ControllableTransaction
        {
            DisposeException = new InvalidOperationException("dispose failed after commit")
        };
        var service = CreateService(
            db,
            new ControllableTransactionFactory { Transaction = transaction });

        var reordered = await service.ReorderImagesAsync(
            item.Id,
            new ReorderInStockImagesRequest([second.Id, first.Id]));

        Assert.NotNull(reordered);
        Assert.Equal([second.Id, first.Id], reordered.Select(image => image.Id).ToArray());
        Assert.Equal(1, transaction.CommitCalls);
        Assert.Equal(0, transaction.RollbackCalls);
        Assert.Equal(1, transaction.DisposeCalls);

        db.ChangeTracker.Clear();
        var persisted = await db.InStockItemImages
            .Where(image => image.InStockItemId == item.Id)
            .OrderBy(image => image.DisplayOrder)
            .Select(image => image.Id)
            .ToArrayAsync();
        Assert.Equal([second.Id, first.Id], persisted);
    }

    [Fact]
    public async Task DisposeFailure_DoesNotReplaceOriginalSaveChangesError()
    {
        var interceptor = new FailSaveChangesInterceptor("reorder save failed");
        await using var db = CreateFailingDb(interceptor);
        var seed = CreateService(db);
        var item = await seed.CreateItemAsync(CreateSaveRequest());
        var first = await seed.AddImageAsync(item.Id, CreateUpload("a.jpg"), null, 0);
        var second = await seed.AddImageAsync(item.Id, CreateUpload("b.jpg"), null, 1);
        Assert.NotNull(first);
        Assert.NotNull(second);

        interceptor.Enabled = true;
        var transaction = new ControllableTransaction
        {
            DisposeException = new InvalidOperationException("dispose failed")
        };
        var service = CreateService(
            db,
            new ControllableTransactionFactory { Transaction = transaction });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReorderImagesAsync(
                item.Id,
                new ReorderInStockImagesRequest([second.Id, first.Id])));

        Assert.Equal("reorder save failed", error.Message);
        Assert.Equal(1, transaction.RollbackCalls);
        Assert.Equal(1, transaction.DisposeCalls);
    }

    [Fact]
    public async Task DisposeFailure_DoesNotReplaceOriginalCommitError()
    {
        await using var db = CreateDb();
        var seed = CreateService(db);
        var item = await seed.CreateItemAsync(CreateSaveRequest());
        var first = await seed.AddImageAsync(item.Id, CreateUpload("a.jpg"), null, 0);
        var second = await seed.AddImageAsync(item.Id, CreateUpload("b.jpg"), null, 1);
        Assert.NotNull(first);
        Assert.NotNull(second);

        var transaction = new ControllableTransaction
        {
            CommitException = new InvalidOperationException("commit failed"),
            DisposeException = new InvalidOperationException("dispose failed")
        };
        var service = CreateService(
            db,
            new ControllableTransactionFactory { Transaction = transaction });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReorderImagesAsync(
                item.Id,
                new ReorderInStockImagesRequest([second.Id, first.Id])));

        Assert.Equal("commit failed", error.Message);
        Assert.Equal(1, transaction.CommitCalls);
        Assert.Equal(1, transaction.RollbackCalls);
        Assert.Equal(1, transaction.DisposeCalls);
    }

    [Fact]
    public async Task DisposeFailure_WhenItemMissing_DoesNotReplaceNullResult()
    {
        await using var db = CreateDb();
        var transaction = new ControllableTransaction
        {
            DisposeException = new InvalidOperationException("dispose failed on missing item")
        };
        var service = CreateService(
            db,
            new ControllableTransactionFactory { Transaction = transaction });

        var result = await service.ReorderImagesAsync(
            Guid.NewGuid(),
            new ReorderInStockImagesRequest([Guid.NewGuid()]));

        Assert.Null(result);
        Assert.Equal(0, transaction.CommitCalls);
        Assert.Equal(0, transaction.RollbackCalls);
        Assert.Equal(1, transaction.DisposeCalls);
    }

    [Fact]
    public async Task RollbackAndDisposeFailures_DoNotReplaceOriginalSaveChangesError()
    {
        var interceptor = new FailSaveChangesInterceptor("reorder save failed");
        await using var db = CreateFailingDb(interceptor);
        var seed = CreateService(db);
        var item = await seed.CreateItemAsync(CreateSaveRequest());
        var first = await seed.AddImageAsync(item.Id, CreateUpload("a.jpg"), null, 0);
        var second = await seed.AddImageAsync(item.Id, CreateUpload("b.jpg"), null, 1);
        Assert.NotNull(first);
        Assert.NotNull(second);

        interceptor.Enabled = true;
        var transaction = new ControllableTransaction
        {
            RollbackException = new InvalidOperationException("rollback failed"),
            DisposeException = new InvalidOperationException("dispose failed")
        };
        var service = CreateService(
            db,
            new ControllableTransactionFactory { Transaction = transaction });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ReorderImagesAsync(
                item.Id,
                new ReorderInStockImagesRequest([second.Id, first.Id])));

        Assert.Equal("reorder save failed", error.Message);
        Assert.Equal(1, transaction.RollbackCalls);
        Assert.Equal(1, transaction.DisposeCalls);
        Assert.Equal(0, transaction.CommitCalls);
    }

    private static InStockService CreateService(BespokeStudioDbContext db) =>
        CreateService(db, new BespokeStudioDbContextTransactionFactory(db));

    private static InStockService CreateService(
        BespokeStudioDbContext db,
        IDbContextTransactionFactory transactions) =>
        new(
            db,
            new FakeUploadService(),
            new NoopDeletionScheduler(),
            transactions,
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

    private static BespokeStudioDbContext CreateFailingDb(FailSaveChangesInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<BespokeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .AddInterceptors(interceptor)
            .Options;
        var db = new BespokeStudioDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static SaveInStockItemRequest CreateSaveRequest(
        string title = "Coat",
        string slug = "coat") =>
        new(slug, title, null, null, 100m, "GBP", InStockItemStatus.Available, false, 0, null, null);

    private static UploadFileRequest CreateUpload(string fileName) =>
        new(fileName, "image/jpeg", 4, new MemoryStream([0xFF, 0xD8, 0xFF, 0xD9]));

    private sealed class FailSaveChangesInterceptor(string message) : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
            if (Enabled)
            {
                throw new InvalidOperationException(message);
            }

            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled)
            {
                throw new InvalidOperationException(message);
            }

            return ValueTask.FromResult(result);
        }
    }

    private sealed class ControllableTransactionFactory : IDbContextTransactionFactory
    {
        public bool SupportsTransactions => true;
        public ControllableTransaction? Transaction { get; set; }
        public Exception? BeginException { get; set; }

        public Task<IDbContextTransaction?> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            if (BeginException is not null)
            {
                return Task.FromException<IDbContextTransaction?>(BeginException);
            }

            return Task.FromResult<IDbContextTransaction?>(Transaction ?? new ControllableTransaction());
        }
    }

    private sealed class ControllableTransaction : IDbContextTransaction
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public Exception? CommitException { get; set; }
        public Exception? RollbackException { get; set; }
        public Exception? DisposeException { get; set; }
        public int CommitCalls { get; private set; }
        public int RollbackCalls { get; private set; }
        public int DisposeCalls { get; private set; }

        public void Commit()
        {
            CommitCalls++;
            if (CommitException is not null)
            {
                throw CommitException;
            }
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            CommitCalls++;
            return CommitException is null
                ? Task.CompletedTask
                : Task.FromException(CommitException);
        }

        public void Rollback()
        {
            RollbackCalls++;
            if (RollbackException is not null)
            {
                throw RollbackException;
            }
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackCalls++;
            return RollbackException is null
                ? Task.CompletedTask
                : Task.FromException(RollbackException);
        }

        public DbTransaction GetDbTransaction() =>
            throw new NotSupportedException();

        public void Dispose()
        {
            DisposeCalls++;
            if (DisposeException is not null)
            {
                throw DisposeException;
            }
        }

        public ValueTask DisposeAsync()
        {
            DisposeCalls++;
            return DisposeException is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(DisposeException);
        }
    }

    private sealed class FakeUploadService : IUploadService
    {
        public Task<IReadOnlyList<UploadedFileResponse>> UploadOrderAttachmentsAsync(
            IReadOnlyCollection<UploadFileRequest> files,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadedFileResponse> UploadPortfolioImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

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

    private sealed class NoopDeletionScheduler : IUploadFileDeletionScheduler
    {
        public Task ScheduleAsync(
            ScheduleUploadFileDeletionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
