using FluentValidation;

namespace booking_service.Application.Features.CatalogFeatures.Commands.PublishFareProduct;

public sealed class PublishFareProductCommandValidator : AbstractValidator<PublishFareProductCommand>
{
    public PublishFareProductCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Version).GreaterThan(0);
        RuleFor(command => command.CabinClass).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Brand).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Baggage).NotNull().ChildRules(baggage =>
        {
            baggage.RuleFor(allowance => allowance.IncludedPieces).InclusiveBetween(0, 10);
            baggage.RuleFor(allowance => allowance.MaxExtraPieces).InclusiveBetween(0, 10);
            baggage.RuleFor(allowance => allowance.MaxWeightPerPieceKg).GreaterThan(0).LessThanOrEqualTo(100);
            baggage.RuleFor(allowance => allowance.ExtraPiecePrice)
                .InclusiveBetween(0, 1_000_000)
                .PrecisionScale(18, 4, false);
        });
    }
}
