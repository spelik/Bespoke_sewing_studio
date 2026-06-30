using BespokeStudio.Application.Contracts.AdminUsers;

namespace BespokeStudio.Application.Validation;

public static class AdminUserManagementValidator
{
    public static Dictionary<string, string[]> Validate(CreateAdminUserRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        AddEmailErrors(errors, request.Email);
        AddPasswordErrors(errors, request.Password);
        return errors;
    }

    public static Dictionary<string, string[]> Validate(ResetAdminUserPasswordRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        AddPasswordErrors(errors, request.Password);
        return errors;
    }

    private static void AddEmailErrors(IDictionary<string, string[]> errors, string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            errors[nameof(CreateAdminUserRequest.Email)] = ["Email is required."];
            return;
        }

        if (email.Trim().Length > 256 || !email.Contains('@'))
        {
            errors[nameof(CreateAdminUserRequest.Email)] = ["Enter a valid email address."];
        }
    }

    private static void AddPasswordErrors(IDictionary<string, string[]> errors, string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            errors[nameof(CreateAdminUserRequest.Password)] = ["Password is required."];
            return;
        }

        if (password.Length < 12)
        {
            errors[nameof(CreateAdminUserRequest.Password)] = ["Password must be at least 12 characters long."];
        }
    }
}
