using FluentResults;
using Shared.Application.Commands;

namespace flight_service.Application.Features.FlightFeatures.Commands.CreateFlight;

public sealed record CreateFlightCommand(
    string FlightNumber,
    string DepartureAirport,
    string ArrivalAirport,
    DateTime DepartureTime,
    DateTime ArrivalTime)
    : IRetryableCommand<CreateFlightCommand, Result>;
