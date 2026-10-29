using FluentResults;
using Shared.Application.Commands;
using Shared.Integration.Contracts;

namespace seating_service.Application.Features.InventoryFeatures.Commands.HoldInventory;

public sealed record HoldInventoryCommand(
    Guid CorrelationId,
    string ReservationId,
    DateTimeOffset HoldUntil,
    InventorySelection[] Selections)
    : ICommand<HoldInventoryCommand, Result>;
