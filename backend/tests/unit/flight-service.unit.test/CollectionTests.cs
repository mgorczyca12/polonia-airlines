using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;

namespace flight_service.unit.test;

public class CollectionTests
{
    private static Airport Airport()
    {
        return flight_service.Domain.Entities.Airport.Create(
            "Airport",
            "EPWA",
            "WAW",
            Address.Create("Warsaw", "Mazovia", "PL"));
    }

    private static Flight Flight()
    {
        return flight_service.Domain.Entities.Flight.Create(
            "PA123",
            new DateTime(2026, 10, 8, 10, 0, 0),
            new DateTime(2026, 10, 8, 12, 0, 0),
            Airport(),
            Airport());
    }

    private static AircraftConfiguration Configuration()
    {
        return AircraftConfiguration.Create(AircraftType.Create("B738", "737", "Boeing"), "STD", "Standard", 1);
    }

    [Fact]
    public void AddSegment_NewSegment_ExposesReadOnlyCollectionAndEnforcesSingleFlightOwnership()
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
    public void AddGate_NewGate_ExposesReadOnlyCollectionAndEnforcesAirportOwnership()
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
    public void Create_Configuration_CreatesItsOwnSeatMap()
    {
        var configuration = Configuration();
        var other = Configuration();

        Assert.Same(configuration, configuration.SeatMap.AircraftConfiguration);
        Assert.Equal(configuration.Id, configuration.SeatMap.AircraftConfigurationId);
        Assert.NotSame(configuration.SeatMap, other.SeatMap);
        Assert.Empty(configuration.SeatMap.SeatMapRows);
    }

    [Fact]
    public void AddRow_NewRow_ExposesReadOnlyCollectionAndEnforcesSeatMapOwnership()
    {
        var seatMap = Configuration().SeatMap;
        var row = SeatMapRow.Create(seatMap, SeatMapCabin.Create("Economy", "Y"), SeatMapRowType.Seats);
        seatMap.AddRow(row);

        Assert.Same(seatMap, row.SeatMap);
        Assert.Equal(seatMap.Id, row.SeatMapId);
        Assert.Same(row, Assert.Single(seatMap.SeatMapRows));
        Assert.Throws<NotSupportedException>(() => ((ICollection<SeatMapRow>)seatMap.SeatMapRows).Clear());
        Assert.Throws<InvalidOperationException>(() => seatMap.AddRow(row));
        var other = Configuration().SeatMap;
        Assert.Throws<InvalidOperationException>(() => other.AddRow(row));
        Assert.Empty(other.SeatMapRows);
        Assert.Throws<ArgumentNullException>(() => seatMap.AddRow(null!));
        Assert.Throws<ArgumentNullException>(() =>
            SeatMapRow.Create(null!, SeatMapCabin.Create("Economy", "Y"), SeatMapRowType.Seats));
    }

    [Fact]
    public void AddPosition_NewPosition_ExposesReadOnlyCollectionAndEnforcesSingleRowOwnership()
    {
        var configuration = Configuration();
        var cabin = SeatMapCabin.Create("Economy", "Y");
        var row = SeatMapRow.Create(configuration.SeatMap, cabin, SeatMapRowType.Seats);
        var position = SeatMapPosition.Create("A", 0, true);
        row.AddPosition(position);

        Assert.Same(row, position.SeatMapRow);
        Assert.Equal(row.Id, position.SeatMapRowId);
        Assert.Same(position, Assert.Single(row.SeatMapPositions));
        Assert.Throws<NotSupportedException>(() => ((ICollection<SeatMapPosition>)row.SeatMapPositions).Clear());
        Assert.Throws<InvalidOperationException>(() => row.AddPosition(position));
        var other = SeatMapRow.Create(configuration.SeatMap, cabin, SeatMapRowType.Seats);
        Assert.Throws<InvalidOperationException>(() => other.AddPosition(position));
        Assert.Empty(other.SeatMapPositions);
        Assert.Throws<ArgumentNullException>(() => row.AddPosition(null!));
    }
}
