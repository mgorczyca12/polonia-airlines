using flight_service.Application.Contracts.Responses;
using FluentResults;

namespace flight_service.Application.Features.ConfigurationFeatures.Queries;

public interface IConfigurationQueries
{
    Task<Result<ConfigurationResponse>> GetConfiguration(int configurationId, CancellationToken cancellationToken);
    Task<Result<FlightSeatMapResponse>> GetFlightSeatMap(int flightId, CancellationToken cancellationToken);
}
