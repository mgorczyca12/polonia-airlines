using seating_service.Domain.Entities;

namespace seating_service.unit.test;

public class SeatAssignmentTests
{
    [Fact]
    public void OverbookingAllowsUnassignedAllocationsButNotExtraPhysicalSeats()
    {
        var inventory = SeatInventory.Create("flight", "economy", 1, 1);
        var first = inventory.ConfirmAllocation("reservation-1", "passenger-1", "seat-A");
        var second = inventory.ConfirmAllocation("reservation-2", "passenger-2");

        Assert.Same(inventory, first.SeatInventory);
        Assert.Equal(inventory.Id, first.SeatInventoryId);
        Assert.Null(second.SeatId);
        Assert.Equal(2, inventory.ReservedSeats);
        Assert.Equal(1, inventory.OverbookedSeats);
        Assert.Equal(0, inventory.RemainingBookingCapacity);
        Assert.Throws<InvalidOperationException>(() => inventory.AssignSeat(second, "seat-B"));
        Assert.Throws<InvalidOperationException>(() => inventory.ConfirmAllocation("reservation-3", "passenger-3"));
        Assert.Throws<NotSupportedException>(() => ((ICollection<SeatAssignment>)inventory.Assignments).Clear());
    }

    [Fact]
    public void SeatChangesDoNotConsumeBookingCapacity()
    {
        var inventory = SeatInventory.Create("flight", "economy", 2);
        var first = inventory.ConfirmAllocation("reservation", "passenger-1", "seat-A");
        var second = inventory.ConfirmAllocation("reservation", "passenger-2", "seat-B");

        Assert.Throws<InvalidOperationException>(() => inventory.AssignSeat(second, "seat-A"));
        inventory.AssignSeat(first, "seat-A");
        inventory.ClearSeat(first);
        inventory.AssignSeat(second, "seat-A");
        Assert.Null(first.SeatId);
        Assert.Equal("seat-A", second.SeatId);
        Assert.Equal(2, inventory.ReservedSeats);
        inventory.ReleaseAllocation(second);
        Assert.Equal(1, inventory.ReservedSeats);
        Assert.Throws<InvalidOperationException>(() => inventory.ReleaseAllocation(second));
    }

    [Fact]
    public void RejectsDuplicatePassengersForeignAllocationsAndInvalidIdsWithoutMutation()
    {
        var inventory = SeatInventory.Create("flight", "economy", 3);
        var assignment = inventory.ConfirmAllocation(" Reservation ", "Passenger", " Seat ");
        var foreign = SeatInventory.Create("other", "economy", 1)
            .ConfirmAllocation("other-reservation", "other-passenger");

        Assert.Equal(" Reservation ", assignment.ReservationId);
        Assert.Equal(" Seat ", assignment.SeatId);
        Assert.Throws<InvalidOperationException>(() => inventory.ConfirmAllocation("different", "Passenger"));
        Assert.Throws<InvalidOperationException>(() => inventory.ConfirmAllocation("different", "different", " Seat "));
        Assert.Throws<InvalidOperationException>(() => inventory.AssignSeat(foreign, "seat"));
        Assert.Throws<InvalidOperationException>(() => inventory.ClearSeat(foreign));
        Assert.Throws<InvalidOperationException>(() => inventory.ReleaseAllocation(foreign));
        Assert.Throws<ArgumentException>(() => inventory.ConfirmAllocation("", "new"));
        Assert.Throws<ArgumentException>(() => inventory.ConfirmAllocation("new", " "));
        Assert.Throws<ArgumentException>(() => inventory.ConfirmAllocation("new", "new", ""));
        Assert.Throws<ArgumentException>(() => inventory.AssignSeat(assignment, " "));
        Assert.Single(inventory.Assignments);
        Assert.Equal(1, inventory.ReservedSeats);
    }
}
