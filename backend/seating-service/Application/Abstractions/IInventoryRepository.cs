using seating_service.Domain.Entities;
using seating_service.Domain.ValueObjects;
using Shared.Application.Abstractions;

namespace seating_service.Application.Abstractions;

public sealed record AssignedSeat(string FlightId, string SeatId);

public interface IInventoryRepository : IRepository<SeatInventory, int>
{
    Task<List<SeatInventory>> GetLockedAsync(
        IEnumerable<HeldSelection> selections,
        CancellationToken cancellationToken);

    Task<AssignedSeat[]> GetAssignedSeatsAsync(
        IReadOnlyCollection<string> flightIds,
        CancellationToken cancellationToken);

    Task<bool> SeatExistsAsync(
        string flightId,
        string cabinId,
        string seatId,
        CancellationToken cancellationToken);

    void RemoveAssignment(SeatAssignment assignment);
}
