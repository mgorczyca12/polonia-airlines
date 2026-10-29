using booking_service.Application.Contracts.Requests;
using booking_service.Domain.ValueObjects;
using FluentValidation;

namespace booking_service.Application.Features.CheckoutFeatures.Queries.Validators;

public sealed class GetOfferRequestValidator : AbstractValidator<GetOfferRequest>
{
    public GetOfferRequestValidator()
    {
        RuleFor(request => request.OfferId).NotEmpty();
        RuleFor(request => request.GuestToken)
            .Must(GuestToken.IsWellFormed)
            .WithMessage("A valid guest token is required.");
    }
}
