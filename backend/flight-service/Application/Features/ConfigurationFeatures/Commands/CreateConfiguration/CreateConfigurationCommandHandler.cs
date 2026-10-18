using DispatchR.Abstractions.Send;
using flight_service.Application.Abstractions;
using flight_service.Application.Features.ConfigurationFeatures.Errors;
using flight_service.Domain.Entities;
using FluentResults;
using Shared.Application.Abstractions;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.CreateConfiguration;

public sealed class CreateConfigurationCommandHandler(
    IConfigurationRepository configurations,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateConfigurationCommand, ValueTask<Result<int>>>
{
    public async ValueTask<Result<int>> Handle(CreateConfigurationCommand request, CancellationToken cancellationToken)
    {
        var aircraftType = await configurations.GetAircraftTypeAsync(request.AircraftTypeId, cancellationToken);
        if (aircraftType is null)
        {
            return Result.Fail<int>(ConfigurationError.Missing("aircraft_type"));
        }

        if (await configurations.ExistsAsync(request.AircraftTypeId, request.Code, request.Version, cancellationToken))
        {
            return Result.Fail<int>(ConfigurationError.Conflict(
                "configuration_exists", "This configuration code and version already exist for the aircraft type."));
        }

        var configuration = AircraftConfiguration.Create(aircraftType, request.Code, request.Name, request.Version);
        configurations.Add(configuration);

        // Flush inside the pipeline's transaction to return the database-generated identity.
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Ok(configuration.Id);
    }
}
