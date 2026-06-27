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
}
