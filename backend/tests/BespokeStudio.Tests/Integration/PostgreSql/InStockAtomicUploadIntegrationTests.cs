using System.Data.Common;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Common;
using BespokeStudio.Application.Contracts.InStock;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Security;
using BespokeStudio.Infrastructure.Services;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Tests.Integration.PostgreSql;

[Collection(PostgreSqlIntegrationTestCollection.Name)]
public sealed class InStockAtomicUploadIntegrationTests
{
    [PostgreSqlIntegrationFact]
    public async Task AddImage_PersistsUploadedFileAndImageLinkTogether()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();

        using var harness = CreateHarness(context);
        var item = await harness.Service.CreateItemAsync(CreateSaveRequest("Linked", "linked-piece", 55m));

        var image = await harness.Service.AddImageAsync(
            item.Id,
            CreateJpegUpload("linked.jpg"),
            "Front",
            0);

        Assert.NotNull(image);
        Assert.Equal(1, await context.UploadedFiles.CountAsync());
        Assert.Equal(1, await context.InStockItemImages.CountAsync());
        Assert.True(harness.Storage.Exists(
            (await context.UploadedFiles.SingleAsync()).StorageKey));
    }

    [PostgreSqlIntegrationFact]
    public async Task AddImage_WhenDbSaveFails_CompensatesPromotedFile_AndLeavesNoPublicImage()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var setup = database.CreateContext();
        await setup.Database.MigrateAsync();

        var item = new InStockItem
        {
            Slug = "fail-link",
            Title = "Fail link",
            Price = 10m,
            Currency = InStockItem.DefaultCurrency,
            Status = InStockItemStatus.Available,
            IsPublished = true
        };
        setup.InStockItems.Add(item);
        await setup.SaveChangesAsync();
        var itemId = item.Id;

        var failingOptions = new DbContextOptionsBuilder<BespokeStudioDbContext>()
            .UseNpgsql(
                database.AppConnectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(BespokeStudioDbContext).Assembly.FullName))
            .AddInterceptors(new FailSaveChangesInterceptor())
            .Options;

        await using var failingContext = new BespokeStudioDbContext(failingOptions);
        using var harness = CreateHarness(failingContext);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Service.AddImageAsync(itemId, CreateJpegUpload("fail.jpg"), "Front", 0));

        await using var verify = database.CreateContext();
        Assert.Equal(0, await verify.UploadedFiles.CountAsync());
        Assert.Equal(0, await verify.InStockItemImages.CountAsync());
        Assert.Empty(harness.Storage.EnumerateFiles());
    }

    [PostgreSqlIntegrationFact]
    public async Task DeleteImage_CreatesDeletionJobWithoutImmediatePhysicalDelete_AndRollbackKeepsRelation()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();

        using var harness = CreateHarness(context);
        var item = await harness.Service.CreateItemAsync(CreateSaveRequest("Delete me", "delete-me", 20m, published: true));
        var image = await harness.Service.AddImageAsync(item.Id, CreateJpegUpload("keep.jpg"), "Front", 0);
        Assert.NotNull(image);

        var storageKey = (await context.UploadedFiles.SingleAsync()).StorageKey;
        Assert.True(harness.Storage.Exists(storageKey));

        var deleted = await harness.Service.DeleteImageAsync(item.Id, image.Id);
        Assert.True(deleted);

        Assert.Equal(0, await context.InStockItemImages.CountAsync());
        Assert.Equal(0, await context.UploadedFiles.CountAsync());
        var job = await context.UploadFileDeletionJobs.SingleAsync();
        Assert.Equal(UploadFileDeletionJobStatus.Pending, job.Status);
        Assert.Equal("in_stock_image.deleted", job.Reason);
        // Physical file remains until background worker runs.
        Assert.True(harness.Storage.Exists(storageKey));
        Assert.Null(await harness.UploadService.OpenPublicInStockImageAsync(image.Id));
    }

    [PostgreSqlIntegrationFact]
    public async Task DeleteImage_WhenSchedulerFails_RelationRemains()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();

        using var harness = CreateHarness(context);
        var item = await harness.Service.CreateItemAsync(CreateSaveRequest("Keep", "keep-rel", 12m));
        var image = await harness.Service.AddImageAsync(item.Id, CreateJpegUpload("rel.jpg"), null, 0);
        Assert.NotNull(image);

        var throwingService = new InStockService(
            context,
            harness.UploadService,
            new ThrowingDeletionScheduler(),
            new BespokeStudioDbContextTransactionFactory(context),
            NullLogger<InStockService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            throwingService.DeleteImageAsync(item.Id, image.Id));

        Assert.Equal(1, await context.InStockItemImages.CountAsync());
        Assert.Equal(1, await context.UploadedFiles.CountAsync());
        Assert.Equal(0, await context.UploadFileDeletionJobs.CountAsync());
    }

    private static UploadHarness CreateHarness(BespokeStudioDbContext context)
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "bespoke-in-stock-pg-atomic",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        var options = Options.Create(new UploadStorageOptions
        {
            RootPath = root,
            MaxFileSizeBytes = 1024 * 1024
        });
        var storage = new LocalUploadStorage(
            options,
            new FakeHostEnvironment(root),
            NullLogger<LocalUploadStorage>.Instance);
        var uploadService = new LocalUploadService(
            context,
            options,
            new ConfiguredMalwareScanner(
                Options.Create(new UploadSecurityOptions
                {
                    MalwareScanner = new MalwareScannerOptions { Provider = "Disabled" }
                }),
                NullLogger<ConfiguredMalwareScanner>.Instance),
            new UploadFileDeletionScheduler(
                context,
                Options.Create(new UploadDeletionOptions()),
                storage),
            storage,
            NullLogger<LocalUploadService>.Instance);
        var service = new InStockService(
            context,
            uploadService,
            new UploadFileDeletionScheduler(
                context,
                Options.Create(new UploadDeletionOptions()),
                storage),
            new BespokeStudioDbContextTransactionFactory(context),
            NullLogger<InStockService>.Instance);

        return new UploadHarness(root, storage, uploadService, service);
    }

    [PostgreSqlIntegrationFact]
    public async Task AddImage_WhenCommitThrows_DoesNotDeletePromotedFile_AndStorageScanSeesOrphan()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();

        using var harness = CreateHarness(context);
        var item = await harness.Service.CreateItemAsync(CreateSaveRequest("Ambiguous", "ambiguous-commit", 33m));

        var ambiguousService = new InStockService(
            context,
            harness.UploadService,
            new UploadFileDeletionScheduler(
                context,
                Options.Create(new UploadDeletionOptions()),
                harness.Storage),
            new CommitThrowsTransactionFactory(context),
            NullLogger<InStockService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ambiguousService.AddImageAsync(item.Id, CreateJpegUpload("ambiguous.jpg"), "Front", 0));

        await using var verify = database.CreateContext();
        Assert.Equal(0, await verify.UploadedFiles.CountAsync());
        Assert.Equal(0, await verify.InStockItemImages.CountAsync());

        var physicalFile = Assert.Single(harness.Storage.EnumerateFiles());

        var maintenance = new StorageMaintenanceService(
            verify,
            new NoOpAuditLogService(),
            harness.Storage,
            NullLogger<StorageMaintenanceService>.Instance);
        var scan = await maintenance.ScanAsync();

        Assert.Equal(1, scan.OrphanPhysicalFileCount);
        Assert.Equal(physicalFile.StorageKey, Assert.Single(scan.OrphanPhysicalFiles).RelativePath);
    }

    private static SaveInStockItemRequest CreateSaveRequest(
        string title,
        string slug,
        decimal price,
        bool published = false) =>
        new(slug, title, null, null, price, "GBP", InStockItemStatus.Available, published, 0, null, null);

    private static UploadFileRequest CreateJpegUpload(string fileName)
    {
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        return new UploadFileRequest(fileName, "image/jpeg", jpeg.Length, new MemoryStream(jpeg));
    }

    private sealed class UploadHarness(
        string root,
        LocalUploadStorage storage,
        LocalUploadService uploadService,
        InStockService service) : IDisposable
    {
        public LocalUploadStorage Storage { get; } = storage;
        public LocalUploadService UploadService { get; } = uploadService;
        public InStockService Service { get; } = service;

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    private sealed class FailSaveChangesInterceptor : SaveChangesInterceptor
    {
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result) =>
            throw new InvalidOperationException("Forced SaveChanges failure for compensation test.");

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Forced SaveChanges failure for compensation test.");
    }

    private sealed class ThrowingDeletionScheduler : BespokeStudio.Application.Abstractions.IUploadFileDeletionScheduler
    {
        public Task ScheduleAsync(
            BespokeStudio.Application.Contracts.Storage.ScheduleUploadFileDeletionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Deletion scheduler failed.");
    }

    private sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "BespokeStudio.Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = contentRootPath;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    /// <summary>
    /// Begins a real PostgreSQL transaction but makes <see cref="IDbContextTransaction.CommitAsync"/>
    /// throw without committing, simulating an ambiguous client-visible commit failure.
    /// </summary>
    private sealed class CommitThrowsTransactionFactory(BespokeStudioDbContext dbContext)
        : IDbContextTransactionFactory
    {
        public bool SupportsTransactions => true;

        public async Task<IDbContextTransaction?> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            var inner = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            return new CommitThrowsTransaction(inner);
        }
    }

    private sealed class CommitThrowsTransaction(IDbContextTransaction inner) : IDbContextTransaction
    {
        public Guid TransactionId => inner.TransactionId;

        public void Commit() =>
            throw new InvalidOperationException("Simulated ambiguous commit failure.");

        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Simulated ambiguous commit failure.");

        public void Rollback() => inner.Rollback();

        public Task RollbackAsync(CancellationToken cancellationToken = default) =>
            inner.RollbackAsync(cancellationToken);

        public DbTransaction GetDbTransaction() => inner.GetDbTransaction();

        public void Dispose() => inner.Dispose();

        public ValueTask DisposeAsync() => inner.DisposeAsync();
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
}
