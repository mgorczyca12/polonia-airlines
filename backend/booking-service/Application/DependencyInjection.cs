using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Application.DependencyInjection;

namespace booking_service.Application;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSharedApplication(typeof(DependencyInjection).Assembly);
        return builder;
    }
}
