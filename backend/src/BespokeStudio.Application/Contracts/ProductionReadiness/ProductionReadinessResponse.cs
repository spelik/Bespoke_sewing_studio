namespace BespokeStudio.Application.Contracts.ProductionReadiness;

public sealed record ProductionReadinessResponse(
    IReadOnlyList<ProductionReadinessCheckResponse> Checks,
    DateTimeOffset GeneratedAt);

public sealed record ProductionReadinessCheckResponse(
    string Key,
    string Label,
    string Status,
    string Detail,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> Missing);
