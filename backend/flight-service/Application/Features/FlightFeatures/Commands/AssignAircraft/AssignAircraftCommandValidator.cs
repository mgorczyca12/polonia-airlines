using FluentValidation;

namespace flight_service.Application.Features.FlightFeatures.Commands.AssignAircraft;

public sealed class AssignAircraftCommandValidator : AbstractValidator<AssignAircraftCommand>
{
    public AssignAircraftCommandValidator()
    {
        RuleFor(command => command.FlightId).GreaterThan(0);
        RuleFor(command => command.AircraftId).GreaterThan(0);
    }
}
