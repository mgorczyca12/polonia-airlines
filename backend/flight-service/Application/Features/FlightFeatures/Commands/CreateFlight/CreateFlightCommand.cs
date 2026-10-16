using FluentResults;
using Shared.Application.Commands;

namespace flight_service.Application.Features.FlightFeatures.Commands.CreateFlight;

public sealed record CreateFlightCommand(
    string FlightNumber,
    string Departure,
    string Destination,
    DateTime DepartureTime,
    DateTime ArrivalTime)
    : ICommand<CreateFlightCommand, Result>;