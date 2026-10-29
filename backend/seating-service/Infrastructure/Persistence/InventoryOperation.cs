namespace seating_service.Infrastructure.Persistence;

public enum InventoryOperationState
{
    Held,
    Confirmed,
    Rejected,
    Released
}

public sealed class InventoryOperation
{
    public Guid CorrelationId { get; set; }
    public string ReservationId { get; set; } = string.Empty;
    public string SelectionsJson { get; set; } = "[]";
    public DateTimeOffset ExpiresAt { get; set; }
    public InventoryOperationState State { get; set; }
    public string? RejectionCode { get; set; }
    public bool AllocationsPendingRelease { get; set; }
}

public sealed class InventoryMutationLock
{
    public int Id { get; set; }
}

public sealed class InventorySeat
{
    public string FlightId { get; set; } = string.Empty;
    public string CabinId { get; set; } = string.Empty;
    public string SeatId { get; set; } = string.Empty;
}
