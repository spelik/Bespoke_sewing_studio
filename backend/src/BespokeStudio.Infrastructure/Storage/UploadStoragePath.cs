using Microsoft.Extensions.Hosting;

namespace BespokeStudio.Infrastructure.Storage;

internal static class UploadStoragePath
{
    public static string ResolveRoot(UploadStorageOptions options, IHostEnvironment environment) =>
        Path.GetFullPath(
            Path.IsPathRooted(options.RootPath)
                ? options.RootPath
                : Path.Combine(environment.ContentRootPath, options.RootPath));

    public static string ResolveFile(string storageRoot, string storageKey)
    {
        var candidate = Path.GetFullPath(Path.Combine(
            storageRoot,
            storageKey.Replace('/', Path.DirectorySeparatorChar)));
        var rootPrefix = storageRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Stored upload path escapes the configured storage root.");
        }

        return candidate;
    }

    public static string NormalizeAndValidateStorageKey(string storageRoot, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey))
        {
            throw new InvalidOperationException("Upload storage key must be a non-empty relative path.");
        }

        var physicalPath = ResolveFile(storageRoot, storageKey);
        return Path.GetRelativePath(storageRoot, physicalPath)
            .Replace(Path.DirectorySeparatorChar, '/');
    }
}
