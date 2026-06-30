namespace BespokeStudio.Application.Contracts.Auth;

public sealed record ChangeOwnPasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword);
