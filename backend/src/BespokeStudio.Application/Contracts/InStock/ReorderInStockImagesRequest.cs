namespace BespokeStudio.Application.Contracts.InStock;

public sealed record ReorderInStockImagesRequest(IReadOnlyList<Guid> ImageIds);
