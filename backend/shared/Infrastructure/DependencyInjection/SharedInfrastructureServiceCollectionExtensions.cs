using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Abstractions;
using Shared.Infrastructure.Messaging;
using Shared.Infrastructure.Persistence;

namespace Shared.Infrastructure.DependencyInjection;

public static class SharedInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddSharedUnitOfWork<TContext>(this IServiceCollection services)
        where TContext : DbContext
    {
        services.AddScoped<IUnitOfWork, EfUnitOfWork<TContext>>();
        services.AddScoped<ICommandExecutionStrategy, EfCommandExecutionStrategy<TContext>>();
        return services;
    }

    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IIntegrationEventPublisher, MassTransitIntegrationEventPublisher>();
        return services;
    }
}
