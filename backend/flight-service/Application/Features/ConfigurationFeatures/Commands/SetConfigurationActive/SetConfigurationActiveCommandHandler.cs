using DispatchR.Abstractions.Send;
using flight_service.Application.Abstractions;
using flight_service.Application.Features.ConfigurationFeatures.Errors;
using FluentResults;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.SetConfigurationActive;

public sealed class SetConfigurationActiveCommandHandler(IConfigurationRepository configurations)
    : IRequestHandler<SetConfigurationActiveCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(SetConfigurationActiveCommand request, CancellationToken cancellationToken)
    {
        var configuration = await configurations.GetByIdAsync(request.ConfigurationId, cancellationToken);
        if (configuration is null)
        {
            return Result.Fail(ConfigurationError.Missing("configuration"));
        }

        if (request.IsActive)
        {
            configuration.Activate();
        }
        else
        {
            configuration.Deactivate();
        }

        return Result.Ok();
    }
}
