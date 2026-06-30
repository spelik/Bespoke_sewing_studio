using BespokeStudio.Application.Contracts.Auth;

namespace BespokeStudio.Application.Validation;

public static class AdminAccountValidator
{
    public const int MinimumPasswordLength = 12;

    public static Dictionary<string, string[]> Validate(ChangeOwnPasswordRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            errors[nameof(ChangeOwnPasswordRequest.CurrentPassword)] = ["Current password is required."];
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword))
        {
            errors[nameof(ChangeOwnPasswordRequest.NewPassword)] = ["New password is required."];
        }
        else if (request.NewPassword.Length < MinimumPasswordLength)
        {
            errors[nameof(ChangeOwnPasswordRequest.NewPassword)] =
            [
                $"New password must be at least {MinimumPasswordLength} characters long."
            ];
        }

        if (string.IsNullOrWhiteSpace(request.ConfirmNewPassword))
        {
            errors[nameof(ChangeOwnPasswordRequest.ConfirmNewPassword)] = ["Confirm the new password."];
        }
        else if (!string.Equals(request.NewPassword, request.ConfirmNewPassword, StringComparison.Ordinal))
        {
            errors[nameof(ChangeOwnPasswordRequest.ConfirmNewPassword)] = ["New password confirmation does not match."];
        }

        if (!string.IsNullOrWhiteSpace(request.CurrentPassword) &&
            !string.IsNullOrWhiteSpace(request.NewPassword) &&
            string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            errors[nameof(ChangeOwnPasswordRequest.NewPassword)] = ["New password must be different from the current password."];
        }

        return errors;
    }
}
