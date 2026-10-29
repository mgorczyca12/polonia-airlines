using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using seating_service.Application.Abstractions;
using seating_service.Application.Features.Inventory;
using seating_service.Domain.Entities;
using seating_service.Infrastructure.Persistence;
using Shared.Integration.Contracts;

namespace seating_service.Infrastructure;

public sealed class InventoryParticipant(
    SeatingDbContext database,
    IInventoryHolds holds,
    IInventoryReleaseFence releaseFence,
    IPublishEndpoint publisher,
    TimeProvider timeProvider)
{
    public async Task Hold(HoldReservationInventory command, CancellationToken cancellationToken)
    {
        await database.LockMutations(cancellationToken);
        var operation = await database.Operations.FindAsync([command.CorrelationId], cancellationToken);
        if (operation is not null)
        {
            await ReplayHold(operation, cancellationToken);
            return;
        }

        operation = new InventoryOperation
        {
            CorrelationId = command.CorrelationId,
            State = InventoryOperationState.Held
        };
        database.Operations.Add(operation);

        if (!InventoryKeys.IsValid(command, timeProvider.GetUtcNow()))
        {
            await Reject(operation, InventoryRejectionCode.InvalidSelectionOrDeadline, cancellationToken);
            return;
        }

        operation.ReservationId = command.ReservationId;
        operation.ExpiresAt = DateTimeOffset.FromUnixTimeMilliseconds(command.HoldUntil.ToUnixTimeMilliseconds());
        operation.SelectionsJson = JsonSerializer.Serialize(command.Selections);

        var inventories = await LoadInventories(command.Selections, cancellationToken);
        if (!await SelectionsExist(command.Selections, inventories, cancellationToken))
        {
            await Reject(operation, InventoryRejectionCode.InventoryNotProvisioned, cancellationToken);
            return;
        }

        var capacities = inventories.Select(inventory => new HoldCapacity(
            InventoryKeys.Bucket(inventory.FlightId, inventory.CabinId),
            inventory.BookingLimit,
            inventory.Assignments.Count,
            inventory.TotalSeats,
            inventory.Assignments.Count(assignment => assignment.SeatId is not null))).ToArray();
        var confirmedPassengers = inventories
            .SelectMany(inventory => inventory.Assignments.Select(assignment =>
                InventoryKeys.Passenger(inventory.FlightId, inventory.CabinId, assignment.PassengerId)))
            .ToArray();
        var flightIds = command.Selections.Select(selection => selection.FlightId).Distinct().ToArray();
        var assignedSeats = await database.Assignments
            .Where(assignment => flightIds.Contains(assignment.FlightId) && assignment.SeatId != null)
            .Select(assignment => new { assignment.FlightId, assignment.SeatId })
            .ToArrayAsync(cancellationToken);
        var confirmedSeats = assignedSeats
            .Select(assignment => InventoryKeys.Seat(assignment.FlightId, assignment.SeatId!))
            .ToArray();

        var acquired = await holds.Acquire(
            InventoryKeys.Hold(command.CorrelationId, command.HoldUntil, command.Selections),
            capacities,
            confirmedPassengers,
            confirmedSeats,
            cancellationToken);
        if (!acquired)
        {
            await Reject(operation, InventoryRejectionCode.InventoryUnavailable, cancellationToken);
            return;
        }

        await publisher.Publish(new ReservationInventoryHeld(operation.CorrelationId), cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task Confirm(ConfirmReservationInventory command, CancellationToken cancellationToken)
    {
        await database.LockMutations(cancellationToken);
        var operation = await database.Operations.FindAsync([command.CorrelationId], cancellationToken);
        if (operation is null)
        {
            operation = new InventoryOperation { CorrelationId = command.CorrelationId };
            database.Operations.Add(operation);
            await Reject(operation, InventoryRejectionCode.HoldNotFound, cancellationToken);
            return;
        }

        if (operation.State == InventoryOperationState.Confirmed)
        {
            await publisher.Publish(new ReservationInventoryConfirmed(command.CorrelationId), cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            return;
        }

        if (operation.State != InventoryOperationState.Held)
        {
            await PublishRejection(operation, cancellationToken);
            await database.SaveChangesAsync(cancellationToken);
            return;
        }

        var selections = JsonSerializer.Deserialize<InventorySelection[]>(operation.SelectionsJson)!;
        if (!await holds.IsValid(
                InventoryKeys.Hold(operation.CorrelationId, operation.ExpiresAt, selections),
                cancellationToken))
        {
            await Reject(operation, InventoryRejectionCode.HoldExpiredOrLost, cancellationToken);
            return;
        }

        var inventories = await LoadInventories(selections, cancellationToken);
        if (!await SelectionsExist(selections, inventories, cancellationToken) ||
            !CanConfirm(selections, inventories))
        {
            await Reject(operation, InventoryRejectionCode.InventoryUnavailable, cancellationToken);
            return;
        }

        foreach (var selection in selections)
        {
            var inventory = inventories.Single(value =>
                value.FlightId == selection.FlightId && value.CabinId == selection.CabinId);
            inventory.ConfirmAllocation(operation.ReservationId, selection.PassengerId, selection.SeatId);
        }

        operation.State = InventoryOperationState.Confirmed;
        await publisher.Publish(new ReservationInventoryConfirmed(command.CorrelationId), cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
        // The confirmation outbox event triggers Redis cleanup only after this SQL transaction commits.
    }

    public async Task Release(ReleaseReservationInventory command, CancellationToken cancellationToken)
    {
        await releaseFence.EnsureReleased(command.CorrelationId, cancellationToken);
        await database.LockMutations(cancellationToken);
        var operation = await database.Operations.FindAsync([command.CorrelationId], cancellationToken)
            ?? throw new InvalidOperationException("The durable release fence is missing.");
        if (operation.AllocationsPendingRelease)
        {
            var selections = JsonSerializer.Deserialize<InventorySelection[]>(operation.SelectionsJson)!;
            var inventories = await LoadInventories(selections, cancellationToken);
            foreach (var selection in selections)
            {
                var inventory = inventories.Single(value =>
                    value.FlightId == selection.FlightId && value.CabinId == selection.CabinId);
                var assignment = inventory.Assignments.Single(value =>
                    value.PassengerId == selection.PassengerId && value.ReservationId == operation.ReservationId);
                inventory.ReleaseAllocation(assignment);
                database.Assignments.Remove(assignment);
            }
        }

        await holds.Release(command.CorrelationId, cancellationToken);
        operation.State = InventoryOperationState.Released;
        operation.RejectionCode = InventoryRejectionCode.Cancelled.ToContractCode();
        operation.AllocationsPendingRelease = false;
        await publisher.Publish(new ReservationInventoryReleased(command.CorrelationId), cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task ReplayHold(InventoryOperation operation, CancellationToken cancellationToken)
    {
        if (operation.State == InventoryOperationState.Held)
        {
            var selections = JsonSerializer.Deserialize<InventorySelection[]>(operation.SelectionsJson)!;
            if (!await holds.IsValid(
                    InventoryKeys.Hold(operation.CorrelationId, operation.ExpiresAt, selections),
                    cancellationToken))
            {
                await Reject(operation, InventoryRejectionCode.HoldExpiredOrLost, cancellationToken);
                return;
            }

            await publisher.Publish(new ReservationInventoryHeld(operation.CorrelationId), cancellationToken);
        }
        else if (operation.State == InventoryOperationState.Confirmed)
        {
            await publisher.Publish(new ReservationInventoryConfirmed(operation.CorrelationId), cancellationToken);
        }
        else
        {
            await PublishRejection(operation, cancellationToken);
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<SeatInventory>> LoadInventories(
        InventorySelection[] selections,
        CancellationToken cancellationToken)
    {
        var inventories = new List<SeatInventory>();
        var buckets = selections
            .Select(selection => (selection.FlightId, selection.CabinId))
            .Distinct()
            .OrderBy(bucket => bucket.FlightId, StringComparer.Ordinal)
            .ThenBy(bucket => bucket.CabinId, StringComparer.Ordinal);

        foreach (var bucket in buckets)
        {
            var rows = await database.Inventories
                .FromSqlInterpolated(
                    $"""SELECT * FROM "SeatInventories" WHERE "FlightId" = {bucket.FlightId} AND "CabinId" = {bucket.CabinId} FOR UPDATE""")
                .ToListAsync(cancellationToken);
            foreach (var inventory in rows)
            {
                await database.Entry(inventory).Collection(value => value.Assignments).LoadAsync(cancellationToken);
                if (inventory.ReservedSeats != inventory.Assignments.Count)
                {
                    throw new InvalidOperationException("Confirmed inventory count is inconsistent.");
                }

                inventories.Add(inventory);
            }
        }

        return inventories;
    }

    private async Task<bool> SelectionsExist(
        InventorySelection[] selections,
        List<SeatInventory> inventories,
        CancellationToken cancellationToken)
    {
        foreach (var selection in selections)
        {
            if (!inventories.Any(inventory =>
                    inventory.FlightId == selection.FlightId && inventory.CabinId == selection.CabinId))
            {
                return false;
            }

            if (selection.SeatId is not null &&
                !await database.Seats.AnyAsync(seat =>
                    seat.FlightId == selection.FlightId &&
                    seat.CabinId == selection.CabinId &&
                    seat.SeatId == selection.SeatId, cancellationToken))
            {
                return false;
            }
        }

        return true;
    }

    private static bool CanConfirm(InventorySelection[] selections, List<SeatInventory> inventories)
    {
        foreach (var inventory in inventories)
        {
            var requested = selections
                .Where(selection => selection.FlightId == inventory.FlightId && selection.CabinId == inventory.CabinId)
                .ToArray();
            if (inventory.Assignments.Count + requested.Length > inventory.BookingLimit ||
                inventory.Assignments.Count(assignment => assignment.SeatId is not null) +
                requested.Count(selection => selection.SeatId is not null) > inventory.TotalSeats ||
                requested.Any(selection => inventory.Assignments.Any(assignment =>
                    assignment.PassengerId == selection.PassengerId ||
                    (selection.SeatId is not null && assignment.SeatId == selection.SeatId))))
            {
                return false;
            }
        }

        return true;
    }

    private async Task Reject(
        InventoryOperation operation,
        InventoryRejectionCode code,
        CancellationToken cancellationToken)
    {
        operation.State = InventoryOperationState.Rejected;
        operation.RejectionCode = code.ToContractCode();
        await PublishRejection(operation, cancellationToken);
        await database.SaveChangesAsync(cancellationToken);
    }

    private Task PublishRejection(InventoryOperation operation, CancellationToken cancellationToken)
    {
        return publisher.Publish(
            new ReservationInventoryRejected(
                operation.CorrelationId,
                operation.RejectionCode ?? InventoryRejectionCode.Cancelled.ToContractCode()),
            cancellationToken);
    }
}
