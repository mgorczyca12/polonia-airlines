using flight_service.Domain.ValueObjects;
using Shared.Domain.Entities;

namespace flight_service.Domain.Entities;

public class SeatMapRow : Entity<int>
{
    public int AircraftConfigurationId { get; private set; }
    public AircraftConfiguration AircraftConfiguration { get; private set; } = null!;
    public int CabinId { get; private set; }
    public SeatMapCabin Cabin { get; private set; } = null!;
    public SeatMapRowType Type { get; private set; } = null!;
    public ICollection<SeatMapPosition> SeatMapPositions { get; private set; } = new List<SeatMapPosition>();

    private SeatMapRow() { }

    private SeatMapRow(AircraftConfiguration aircraftConfiguration, SeatMapCabin cabin, SeatMapRowType type)
    {
        AircraftConfigurationId = aircraftConfiguration.Id;
        AircraftConfiguration = aircraftConfiguration;
        CabinId = cabin.Id;
        Cabin = cabin;
        Type = type;
    }

    public static SeatMapRow Create(AircraftConfiguration aircraftConfiguration, SeatMapCabin cabin, SeatMapRowType type)
    {
        ArgumentNullException.ThrowIfNull(aircraftConfiguration);
        ArgumentNullException.ThrowIfNull(cabin);
        ArgumentNullException.ThrowIfNull(type);

        return new SeatMapRow(aircraftConfiguration, cabin, type);
    }

    public void AddPosition(SeatMapPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        SeatMapPositions.Add(position);
    }
}