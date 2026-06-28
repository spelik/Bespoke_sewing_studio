namespace BespokeStudio.Application.Contracts.Content;
public sealed record DeletePageContentResponse(Guid Id, bool Archived, string Message);
