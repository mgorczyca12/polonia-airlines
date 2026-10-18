using FluentResults;
using Shared.Application.Commands;

namespace flight_service.Application.Features.FlightFeatures.Commands.AssignAircraft;

/// <summary>Assigns or replaces a flight's aircraft.</summary>
public sealed record AssignAircraftCommand(int FlightId, int AircraftId)
    : ITransactionalCommand<AssignAircraftCommand, Result>;
