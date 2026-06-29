namespace BespokeStudio.Infrastructure.Security;

public sealed class UploadSecurityOptions
{
    public const string SectionName = "UploadSecurity";

    public MalwareScannerOptions MalwareScanner { get; init; } = new();
}

public sealed class MalwareScannerOptions
{
    public string Provider { get; init; } = "Disabled";
    public string DisplayName { get; init; } = "Not configured";
    public string ExecutablePath { get; init; } = "clamscan";
    public List<string> Arguments { get; init; } = ["--no-summary", "{filePath}"];
    public int TimeoutSeconds { get; init; } = 30;
    public List<int> CleanExitCodes { get; init; } = [0];
    public List<int> InfectedExitCodes { get; init; } = [1];
    public List<int> ErrorExitCodes { get; init; } = [2];
    public bool TreatScannerErrorAsRejection { get; init; } = true;
}
