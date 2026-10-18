using Shared.Application.DependencyInjection;

namespace flight_service.Application;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddSharedApplication(typeof(DependencyInjection).Assembly);
        return builder;
    }
}
