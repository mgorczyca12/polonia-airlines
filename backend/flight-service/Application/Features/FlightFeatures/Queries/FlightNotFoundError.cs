using FluentResults;

namespace flight_service.Application.Features.FlightFeatures.Queries;

public sealed class FlightNotFoundError(int flightId)
    : Error($"Flight '{flightId}' was not found.")
{
    public const string Code = "flight_not_found";
}
