using FluentValidation;

namespace flight_service.Application.Features.ConfigurationFeatures.Queries.Validators;

public sealed class LayoutIdValidator : AbstractValidator<int>
{
    public LayoutIdValidator()
    {
        RuleFor(id => id).GreaterThan(0);
    }
}
