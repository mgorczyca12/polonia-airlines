using Microsoft.EntityFrameworkCore;
using seating_service.Application.Abstractions;
using seating_service.Domain.Entities;
using seating_service.Domain.ValueObjects;
using seating_service.Infrastructure.Persistence;
using Shared.Infrastructure.Repositories;

namespace seating_service.Infrastructure.Repositories;

public sealed class InventoryRepository(SeatingDbContext context)
    : RepositoryBase<SeatInventory, int>(context), IInventoryRepository
{
    public async Task<List<SeatInventory>> GetLockedAsync(
        IEnumerable<HeldSelection> selections,
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
            var rows = await context.Inventories
                .FromSqlInterpolated(
                    $"""SELECT * FROM "SeatInventories" WHERE "FlightId" = {bucket.FlightId} AND "CabinId" = {bucket.CabinId} FOR UPDATE""")
                .ToListAsync(cancellationToken);

            foreach (var inventory in rows)
            {
                await context.Entry(inventory).Collection(value => value.Assignments).LoadAsync(cancellationToken);
                inventory.EnsureConsistent();
                inventories.Add(inventory);
            }
        }

        return inventories;
    }

    public async Task<AssignedSeat[]> GetAssignedSeatsAsync(
        IReadOnlyCollection<string> flightIds,
        CancellationToken cancellationToken)
    {
        var flights = flightIds.ToArray();

        return await context.Assignments
            .Where(assignment => flights.Contains(assignment.FlightId) && assignment.SeatId != null)
            .Select(assignment => new AssignedSeat(assignment.FlightId, assignment.SeatId!))
            .ToArrayAsync(cancellationToken);
    }

    public Task<bool> SeatExistsAsync(
        string flightId,
        string cabinId,
        string seatId,
        CancellationToken cancellationToken)
    {
        return context.Seats.AnyAsync(
            seat => seat.FlightId == flightId && seat.CabinId == cabinId && seat.SeatId == seatId,
            cancellationToken);
    }

    public void RemoveAssignment(SeatAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        context.Assignments.Remove(assignment);
    }
}
