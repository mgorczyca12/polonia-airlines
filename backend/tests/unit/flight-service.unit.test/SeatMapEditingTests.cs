using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;

namespace flight_service.unit.test;

public sealed class SeatMapEditingTests
{
    [Fact]
    public void AddRow_DuplicateNumber_RejectsWithoutChangingLayout()
    {
        var map = Configuration().SeatMap;
        var cabin = SeatMapCabin.Create("Economy", "Y");
        var first = SeatMapRow.Create(map, cabin, SeatMapRowType.Seats, 5);
        map.AddRow(first);

        Assert.Equal(6, SeatMapRow.Create(map, cabin, SeatMapRowType.Seats).Number);
        Assert.Throws<InvalidOperationException>(() =>
            map.AddRow(SeatMapRow.Create(map, cabin, SeatMapRowType.Seats, 5)));
        Assert.Same(first, Assert.Single(map.SeatMapRows));
        Assert.Throws<ArgumentOutOfRangeException>(() => SeatMapRow.Create(map, cabin, SeatMapRowType.Seats, 0));
        map.AddRow(SeatMapRow.Create(map, cabin, SeatMapRowType.Seats, int.MaxValue));
        Assert.Throws<OverflowException>(() => SeatMapRow.Create(map, cabin, SeatMapRowType.Seats));
    }

    [Fact]
    public void AddPosition_DuplicateColumn_RejectsWithoutAttachingPosition()
    {
        var map = Configuration().SeatMap;
        var cabin = SeatMapCabin.Create("Economy", "Y");
        var firstRow = SeatMapRow.Create(map, cabin, SeatMapRowType.Seats, 1);
        var secondRow = SeatMapRow.Create(map, cabin, SeatMapRowType.Seats, 2);
        firstRow.AddPosition(SeatMapPosition.Create("A", 0, true));
        var duplicate = SeatMapPosition.Create("A", 1, false);

        Assert.Throws<InvalidOperationException>(() => firstRow.AddPosition(duplicate));
        secondRow.AddPosition(duplicate);
        Assert.Same(secondRow, duplicate.SeatMapRow);
        duplicate.Update(2, true);
        Assert.Equal(2, duplicate.AisleGroup);
        Assert.True(duplicate.IsSeat);
        Assert.Throws<ArgumentOutOfRangeException>(() => duplicate.Update(-1, false));
        Assert.True(duplicate.IsSeat);
    }

    [Fact]
    public void RemoveRowAndPosition_NonMembers_RejectWithoutRemovingExistingChildren()
    {
        var map = Configuration().SeatMap;
        var cabin = SeatMapCabin.Create("Economy", "Y");
        var row = SeatMapRow.Create(map, cabin, SeatMapRowType.Seats, 1);
        var position = SeatMapPosition.Create("A", 0, true);
        map.AddRow(row);
        row.AddPosition(position);

        Assert.Throws<InvalidOperationException>(() =>
            map.RemoveRow(SeatMapRow.Create(map, cabin, SeatMapRowType.Seats, 2)));
        Assert.Throws<InvalidOperationException>(() => row.RemovePosition(SeatMapPosition.Create("B", 0, true)));
        row.RemovePosition(position);
        map.RemoveRow(row);
        Assert.Empty(map.SeatMapRows);
        Assert.Empty(row.SeatMapPositions);
    }

    [Fact]
    public void AssignAircraft_ActiveMatchingAircraft_ReassignsAndRejectsUnavailableOrMismatchedAircraft()
    {
        var configuration = Configuration();
        var aircraft = Aircraft.Create("SP-ONE", "000001", configuration.AircraftType, configuration);
        var airport = Airport.Create("Warsaw", "EPWA", "WAW", Address.Create("Warsaw", "Mazovia", "PL"));
        var flight = Flight.Create("PA123", DateTime.UtcNow, DateTime.UtcNow.AddHours(2), airport, airport);
        flight.AssignAircraft(aircraft);
        Assert.Same(aircraft, flight.Aircraft);

        var second = Aircraft.Create("SP-TWO", "000002", configuration.AircraftType, configuration);
        flight.AssignAircraft(second);
        Assert.Same(second, flight.Aircraft);
        aircraft.SendToMaintenance();
        Assert.Throws<InvalidOperationException>(() => flight.AssignAircraft(aircraft));
        configuration.Deactivate();
        Assert.Throws<InvalidOperationException>(() => flight.AssignAircraft(second));
        configuration.Activate();
        var mismatched = Aircraft.Create("SP-ERR", "000003",
            AircraftType.Create("A320", "A320", "Airbus"), configuration);
        Assert.Throws<InvalidOperationException>(() => flight.AssignAircraft(mismatched));
        Assert.Same(second, flight.Aircraft);
    }

    private static AircraftConfiguration Configuration()
    {
        return AircraftConfiguration.Create(AircraftType.Create("B788", "787-8", "Boeing"), "STD", "Standard", 1);
    }
}
