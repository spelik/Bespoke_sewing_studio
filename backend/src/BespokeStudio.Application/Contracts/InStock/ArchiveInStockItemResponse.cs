namespace BespokeStudio.Application.Contracts.InStock;

public sealed record ArchiveInStockItemResponse(
    Guid Id,
    bool Archived,
    bool Restored,
    string Message);
