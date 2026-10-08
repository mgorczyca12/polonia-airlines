using Shared.Domain.Entities;

namespace seating_service.Domain.Entities;

public class SeatAssignment : Entity<int>
{
    public int SeatInventoryId { get; private set; }
    public SeatInventory SeatInventory { get; private set; } = null!;
    public string ReservationId { get; private set; } = null!;
    public string PassengerId { get; private set; } = null!;
    public string? SeatId { get; private set; }

    private SeatAssignment() { }

    internal static SeatAssignment Create(SeatInventory inventory, string reservationId,
        string passengerId, string? seatId)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentException.ThrowIfNullOrWhiteSpace(reservationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(passengerId);
        if (seatId is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(seatId);

        return new SeatAssignment
        {
            SeatInventoryId = inventory.Id,
            SeatInventory = inventory,
            ReservationId = reservationId,
            PassengerId = passengerId,
            SeatId = seatId
        };
    }

    internal void ChangeSeat(string? seatId)
    {
        if (seatId is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(seatId);
        SeatId = seatId;
    }
}