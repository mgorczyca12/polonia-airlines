using FluentValidation;

namespace flight_service.Application.Features.FlightFeatures.Commands.CreateFlight;

public sealed class CreateFlightCommandValidator : AbstractValidator<CreateFlightCommand>
{
    public CreateFlightCommandValidator()
    {
        RuleFor(x => x.FlightNumber).NotEmpty();
        RuleFor(x => x.Departure).NotEmpty();
        RuleFor(x => x.Destination).NotEmpty();
        RuleFor(x => x.DepartureTime).LessThan(x => x.ArrivalTime);
        RuleFor(x => x.ArrivalTime).GreaterThan(x => x.DepartureTime);
    }
}