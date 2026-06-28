namespace BespokeStudio.Application.Validation;
public sealed class PageContentConflictException(string field, string message) : Exception(message) { public string Field { get; } = field; }
