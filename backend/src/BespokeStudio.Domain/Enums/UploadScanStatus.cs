namespace BespokeStudio.Domain.Enums;

public enum UploadScanStatus
{
    Pending = 0,
    Clean = 1,
    Skipped = 2,
    Infected = 3,
    Rejected = 4,
    ScanFailed = 5
}
