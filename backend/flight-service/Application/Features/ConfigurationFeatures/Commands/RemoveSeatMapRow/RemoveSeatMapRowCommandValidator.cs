using FluentValidation;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.RemoveSeatMapRow;

public sealed class RemoveSeatMapRowCommandValidator : AbstractValidator<RemoveSeatMapRowCommand>
{
    public RemoveSeatMapRowCommandValidator()
    {
        RuleFor(command => command.ConfigurationId).GreaterThan(0);
        RuleFor(command => command.RowNumber).GreaterThan(0);
    }
}
