using BespokeStudio.Application.Contracts.EmailDeliveryLog;
using BespokeStudio.Application.Contracts.Notifications;

namespace BespokeStudio.Application.Abstractions;

public interface IEmailOutboxService
{
    Task<Guid> EnqueueAsync(
        EmailOutboxEnqueueRequest request,
        CancellationToken cancellationToken = default);

    Task<EmailDeliveryManualRetryResponse> QueueManualRetryAsync(
        Guid emailDeliveryLogEntryId,
        CancellationToken cancellationToken = default);
}
