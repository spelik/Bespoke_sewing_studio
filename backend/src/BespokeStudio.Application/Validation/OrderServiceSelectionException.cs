namespace BespokeStudio.Application.Validation;

public sealed class OrderServiceSelectionException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
