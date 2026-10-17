using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Application.Abstractions;
using Shared.Application.Events;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.DependencyInjection;

public static class SharedInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSharedUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.TryAddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork<TContext>>();
        services.AddScoped<ICommandExecutionStrategy, EfCommandExecutionStrategy<TContext>>();
        return services;
    }

    public static void AddSharedEntityFrameworkOutbox<TContext>(
        this IBusRegistrationConfigurator configurator,
        Action<IEntityFrameworkOutboxConfigurator> configureDatabase)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configureDatabase);

        configurator.AddEntityFrameworkOutbox<TContext>(outbox =>
        {
            configureDatabase(outbox);
            outbox.UseBusOutbox();
        });
    }

    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        return services;
    }
}
