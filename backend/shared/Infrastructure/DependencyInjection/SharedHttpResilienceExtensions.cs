using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace Shared.Infrastructure.DependencyInjection;

public static class SharedHttpResilienceExtensions
{
    public static IHttpClientBuilder AddSharedHttpResilience(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddStandardResilienceHandler(options =>
        {
            options.Retry.DisableForUnsafeHttpMethods();
        });

        return builder;
    }
}
