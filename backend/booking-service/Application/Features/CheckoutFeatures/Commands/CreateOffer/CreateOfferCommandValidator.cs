using booking_service.Domain.ValueObjects;
using FluentValidation;

namespace booking_service.Application.Features.CheckoutFeatures.Commands.CreateOffer;

public sealed class CreateOfferCommandValidator : AbstractValidator<CreateOfferCommand>
{
    public CreateOfferCommandValidator()
    {
        RuleFor(command => command.GuestToken)
            .Must(GuestToken.IsWellFormed)
            .WithMessage("A valid guest token is required.");
        RuleFor(command => command.Request).NotNull().SetValidator(new OfferRequestValidator());
    }
}
