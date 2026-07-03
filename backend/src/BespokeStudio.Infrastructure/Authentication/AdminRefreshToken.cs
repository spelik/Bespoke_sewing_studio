namespace BespokeStudio.Infrastructure.Authentication;

public sealed class AdminRefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid TokenFamilyId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public DateTimeOffset? LastUsedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public string? UserAgent { get; set; }
    public string? RevocationReason { get; set; }
    public AdminUser User { get; set; } = null!;
    public AdminRefreshToken? ReplacedByToken { get; set; }
}
