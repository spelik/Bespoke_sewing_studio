using BespokeStudio.Infrastructure.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Infrastructure.Services;

public sealed class EmailOutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOutboxOptions> options,
    ILogger<EmailOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(options.Value.WorkerIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IEmailOutboxProcessor>();
                var processed = await processor.ProcessDueAsync(stoppingToken);

                if (processed > 0)
                {
                    TryLog(() => logger.LogInformation(
                        "Email outbox worker processed {ProcessedCount} message(s).",
                        processed));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                TryLog(() => logger.LogError(exception, "Email outbox worker cycle failed."));
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
