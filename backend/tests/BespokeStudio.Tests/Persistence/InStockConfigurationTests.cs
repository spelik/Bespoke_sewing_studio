using BespokeStudio.Domain.Entities;
using BespokeStudio.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BespokeStudio.Tests.Persistence;

public sealed class InStockConfigurationTests
{
    [Fact]
    public void InStockItem_HasFilteredUniqueSlugAndPricePrecision()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(InStockItem));
        Assert.NotNull(entity);

        var slugIndex = entity.GetIndexes()
            .Single(index => index.Properties.Any(property => property.Name == nameof(InStockItem.Slug)));
        Assert.True(slugIndex.IsUnique);
        Assert.Equal("\"ArchivedAt\" IS NULL", slugIndex.GetFilter());

        var price = entity.FindProperty(nameof(InStockItem.Price));
        Assert.NotNull(price);
        Assert.Equal(12, price.GetPrecision());
        Assert.Equal(2, price.GetScale());

        Assert.Contains(
            entity.GetIndexes(),
            index => index.Properties.Select(property => property.Name).SequenceEqual(
            [
                nameof(InStockItem.IsPublished),
                nameof(InStockItem.DisplayOrder),
                nameof(InStockItem.CreatedAt)
            ]));
    }

    [Fact]
    public void InStockItemImage_HasItemDisplayOrderIndexAndRestrictFileDelete()
    {
        using var context = CreateContext();
        var entity = context.Model.FindEntityType(typeof(InStockItemImage));
        Assert.NotNull(entity);

        Assert.Contains(
            entity.GetIndexes(),
            index => index.Properties.Select(property => property.Name).SequenceEqual(
            [
                nameof(InStockItemImage.InStockItemId),
                nameof(InStockItemImage.DisplayOrder)
            ]));

        var fileFk = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Any(property => property.Name == nameof(InStockItemImage.UploadedFileId)));
        Assert.Equal(DeleteBehavior.Restrict, fileFk.DeleteBehavior);

        var itemFk = entity.GetForeignKeys()
            .Single(fk => fk.Properties.Any(property => property.Name == nameof(InStockItemImage.InStockItemId)));
        Assert.Equal(DeleteBehavior.Cascade, itemFk.DeleteBehavior);
    }

    private static BespokeStudioDbContext CreateContext()
    {
        // Use Npgsql provider only to materialize the relational model; no database connection is opened.
        var options = new DbContextOptionsBuilder<BespokeStudioDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=bespoke_studio_model_probe;Username=probe;Password=probe")
            .Options;
        return new BespokeStudioDbContext(options);
    }
}
