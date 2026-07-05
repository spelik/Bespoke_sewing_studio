using BespokeStudio.Application.Contracts.EmailDeliveryLog;

namespace BespokeStudio.Application.Abstractions;

public interface IEmailOutboxRetentionService
{
    Task<EmailOutboxRetentionSummaryResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<EmailOutboxRetentionCleanupResponse> RunCleanupAsync(
        CancellationToken cancellationToken = default);
}
