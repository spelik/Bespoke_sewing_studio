namespace BespokeStudio.Application.Validation;

public sealed class AdminUserManagementException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Admin user management validation failed.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public static AdminUserManagementException For(string propertyName, string message) =>
        new(new Dictionary<string, string[]> { [propertyName] = [message] });
}
