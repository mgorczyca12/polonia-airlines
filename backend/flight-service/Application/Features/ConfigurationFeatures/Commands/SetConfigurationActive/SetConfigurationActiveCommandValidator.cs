using FluentValidation;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.SetConfigurationActive;

public sealed class SetConfigurationActiveCommandValidator : AbstractValidator<SetConfigurationActiveCommand>
{
    public SetConfigurationActiveCommandValidator()
    {
        RuleFor(command => command.ConfigurationId).GreaterThan(0);
    }
}
