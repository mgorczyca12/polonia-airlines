using FluentValidation;

namespace booking_service.Application.Features.CatalogFeatures.Commands.SetFlightPrice;

public sealed class SetFlightPriceCommandValidator : AbstractValidator<SetFlightPriceCommand>
{
    public SetFlightPriceCommandValidator()
    {
        RuleFor(command => command.FlightId).NotEmpty().MaximumLength(200);
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.CabinId).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Currency).NotEmpty().Matches("^[A-Z]{3}$");
        RuleFor(command => command.Amount).InclusiveBetween(0, 1_000_000).PrecisionScale(18, 4, false);
    }
}
