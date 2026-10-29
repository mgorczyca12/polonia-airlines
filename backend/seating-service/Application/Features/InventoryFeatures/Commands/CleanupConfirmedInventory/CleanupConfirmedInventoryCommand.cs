using FluentResults;
using Shared.Application.Commands;

namespace seating_service.Application.Features.InventoryFeatures.Commands.CleanupConfirmedInventory;

public sealed record CleanupConfirmedInventoryCommand(Guid CorrelationId)
    : ICommand<CleanupConfirmedInventoryCommand, Result>;
