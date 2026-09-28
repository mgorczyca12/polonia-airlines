using MassTransit;
using Shared.Integration.Contracts;

namespace Shared.Infrastructure.Messaging;

public sealed class MassTransitIntegrationEventPublisher(IPublishEndpoint publishEndpoint)
    : IIntegrationEventPublisher
{
    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : class, IIntegrationEvent =>
        publishEndpoint.Publish(integrationEvent, cancellationToken);
}