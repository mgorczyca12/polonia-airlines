using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Application.Abstractions;

namespace Shared.Infrastructure.Persistence;

public sealed class EfUnitOfWork<TContext>(TContext dbContext) : IUnitOfWork
    where TContext : DbContext
{
    private IDbContextTransaction? transaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (transaction is not null || dbContext.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active.");
        }

        transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        var activeTransaction = transaction
            ?? throw new InvalidOperationException("No transaction is active.");

        await activeTransaction.CommitAsync(cancellationToken);
        transaction = null;
        await activeTransaction.DisposeAsync();
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        var activeTransaction = transaction;
        if (activeTransaction is null)
        {
            return;
        }

        try
        {
            await activeTransaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            transaction = null;
            await activeTransaction.DisposeAsync();
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}
