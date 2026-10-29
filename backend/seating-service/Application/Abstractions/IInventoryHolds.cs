namespace seating_service.Application.Abstractions;

public sealed record HoldItem(string Bucket, string Passenger, string? Seat);

public sealed record HoldCapacity(
    string Bucket,
    int BookingLimit,
    int Confirmed,
    int PhysicalLimit,
    int ConfirmedPhysical);

public sealed record InventoryHold(
    Guid Owner,
    DateTimeOffset ExpiresAt,
    HoldItem[] Items);

public interface IInventoryHolds
{
    Task<bool> Acquire(
        InventoryHold hold,
        HoldCapacity[] capacities,
        string[] confirmedPassengers,
        string[] confirmedSeats,
        CancellationToken cancellationToken);

    Task<bool> IsValid(InventoryHold hold, CancellationToken cancellationToken);

    Task Release(Guid owner, CancellationToken cancellationToken);
}
