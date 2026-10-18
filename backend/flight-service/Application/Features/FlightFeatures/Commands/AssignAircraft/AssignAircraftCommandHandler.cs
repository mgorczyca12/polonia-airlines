using DispatchR.Abstractions.Send;
using flight_service.Application.Abstractions;
using flight_service.Application.Features.ConfigurationFeatures.Errors;
using flight_service.Domain.ValueObjects;
using FluentResults;

namespace flight_service.Application.Features.FlightFeatures.Commands.AssignAircraft;

public sealed class AssignAircraftCommandHandler(
    IFlightRepository flights,
    IAircraftRepository aircrafts) : IRequestHandler<AssignAircraftCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(AssignAircraftCommand request, CancellationToken cancellationToken)
    {
        var flight = await flights.GetByIdAsync(request.FlightId, cancellationToken);
        if (flight is null)
        {
            return Result.Fail(ConfigurationError.Missing("flight"));
        }

        var aircraft = await aircrafts.GetForAssignmentAsync(request.AircraftId, cancellationToken);
        if (aircraft is null)
        {
            return Result.Fail(ConfigurationError.Missing("aircraft"));
        }

        if (aircraft.OperationalState.Code != AircraftOperationalState.Active.Code
            || !aircraft.AircraftConfiguration.IsActive)
        {
            return Result.Fail(ConfigurationError.Conflict(
                "aircraft_unavailable", "Assignment requires an active aircraft and configuration."));
        }

        if (aircraft.AircraftTypeId != aircraft.AircraftConfiguration.AircraftTypeId)
        {
            return Result.Fail(ConfigurationError.Conflict(
                "aircraft_configuration_mismatch", "The aircraft configuration does not match its aircraft type."));
        }

        flight.AssignAircraft(aircraft);
        return Result.Ok();
    }
}
