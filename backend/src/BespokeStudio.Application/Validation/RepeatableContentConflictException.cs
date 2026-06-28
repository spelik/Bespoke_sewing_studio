namespace BespokeStudio.Application.Validation;

public sealed class RepeatableContentConflictException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
