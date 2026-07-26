namespace BespokeStudio.Application.Validation;

public sealed class InStockConflictException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
