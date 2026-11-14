using booking_service.Application.Contracts.Requests;
using booking_service.Domain.ValueObjects;
using FluentValidation;

namespace booking_service.Application.Features.ReservationFeatures.Queries.Validators;

public sealed class GetReservationByRequestValidator : AbstractValidator<GetReservationByRequest>
{
    public GetReservationByRequestValidator()
    {
        RuleFor(request => request.RequestId).NotEmpty().MaximumLength(100);
        RuleFor(request => request.GuestToken)
            .Must(GuestToken.IsWellFormed)
            .WithMessage("A valid guest token is required.");
    }
}
