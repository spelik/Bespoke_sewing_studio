namespace BespokeStudio.Domain.Enums;

public enum EmailOutboxStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Skipped
}
