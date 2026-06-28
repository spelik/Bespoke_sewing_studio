using BespokeStudio.Domain.Enums;

namespace BespokeStudio.Application.Contracts.ContactMessages;

public sealed record UpdateContactMessageStatusRequest(ContactMessageStatus Status);
