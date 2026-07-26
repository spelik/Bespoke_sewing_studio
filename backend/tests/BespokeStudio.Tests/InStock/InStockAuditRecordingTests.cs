using BespokeStudio.Application.Abstractions;
using BespokeStudio.Application.Contracts.AdminAuditLog;
using BespokeStudio.Application.Contracts.Common;
using BespokeStudio.Domain.Entities;
using BespokeStudio.Infrastructure.Persistence;
using BespokeStudio.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Tests.InStock;

public sealed class InStockAuditRecordingTests
{
    [Theory]
    [InlineData("in_stock.created", "InStockItem")]
    [InlineData("in_stock.updated", "InStockItem")]
    [InlineData("in_stock.archived", "InStockItem")]
    [InlineData("in_stock.restored", "InStockItem")]
    [InlineData("in_stock.image_uploaded", "InStockItemImage")]
    [InlineData("in_stock.image_deleted", "InStockItemImage")]
    public async Task AdminAuditLogService_PersistsInStockActionsWithoutFileContents(
        string action,
        string entityType)
    {
        await using var db = CreateDb();
        var audit = new AdminAuditLogService(db);

        await audit.RecordAsync(new AdminAuditLogWriteRequest(
            Guid.NewGuid(),
            "owner@example.com",
            action,
            entityType,
            Guid.NewGuid().ToString("D"),
            "Sample piece",
            $"Audit for {action}."));

        var entry = await db.AdminAuditLogEntries.SingleAsync();
        Assert.Equal(action, entry.Action);
        Assert.Equal(entityType, entry.EntityType);
        Assert.Equal("owner@example.com", entry.ActorEmail);
        Assert.DoesNotContain("FF D8", entry.Summary, StringComparison.OrdinalIgnoreCase);
        Assert.Null(entry.MetadataJson);
    }

    private static BespokeStudioDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<BespokeStudioDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new BespokeStudioDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
