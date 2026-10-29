using DispatchR.Abstractions.Send;
using FluentResults;
using seating_service.Application.Abstractions;
using seating_service.Application.Features.InventoryFeatures.Errors;
using seating_service.Domain.Entities;
using seating_service.Domain.ValueObjects;
using Shared.Application.Abstractions;
using Shared.Infrastructure.Messaging;
using Shared.Integration.Contracts;

namespace seating_service.Application.Features.InventoryFeatures.Commands.HoldInventory;

public sealed class HoldInventoryCommandHandler(
    IInventoryMutationLock mutationLock,
    IInventoryOperationRepository operations,
    IInventoryRepository inventories,
    IInventoryHolds holds,
    IIntegrationEventPublisher publisher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IRequestHandler<HoldInventoryCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(HoldInventoryCommand request, CancellationToken cancellationToken)
    {
        await mutationLock.AcquireAsync(cancellationToken);

        var existing = await operations.GetByIdAsync(request.CorrelationId, cancellationToken);
        if (existing is not null)
        {
            await ReplayAsync(existing, cancellationToken);
            return Result.Ok();
        }

        var operation = InventoryOperation.Open(request.CorrelationId);
        operations.Add(operation);

        if (!HoldInventoryRequestRule.IsSatisfiedBy(request, timeProvider.GetUtcNow()))
        {
            await RejectAsync(operation, InventoryRejectionCode.InvalidSelectionOrDeadline, cancellationToken);
            return Result.Ok();
        }

        var selections = HeldSelections.From(request.Selections.Select(selection => new HeldSelection(
            selection.PassengerId,
            selection.FlightId,
            selection.CabinId,
            selection.SeatId)));
        var expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(request.HoldUntil.ToUnixTimeMilliseconds());
        operation.RecordHold(request.ReservationId, expiresAt, selections);

        var loaded = await inventories.GetLockedAsync(selections.Items, cancellationToken);
        if (!await InventoryProvisioning.AreProvisionedAsync(inventories, selections, loaded, cancellationToken))
        {
            await RejectAsync(operation, InventoryRejectionCode.InventoryNotProvisioned, cancellationToken);
            return Result.Ok();
        }

        var capacities = loaded
            .Select(inventory => new HoldCapacity(
                InventoryKeys.Bucket(inventory.FlightId, inventory.CabinId),
                inventory.BookingLimit,
                inventory.Assignments.Count,
                inventory.TotalSeats,
                inventory.Assignments.Count(assignment => assignment.SeatId is not null)))
            .ToArray();
        var confirmedPassengers = loaded
            .SelectMany(inventory => inventory.Assignments.Select(assignment =>
                InventoryKeys.Passenger(inventory.FlightId, inventory.CabinId, assignment.PassengerId)))
            .ToArray();
        var flightIds = selections.Items
            .Select(selection => selection.FlightId)
            .Distinct()
            .ToArray();
        var assignedSeats = await inventories.GetAssignedSeatsAsync(flightIds, cancellationToken);
        var confirmedSeats = assignedSeats
            .Select(seat => InventoryKeys.Seat(seat.FlightId, seat.SeatId))
            .ToArray();

        var acquired = await holds.Acquire(
            InventoryKeys.Hold(request.CorrelationId, request.HoldUntil, selections.Items),
            capacities,
            confirmedPassengers,
            confirmedSeats,
            cancellationToken);
        if (!acquired)
        {
            await RejectAsync(operation, InventoryRejectionCode.InventoryUnavailable, cancellationToken);
            return Result.Ok();
        }

        await publisher.PublishAsync(new ReservationInventoryHeld(operation.CorrelationId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Ok();
    }

    private async Task ReplayAsync(InventoryOperation operation, CancellationToken cancellationToken)
    {
        if (operation.State == InventoryOperationState.Held)
        {
            var hold = InventoryKeys.Hold(operation.CorrelationId, operation.ExpiresAt, operation.Selections.Items);
            if (!await holds.IsValid(hold, cancellationToken))
            {
                await RejectAsync(operation, InventoryRejectionCode.HoldExpiredOrLost, cancellationToken);
                return;
            }

            await publisher.PublishAsync(new ReservationInventoryHeld(operation.CorrelationId), cancellationToken);
        }
        else if (operation.State == InventoryOperationState.Confirmed)
        {
            await publisher.PublishAsync(
                new ReservationInventoryConfirmed(operation.CorrelationId),
                cancellationToken);
        }
        else
        {
            await publisher.PublishRejectionAsync(operation, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RejectAsync(
        InventoryOperation operation,
        InventoryRejectionCode code,
        CancellationToken cancellationToken)
    {
        operation.Reject(code.ToContractCode());
        await publisher.PublishRejectionAsync(operation, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
