using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;

namespace flight_service.unit.test;

public class CollectionTests
{
    private static Airport Airport() =>
        flight_service.Domain.Entities.Airport.Create("Airport", "EPWA", "WAW", Address.Create("Warsaw", "Mazovia", "PL"));

    private static Flight Flight() =>
        flight_service.Domain.Entities.Flight.Create("PA123", new DateTime(2026, 10, 8, 10, 0, 0),
            new DateTime(2026, 10, 8, 12, 0, 0), Airport(), Airport());

    private static AircraftConfiguration Configuration() =>
        AircraftConfiguration.Create(AircraftType.Create("B738", "737", "Boeing"), "STD", "Standard", 1);

    [Fact]
    public void SegmentsAreReadOnlyAndConnectedToOneFlight()
    {
        var flight = Flight();
        var segment = FlightSegment.Create(1, Airport(), Airport());
        flight.AddSegment(segment);

        Assert.Same(flight, segment.Flight);
        Assert.Equal(flight.Id, segment.FlightId);
        Assert.Same(segment, Assert.Single(flight.FlightSegments));
        Assert.Throws<NotSupportedException>(() => ((ICollection<FlightSegment>)flight.FlightSegments).Clear());
        Assert.Throws<InvalidOperationException>(() => flight.AddSegment(segment));
        var other = Flight();
        Assert.Throws<InvalidOperationException>(() => other.AddSegment(segment));
        Assert.Empty(other.FlightSegments);
        Assert.Throws<ArgumentNullException>(() => flight.AddSegment(null!));
    }

    [Fact]
    public void GatesAreReadOnlyAndMustBelongToAirport()
    {
        var airport = Airport();
        var gate = AirportGate.Create(airport, "A", "1");
        airport.AddGate(gate);

        Assert.Same(gate, Assert.Single(airport.Gates));
        Assert.Throws<NotSupportedException>(() => ((ICollection<AirportGate>)airport.Gates).Clear());
        Assert.Throws<InvalidOperationException>(() => airport.AddGate(gate));
        Assert.Throws<InvalidOperationException>(() => Airport().AddGate(gate));
        Assert.Throws<ArgumentNullException>(() => airport.AddGate(null!));
    }

    [Fact]
    public void RowsAreReadOnlyAndMustBelongToConfiguration()
    {
        var configuration = Configuration();
        var row = SeatMapRow.Create(configuration, SeatMapCabin.Create("Economy", "Y"), SeatMapRowType.Seats);
        configuration.AddSeatMapRow(row);

        Assert.Same(row, Assert.Single(configuration.SeatMapRows));
        Assert.Throws<NotSupportedException>(() => ((ICollection<SeatMapRow>)configuration.SeatMapRows).Clear());
        Assert.Throws<InvalidOperationException>(() => configuration.AddSeatMapRow(row));
        Assert.Throws<InvalidOperationException>(() => Configuration().AddSeatMapRow(row));
        Assert.Throws<ArgumentNullException>(() => configuration.AddSeatMapRow(null!));
    }

    [Fact]
    public void PositionsAreReadOnlyAndConnectedToOneRow()
    {
        var configuration = Configuration();
        var cabin = SeatMapCabin.Create("Economy", "Y");
        var row = SeatMapRow.Create(configuration, cabin, SeatMapRowType.Seats);
        var position = SeatMapPosition.Create("A", 0, true);
        row.AddPosition(position);

        Assert.Same(row, position.SeatMapRow);
        Assert.Equal(row.Id, position.SeatMapRowId);
        Assert.Same(position, Assert.Single(row.SeatMapPositions));
        Assert.Throws<NotSupportedException>(() => ((ICollection<SeatMapPosition>)row.SeatMapPositions).Clear());
        Assert.Throws<InvalidOperationException>(() => row.AddPosition(position));
        var other = SeatMapRow.Create(configuration, cabin, SeatMapRowType.Seats);
        Assert.Throws<InvalidOperationException>(() => other.AddPosition(position));
        Assert.Empty(other.SeatMapPositions);
        Assert.Throws<ArgumentNullException>(() => row.AddPosition(null!));
    }
}
