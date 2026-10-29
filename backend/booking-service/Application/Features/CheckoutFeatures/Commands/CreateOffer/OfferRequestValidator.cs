using booking_service.Application.Contracts.Requests;
using FluentValidation;

namespace booking_service.Application.Features.CheckoutFeatures.Commands.CreateOffer;

public sealed class OfferRequestValidator : AbstractValidator<OfferRequest>
{
    private const int MaxPassengers = 9;
    private const int MaxFlights = 8;
    private const int MaxSelections = MaxPassengers * MaxFlights;

    public OfferRequestValidator()
    {
        RuleFor(request => request.Currency).NotEmpty().Matches("^[A-Z]{3}$");
        RuleFor(request => request.PassengerCount).InclusiveBetween(1, MaxPassengers);
        RuleFor(request => request.Selections)
            .NotEmpty()
            .Must(selections => selections is null || selections.Length <= MaxSelections);
        RuleForEach(request => request.Selections).NotNull().ChildRules(selection =>
        {
            selection.RuleFor(item => item.FlightId).NotEmpty().MaximumLength(200);
            selection.RuleFor(item => item.ProductId).NotEmpty();
            selection.RuleFor(item => item.PassengerIndex)
                .GreaterThanOrEqualTo(0)
                .When(item => item.PassengerIndex.HasValue);
        });
        RuleFor(request => request).Must(HasCompleteSelections)
            .WithMessage("Select exactly one product per passenger per flight, with at most eight distinct flights.");
    }

    private static bool HasCompleteSelections(OfferRequest request)
    {
        if (request.Selections is null
            || request.Selections.Any(selection => selection is null)
            || request.PassengerCount is < 1 or > MaxPassengers)
        {
            return false;
        }

        var flights = request.Selections
            .Select(selection => selection.FlightId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (flights.Length is < 1 or > MaxFlights
            || request.Selections.Any(selection => selection.PassengerIndex >= request.PassengerCount))
        {
            return false;
        }

        return flights.All(flight => Enumerable.Range(0, request.PassengerCount)
            .All(index => request.Selections.Count(selection => selection.FlightId == flight
                && (!selection.PassengerIndex.HasValue || selection.PassengerIndex == index)) == 1));
    }
}
