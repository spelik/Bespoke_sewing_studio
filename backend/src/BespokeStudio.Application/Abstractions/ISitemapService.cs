namespace BespokeStudio.Application.Abstractions;

public interface ISitemapService
{
    Task<string> BuildXmlAsync(CancellationToken cancellationToken = default);
}
