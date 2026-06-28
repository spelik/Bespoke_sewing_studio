namespace BespokeStudio.Application.Contracts.RepeatableContent;

public sealed record DeleteRepeatableContentItemResponse(Guid Id, bool Archived, string Message);
