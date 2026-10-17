using Shared.Domain.Entities;

namespace flight_service.Domain.Entities;

public class SeatMap : Entity<int>
{
    private readonly List<SeatMapRow> seatMapRows = new();

    public int AircraftConfigurationId { get; private set; }
    public AircraftConfiguration AircraftConfiguration { get; private set; } = null!;
    public IReadOnlyCollection<SeatMapRow> SeatMapRows => seatMapRows.AsReadOnly();

    private SeatMap()
    {
    }

    internal SeatMap(AircraftConfiguration aircraftConfiguration)
    {
        ArgumentNullException.ThrowIfNull(aircraftConfiguration);

        AircraftConfigurationId = aircraftConfiguration.Id;
        AircraftConfiguration = aircraftConfiguration;
    }

    public void AddRow(SeatMapRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (!ReferenceEquals(row.SeatMap, this))
        {
            throw new InvalidOperationException("Seat map row belongs to another seat map.");
        }

        if (seatMapRows.Any(existing => ReferenceEquals(existing, row)))
        {
            throw new InvalidOperationException("Seat map row is already part of this seat map.");
        }

        seatMapRows.Add(row);
    }
}
