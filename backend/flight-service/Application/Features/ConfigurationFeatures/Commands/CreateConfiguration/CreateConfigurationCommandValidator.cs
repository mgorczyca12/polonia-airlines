using FluentValidation;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.CreateConfiguration;

public sealed class CreateConfigurationCommandValidator : AbstractValidator<CreateConfigurationCommand>
{
    public CreateConfigurationCommandValidator()
    {
        RuleFor(command => command.AircraftTypeId).GreaterThan(0);
        RuleFor(command => command.Code).NotEmpty().MaximumLength(32);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(200);
        RuleFor(command => command.Version).GreaterThan(0);
    }
}
