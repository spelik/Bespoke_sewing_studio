using BespokeStudio.Application.Contracts.Common;
using BespokeStudio.Application.Contracts.EmailDeliveryLog;

namespace BespokeStudio.Application.Abstractions;

public interface IEmailDeliveryLogService
{
    Task<PagedResponse<EmailDeliveryLogEntryResponse>> GetAsync(
        EmailDeliveryLogQueryRequest request,
        CancellationToken cancellationToken = default);

    Task RecordAsync(
        EmailDeliveryLogWriteRequest request,
        CancellationToken cancellationToken = default);

    Task<EmailOutboxMonitoringSummaryResponse> GetOutboxMonitoringSummaryAsync(
        CancellationToken cancellationToken = default);
}
