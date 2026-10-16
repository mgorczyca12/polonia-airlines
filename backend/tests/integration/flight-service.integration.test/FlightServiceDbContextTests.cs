using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;
using flight_service.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace flight_service.integration.test;

public class FlightServiceDbContextTests
{
    [Fact]
    public void OnModelCreating_AllDomainEntities_MapsKeysAndFieldAccessWithoutShadowForeignKeysOrDomainEvents()
    {
        using var context = CreateContext(new SqliteConnection("Data Source=:memory:"));
        var entities = context.Model.GetEntityTypes().Where(entity => !entity.IsOwned()).ToList();

        Assert.Equal(11, entities.Count);
        Assert.All(entities, entity =>
        {
            Assert.NotNull(entity.FindPrimaryKey());
            Assert.Null(entity.FindProperty("DomainEvents"));
            Assert.Null(entity.FindNavigation("DomainEvents"));
            Assert.All(entity.GetForeignKeys(), key =>
                Assert.All(key.Properties, property => Assert.False(property.IsShadowProperty())));
        });
        AssertFieldAccess<Flight>(context, nameof(Flight.FlightSegments), "flightSegments");
        AssertFieldAccess<Airport>(context, nameof(Airport.Gates), "gates");
        AssertFieldAccess<AircraftConfiguration>(context, nameof(AircraftConfiguration.SeatMapRows), "seatMapRows");
        AssertFieldAccess<SeatMapRow>(context, nameof(SeatMapRow.SeatMapPositions), "seatMapPositions");
    }

    [Fact]
    public async Task SaveChangesAsync_FlightAirportAndScheduleGraph_RoundTripsRelationshipsWithGeneratedKeys()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var departure = Airport.Create("Warsaw", "EPWA", "WAW", Address.Create("Warsaw", "Mazovia", "PL"));
        var arrival = Airport.Create("Krakow", "EPKK", "KRK", Address.Create("Krakow", "Malopolska", "PL"));
        departure.AddGate(AirportGate.Create(departure, "A", "1"));
        var flight = Flight.Create("PA123", new DateTime(2026, 10, 13, 10, 0, 0),
            new DateTime(2026, 10, 13, 12, 0, 0), departure, arrival);
        flight.AddSegment(FlightSegment.Create(1, departure, arrival));
        var schedule = FlightSchedule.Create(flight, departure, arrival,
            new TimeOnly(10, 0), new TimeOnly(12, 0), new DateOnly(2026, 10, 13),
            new DateOnly(2026, 12, 31), OperatingDays.Monday | OperatingDays.Friday);
        context.FlightSchedules.Add(schedule);
        await context.SaveChangesAsync();

        Assert.True(flight.Id > 0);
        Assert.Equal(flight.Id, flight.FlightSegments.Single().FlightId);
        Assert.Equal(departure.Id, departure.Gates.Single().AirportId);
        context.ChangeTracker.Clear();

        var loaded = await context.FlightSchedules
            .Include(value => value.Flight).ThenInclude(value => value.FlightSegments)
            .Include(value => value.DepartureAirport).ThenInclude(value => value.Gates)
            .Include(value => value.ArrivalAirport).SingleAsync();
        Assert.Equal("PA123", loaded.Flight.FlightNumber);
        Assert.Equal(new TimeOnly(10, 0), loaded.ScheduledDepartureTime);
        Assert.Equal(new TimeOnly(12, 0), loaded.ScheduledArrivalTime);
        Assert.Equal(new DateOnly(2026, 10, 13), loaded.EffectiveFrom);
        Assert.Equal(new DateOnly(2026, 12, 31), loaded.EffectiveUntil);
        Assert.Equal(OperatingDays.Monday | OperatingDays.Friday, loaded.OperatingDays);
        Assert.Equal(Address.Create("Warsaw", "Mazovia", "PL"), loaded.DepartureAirport.Address);
        Assert.Single(loaded.DepartureAirport.Gates);
        Assert.Same(loaded.Flight, loaded.Flight.FlightSegments.Single().Flight);

