namespace BespokeStudio.Application.Validation;

public sealed class PortfolioConflictException(string field, string message) : Exception(message)
{
    public string Field { get; } = field;
}
