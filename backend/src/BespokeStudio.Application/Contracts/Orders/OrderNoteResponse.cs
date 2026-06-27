namespace BespokeStudio.Application.Contracts.Orders;

public sealed record OrderNoteResponse(
    Guid Id,
    string Text,
    DateTimeOffset CreatedAt);
