using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Shared.Application.Abstractions;
using Shared.Domain.Events;

namespace Shared.Infrastructure.Persistence;

public sealed class EfUnitOfWork<TContext>(
    TContext dbContext,
    IDomainEventDispatcher domainEventDispatcher) : IUnitOfWork
    where TContext : DbContext
{
    private IDbContextTransaction? transaction;
    private bool saveInProgress;
    private bool saveFailed;

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
        if (saveInProgress || saveFailed)
        {
            throw new InvalidOperationException(
                "Saving cannot be nested or repeated after a failed save. Discard a failed command's scope.");
        }

        saveInProgress = true;
        var completed = false;
        try
        {
            await DispatchDomainEventsAsync(cancellationToken);
            var affectedRows = await dbContext.SaveChangesAsync(cancellationToken);
            completed = true;
            return affectedRows;
        }
        finally
        {
            saveInProgress = false;
            saveFailed = !completed;
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var aggregates = dbContext.ChangeTracker.Entries<IHasDomainEvents>()
                .Select(entry => entry.Entity)
                .Where(entity => entity.DomainEvents.Count > 0)
                .ToArray();

            if (aggregates.Length == 0)
            {
                break;
            }

            var events = aggregates.SelectMany(entity => entity.DomainEvents).ToArray();
            foreach (var aggregate in aggregates)
            {
                aggregate.ClearDomainEvents();
            }

            foreach (var domainEvent in events)
            {
                await domainEventDispatcher.DispatchAsync(domainEvent, cancellationToken);
            }
        }
    }
}
