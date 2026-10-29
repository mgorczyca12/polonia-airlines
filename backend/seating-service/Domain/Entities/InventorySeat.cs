using Shared.Domain.Entities;

namespace seating_service.Domain.Entities;

public sealed class InventorySeat : Entity<(string FlightId, string SeatId)>
{
    public string FlightId { get; private set; } = null!;
    public string CabinId { get; private set; } = null!;
    public string SeatId { get; private set; } = null!;

    private InventorySeat()
    {
    }

    private InventorySeat(string flightId, string cabinId, string seatId) : base((flightId, seatId))
    {
        FlightId = flightId;
        CabinId = cabinId;
        SeatId = seatId;
    }

    public static InventorySeat Create(string flightId, string cabinId, string seatId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flightId);
        ArgumentException.ThrowIfNullOrWhiteSpace(cabinId);
        ArgumentException.ThrowIfNullOrWhiteSpace(seatId);

        return new InventorySeat(flightId, cabinId, seatId);
    }
}
