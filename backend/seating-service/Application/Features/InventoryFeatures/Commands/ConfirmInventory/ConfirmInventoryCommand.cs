using FluentResults;
using Shared.Application.Commands;

namespace seating_service.Application.Features.InventoryFeatures.Commands.ConfirmInventory;

public sealed record ConfirmInventoryCommand(Guid CorrelationId)
    : ICommand<ConfirmInventoryCommand, Result>;
