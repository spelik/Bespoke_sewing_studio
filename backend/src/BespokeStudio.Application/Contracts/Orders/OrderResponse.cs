using BespokeStudio.Application.Contracts.Clients;
using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.Orders;

public sealed record OrderResponse(
    Guid Id,
    string ReferenceNumber,
    Guid ClientId,
    ClientResponse Client,
    Guid? ServiceOfferingId,
    string ServiceName,
    OrderServiceType ServiceType,
    OrderStatus Status,
    string Description,
    DateOnly? PreferredDate,
    bool ConsentGiven,
    DateTimeOffset? ConsentRecordedAt,
    decimal? QuotedAmount,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyCollection<OrderAttachmentResponse> Attachments,
    IReadOnlyCollection<OrderNoteResponse> Notes);
