namespace BespokeStudio.Application.Contracts.Portfolio;

public sealed record DeletePortfolioResponse(
    Guid Id,
    bool Deleted,
    bool Archived,
    string Message);
