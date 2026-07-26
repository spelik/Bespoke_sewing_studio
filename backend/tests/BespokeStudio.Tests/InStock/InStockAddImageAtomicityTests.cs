using System.Data.Common;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Common;
using BespokeStudio.Application.Contracts.Orders;
using BespokeStudio.Application.Contracts.Storage;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Services;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Tests.InStock;

/// <summary>
/// Atomicity failure-path coverage for <see cref="InStockService.AddImageAsync"/> using
/// test doubles at specific failure points (not EF InMemory transactions).
/// </summary>
public sealed class InStockAddImageAtomicityTests
{
    [Fact]
    public async Task BeginTransactionFailure_AfterPromotion_InvokesCompensation()
    {
        await using var db = CreateDb();
        var item = await SeedItemAsync(db);
        var upload = new RecordingUploadService();
        var transactions = new ControllableTransactionFactory
        {
            BeginException = new InvalidOperationException("begin failed")
        };
        var service = CreateService(db, upload, transactions);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddImageAsync(item.Id, CreateUpload(), "Front", 0));

        Assert.Equal("begin failed", error.Message);
        Assert.Equal([upload.LastStorageKey], upload.CompensatedKeys);
    }

    [Fact]
    public async Task RequestCancellation_DoesNotCancelCleanupToken()
    {
        await using var db = CreateDb();
        var item = await SeedItemAsync(db);
        using var requestCts = new CancellationTokenSource();
        var upload = new RecordingUploadService
        {
            AfterPrepare = requestCts.Cancel
        };
        var transactions = new ControllableTransactionFactory
        {
            BeginException = new InvalidOperationException("begin failed after cancel")
        };
        var service = CreateService(db, upload, transactions);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddImageAsync(item.Id, CreateUpload(), "Front", 0, requestCts.Token));

        Assert.True(requestCts.IsCancellationRequested);
        var cleanupToken = Assert.Single(upload.CompensationTokens);
        Assert.False(cleanupToken.IsCancellationRequested);
        Assert.Equal([upload.LastStorageKey], upload.CompensatedKeys);
    }

    [Fact]
    public async Task RollbackFailure_DoesNotBlockCompensation()
    {
        var interceptor = new FailSaveChangesInterceptor("save failed");
        await using var db = CreateFailingDb(interceptor);
        var item = await SeedItemAsync(db);
        interceptor.Enabled = true;

        var upload = new RecordingUploadService();
        var transaction = new ControllableTransaction
        {
            RollbackException = new InvalidOperationException("rollback failed")
        };
        var service = CreateService(
            db,
            upload,
            new ControllableTransactionFactory { Transaction = transaction });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddImageAsync(item.Id, CreateUpload(), "Front", 0));

        Assert.Equal("save failed", error.Message);
        Assert.Equal(1, transaction.RollbackCalls);
        Assert.Equal([upload.LastStorageKey], upload.CompensatedKeys);
    }

    [Fact]
    public async Task OriginalException_IsPreserved_WhenRollbackAndCompensationFail()
    {
        var interceptor = new FailSaveChangesInterceptor("original link failure");
        await using var db = CreateFailingDb(interceptor);
        var item = await SeedItemAsync(db);
        interceptor.Enabled = true;

        var upload = new RecordingUploadService
        {
            CompensationException = new InvalidOperationException("compensation failed")
        };
        var transaction = new ControllableTransaction
        {
            RollbackException = new InvalidOperationException("rollback failed")
        };
        var service = CreateService(
            db,
            upload,
            new ControllableTransactionFactory { Transaction = transaction });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddImageAsync(item.Id, CreateUpload(), "Front", 0));

        Assert.Equal("original link failure", error.Message);
        Assert.Equal(1, transaction.RollbackCalls);
        Assert.Equal(1, upload.CompensationAttempts);
    }

    [Fact]
    public async Task PreCommitFailure_CompensatesPromotedFile()
    {
        var interceptor = new FailSaveChangesInterceptor("pre-commit failure");
        await using var db = CreateFailingDb(interceptor);
        var item = await SeedItemAsync(db);
        interceptor.Enabled = true;

        var upload = new RecordingUploadService();
        var transaction = new ControllableTransaction();
        var service = CreateService(
            db,
            upload,
            new ControllableTransactionFactory { Transaction = transaction });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddImageAsync(item.Id, CreateUpload(), "Front", 0));

        Assert.Equal(0, transaction.CommitCalls);
        Assert.Equal([upload.LastStorageKey], upload.CompensatedKeys);
    }

    [Fact]
    public async Task AmbiguousCommitFailure_DoesNotCompensateImmediately()
    {
        await using var db = CreateDb();
        var item = await SeedItemAsync(db);
        var upload = new RecordingUploadService();
        var transaction = new ControllableTransaction
        {
            CommitException = new InvalidOperationException("ambiguous commit")
        };
        var transactions = new ControllableTransactionFactory { Transaction = transaction };
        var service = CreateService(db, upload, transactions);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddImageAsync(item.Id, CreateUpload(), "Front", 0));

        Assert.Equal("ambiguous commit", error.Message);
        Assert.Equal(1, transaction.CommitCalls);
        Assert.Empty(upload.CompensatedKeys);
        Assert.Contains(upload.LastStorageKey, upload.PromotedKeys);
    }

    [Fact]
    public async Task StorageMaintenance_CanDiscoverUnconfirmedPromotedOrphan()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "bespoke-in-stock-orphan-scan",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var options = Options.Create(new UploadStorageOptions
            {
                RootPath = root,
                MaxFileSizeBytes = 1024 * 1024
            });
            var storage = new LocalUploadStorage(
                options,
                new FakeHostEnvironment(root),
                NullLogger<LocalUploadStorage>.Instance);

            const string storageKey = "in-stock-images/2026/07/unconfirmed-orphan.jpg";
            await storage.WriteNewFileAsync(
                storageKey,
                new MemoryStream([0xFF, 0xD8, 0xFF, 0xD9]),
                1024,
                CancellationToken.None);

            await using var db = CreateDb();
            var maintenance = new StorageMaintenanceService(
                db,
                new NoOpAuditLogService(),
                storage,
                NullLogger<StorageMaintenanceService>.Instance);

            var scan = await maintenance.ScanAsync();

            Assert.Equal(1, scan.OrphanPhysicalFileCount);
            Assert.Equal(storageKey, Assert.Single(scan.OrphanPhysicalFiles).RelativePath);
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    [Fact]
    public async Task SuccessfulAddImage_PersistsUploadedFileAndInStockItemImageTogether()
    {
        await using var db = CreateDb();
        var item = await SeedItemAsync(db);
        var upload = new RecordingUploadService();
        var transaction = new ControllableTransaction();
        var transactions = new ControllableTransactionFactory { Transaction = transaction };
        var service = CreateService(db, upload, transactions);

        var image = await service.AddImageAsync(item.Id, CreateUpload("ok.jpg"), "Front", 0);

        Assert.NotNull(image);
        Assert.Equal(1, transaction.CommitCalls);
        Assert.Empty(upload.CompensatedKeys);
        Assert.Equal(1, await db.UploadedFiles.CountAsync());
        Assert.Equal(1, await db.InStockItemImages.CountAsync());
        Assert.Equal(
            (await db.UploadedFiles.SingleAsync()).Id,
            (await db.InStockItemImages.SingleAsync()).UploadedFileId);
    }

    [Fact]
    public async Task DisposeFailure_DoesNotReplaceOriginalLinkError()
    {
        var interceptor = new FailSaveChangesInterceptor("original link failure");
        await using var db = CreateFailingDb(interceptor);
        var item = await SeedItemAsync(db);
        interceptor.Enabled = true;

        var upload = new RecordingUploadService();
        var transaction = new ControllableTransaction
        {
            DisposeException = new InvalidOperationException("dispose failed")
        };
        var service = CreateService(
            db,
            upload,
            new ControllableTransactionFactory { Transaction = transaction });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddImageAsync(item.Id, CreateUpload(), "Front", 0));

        Assert.Equal("original link failure", error.Message);
        Assert.Equal(1, transaction.DisposeCalls);
        Assert.Equal([upload.LastStorageKey], upload.CompensatedKeys);
    }

    [Fact]
    public async Task DisposeFailure_AfterSuccessfulCommit_DoesNotFailOperation()
    {
        await using var db = CreateDb();
        var item = await SeedItemAsync(db);
        var upload = new RecordingUploadService();
        var transaction = new ControllableTransaction
        {
            DisposeException = new InvalidOperationException("dispose failed after commit")
        };
        var service = CreateService(
            db,
            upload,
            new ControllableTransactionFactory { Transaction = transaction });

        var image = await service.AddImageAsync(item.Id, CreateUpload("ok.jpg"), "Front", 0);

        Assert.NotNull(image);
        Assert.Equal(1, transaction.CommitCalls);
        Assert.Equal(1, transaction.DisposeCalls);
        Assert.Equal(1, await db.UploadedFiles.CountAsync());
        Assert.Equal(1, await db.InStockItemImages.CountAsync());
    }

    [Fact]
    public async Task DisposeFailure_AfterSuccessfulCommit_DoesNotCompensatePromotedFile()
    {
        await using var db = CreateDb();
        var item = await SeedItemAsync(db);
        var upload = new RecordingUploadService();
        var transaction = new ControllableTransaction
        {
            DisposeException = new InvalidOperationException("dispose failed after commit")
        };
        var service = CreateService(
            db,
            upload,
            new ControllableTransactionFactory { Transaction = transaction });

        var image = await service.AddImageAsync(item.Id, CreateUpload("keep.jpg"), "Front", 0);

        Assert.NotNull(image);
        Assert.Empty(upload.CompensatedKeys);
        Assert.Contains(upload.LastStorageKey, upload.PromotedKeys);
    }

    private static InStockService CreateService(
        BespokeStudioDbContext db,
        RecordingUploadService upload,
        IDbContextTransactionFactory transactions) =>
        new(
            db,
            upload,
            new NoOpDeletionScheduler(),
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

    private static async Task<InStockItem> SeedItemAsync(BespokeStudioDbContext db)
    {
        var item = new InStockItem
        {
            Slug = $"item-{Guid.NewGuid():N}",
            Title = "Piece",
            Price = 10m,
            Currency = InStockItem.DefaultCurrency,
            Status = InStockItemStatus.Available,
            IsPublished = false
        };
        db.InStockItems.Add(item);
        await db.SaveChangesAsync();
        return item;
    }

    private static UploadFileRequest CreateUpload(string fileName = "piece.jpg") =>
        new(fileName, "image/jpeg", 4, new MemoryStream([0xFF, 0xD8, 0xFF, 0xD9]));

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

    private sealed class RecordingUploadService : IUploadService
    {
        public List<string> PromotedKeys { get; } = [];
        public List<string> CompensatedKeys { get; } = [];
        public List<CancellationToken> CompensationTokens { get; } = [];
        public int CompensationAttempts { get; private set; }
        public string LastStorageKey { get; private set; } = string.Empty;
        public Action? AfterPrepare { get; set; }
        public Exception? CompensationException { get; set; }

        public Task<PreparedUploadFile> PrepareInStockImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;
            var storageKey = $"in-stock-images/2026/07/{Guid.NewGuid():N}.jpg";
            LastStorageKey = storageKey;
            PromotedKeys.Add(storageKey);
            var metadata = new UploadedFileMetadata
            {
                Id = Guid.NewGuid(),
                Purpose = UploadPurpose.InStockImage,
                OriginalFileName = file.OriginalFileName,
                StoredFileName = file.OriginalFileName,
                StorageKey = storageKey,
                ContentType = file.ContentType,
                SizeBytes = file.SizeBytes,
                ScanStatus = UploadScanStatus.Clean,
                ScanProvider = "Disabled",
                ScannedAt = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            AfterPrepare?.Invoke();
            return Task.FromResult(new PreparedUploadFile(metadata, storageKey));
        }

        public Task CompensateOrphanedPromotedFileAsync(
            string storageKey,
            string? originalFileName,
            long? fileSizeBytes,
            CancellationToken cancellationToken = default)
        {
            CompensationAttempts++;
            CompensationTokens.Add(cancellationToken);
            if (CompensationException is not null)
            {
                throw CompensationException;
            }

            CompensatedKeys.Add(storageKey);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<UploadedFileResponse>> UploadOrderAttachmentsAsync(
            IReadOnlyCollection<UploadFileRequest> files,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UploadedFileResponse> UploadPortfolioImageAsync(
            UploadFileRequest file,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

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

    private sealed class NoOpDeletionScheduler : IUploadFileDeletionScheduler
    {
        public Task ScheduleAsync(
            ScheduleUploadFileDeletionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class NoOpAuditLogService : IAdminAuditLogService
    {
        public Task<PagedResponse<AdminAuditLogEntryResponse>> GetAsync(
            AdminAuditLogQueryRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RecordAsync(
            AdminAuditLogWriteRequest request,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void AddPending(AdminAuditLogWriteRequest request)
        {
        }
    }

    private sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "BespokeStudio.Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = contentRootPath;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
