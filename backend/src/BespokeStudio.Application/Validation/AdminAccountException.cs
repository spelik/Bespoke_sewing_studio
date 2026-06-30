namespace BespokeStudio.Application.Validation;

public sealed class AdminAccountException(IReadOnlyDictionary<string, string[]> errors) : Exception("Admin account validation failed.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public static AdminAccountException For(string propertyName, string error) =>
        new(new Dictionary<string, string[]> { [propertyName] = [error] });
}
