using flight_service.Application.Contracts.Requests;
using flight_service.Application.Features.FlightFeatures.Queries;
using FluentValidation;

namespace flight_service.Application;

public static class DependencyInjection
{
    public static WebApplicationBuilder AddApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IValidator<SearchFlightsRequest>, SearchFlightsRequestValidator>();
        return builder;
    }
}