        loaded.DepartureAirport.AddGate(AirportGate.Create(loaded.DepartureAirport, "A", "2"));
        await context.SaveChangesAsync();
        Assert.Equal(2, await context.AirportGates.CountAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_AircraftLayout_RoundTripsStateAndPreservesTransitionGuards()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var type = AircraftType.Create("B788", "787-8", "Boeing");
        var layout = AircraftConfiguration.Create(type, "STD", "Standard", 1);
        var row = SeatMapRow.Create(layout, SeatMapCabin.Create("Economy", "Y"),
            SeatMapRowType.Seats | SeatMapRowType.ExitRow);
        row.AddPosition(SeatMapPosition.Create("A", 0, true));
        row.AddPosition(SeatMapPosition.Create("B", 0, false));
        layout.AddSeatMapRow(row);
        var aircraft = Aircraft.Create("SP-LRA", "48AE00", type, layout);
        aircraft.Decommission();
        context.Aircrafts.Add(aircraft);
        await context.SaveChangesAsync();

        Assert.True(row.Id > 0);
        Assert.Equal(layout.Id, row.AircraftConfigurationId);
        Assert.All(row.SeatMapPositions, position => Assert.Equal(row.Id, position.SeatMapRowId));
        context.ChangeTracker.Clear();

        var loaded = await context.Aircrafts.Include(value => value.AircraftType)
            .Include(value => value.AircraftConfiguration).ThenInclude(value => value.SeatMapRows)
                .ThenInclude(value => value.SeatMapPositions)
            .Include(value => value.AircraftConfiguration).ThenInclude(value => value.SeatMapRows)
                .ThenInclude(value => value.Cabin).SingleAsync();
        Assert.Same(AircraftOperationalState.Decommissioned, loaded.OperationalState);
        Assert.Throws<InvalidOperationException>(loaded.Activate);
        Assert.Throws<InvalidOperationException>(loaded.Deactivate);
        var loadedRow = Assert.Single(loaded.AircraftConfiguration.SeatMapRows);
        Assert.Equal(SeatMapRowType.Seats | SeatMapRowType.ExitRow, loadedRow.Type);
        Assert.Equal("Economy", loadedRow.Cabin.Name);
        Assert.Equal(2, loadedRow.SeatMapPositions.Count);
        loadedRow.SeatMapPositions.Single(position => position.Column == "B").MarkAsSeat();
        loaded.SendToMaintenance();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.Same(AircraftOperationalState.Maintenance, (await context.Aircrafts.SingleAsync()).OperationalState);
        Assert.All(await context.SeatMapPositions.ToListAsync(), position => Assert.True(position.IsSeat));
    }

    [Fact]
    public async Task ExecuteSqlRawAsync_DeletingReferencedAirportsOrFlights_RestrictsAirportsAndCascadesOwnedSegments()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var airport = Airport.Create("Warsaw", "EPWA", "WAW", Address.Create("Warsaw", "Mazovia", "PL"));
        var flight = Flight.Create("PA123", new DateTime(2026, 10, 13, 10, 0, 0),
            new DateTime(2026, 10, 13, 12, 0, 0), airport, airport);
        flight.AddSegment(FlightSegment.Create(1, airport, airport));
        context.Flights.Add(flight);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync(
            "DELETE FROM Airports"));
        await context.Database.ExecuteSqlRawAsync("DELETE FROM Flights");
        Assert.Empty(await context.FlightSegments.ToListAsync());
        Assert.Single(await context.Airports.ToListAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_DuplicateSegmentSequence_ThrowsDbUpdateException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var airport = Airport.Create("Warsaw", "EPWA", "WAW", Address.Create("Warsaw", "Mazovia", "PL"));
        var flight = Flight.Create("PA123", new DateTime(2026, 10, 13, 10, 0, 0),
            new DateTime(2026, 10, 13, 12, 0, 0), airport, airport);
        flight.AddSegment(FlightSegment.Create(1, airport, airport));
        flight.AddSegment(FlightSegment.Create(1, airport, airport));
        context.Flights.Add(flight);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static FlightServiceDbContext CreateContext(SqliteConnection connection)
    {
        return new(new DbContextOptionsBuilder<FlightServiceDbContext>().UseSqlite(connection).Options);
    }

    private static void AssertFieldAccess<TEntity>(FlightServiceDbContext context, string navigation, string field)
    {
        var mapped = context.Model.FindEntityType(typeof(TEntity))!.FindNavigation(navigation)!;
        Assert.Equal(field, mapped.FieldInfo!.Name);
        Assert.Equal(PropertyAccessMode.Field, mapped.GetPropertyAccessMode());
    }
}
