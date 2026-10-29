using FluentValidation;
using booking_service.Domain.ValueObjects;

namespace booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;

public sealed class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator(TimeProvider clock)
    {
        RuleFor(command => command.GuestToken)
            .Must(GuestToken.IsWellFormed)
            .WithMessage("A valid guest token is required.");
        RuleFor(command => command.Request).NotNull().SetValidator(new ReservationRequestValidator(clock));
    }
}
