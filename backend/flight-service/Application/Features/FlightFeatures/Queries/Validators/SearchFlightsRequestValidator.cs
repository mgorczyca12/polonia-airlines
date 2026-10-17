using flight_service.Application.Contracts.Requests;
using FluentValidation;

namespace flight_service.Application.Features.FlightFeatures.Queries.Validators;

public sealed class SearchFlightsRequestValidator : AbstractValidator<SearchFlightsRequest>
{
    public SearchFlightsRequestValidator()
    {
        RuleFor(request => request.DepartureAirport)
            .NotEmpty().Matches(@"\A[A-Z]{3}\z");
        RuleFor(request => request.ArrivalAirport)
            .NotEmpty().Matches(@"\A[A-Z]{3}\z");
        RuleFor(request => request.DepartureDate)
            .NotEmpty().LessThan(DateOnly.MaxValue);
        RuleFor(request => request.Page).GreaterThan(0);
        RuleFor(request => request.PageSize).InclusiveBetween(1, 100);
        RuleFor(request => request.Page)
            .Must((request, page) => page <= 0 || request.PageSize <= 0
                || (long)(page - 1) * request.PageSize <= int.MaxValue)
            .WithMessage("The requested page exceeds the supported pagination range.");
    }
}
