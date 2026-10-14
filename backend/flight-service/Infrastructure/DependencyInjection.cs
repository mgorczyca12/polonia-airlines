using flight_service.Application.Features.FlightFeatures.Queries;
using flight_service.Infrastructure.Persistence;
using flight_service.Infrastructure.Persistence.Queries;

namespace flight_service.Infrastructure;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<FlightServiceDbContext>();
        builder.Services.AddScoped<IFlightQueries, FlightQueries>();
        return builder;
    }
}
