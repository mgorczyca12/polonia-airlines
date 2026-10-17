using Shared.Domain.Entities;

namespace Shared.Application.Abstractions;

public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken);
    void Add(TAggregate aggregate);
    void AddRange(IEnumerable<TAggregate> aggregates);
    void Remove(TAggregate aggregate);
    void RemoveRange(IEnumerable<TAggregate> aggregates);
}
