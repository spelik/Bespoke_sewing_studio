namespace BespokeStudio.Application.Validation;

public sealed class EmailDeliverySettingsValidationException(
    IReadOnlyDictionary<string, string[]> errors) : Exception("Email delivery settings are invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
