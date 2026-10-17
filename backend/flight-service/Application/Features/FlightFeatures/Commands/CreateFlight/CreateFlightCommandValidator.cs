using FluentValidation;

namespace flight_service.Application.Features.FlightFeatures.Commands.CreateFlight;

public sealed class CreateFlightCommandValidator : AbstractValidator<CreateFlightCommand>
{
    public CreateFlightCommandValidator()
    {
        RuleFor(x => x.FlightNumber).NotEmpty();
        RuleFor(x => x.DepartureAirport).NotEmpty();
        RuleFor(x => x.ArrivalAirport).NotEmpty();
        RuleFor(x => x.DepartureTime).LessThan(x => x.ArrivalTime);
        RuleFor(x => x.ArrivalTime).GreaterThan(x => x.DepartureTime);
    }
}
