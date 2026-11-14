using booking_service.Application.Contracts.Requests;
using FluentValidation;

namespace booking_service.Application.Features.CatalogFeatures.Queries.Validators;

public sealed class SearchFaresRequestValidator : AbstractValidator<SearchFaresRequest>
{
    public SearchFaresRequestValidator()
    {
        RuleFor(request => request.Currency).NotEmpty().Matches("^[A-Z]{3}$");
        RuleFor(request => request.FlightIds)
            .NotEmpty()
            .Must(ids => ids is null || ids.Length <= 100)
            .WithMessage("At most one hundred flight references are supported.")
            .Must(ids => ids is null || ids.Distinct(StringComparer.Ordinal).Count() == ids.Length)
            .WithMessage("Flight references must be distinct.");
        RuleForEach(request => request.FlightIds).NotEmpty().MaximumLength(200);
    }
}
