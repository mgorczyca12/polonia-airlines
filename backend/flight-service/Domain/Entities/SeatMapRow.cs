using flight_service.Domain.ValueObjects;
using Shared.Domain.Entities;

namespace flight_service.Domain.Entities;

public class SeatMapRow : Entity<int>
{
    private readonly List<SeatMapPosition> seatMapPositions = new();
    public int SeatMapId { get; private set; }
    public SeatMap SeatMap { get; private set; } = null!;
    public int CabinId { get; private set; }
    public SeatMapCabin Cabin { get; private set; } = null!;
    public SeatMapRowType Type { get; private set; } = null!;
    public IReadOnlyCollection<SeatMapPosition> SeatMapPositions => seatMapPositions.AsReadOnly();

    private SeatMapRow()
    {
    }

    private SeatMapRow(SeatMap seatMap, SeatMapCabin cabin, SeatMapRowType type)
    {
        SeatMapId = seatMap.Id;
        SeatMap = seatMap;
        CabinId = cabin.Id;
        Cabin = cabin;
        Type = type;
    }

    public static SeatMapRow Create(
        SeatMap seatMap,
        SeatMapCabin cabin,
        SeatMapRowType type)
    {
        ArgumentNullException.ThrowIfNull(seatMap);
        ArgumentNullException.ThrowIfNull(cabin);
        ArgumentNullException.ThrowIfNull(type);

        return new SeatMapRow(seatMap, cabin, type);
    }

    public void AddPosition(SeatMapPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (seatMapPositions.Any(existing => ReferenceEquals(existing, position)))
        {
            throw new InvalidOperationException("Position is already part of this seat map row.");
        }

        position.AttachTo(this);
        seatMapPositions.Add(position);
    }
}
