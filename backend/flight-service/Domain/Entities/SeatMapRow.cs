using flight_service.Domain.ValueObjects;
using Shared.Domain.Entities;

namespace flight_service.Domain.Entities;

public class SeatMapRow : Entity<int>
{
    private readonly List<SeatMapPosition> seatMapPositions = new();
    public int SeatMapId { get; private set; }
    public SeatMap SeatMap { get; private set; } = null!;
    public int Number { get; private set; }
    public int CabinId { get; private set; }
    public SeatMapCabin Cabin { get; private set; } = null!;
    public SeatMapRowType Type { get; private set; } = null!;
    public IReadOnlyCollection<SeatMapPosition> SeatMapPositions => seatMapPositions.AsReadOnly();

    private SeatMapRow()
    {
    }

    private SeatMapRow(SeatMap seatMap, SeatMapCabin cabin, SeatMapRowType type, int number)
    {
        SeatMapId = seatMap.Id;
        SeatMap = seatMap;
        CabinId = cabin.Id;
        Cabin = cabin;
        Type = type;
        Number = number;
    }

    public static SeatMapRow Create(
        SeatMap seatMap,
        SeatMapCabin cabin,
        SeatMapRowType type,
        int? number = null)
    {
        ArgumentNullException.ThrowIfNull(seatMap);
        ArgumentNullException.ThrowIfNull(cabin);
        ArgumentNullException.ThrowIfNull(type);

        var rowNumber = number ?? checked(seatMap.SeatMapRows.Select(row => row.Number).DefaultIfEmpty(0).Max() + 1);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rowNumber);

        return new SeatMapRow(seatMap, cabin, type, rowNumber);
    }

    public void AddPosition(SeatMapPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (seatMapPositions.Any(existing => ReferenceEquals(existing, position)))
        {
            throw new InvalidOperationException("Position is already part of this seat map row.");
        }

        if (seatMapPositions.Any(existing => string.Equals(existing.Column, position.Column, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("A position with this column already exists in the row.");
        }

        position.AttachTo(this);
        seatMapPositions.Add(position);
    }

    public void Update(SeatMapCabin cabin, SeatMapRowType type)
    {
        ArgumentNullException.ThrowIfNull(cabin);
        ArgumentNullException.ThrowIfNull(type);
        Cabin = cabin;
        CabinId = cabin.Id;
        Type = type;
    }

    public void RemovePosition(SeatMapPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (!seatMapPositions.Remove(position))
        {
            throw new InvalidOperationException("Position is not part of this row.");
        }
    }
}
