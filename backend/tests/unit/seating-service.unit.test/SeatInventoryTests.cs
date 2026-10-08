using seating_service.Domain.Entities;

namespace seating_service.unit.test;

public class SeatInventoryTests
{
    [Fact]
    public void DefaultsToNoOverbookingAndNoAllocations()
    {
        var inventory = SeatInventory.Create("flight-1", "economy", 180);

        Assert.Equal("flight-1", inventory.FlightId);
        Assert.Equal("economy", inventory.CabinId);
        Assert.Equal(180, inventory.TotalSeats);
        Assert.Equal(0, inventory.OverbookingAllowance);
        Assert.Equal(0, inventory.ReservedSeats);
        Assert.Equal(180, inventory.BookingLimit);
        Assert.Equal(180, inventory.RemainingBookingCapacity);
        Assert.Equal(0, inventory.OverbookedSeats);
    }

    [Fact]
    public void AllowanceIncreasesBookingLimitNotPhysicalCapacityOrActualOverbooking()
    {
        var inventory = SeatInventory.Create("flight-1", "economy", 180, 5);

        Assert.Equal(180, inventory.TotalSeats);
        Assert.Equal(185, inventory.BookingLimit);
        Assert.Equal(185, inventory.RemainingBookingCapacity);
        Assert.Equal(0, inventory.OverbookedSeats);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 0)]
    [InlineData(180, -1)]
    [InlineData(int.MaxValue, 1)]
    public void RejectsInvalidCapacity(int seats, int allowance) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => SeatInventory.Create("flight-1", "economy", seats, allowance));

    [Theory]
    [InlineData(null, "economy")]
    [InlineData("", "economy")]
    [InlineData(" ", "economy")]
    [InlineData("flight-1", null)]
    [InlineData("flight-1", "")]
    [InlineData("flight-1", " ")]
    public void RejectsMissingExternalIds(string? flightId, string? cabinId) =>
        Assert.ThrowsAny<ArgumentException>(() => SeatInventory.Create(flightId!, cabinId!, 180));

    [Fact]
    public void PreservesExternalIdentifierRepresentation()
    {
        var inventory = SeatInventory.Create(" Flight-A ", " Economy ", 180);

        Assert.Equal(" Flight-A ", inventory.FlightId);
        Assert.Equal(" Economy ", inventory.CabinId);
    }
}
