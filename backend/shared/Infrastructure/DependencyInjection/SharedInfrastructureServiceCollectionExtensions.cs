using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Messaging;

namespace Shared.Infrastructure.DependencyInjection;

public static class SharedInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        return services;
    }
}