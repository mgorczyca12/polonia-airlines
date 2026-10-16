using System.Reflection;
using DispatchR.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application.Pipelines;

namespace Shared.Application.DependencyInjection;

public static class SharedApplicationServiceCollectionExtensions
{
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
