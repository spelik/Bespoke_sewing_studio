using BespokeStudio.Application.Abstractions;
using BespokeStudio.Infrastructure.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

public sealed class EmailOutboxRetentionWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOutboxRetentionOptions> options,
    ILogger<EmailOutboxRetentionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.WorkerEnabled)
        {
            TryLog(() => logger.LogInformation(
                "Email outbox retention worker is disabled by configuration."));
            return;
        }

        var delay = TimeSpan.FromHours(options.Value.WorkerIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var retentionService = scope.ServiceProvider
                    .GetRequiredService<IEmailOutboxRetentionService>();
                var result = await retentionService.RunCleanupAsync(stoppingToken);

                if (result.SucceededBodyPurgedCount > 0
                    || result.SkippedBodyPurgedCount > 0
                    || result.SucceededDeletedCount > 0
                    || result.SkippedDeletedCount > 0)
                {
                    TryLog(() => logger.LogInformation(
                        "Email outbox retention worker purged {SucceededBodyPurgedCount} succeeded and {SkippedBodyPurgedCount} skipped bodies; deleted {SucceededDeletedCount} succeeded and {SkippedDeletedCount} skipped messages.",
                        result.SucceededBodyPurgedCount,
                        result.SkippedBodyPurgedCount,
                        result.SucceededDeletedCount,
                        result.SkippedDeletedCount));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                TryLog(() => logger.LogWarning(
                    exception,
                    "Email outbox retention worker cycle failed."));
            }

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private static void TryLog(Action write)
    {
        try
        {
            write();
        }
        catch
        {
            // Keep the worker alive even if an optional logging provider is unavailable.
        }
    }
}
