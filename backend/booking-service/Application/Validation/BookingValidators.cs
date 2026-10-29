using booking_service.Application.Contracts;
using booking_service.Application.Features.Checkout;
using booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;
using FluentValidation;
using System.Net.Mail;

namespace booking_service.Application.Validation;

public sealed class OfferRequestValidator : AbstractValidator<OfferRequest>
{
    public OfferRequestValidator()
    {
        RuleFor(x => x.Currency).Matches("^[A-Z]{3}$");
        RuleFor(x => x.PassengerCount).InclusiveBetween(1, 9);
        RuleFor(x => x.Selections).NotEmpty().Must(x => x is null || x.Length <= 72);
        RuleForEach(x => x.Selections).NotNull().ChildRules(selection =>
        {
            selection.RuleFor(x => x.FlightId).NotEmpty().MaximumLength(200);
            selection.RuleFor(x => x.ProductId).NotEmpty();
            selection.RuleFor(x => x.PassengerIndex).GreaterThanOrEqualTo(0).When(x => x.PassengerIndex.HasValue);
        });
        RuleFor(x => x).Must(HasCompleteSelections)
            .WithMessage("Select exactly one product per passenger per flight, with at most eight distinct flights.");
    }

    private static bool HasCompleteSelections(OfferRequest request)
    {
        if (request.Selections is null || request.Selections.Any(x => x is null) ||
            request.PassengerCount is < 1 or > 9)
        {
            return false;
        }

        var flights = request.Selections.Select(x => x.FlightId).Distinct(StringComparer.Ordinal).ToArray();
        if (flights.Length is < 1 or > 8 ||
            request.Selections.Any(x => x.PassengerIndex >= request.PassengerCount))
        {
            return false;
        }

        return flights.All(flight => Enumerable.Range(0, request.PassengerCount)
            .All(index => request.Selections.Count(x => x.FlightId == flight &&
                (!x.PassengerIndex.HasValue || x.PassengerIndex == index)) == 1));
    }
}

public sealed class ReservationRequestValidator : AbstractValidator<ReservationRequest>
{
    public ReservationRequestValidator(TimeProvider clock)
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.RequestId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ContactName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.ContactEmail).NotEmpty().EmailAddress().MaximumLength(254)
            .Must(email => email is not null && MailAddress.TryCreate(email.Trim(), out var parsed) &&
                parsed.Address == email.Trim())
            .WithMessage("A valid contact email address is required.");
        RuleFor(x => x.Passengers).NotEmpty().Must(x => x is null || x.Length <= 9);
        RuleForEach(x => x.Passengers).NotNull().ChildRules(passenger =>
        {
            passenger.RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
            passenger.RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
            passenger.RuleFor(x => x.DateOfBirth).NotEmpty()
                .LessThanOrEqualTo(_ => DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime));
        });
    }

}

public sealed record BookingAccessRequest(string GuestToken, int? ReservationId = null, Guid? OfferId = null);

public sealed class BookingAccessValidator : AbstractValidator<BookingAccessRequest>
{
    public BookingAccessValidator()
    {
        RuleFor(x => x.GuestToken).NotEmpty().Matches("^[A-F0-9]{64}$");
        RuleFor(x => x.ReservationId).GreaterThan(0).When(x => x.ReservationId.HasValue);
        RuleFor(x => x.OfferId).NotEqual(Guid.Empty).When(x => x.OfferId.HasValue);
    }
}

public sealed class CreateOfferValidator : AbstractValidator<CreateOfferCommand>
{
    public CreateOfferValidator(OfferRequestValidator validator)
    {
        RuleFor(x => x.GuestToken).Matches("^[A-F0-9]{64}$");
        RuleFor(x => x.Request).NotNull().SetValidator(validator);
    }
}

public sealed class CreateReservationValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationValidator(ReservationRequestValidator validator)
    {
        RuleFor(x => x.GuestToken).Matches("^[A-F0-9]{64}$");
        RuleFor(x => x.Request).NotNull().SetValidator(validator);
    }
}
