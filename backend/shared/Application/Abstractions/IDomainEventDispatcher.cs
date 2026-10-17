using Shared.Domain.Events;

namespace Shared.Application.Abstractions;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
