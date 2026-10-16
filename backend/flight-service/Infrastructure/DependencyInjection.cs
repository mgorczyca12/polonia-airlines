using flight_service.Application.Features.FlightFeatures.Queries;
using flight_service.Infrastructure.Persistence;
using flight_service.Infrastructure.Persistence.Queries;
using Shared.Infrastructure.DependencyInjection;

namespace flight_service.Infrastructure;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddInfrastructure(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<FlightServiceDbContext>();
        builder.Services.AddSharedUnitOfWork<FlightServiceDbContext>();
        builder.Services.AddScoped<IFlightQueries, FlightQueries>();
        return builder;
    }
}
