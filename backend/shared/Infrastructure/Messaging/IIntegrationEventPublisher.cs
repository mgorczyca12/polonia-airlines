using Shared.Integration.Contracts;

namespace Shared.Infrastructure.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent;
}