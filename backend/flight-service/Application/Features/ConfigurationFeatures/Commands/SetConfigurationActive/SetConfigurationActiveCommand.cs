using FluentResults;
using Shared.Application.Commands;

namespace flight_service.Application.Features.ConfigurationFeatures.Commands.SetConfigurationActive;

/// <summary>Activates or deactivates a configuration without changing existing aircraft assignments.</summary>
public sealed record SetConfigurationActiveCommand(int ConfigurationId, bool IsActive)
    : ITransactionalCommand<SetConfigurationActiveCommand, Result>;
