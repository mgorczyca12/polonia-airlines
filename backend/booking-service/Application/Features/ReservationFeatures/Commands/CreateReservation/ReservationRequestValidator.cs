using System.Net.Mail;
using booking_service.Application.Contracts.Requests;
using FluentValidation;

namespace booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;

public sealed class ReservationRequestValidator : AbstractValidator<ReservationRequest>
{
    private const int MaxPassengers = 9;

    public ReservationRequestValidator(TimeProvider clock)
    {
        RuleFor(request => request.OfferId).NotEmpty();
        RuleFor(request => request.RequestId).NotEmpty().MaximumLength(100);
        RuleFor(request => request.ContactName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.ContactEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(254)
            .Must(IsCanonicalAddress)
            .WithMessage("A valid contact email address is required.");
        RuleFor(request => request.Passengers)
            .NotEmpty()
            .Must(passengers => passengers is null || passengers.Length <= MaxPassengers);
        RuleForEach(request => request.Passengers).NotNull().ChildRules(passenger =>
        {
            passenger.RuleFor(item => item.FirstName).NotEmpty().MaximumLength(100);
            passenger.RuleFor(item => item.LastName).NotEmpty().MaximumLength(100);
            passenger.RuleFor(item => item.DateOfBirth)
                .NotEmpty()
                .LessThanOrEqualTo(_ => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));
        });
    }

    private static bool IsCanonicalAddress(string? email)
    {
        return email is not null
            && MailAddress.TryCreate(email.Trim(), out var parsed)
            && parsed.Address == email.Trim();
    }
}
