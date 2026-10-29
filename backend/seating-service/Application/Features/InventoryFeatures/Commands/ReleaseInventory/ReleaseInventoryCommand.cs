using FluentResults;
using Shared.Application.Commands;

namespace seating_service.Application.Features.InventoryFeatures.Commands.ReleaseInventory;

public sealed record ReleaseInventoryCommand(Guid CorrelationId)
    : ICommand<ReleaseInventoryCommand, Result>;
