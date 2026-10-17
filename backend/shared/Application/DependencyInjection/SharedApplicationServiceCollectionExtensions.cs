using System.Reflection;
using DispatchR.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Application.Abstractions;
using Shared.Application.Events;
using Shared.Application.Pipelines;
using Shared.Domain.Events;

namespace Shared.Application.DependencyInjection;

public static class SharedApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddDomainEventHandler<TEvent, THandler>(
        this IServiceCollection services)
        where TEvent : IDomainEvent
        where THandler : class, IDomainEventHandler<TEvent>
    {
        services.TryAddScoped<THandler>();
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IDomainEventHandlerAdapter, DomainEventHandlerAdapter<TEvent, THandler>>());
        return services;
    }

    public static IServiceCollection AddSharedApplication(
        this IServiceCollection services,
        Assembly applicationAssembly)
    {
        ArgumentNullException.ThrowIfNull(applicationAssembly);

        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddScoped<CommandExecutionState>();
        services.AddDispatchR(options =>
        {
            options.Assemblies.Add(typeof(SharedApplicationServiceCollectionExtensions).Assembly);
            if (applicationAssembly != typeof(SharedApplicationServiceCollectionExtensions).Assembly)
            {
                options.Assemblies.Add(applicationAssembly);
            }

            options.RegisterPipelines = true;
            options.RegisterNotifications = true;
            options.PipelineOrder =
            [
                typeof(ValidationBehavior<,>),
                typeof(ExecutionStrategyBehavior<,>),
                typeof(TransactionBehavior<,>)
            ];
        });

        return services;
    }
}
