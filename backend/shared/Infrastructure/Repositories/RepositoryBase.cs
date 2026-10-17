using Microsoft.EntityFrameworkCore;
using Shared.Application.Abstractions;
using Shared.Domain.Entities;

namespace Shared.Infrastructure.Repositories;

public abstract class RepositoryBase<TAggregate, TId> : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    protected DbSet<TAggregate> Entities { get; }

    protected RepositoryBase(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        Entities = dbContext.Set<TAggregate>();
    }

    public virtual async Task<TAggregate?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await Entities.FindAsync(new object[] { id }, cancellationToken);
    }

    public void Add(TAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        Entities.Add(aggregate);
    }

    public void AddRange(IEnumerable<TAggregate> aggregates)
    {
        ArgumentNullException.ThrowIfNull(aggregates);
        Entities.AddRange(aggregates);
    }

    public void Remove(TAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        Entities.Remove(aggregate);
    }

    public void RemoveRange(IEnumerable<TAggregate> aggregates)
    {
        ArgumentNullException.ThrowIfNull(aggregates);
        Entities.RemoveRange(aggregates);
    }
}
