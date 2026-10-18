using FluentResults;
using Shared.Application.Commands;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.CreateConfiguration;

/// <summary>Creates a versioned aircraft configuration with an empty passenger seat map.</summary>
public sealed record CreateConfigurationCommand(int AircraftTypeId, string Code, string Name, int Version)
    : ITransactionalCommand<CreateConfigurationCommand, Result<int>>;
