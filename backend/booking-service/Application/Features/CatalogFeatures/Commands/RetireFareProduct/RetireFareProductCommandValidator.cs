using FluentValidation;

namespace booking_service.Application.Features.CatalogFeatures.Commands.RetireFareProduct;

public sealed class RetireFareProductCommandValidator : AbstractValidator<RetireFareProductCommand>
{
    public RetireFareProductCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
    }
}
