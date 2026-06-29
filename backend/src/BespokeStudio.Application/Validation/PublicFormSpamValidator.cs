namespace BespokeStudio.Application.Validation;

public static class PublicFormSpamValidator
{
    private static readonly string[] GenericRejection =
        ["The form could not be accepted. Please refresh the page and try again."];

    private static readonly TimeSpan MinimumSubmissionAge = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaximumSubmissionAge = TimeSpan.FromHours(24);
    private static readonly TimeSpan FutureClockSkewTolerance = TimeSpan.FromMinutes(1);

    public static void AddValidationErrors(
        IDictionary<string, string[]> errors,
        string? honeypotValue,
        DateTimeOffset? formLoadedAt)
    {
        if (!string.IsNullOrWhiteSpace(honeypotValue))
        {
            errors["Form"] = GenericRejection;
            return;
        }

        if (!formLoadedAt.HasValue)
        {
            errors["Form"] = GenericRejection;
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var loadedAt = formLoadedAt.Value.ToUniversalTime();

        if (loadedAt > now.Add(FutureClockSkewTolerance))
        {
            errors["Form"] = GenericRejection;
            return;
        }

        var formAge = now - loadedAt;
        if (formAge < MinimumSubmissionAge || formAge > MaximumSubmissionAge)
        {
            errors["Form"] = GenericRejection;
        }
    }
}
