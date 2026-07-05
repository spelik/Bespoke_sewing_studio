using System.Security.Claims;
using System.Text.Json;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.Common;
using BespokeStudio.Application.Contracts.EmailDeliveryLog;
using BespokeStudio.Application.Notifications;
using BespokeStudio.Application.Security;

namespace BespokeStudio.Api.Endpoints;

public static class EmailDeliveryLogEndpoints
{
    public static IEndpointRouteBuilder MapEmailDeliveryLogEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var admin = endpoints.MapGroup("/api/admin/email-log")
            .RequireAuthorization(AdminAccess.PolicyName)
            .WithTags("Admin Email Log");

        admin.MapGet(string.Empty, GetAsync)
            .WithName("GetAdminEmailDeliveryLog")
            .Produces<PagedResponse<EmailDeliveryLogEntryResponse>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapGet("/summary", GetSummaryAsync)
            .WithName("GetAdminEmailOutboxMonitoringSummary")
            .Produces<EmailOutboxMonitoringSummaryResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapGet("/retention", GetRetentionAsync)
            .WithName("GetAdminEmailOutboxRetentionSummary")
            .Produces<EmailOutboxRetentionSummaryResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapPost("/retention/cleanup", RunRetentionCleanupAsync)
            .WithName("RunAdminEmailOutboxRetentionCleanup")
            .Produces<EmailOutboxRetentionCleanupResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        admin.MapPost("/{id:guid}/retry", RetryAsync)
            .WithName("RetryAdminEmailDeliveryLogEntry")
            .Produces<EmailDeliveryManualRetryResponse>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        int? page,
        int? pageSize,
        string? search,
        string? messageType,
        string? status,
        string? recipientEmail,
        string? provider,
        IEmailDeliveryLogService service,
        CancellationToken cancellationToken)
    {
        var pagination = PaginationQuery.Normalize(page, pageSize);

        var entries = await service.GetAsync(
            new EmailDeliveryLogQueryRequest(
                pagination.Page,
                pagination.PageSize,
                search,
                messageType,
                status,
                recipientEmail,
                provider),
            cancellationToken);

        return TypedResults.Ok(entries);
    }

    private static async Task<IResult> GetSummaryAsync(
        IEmailDeliveryLogService service,
        CancellationToken cancellationToken)
    {
        var summary = await service.GetOutboxMonitoringSummaryAsync(cancellationToken);
        return TypedResults.Ok(summary);
    }

    private static async Task<IResult> GetRetentionAsync(
        IEmailOutboxRetentionService retentionService,
        CancellationToken cancellationToken)
    {
        var summary = await retentionService.GetSummaryAsync(cancellationToken);
        return TypedResults.Ok(summary);
    }

    private static async Task<IResult> RunRetentionCleanupAsync(
        ClaimsPrincipal principal,
        IEmailOutboxRetentionService retentionService,
        IAdminAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        var response = await retentionService.RunCleanupAsync(cancellationToken);

        var metadataJson = JsonSerializer.Serialize(new
        {
            succeededBodyPurgedCount = response.SucceededBodyPurgedCount,
            skippedBodyPurgedCount = response.SkippedBodyPurgedCount,
            succeededDeletedCount = response.SucceededDeletedCount,
            skippedDeletedCount = response.SkippedDeletedCount
        });

        await auditLogService.RecordAsync(
            AdminAuditEndpointHelpers.CreateAuditRequest(
                principal,
                "email_outbox.retention_cleanup_ran",
                "EmailOutboxRetention",
                "email-outbox-retention",
                "Email outbox retention",
                "Email outbox retention cleanup was run.",
                metadataJson),
            cancellationToken);

        return TypedResults.Ok(response);
    }

    private static async Task<IResult> RetryAsync(
        Guid id,
        ClaimsPrincipal principal,
        IEmailOutboxService outboxService,
        IAdminAuditLogService auditLogService,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await outboxService.QueueManualRetryAsync(id, cancellationToken);

            var metadataJson = JsonSerializer.Serialize(new
            {
                emailDeliveryLogEntryId = response.EmailDeliveryLogEntryId,
                outboxMessageId = response.OutboxMessageId,
                messageType = response.MessageType
            });

            await auditLogService.RecordAsync(
                AdminAuditEndpointHelpers.CreateAuditRequest(
                    principal,
                    "email_outbox.manual_retry_queued",
                    "EmailOutboxMessage",
                    response.OutboxMessageId.ToString(),
                    response.RelatedEntityLabel ?? response.MessageType,
                    "Manual email retry was queued.",
                    metadataJson),
                cancellationToken);

            return TypedResults.Ok(response);
        }
        catch (EmailOutboxMessageNotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (EmailManualRetryNotAllowedException)
        {
            return TypedResults.Problem(
                title: "Manual retry not allowed",
                detail: "This email is not eligible for manual retry anymore.",
                statusCode: StatusCodes.Status409Conflict);
        }
    }
}
