using System.Net.Http.Json;
using System.Text.Json.Serialization;
using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.ProductionReadiness;
using BespokeStudio.Application.Validation;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

public sealed class ProductionReadinessService(
    IEmailDeliverySettingsService emailDeliverySettingsService,
    IEmailDeliveryLogService emailDeliveryLogService,
    IMalwareScanner malwareScanner,
    IOptions<UploadSecurityOptions> uploadSecurityOptions,
    BespokeStudioDbContext dbContext,
    IHttpClientFactory httpClientFactory,
    ILogger<ProductionReadinessService> logger) : IProductionReadinessService
{
    private const string Domain = "oksanalogosha.com";

    public async Task<ProductionReadinessResponse> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var checks = new List<ProductionReadinessCheckResponse>
        {
            await CheckEmailDeliveryAsync(cancellationToken),
            await CheckEmailOutboxAsync(cancellationToken),
            await CheckUploadSecurityAsync(cancellationToken),
            await CheckDnsEmailRecordsAsync(cancellationToken)
        };

        return new ProductionReadinessResponse(checks, DateTimeOffset.UtcNow);
    }

    private async Task<ProductionReadinessCheckResponse> CheckEmailDeliveryAsync(
        CancellationToken cancellationToken)
    {
        var settings = await emailDeliverySettingsService.GetResolvedSettingsAsync(cancellationToken);
        var evidence = new List<string>();
        var missing = new List<string>();

        if (settings.Provider == EmailDeliverySettingsValidator.ResendApiProvider)
        {
            if (settings.ResendApiKeyConfigured)
            {
                evidence.Add("Resend API key is configured.");
            }
            else
            {
                missing.Add("Resend API key is not configured.");
            }

            AddConfiguredOrMissing(settings.ResendFromEmail, "Resend From email", evidence, missing);
            AddConfiguredOrMissing(settings.ReplyToEmail, "Reply-To email", evidence, missing);
        }
        else if (settings.Provider == EmailDeliverySettingsValidator.GmailSmtpProvider)
        {
            AddConfiguredOrMissing(settings.GmailAddress, "Gmail address", evidence, missing);
            if (settings.AppPasswordConfigured)
            {
                evidence.Add("Gmail App Password is configured.");
            }
            else
            {
                missing.Add("Gmail App Password is not configured.");
            }
        }
        else
        {
            evidence.Add("Email delivery is controlled by server configuration.");
        }

        if (settings.ConfigurationError is not null)
        {
            missing.Add(settings.ConfigurationError);
        }

        var ready = missing.Count == 0;
        return new ProductionReadinessCheckResponse(
            "emailDelivery",
            "Email delivery",
            ready ? "ready" : "warning",
            ready
                ? $"{settings.Provider} is configured for outbound email."
                : "Email delivery settings are incomplete.",
            evidence,
            missing);
    }

    private async Task<ProductionReadinessCheckResponse> CheckEmailOutboxAsync(
        CancellationToken cancellationToken)
    {
        var summary = await emailDeliveryLogService.GetOutboxMonitoringSummaryAsync(cancellationToken);
        var ready = summary.HealthStatus == "Healthy";
        return new ProductionReadinessCheckResponse(
            "emailOutbox",
            "Email outbox",
            ready ? "ready" : "warning",
            summary.SummaryMessage,
            [
                $"{summary.SentLast24HoursCount} sent in last 24h.",
                $"{summary.PendingCount} pending, {summary.RetryingCount} retrying, {summary.FailedCount} failed."
            ],
            ready ? [] : ["Review Admin Email Log for failed, retrying or stale pending messages."]);
    }

    private async Task<ProductionReadinessCheckResponse> CheckUploadSecurityAsync(
        CancellationToken cancellationToken)
    {
        var options = uploadSecurityOptions.Value.MalwareScanner;
        var evidence = new List<string>();
        var missing = new List<string>();

        if (!string.Equals(options.Provider, "ClamAV", StringComparison.OrdinalIgnoreCase))
        {
            missing.Add("Malware scanner provider is not ClamAV.");
        }
        else
        {
            evidence.Add("Malware scanner provider is ClamAV.");
        }

        if (string.IsNullOrWhiteSpace(options.ClamAv.Host))
        {
            missing.Add("ClamAV host is not configured.");
        }
        else
        {
            evidence.Add("ClamAV host is configured.");
        }

        if (options.ClamAv.Port is < 1 or > 65535)
        {
            missing.Add("ClamAV port is invalid.");
        }
        else
        {
            evidence.Add($"ClamAV port {options.ClamAv.Port} is configured.");
        }

        var latestCleanScan = await dbContext.UploadedFiles
            .AsNoTracking()
            .Where(file =>
                file.ScanStatus == UploadScanStatus.Clean &&
                file.ScanProvider != null &&
                file.ScanProvider.Contains("ClamAV"))
            .OrderByDescending(file => file.ScannedAt ?? file.UpdatedAt)
            .Select(file => file.ScannedAt ?? file.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestCleanScan != default)
        {
            evidence.Add($"Latest clean ClamAV upload scan: {latestCleanScan:O}.");
        }

        if (missing.Count == 0)
        {
            var probe = await RunClamAvProbeAsync(cancellationToken);
            if (probe)
            {
                evidence.Add("ClamAV clean-file probe completed successfully.");
            }
            else
            {
                missing.Add("ClamAV clean-file probe did not return Clean.");
            }
        }

        var ready = missing.Count == 0;
        return new ProductionReadinessCheckResponse(
            "uploadSecurity",
            "Upload security",
            ready ? "ready" : "warning",
            ready
                ? "ClamAV is configured and responded to a clean-file probe."
                : "ClamAV readiness is not fully verified.",
            evidence,
            missing);
    }

    private async Task<ProductionReadinessCheckResponse> CheckDnsEmailRecordsAsync(
        CancellationToken cancellationToken)
    {
        var evidence = new List<string>();
        var missing = new List<string>();

        await CheckTxtRecordAsync(
            $"resend._domainkey.{Domain}",
            value => !string.IsNullOrWhiteSpace(value),
            "Resend DKIM TXT record exists.",
            "TXT resend._domainkey.oksanalogosha.com was not found.",
            evidence,
            missing,
            cancellationToken);

        await CheckTxtRecordAsync(
            $"send.{Domain}",
            value => value.Contains("include:amazonses.com", StringComparison.OrdinalIgnoreCase),
            "SPF TXT for send.oksanalogosha.com includes amazonses.com.",
            "TXT send.oksanalogosha.com does not include amazonses.com.",
            evidence,
            missing,
            cancellationToken);

        await CheckMxRecordAsync(
            $"send.{Domain}",
            value => value.Contains("feedback-smtp", StringComparison.OrdinalIgnoreCase) &&
                     value.Contains("amazonses.com", StringComparison.OrdinalIgnoreCase),
            "MX send.oksanalogosha.com points to Amazon SES feedback SMTP.",
            "MX send.oksanalogosha.com does not point to Amazon SES feedback SMTP.",
            evidence,
            missing,
            cancellationToken);

        await CheckTxtRecordAsync(
            $"_dmarc.{Domain}",
            value => value.Trim('"').StartsWith("v=DMARC1", StringComparison.OrdinalIgnoreCase),
            "DMARC TXT record exists.",
            "TXT _dmarc.oksanalogosha.com starting with v=DMARC1 was not found.",
            evidence,
            missing,
            cancellationToken);

        var ready = missing.Count == 0;
        return new ProductionReadinessCheckResponse(
            "dnsEmailRecords",
            "DNS email records",
            ready ? "ready" : "warning",
            ready
                ? "Required Resend/Amazon SES and DMARC DNS records were found."
                : "One or more required sender DNS records are missing.",
            evidence,
            missing);
    }

    private async Task<bool> RunClamAvProbeAsync(CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"bespoke-clamav-probe-{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllTextAsync(tempPath, "Bespoke Sewing Studio ClamAV readiness probe.", cancellationToken);
            var result = await malwareScanner.ScanAsync(tempPath, cancellationToken);
            return result.Status == UploadScanStatus.Clean;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "ClamAV readiness probe failed.");
            return false;
        }
        finally
        {
            try
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
            catch
            {
                // Readiness probes must not fail because temp cleanup failed.
            }
        }
    }

    private async Task CheckTxtRecordAsync(
        string name,
        Func<string, bool> predicate,
        string success,
        string failure,
        ICollection<string> evidence,
        ICollection<string> missing,
        CancellationToken cancellationToken)
    {
        var records = await QueryDnsAsync(name, "TXT", cancellationToken);
        if (records.Any(predicate))
        {
            evidence.Add(success);
            return;
        }

        missing.Add(failure);
    }

    private async Task CheckMxRecordAsync(
        string name,
        Func<string, bool> predicate,
        string success,
        string failure,
        ICollection<string> evidence,
        ICollection<string> missing,
        CancellationToken cancellationToken)
    {
        var records = await QueryDnsAsync(name, "MX", cancellationToken);
        if (records.Any(predicate))
        {
            evidence.Add(success);
            return;
        }

        missing.Add(failure);
    }

    private async Task<IReadOnlyList<string>> QueryDnsAsync(
        string name,
        string type,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient("DnsOverHttps");
            var response = await client.GetFromJsonAsync<DnsJsonResponse>(
                $"dns-query?name={Uri.EscapeDataString(name)}&type={Uri.EscapeDataString(type)}",
                cancellationToken);

            return response?.Answer?
                .Select(answer => answer.Data)
                .Where(data => !string.IsNullOrWhiteSpace(data))
                .ToArray() ?? [];
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "DNS readiness lookup failed for {DnsName} {RecordType}.", name, type);
            return [];
        }
    }

    private static void AddConfiguredOrMissing(
        string? value,
        string label,
        ICollection<string> evidence,
        ICollection<string> missing)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            missing.Add($"{label} is not configured.");
        }
        else
        {
            evidence.Add($"{label} is configured.");
        }
    }

    private sealed record DnsJsonResponse(
        [property: JsonPropertyName("Answer")] IReadOnlyList<DnsJsonAnswer>? Answer);

    private sealed record DnsJsonAnswer(
        [property: JsonPropertyName("data")] string Data);
}
