namespace Shared.Integration.Contracts;

public sealed record InventorySelection(
    string PassengerId,
    string FlightId,
    string CabinId,
    string? SeatId);

public sealed record ReservationRequested(
    Guid CorrelationId,
    string ReservationId,
    DateTimeOffset HoldUntil,
    InventorySelection[] Selections);

public sealed record HoldReservationInventory(
    Guid CorrelationId,
    string ReservationId,
    DateTimeOffset HoldUntil,
    InventorySelection[] Selections);

public sealed record ReservationInventoryHeld(Guid CorrelationId);

public sealed record ReservationInventoryRejected(Guid CorrelationId, string Code);

public sealed record ConfirmReservationInventory(Guid CorrelationId);

public sealed record ReservationInventoryConfirmed(Guid CorrelationId);

public sealed record ReleaseReservationInventory(Guid CorrelationId);

public sealed record ReservationInventoryReleased(Guid CorrelationId);

public sealed record ReservationWorkflowExpired(Guid CorrelationId);
