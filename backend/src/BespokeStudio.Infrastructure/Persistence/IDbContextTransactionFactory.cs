using Microsoft.EntityFrameworkCore.Storage;

namespace BespokeStudio.Infrastructure.Persistence;

public interface IDbContextTransactionFactory
{
    bool SupportsTransactions { get; }

    Task<IDbContextTransaction?> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
