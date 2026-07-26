using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BespokeStudio.Application.Contracts.Uploads;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Security;
using BespokeStudio.Infrastructure.Services;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Tests.InStock;

public sealed class InStockUploadServiceTests : IDisposable
{
    private readonly List<string> _tempRoots = [];

    [Fact]
    public async Task PrepareInStockImage_RejectsUnsupportedMimeAndOversizedFiles()
    {
        await using var db = CreateDb();
        var (_, service) = CreateUploadService(db, maxBytes: 32);

        await Assert.ThrowsAsync<UploadValidationException>(() =>
            service.PrepareInStockImageAsync(
                new UploadFileRequest("note.txt", "text/plain", 4, new MemoryStream([1, 2, 3, 4]))));

        var oversized = new byte[64];
        oversized[0] = 0xFF;
        oversized[1] = 0xD8;
        oversized[2] = 0xFF;
        await Assert.ThrowsAsync<UploadValidationException>(() =>
            service.PrepareInStockImageAsync(
                new UploadFileRequest("big.jpg", "image/jpeg", oversized.Length, new MemoryStream(oversized))));

        Assert.Empty(await db.UploadedFiles.ToListAsync());
        Assert.Empty(await db.InStockItemImages.ToListAsync());
    }

    [Fact]
    public async Task PrepareInStockImage_RejectsPdfAndMismatchedSignature()
    {
        await using var db = CreateDb();
        var (_, service) = CreateUploadService(db);

        await Assert.ThrowsAsync<UploadValidationException>(() =>
            service.PrepareInStockImageAsync(
                new UploadFileRequest("notes.pdf", "application/pdf", 5, new MemoryStream("%PDF-"u8.ToArray()))));

        await Assert.ThrowsAsync<UploadValidationException>(() =>
            service.PrepareInStockImageAsync(
                new UploadFileRequest("fake.jpg", "image/jpeg", 4, new MemoryStream([1, 2, 3, 4]))));
    }

    [Fact]
    public async Task PrepareInStockImage_InfectedScan_DoesNotPersistMetadataOrLeaveFinalFile()
    {
        await using var db = CreateDb();
        using var fakeClamAv = await FakeClamAvServer.StartAsync("stream: Eicar-Test-Signature FOUND\0");
        var (storage, service) = CreateUploadService(db, scanner: CreateClamAvScanner(fakeClamAv.Port));
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };

        await Assert.ThrowsAsync<UploadValidationException>(() =>
            service.PrepareInStockImageAsync(
                new UploadFileRequest("bad.jpg", "image/jpeg", jpeg.Length, new MemoryStream(jpeg))));

