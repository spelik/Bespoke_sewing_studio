using BespokeStudio.Application.Contracts.EmailDeliveryLog;

namespace BespokeStudio.Application.Abstractions;

public interface IEmailDeliveryLogService
{
    Task<IReadOnlyList<EmailDeliveryLogEntryResponse>> GetAsync(
        EmailDeliveryLogQueryRequest request,
        CancellationToken cancellationToken = default);

    Task RecordAsync(
        EmailDeliveryLogWriteRequest request,
        CancellationToken cancellationToken = default);
}
