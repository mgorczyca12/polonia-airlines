using booking_service.Application.Contracts.Requests;
using booking_service.Domain.ValueObjects;
using FluentValidation;

namespace booking_service.Application.Features.ReservationFeatures.Queries.Validators;

public sealed class GetReservationRequestValidator : AbstractValidator<GetReservationRequest>
{
    public GetReservationRequestValidator()
    {
        RuleFor(request => request.ReservationId).GreaterThan(0);
        RuleFor(request => request.GuestToken)
            .Must(GuestToken.IsWellFormed)
            .WithMessage("A valid guest token is required.");
    }
}
