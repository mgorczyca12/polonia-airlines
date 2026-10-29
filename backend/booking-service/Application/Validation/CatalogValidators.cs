using booking_service.Application.Features.Catalog;
using FluentValidation;

namespace booking_service.Application.Validation;

public sealed class PublishFareProductValidator : AbstractValidator<PublishFareProductCommand>
{
    public PublishFareProductValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Version).GreaterThan(0);
        RuleFor(x => x.CabinClass).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Baggage).NotNull().ChildRules(baggage =>
        {
            baggage.RuleFor(x => x.IncludedPieces).InclusiveBetween(0, 10);
            baggage.RuleFor(x => x.MaxExtraPieces).InclusiveBetween(0, 10);
            baggage.RuleFor(x => x.MaxWeightPerPieceKg).GreaterThan(0).LessThanOrEqualTo(100);
            baggage.RuleFor(x => x.ExtraPiecePrice).InclusiveBetween(0, 1_000_000).PrecisionScale(18, 4, false);
        });
    }
}

public sealed class SetFlightPriceValidator : AbstractValidator<SetFlightPriceCommand>
{
    public SetFlightPriceValidator()
    {
        RuleFor(x => x.FlightId).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.CabinId).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Currency).NotEmpty().Matches("^[A-Z]{3}$");
        RuleFor(x => x.Amount).InclusiveBetween(0, 1_000_000).PrecisionScale(18, 4, false);
    }
}

public sealed class RetireFareProductValidator : AbstractValidator<RetireFareProductCommand>
{
    public RetireFareProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
