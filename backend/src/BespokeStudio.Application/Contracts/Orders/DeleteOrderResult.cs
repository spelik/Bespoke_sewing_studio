namespace BespokeStudio.Application.Contracts.Orders;

public sealed record DeleteOrderResult(
    Guid Id,
    string ReferenceNumber,
    string ClientName);
