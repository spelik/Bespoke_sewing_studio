namespace BespokeStudio.Application.Contracts.Services;

public sealed record DeleteServiceOfferingResponse(
    Guid Id,
    bool Deleted,
    bool Archived,
    string Message);
