using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BespokeStudio.Infrastructure.Persistence;

public sealed class BespokeStudioDbContextTransactionFactory(BespokeStudioDbContext dbContext)
    : IDbContextTransactionFactory
{
    public bool SupportsTransactions => dbContext.Database.IsRelational();

    public async Task<IDbContextTransaction?> BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (!SupportsTransactions)
        {
            return null;
        }

        return await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }
}
