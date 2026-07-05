using BespokeStudio.Application.Validation;
using BespokeStudio.Infrastructure.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Tests.Storage;

public sealed class LocalUploadStorageTests : IDisposable
{
    private readonly List<string> _tempRoots = [];

    [Fact]
    public void NormalizeAndValidateStorageKey_NormalizesRelativeKey()
    {
        var storage = CreateStorage();

        var key = storage.NormalizeAndValidateStorageKey("order-attachments/2026/07/file.jpg");

        Assert.Equal("order-attachments/2026/07/file.jpg", key);
    }

    [Fact]
    public void NormalizeAndValidateStorageKey_RejectsParentTraversal()
    {
        var storage = CreateStorage();

        Assert.Throws<InvalidOperationException>(() =>
            storage.NormalizeAndValidateStorageKey("../escape.txt"));
    }

    [Fact]
    public void NormalizeAndValidateStorageKey_RejectsAbsolutePath()
    {
        var storage = CreateStorage();
        var absolute = Path.Combine(Path.GetTempPath(), "outside.txt");

        Assert.Throws<InvalidOperationException>(() =>
            storage.NormalizeAndValidateStorageKey(absolute));
    }

    [Fact]
    public void BuildStorageKeys_ProducesQuarantineAndFinalLayout()
    {
        var storage = CreateStorage();
        var timestamp = new DateTimeOffset(2026, 7, 5, 10, 0, 0, TimeSpan.Zero);

        var keys = storage.BuildStorageKeys("order-attachments", ".jpg", timestamp);

        Assert.Equal($"order-attachments/2026/07/{keys.StoredFileName}", keys.FinalKey);
        Assert.Equal($"quarantine/order-attachments/2026/07/{keys.StoredFileName}", keys.QuarantineKey);
        Assert.EndsWith(".jpg", keys.StoredFileName);
    }

    [Fact]
    public async Task WriteOpenDelete_RoundTripsContent()
    {
        var storage = CreateStorage();
        const string key = "order-attachments/2026/07/roundtrip.bin";
        var payload = new byte[] { 1, 2, 3, 4, 5 };

        await storage.WriteNewFileAsync(key, new MemoryStream(payload), maxBytes: 1024, CancellationToken.None);

        Assert.True(storage.Exists(key));
        Assert.Equal(payload.Length, (int)storage.GetFileSize(key));

        await using (var stream = storage.OpenRead(key))
        {
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            Assert.Equal(payload, buffer.ToArray());
        }

        storage.DeleteFile(key);
        Assert.False(storage.Exists(key));
    }

    [Fact]
    public async Task WriteNewFileAsync_ThrowsWhenExceedingLimit()
    {
        var storage = CreateStorage();
        var payload = new byte[64];

        await Assert.ThrowsAsync<UploadValidationException>(() =>
            storage.WriteNewFileAsync(
                "content-images/2026/07/big.bin",
                new MemoryStream(payload),
                maxBytes: 16,
                CancellationToken.None));
    }

    [Fact]
    public async Task MoveToFinal_MovesQuarantineFileToFinalLocation()
    {
        var storage = CreateStorage();
        const string quarantineKey = "quarantine/order-attachments/2026/07/moved.bin";
        const string finalKey = "order-attachments/2026/07/moved.bin";
        await storage.WriteNewFileAsync(quarantineKey, new MemoryStream([9]), 1024, CancellationToken.None);

        storage.MoveToFinal(quarantineKey, finalKey);

        Assert.False(storage.Exists(quarantineKey));
        Assert.True(storage.Exists(finalKey));
    }

    [Fact]
    public async Task EnumerateFiles_ReturnsSafeRelativeKeys()
    {
        var storage = CreateStorage();
        await storage.WriteNewFileAsync("order-attachments/2026/07/a.bin", new MemoryStream([1]), 1024, CancellationToken.None);
        await storage.WriteNewFileAsync("portfolio-images/2026/07/b.bin", new MemoryStream([2]), 1024, CancellationToken.None);

        var keys = storage.EnumerateFiles().Select(file => file.StorageKey).ToArray();

        Assert.Contains("order-attachments/2026/07/a.bin", keys);
        Assert.Contains("portfolio-images/2026/07/b.bin", keys);
        Assert.All(keys, key =>
        {
            Assert.False(Path.IsPathRooted(key));
            Assert.DoesNotContain('\\', key);
        });
    }

    [Fact]
    public void DeleteIfExists_MissingFile_ReturnsTrueWithoutThrowing()
    {
        var storage = CreateStorage();

        var result = storage.DeleteIfExists("order-attachments/2026/07/missing.bin");

        Assert.True(result);
    }

    private LocalUploadStorage CreateStorage()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "bespoke-upload-storage-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        _tempRoots.Add(root);

        var options = Options.Create(new UploadStorageOptions { RootPath = root });
        return new LocalUploadStorage(
            options,
            new FakeHostEnvironment(root),
            NullLogger<LocalUploadStorage>.Instance);
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
                // Best-effort cleanup of temporary test files.
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
}
