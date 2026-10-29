using seating_service.Domain.ValueObjects;
using Shared.Domain.Entities;

namespace seating_service.Domain.Entities;

public sealed class InventoryOperation : AggregateRoot<Guid>
{
    public Guid CorrelationId => Id;
    public string ReservationId { get; private set; } = string.Empty;
    public HeldSelections Selections { get; private set; } = HeldSelections.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public InventoryOperationState State { get; private set; } = InventoryOperationState.Held;
    public string? RejectionCode { get; private set; }
    public bool AllocationsPendingRelease { get; private set; }

    private InventoryOperation()
    {
    }

    private InventoryOperation(Guid correlationId) : base(correlationId)
    {
    }

    public static InventoryOperation Open(Guid correlationId)
    {
        return new InventoryOperation(correlationId);
    }

    public void RecordHold(string reservationId, DateTimeOffset expiresAt, HeldSelections selections)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reservationId);
        ArgumentNullException.ThrowIfNull(selections);
        EnsureState(InventoryOperationState.Held);

        if (ReservationId.Length > 0)
        {
            throw new InvalidOperationException("The hold has already been recorded.");
        }

        ReservationId = reservationId;
        ExpiresAt = expiresAt;
        Selections = selections;
    }

    public void Confirm()
    {
        EnsureState(InventoryOperationState.Held);
        State = InventoryOperationState.Confirmed;
    }

    public void Reject(string rejectionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rejectionCode);
        EnsureState(InventoryOperationState.Held);

        State = InventoryOperationState.Rejected;
        RejectionCode = rejectionCode;
    }

    public void Fence(string cancellationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cancellationCode);

        AllocationsPendingRelease |= State == InventoryOperationState.Confirmed;
        State = InventoryOperationState.Released;
        RejectionCode = cancellationCode;
    }

    public void CompleteRelease(string cancellationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cancellationCode);
        EnsureState(InventoryOperationState.Released);

        RejectionCode = cancellationCode;
        AllocationsPendingRelease = false;
    }

    private void EnsureState(InventoryOperationState expected)
    {
        if (State != expected)
        {
            throw new InvalidOperationException($"The operation is {State.Code}, expected {expected.Code}.");
        }
    }
}
