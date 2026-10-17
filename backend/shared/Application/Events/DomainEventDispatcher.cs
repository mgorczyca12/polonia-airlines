using Shared.Application.Abstractions;
using Shared.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Application.Events;

public sealed class DomainEventDispatcher(IEnumerable<IDomainEventHandlerAdapter> handlers)
    : IDomainEventDispatcher
{
    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (var handler in handlers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}

public interface IDomainEventHandlerAdapter
{
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}

internal sealed class DomainEventHandlerAdapter<TEvent, THandler>(IServiceProvider services)
    : IDomainEventHandlerAdapter
    where TEvent : IDomainEvent
    where THandler : class, IDomainEventHandler<TEvent>
{
    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        if (domainEvent is TEvent typedEvent)
        {
            var handler = services.GetRequiredService<THandler>();
            await handler.HandleAsync(typedEvent, cancellationToken);
        }
    }
}
