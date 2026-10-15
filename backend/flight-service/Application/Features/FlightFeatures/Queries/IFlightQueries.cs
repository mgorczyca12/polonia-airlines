using flight_service.Application.Contracts.Requests;
using flight_service.Application.Contracts.Responses;
using FluentResults;

namespace flight_service.Application.Features.FlightFeatures.Queries;

public interface IFlightQueries
{
    Task<Result<IReadOnlyList<FlightResponse>>> GetFlights(
        SearchFlightsRequest request,
        CancellationToken cancellationToken);

    Task<Result<FlightResponse>> GetFlight(
        int flightId,
        CancellationToken cancellationToken);
}
