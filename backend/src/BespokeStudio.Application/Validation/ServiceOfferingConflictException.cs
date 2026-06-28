namespace BespokeStudio.Application.Validation;

public sealed class ServiceOfferingConflictException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
