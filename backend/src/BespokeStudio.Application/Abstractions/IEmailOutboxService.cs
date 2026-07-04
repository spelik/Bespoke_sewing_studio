using BespokeStudio.Application.Contracts.Notifications;

namespace BespokeStudio.Application.Abstractions;

public interface IEmailOutboxService
{
    Task<Guid> EnqueueAsync(
        EmailOutboxEnqueueRequest request,
        CancellationToken cancellationToken = default);
}
