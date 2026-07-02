using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.ContactMessages;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Infrastructure.Services;

public sealed class ContactMessageService(
    BespokeStudioDbContext dbContext,
    IAdminAuditLogService auditLogService) : IContactMessageService
{
    public async Task<ContactMessageResponse> CreateAsync(
        CreateContactMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var referenceNumber = await RequestReferenceNumberGenerator.CreateAsync(
            dbContext,
            "ContactMessageReferenceSequence",
            "BSS-CON",
            now,
            cancellationToken);
        var message = new ContactMessage
        {
            ReferenceNumber = referenceNumber,
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = N(request.Phone),
            Subject = N(request.Subject),
            Message = request.Message.Trim(),
            ConsentGiven = request.Consent,
            ConsentRecordedAt = request.Consent ? now : null,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.ContactMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToResponse(message);
    }

    public async Task<IReadOnlyList<ContactMessageListItemResponse>> GetAllAsync(
        int take,
        CancellationToken cancellationToken = default)
    {
        var messages = await dbContext.ContactMessages
            .AsNoTracking()
            .OrderByDescending(message => message.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return messages.Select(message => new ContactMessageListItemResponse(
            message.Id,
            message.ReferenceNumber,
            message.FullName,
            message.Email,
            message.Phone,
            message.Subject,
            Preview(message.Message),
            message.Status,
            message.CreatedAt,
            message.UpdatedAt)).ToArray();
    }

    public async Task<ContactMessageResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var message = await dbContext.ContactMessages
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        return message is null ? null : ToResponse(message);
    }

    public async Task<ContactMessageResponse?> UpdateStatusAsync(
        Guid id,
        UpdateContactMessageStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var message = await dbContext.ContactMessages
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (message is null)
        {
            return null;
        }

        message.Status = request.Status;
        message.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(message);
    }


    public async Task<DeleteContactMessageResult?> DeleteAsync(
        Guid id,
        AdminAuditActor actor,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var message = await dbContext.ContactMessages
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);

        if (message is null)
        {
            return null;
        }

        var result = new DeleteContactMessageResult(
            message.Id,
            message.ReferenceNumber,
            message.FullName,
            message.Email);

        dbContext.ContactMessages.Remove(message);

        auditLogService.AddPending(new AdminAuditLogWriteRequest(
            actor.UserId,
            actor.Email,
            "contact_message.deleted",
            "ContactMessage",
            result.Id.ToString(),
            result.ReferenceNumber,
            $"Contact message {result.ReferenceNumber} from {result.FullName} was deleted."));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    private static ContactMessageResponse ToResponse(ContactMessage message) => new(
        message.Id,
        message.ReferenceNumber,
        message.FullName,
        message.Email,
        message.Phone,
        message.Subject,
        message.Message,
        message.Status,
        message.ConsentGiven,
        message.ConsentRecordedAt,
        message.CreatedAt,
        message.UpdatedAt);

    private static string Preview(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= 180 ? trimmed : trimmed[..180] + "…";
    }

    private static string? N(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
