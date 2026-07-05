using BespokeStudio.Domain.Entities;
using BespokeStudio.Domain.Enums;
using BespokeStudio.Infrastructure.Notifications;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BespokeStudio.Tests.Integration.PostgreSql;

[Collection(PostgreSqlIntegrationTestCollection.Name)]
public sealed class PostgreSqlPersistenceIntegrationTests
{
    private const string PurgedBodyPlaceholder = "[Email body purged by retention policy.]";

    [PostgreSqlIntegrationFact]
    public async Task Migrations_ApplyToFreshPostgresDatabase()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();

        await context.Database.MigrateAsync();

        Assert.True(await context.Database.CanConnectAsync());
        Assert.Equal(0, await context.EmailDeliveryLogEntries.CountAsync());
        Assert.Equal(0, await context.EmailOutboxMessages.CountAsync());
    }

    [PostgreSqlIntegrationFact]
    public async Task EmailOutbox_RoundTripsEnumBodyAndLogReference()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.MigrateAsync();

            var logEntry = CreateLogEntry("roundtrip");
            setupContext.EmailDeliveryLogEntries.Add(logEntry);
            setupContext.EmailOutboxMessages.Add(new EmailOutboxMessage
            {
                MessageType = "integration_test",
                RecipientEmail = "integration-roundtrip@example.com",
                Subject = "Round-trip test",
                HtmlBody = "<p>Integration body</p>",
                TextBody = "Integration body",
                Status = EmailOutboxStatus.Pending,
                EmailDeliveryLogEntryId = logEntry.Id
            });
            await setupContext.SaveChangesAsync();
        }

        await using var verifyContext = database.CreateContext();
        var message = await verifyContext.EmailOutboxMessages
            .SingleAsync(item => item.RecipientEmail == "integration-roundtrip@example.com");

        Assert.Equal(EmailOutboxStatus.Pending, message.Status);
        Assert.Equal("<p>Integration body</p>", message.HtmlBody);
        Assert.Equal("Integration body", message.TextBody);
        Assert.NotNull(message.EmailDeliveryLogEntryId);

        var linkedLog = await verifyContext.EmailDeliveryLogEntries
            .SingleAsync(entry => entry.Id == message.EmailDeliveryLogEntryId);
        Assert.Equal("integration-roundtrip@example.com", linkedLog.RecipientEmail);
    }

    [PostgreSqlIntegrationFact]
    public async Task EmailOutbox_BodyCheckConstraintRejectsEmptyBody()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();

        context.EmailOutboxMessages.Add(new EmailOutboxMessage
        {
            MessageType = "integration_test",
            RecipientEmail = "integration-empty-body@example.com",
            Subject = "Empty body constraint test",
            HtmlBody = null,
            TextBody = null,
            Status = EmailOutboxStatus.Pending
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [PostgreSqlIntegrationFact]
    public async Task EmailOutboxRetentionService_RunCleanupOnPostgres_PurgesDeletesAndRetainsFailed()
    {
        await using var database = await PostgreSqlTestDatabase.CreateAsync();
        var now = DateTimeOffset.UtcNow;

        Guid succeededLogId;
        Guid failedLogId;
        Guid skippedLogId;
        Guid succeededMessageId;
        Guid failedMessageId;

        await using (var setupContext = database.CreateContext())
        {
            await setupContext.Database.MigrateAsync();

            var succeededLog = CreateLogEntry("succeeded");
            var skippedLog = CreateLogEntry("skipped");
            var failedLog = CreateLogEntry("failed");

            setupContext.EmailDeliveryLogEntries.AddRange(succeededLog, skippedLog, failedLog);

            var succeededMessage = new EmailOutboxMessage
            {
                MessageType = "integration_test",
                RecipientEmail = "integration-succeeded@example.com",
                Subject = "Succeeded retention test",
                HtmlBody = "<p>Succeeded body</p>",
                TextBody = "Succeeded body",
                Status = EmailOutboxStatus.Succeeded,
                SentAt = now.AddDays(-45),
                CreatedAt = now.AddDays(-45),
                UpdatedAt = now.AddDays(-45),
                EmailDeliveryLogEntryId = succeededLog.Id
            };

            var skippedMessage = new EmailOutboxMessage
            {
                MessageType = "integration_test",
                RecipientEmail = "integration-skipped@example.com",
                Subject = "Skipped retention test",
                HtmlBody = null,
                TextBody = "Skipped body",
                Status = EmailOutboxStatus.Skipped,
                CreatedAt = now.AddDays(-100),
                UpdatedAt = now.AddDays(-100),
                EmailDeliveryLogEntryId = skippedLog.Id
            };

            var failedMessage = new EmailOutboxMessage
            {
                MessageType = "integration_test",
                RecipientEmail = "integration-failed@example.com",
                Subject = "Failed retention test",
                HtmlBody = "<p>Failed body</p>",
                TextBody = "Failed body",
                Status = EmailOutboxStatus.Failed,
                Attempts = 5,
                MaxAttempts = 5,
                CreatedAt = now.AddDays(-100),
                UpdatedAt = now.AddDays(-100),
                EmailDeliveryLogEntryId = failedLog.Id
            };

            setupContext.EmailOutboxMessages.AddRange(succeededMessage, skippedMessage, failedMessage);
            await setupContext.SaveChangesAsync();

            succeededLogId = succeededLog.Id;
            skippedLogId = skippedLog.Id;
            failedLogId = failedLog.Id;
            succeededMessageId = succeededMessage.Id;
            failedMessageId = failedMessage.Id;
        }

        await using (var cleanupContext = database.CreateContext())
        {
            var retentionService = new EmailOutboxRetentionService(
                cleanupContext,
                Options.Create(new EmailOutboxRetentionOptions
                {
                    BatchSize = 200,
                    SucceededBodyRetentionDays = 30,
                    SucceededMessageRetentionDays = 90,
                    SkippedBodyRetentionDays = 30,
                    SkippedMessageRetentionDays = 90,
                    PurgedBodyPlaceholder = PurgedBodyPlaceholder
                }));

            var result = await retentionService.RunCleanupAsync();

            Assert.Equal(1, result.SucceededBodyPurgedCount);
            Assert.Equal(0, result.SkippedBodyPurgedCount);
            Assert.Equal(0, result.SucceededDeletedCount);
            Assert.Equal(1, result.SkippedDeletedCount);
        }

        await using var verifyContext = database.CreateContext();

        var succeededAfterCleanup = await verifyContext.EmailOutboxMessages
            .SingleAsync(message => message.Id == succeededMessageId);
        Assert.Null(succeededAfterCleanup.HtmlBody);
        Assert.Equal(PurgedBodyPlaceholder, succeededAfterCleanup.TextBody);

        Assert.False(await verifyContext.EmailOutboxMessages
            .AnyAsync(message => message.RecipientEmail == "integration-skipped@example.com"));

        var failedAfterCleanup = await verifyContext.EmailOutboxMessages
            .SingleAsync(message => message.Id == failedMessageId);
        Assert.Equal("<p>Failed body</p>", failedAfterCleanup.HtmlBody);
        Assert.Equal("Failed body", failedAfterCleanup.TextBody);

        Assert.Equal(3, await verifyContext.EmailDeliveryLogEntries.CountAsync());
        Assert.True(await verifyContext.EmailDeliveryLogEntries.AnyAsync(entry => entry.Id == succeededLogId));
        Assert.True(await verifyContext.EmailDeliveryLogEntries.AnyAsync(entry => entry.Id == skippedLogId));
        Assert.True(await verifyContext.EmailDeliveryLogEntries.AnyAsync(entry => entry.Id == failedLogId));
    }

    private static EmailDeliveryLogEntry CreateLogEntry(string suffix) => new()
    {
        MessageType = "integration_test",
        RecipientEmail = $"integration-{suffix}@example.com",
        Subject = $"Integration test {suffix}",
        Provider = "Outbox",
        Status = "Sent",
        SentExternally = true,
        ResultMessage = "Integration test entry."
    };
}
