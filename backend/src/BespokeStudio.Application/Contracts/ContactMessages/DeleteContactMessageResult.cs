namespace BespokeStudio.Application.Contracts.ContactMessages;

public sealed record DeleteContactMessageResult(
    Guid Id,
    string ReferenceNumber,
    string FullName,
    string Email);
