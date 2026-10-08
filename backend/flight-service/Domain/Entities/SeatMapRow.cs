using flight_service.Domain.ValueObjects;
using Shared.Domain.Entities;

namespace flight_service.Domain.Entities;

public class SeatMapRow : Entity<int>
{
    private readonly List<SeatMapPosition> seatMapPositions = new();
    public int AircraftConfigurationId { get; private set; }
    public AircraftConfiguration AircraftConfiguration { get; private set; } = null!;
    public int CabinId { get; private set; }
    public SeatMapCabin Cabin { get; private set; } = null!;
    public SeatMapRowType Type { get; private set; } = null!;
    public IReadOnlyCollection<SeatMapPosition> SeatMapPositions => seatMapPositions.AsReadOnly();

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
        if (seatMapPositions.Any(existing => ReferenceEquals(existing, position)))
            throw new InvalidOperationException("Position is already part of this seat map row.");
        position.AttachTo(this);
        seatMapPositions.Add(position);
    }
}