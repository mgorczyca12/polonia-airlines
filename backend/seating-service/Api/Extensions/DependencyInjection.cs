namespace seating_service.Api.Extensions;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApi(this WebApplicationBuilder builder)
    {
        builder.Services.AddHealthChecks();
        return builder;
    }
}