        Assert.Empty(await db.UploadedFiles.ToListAsync());
        Assert.Empty(await db.InStockItemImages.ToListAsync());
        Assert.Empty(storage.EnumerateFiles());
    }

    [Fact]
    public async Task PrepareInStockImage_ScanFailure_DoesNotCreatePublicFile()
    {
        await using var db = CreateDb();
        using var fakeClamAv = await FakeClamAvServer.StartAsync("stream: broken response\0");
        var (storage, service) = CreateUploadService(db, scanner: CreateClamAvScanner(fakeClamAv.Port));
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };

        await Assert.ThrowsAsync<UploadValidationException>(() =>
            service.PrepareInStockImageAsync(
                new UploadFileRequest("scan-fail.jpg", "image/jpeg", jpeg.Length, new MemoryStream(jpeg))));

        Assert.Empty(await db.UploadedFiles.ToListAsync());
        Assert.DoesNotContain(
            storage.EnumerateFiles(),
            file => !file.StorageKey.StartsWith("quarantine/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task OpenPublicInStockImage_HiddenForUnpublishedAndArchived_AdminStillAvailable()
    {
        await using var db = CreateDb();
        var (_, service) = CreateUploadService(db);
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        var prepared = await service.PrepareInStockImageAsync(
            new UploadFileRequest("piece.jpg", "image/jpeg", jpeg.Length, new MemoryStream(jpeg)));

        var item = new InStockItem
        {
            Id = Guid.NewGuid(),
            Slug = "piece",
            Title = "Piece",
            Price = 10m,
            IsPublished = false
        };
        var image = new InStockItemImage
        {
            Id = Guid.NewGuid(),
            InStockItemId = item.Id,
            UploadedFileId = prepared.Metadata.Id,
            DisplayOrder = 0
        };
        db.UploadedFiles.Add(prepared.Metadata);
        db.InStockItems.Add(item);
        db.InStockItemImages.Add(image);
        await db.SaveChangesAsync();

        Assert.Null(await service.OpenPublicInStockImageAsync(image.Id));
        var adminOpen = await service.OpenInStockImageForAdminAsync(image.Id);
        Assert.NotNull(adminOpen);
        Assert.Equal("image/jpeg", adminOpen.ContentType);

        item.IsPublished = true;
        item.ArchivedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        Assert.Null(await service.OpenPublicInStockImageAsync(image.Id));
        Assert.NotNull(await service.OpenInStockImageForAdminAsync(image.Id));
    }

    [Fact]
    public async Task CompensateOrphanedPromotedFile_DeletesPhysicalFile()
    {
        await using var db = CreateDb();
        var (storage, service) = CreateUploadService(db);
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
        var prepared = await service.PrepareInStockImageAsync(
            new UploadFileRequest("orphan.jpg", "image/jpeg", jpeg.Length, new MemoryStream(jpeg)));

        Assert.True(storage.Exists(prepared.FinalStorageKey));

        await service.CompensateOrphanedPromotedFileAsync(
            prepared.FinalStorageKey,
            prepared.Metadata.OriginalFileName,
            prepared.Metadata.SizeBytes);

        Assert.False(storage.Exists(prepared.FinalStorageKey));
        Assert.Empty(await db.UploadedFiles.ToListAsync());
    }

    private (LocalUploadStorage Storage, LocalUploadService Service) CreateUploadService(
        BespokeStudioDbContext db,
        long maxBytes = 1024,
        ConfiguredMalwareScanner? scanner = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "bespoke-in-stock-upload-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _tempRoots.Add(root);

        var options = Options.Create(new UploadStorageOptions
        {
            RootPath = root,
            MaxFileSizeBytes = maxBytes
        });
        var storage = new LocalUploadStorage(
            options,
            new FakeHostEnvironment(root),
            NullLogger<LocalUploadStorage>.Instance);

        var service = new LocalUploadService(
            db,
            options,
            scanner ?? CreateDisabledScanner(),
            new UploadFileDeletionScheduler(
                db,
                Options.Create(new UploadDeletionOptions()),
                storage),
            storage,
            NullLogger<LocalUploadService>.Instance);

        return (storage, service);
    }

    private static ConfiguredMalwareScanner CreateDisabledScanner() =>
        new(
            Options.Create(new UploadSecurityOptions
            {
                MalwareScanner = new MalwareScannerOptions { Provider = "Disabled" }
            }),
            NullLogger<ConfiguredMalwareScanner>.Instance);

    private static ConfiguredMalwareScanner CreateClamAvScanner(int port) =>
        new(
            Options.Create(new UploadSecurityOptions
            {
                MalwareScanner = new MalwareScannerOptions
                {
                    Provider = "ClamAV",
                    DisplayName = "ClamAV",
                    TimeoutSeconds = 5,
                    TreatScannerErrorAsRejection = true,
                    ClamAv = new ClamAvScannerOptions
                    {
                        Host = "127.0.0.1",
                        Port = port,
                        MaxChunkSizeBytes = 4
                    }
                }
            }),
            NullLogger<ConfiguredMalwareScanner>.Instance);

    private static BespokeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BespokeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new BespokeStudioDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public void Dispose()
    {
        foreach (var root in _tempRoots)
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

    private sealed class FakeHostEnvironment(string contentRootPath) : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "BespokeStudio.Tests";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = contentRootPath;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    private sealed class FakeClamAvServer : IDisposable
    {
        private readonly TcpListener _listener;
        private Task _serverTask;

        private FakeClamAvServer(TcpListener listener, Task serverTask, int port)
        {
            _listener = listener;
            _serverTask = serverTask;
            Port = port;
        }

        public int Port { get; }

        public static Task<FakeClamAvServer> StartAsync(string response)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var server = new FakeClamAvServer(
                listener,
                Task.CompletedTask,
                ((IPEndPoint)listener.LocalEndpoint).Port);
            server._serverTask = server.HandleConnectionAsync(response);
            return Task.FromResult(server);
        }

        public void Dispose()
        {
            _listener.Stop();
            _serverTask.GetAwaiter().GetResult();
        }

        private async Task HandleConnectionAsync(string response)
        {
            using var client = await _listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            await ReadCommandAsync(stream);

            while (true)
            {
                var sizeBytes = await ReadExactAsync(stream, 4);
                var size = BinaryPrimitives.ReadInt32BigEndian(sizeBytes);
                if (size == 0)
                {
                    break;
                }

                _ = await ReadExactAsync(stream, size);
            }

            await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
        }

        private static async Task ReadCommandAsync(NetworkStream stream)
        {
            var buffer = new byte[1];
            do
            {
                var read = await stream.ReadAsync(buffer);
                if (read == 0)
                {
                    break;
                }
            }
            while (buffer[0] != 0);
        }

        private static async Task<byte[]> ReadExactAsync(NetworkStream stream, int length)
        {
            var buffer = new byte[length];
            var offset = 0;
            while (offset < length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(offset, length - offset));
                if (read == 0)
                {
                    throw new IOException("Unexpected end of fake ClamAV stream.");
                }

                offset += read;
            }

            return buffer;
        }
    }
}
