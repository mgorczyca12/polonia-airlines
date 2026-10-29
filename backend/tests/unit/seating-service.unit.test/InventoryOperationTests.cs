using seating_service.Domain.Entities;
using seating_service.Domain.ValueObjects;

namespace seating_service.unit.test;

public class InventoryOperationTests
{
    private static readonly HeldSelections Selections =
        HeldSelections.From([new HeldSelection("P", "F", "C", "1A")]);

    [Fact]
    public void Open_StartsHeldWithEmptyDetails()
    {
        var id = Guid.NewGuid();

        var operation = InventoryOperation.Open(id);

        Assert.Equal(id, operation.CorrelationId);
        Assert.Equal(InventoryOperationState.Held, operation.State);
        Assert.Empty(operation.Selections.Items);
        Assert.Null(operation.RejectionCode);
        Assert.False(operation.AllocationsPendingRelease);
    }

    [Fact]
    public void RecordHold_StoresDetailsOnceAndRejectsBlankReservation()
    {
        var operation = InventoryOperation.Open(Guid.NewGuid());
        var expiry = DateTimeOffset.UtcNow.AddMinutes(5);

        Assert.ThrowsAny<ArgumentException>(() => operation.RecordHold(" ", expiry, Selections));
        operation.RecordHold("reservation", expiry, Selections);

        Assert.Equal("reservation", operation.ReservationId);
        Assert.Equal(expiry, operation.ExpiresAt);
        Assert.Same(Selections, operation.Selections);
        Assert.Throws<InvalidOperationException>(() => operation.RecordHold("reservation", expiry, Selections));
    }

    [Fact]
    public void Confirm_TransitionsOnlyFromHeld()
    {
        var operation = InventoryOperation.Open(Guid.NewGuid());

        operation.Confirm();

        Assert.Equal(InventoryOperationState.Confirmed, operation.State);
        Assert.Throws<InvalidOperationException>(operation.Confirm);
        Assert.Throws<InvalidOperationException>(() => operation.Reject("code"));
    }

    [Fact]
    public void Reject_RecordsCodeAndIsTerminal()
    {
        var operation = InventoryOperation.Open(Guid.NewGuid());

        Assert.ThrowsAny<ArgumentException>(() => operation.Reject(" "));
        operation.Reject("inventory_unavailable");

        Assert.Equal(InventoryOperationState.Rejected, operation.State);
        Assert.Equal("inventory_unavailable", operation.RejectionCode);
        Assert.Throws<InvalidOperationException>(operation.Confirm);
        Assert.Throws<InvalidOperationException>(() => operation.Reject("other"));
    }

    [Fact]
    public void Fence_FromConfirmedMarksAllocationsPendingAndIsIdempotent()
    {
        var operation = InventoryOperation.Open(Guid.NewGuid());
        operation.Confirm();

        operation.Fence("cancelled");
        operation.Fence("cancelled");

        Assert.Equal(InventoryOperationState.Released, operation.State);
        Assert.Equal("cancelled", operation.RejectionCode);
        Assert.True(operation.AllocationsPendingRelease);
        Assert.Throws<InvalidOperationException>(operation.Confirm);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Fence_FromOtherStatesLeavesNoPendingAllocations(bool rejected)
    {
        var operation = InventoryOperation.Open(Guid.NewGuid());
        if (rejected)
        {
            operation.Reject("inventory_unavailable");
        }

        operation.Fence("cancelled");

        Assert.Equal(InventoryOperationState.Released, operation.State);
        Assert.False(operation.AllocationsPendingRelease);
    }

    [Fact]
    public void CompleteRelease_ClearsPendingFlagOnlyAfterFence()
    {
        var operation = InventoryOperation.Open(Guid.NewGuid());
        operation.Confirm();

        Assert.Throws<InvalidOperationException>(() => operation.CompleteRelease("cancelled"));
        operation.Fence("cancelled");
        operation.CompleteRelease("cancelled");

        Assert.False(operation.AllocationsPendingRelease);
        Assert.Equal(InventoryOperationState.Released, operation.State);
    }

    [Fact]
    public void State_FromRestoresCanonicalInstancesAndRejectsUnknownCodes()
    {
        Assert.Same(InventoryOperationState.Held, InventoryOperationState.From("Held"));
        Assert.Same(InventoryOperationState.Confirmed, InventoryOperationState.From("Confirmed"));
        Assert.Same(InventoryOperationState.Rejected, InventoryOperationState.From("Rejected"));
        Assert.Same(InventoryOperationState.Released, InventoryOperationState.From("Released"));
        Assert.Throws<ArgumentException>(() => InventoryOperationState.From("Unknown"));
    }

    [Fact]
    public void InventorySeat_CreatePreservesIdsAndRejectsBlankValues()
    {
        var seat = InventorySeat.Create(" F ", "C", "1A");

        Assert.Equal(" F ", seat.FlightId);
        Assert.Equal("C", seat.CabinId);
        Assert.Equal("1A", seat.SeatId);
        Assert.ThrowsAny<ArgumentException>(() => InventorySeat.Create("F", " ", "1A"));
    }
}
