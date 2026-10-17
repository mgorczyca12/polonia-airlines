using flight_service.Domain.Entities;
using flight_service.Infrastructure.Persistence.Configuration;
using Microsoft.EntityFrameworkCore;
using MassTransit;

namespace flight_service.Infrastructure.Persistence;

public class FlightServiceDbContext : DbContext
{
    public DbSet<Flight> Flights { get; set; }
    public DbSet<Aircraft> Aircrafts { get; set; }
    public DbSet<AircraftConfiguration> AircraftConfigurations { get; set; }
    public DbSet<AircraftType> AircraftTypes { get; set; }
    public DbSet<Airport> Airports { get; set; }
    public DbSet<AirportGate> AirportGates { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<FlightSegment> FlightSegments { get; set; }
    public DbSet<SeatMapCabin> SeatMapCabins { get; set; }
    public DbSet<SeatMap> SeatMaps { get; set; }
    public DbSet<SeatMapPosition> SeatMapPositions { get; set; }
    public DbSet<SeatMapRow> SeatMapRows { get; set; }

    public FlightServiceDbContext(DbContextOptions<FlightServiceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FlightConfiguration).Assembly,
            type => type.Namespace == typeof(FlightConfiguration).Namespace);
    }
}
