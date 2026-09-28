namespace flight_service.Domain.Entities;

using Shared.Domain.Entities;

public class SeatMapPosition : Entity<int>
{
    public int SeatMapRowId { get; private set; }
    public SeatMapRow SeatMapRow { get; private set; } = null!;

    public string Column { get; private set; } = null!;
    public int AisleGroup { get; private set; }
    public bool IsSeat { get; private set; }
    private SeatMapPosition() { }

    private SeatMapPosition(string column, int aisleGroup, bool isSeat)
    {
        Column = column;
        AisleGroup = aisleGroup;
        IsSeat = isSeat;
    }

    public static SeatMapPosition Create(string column, int aisleGroup, bool isSeat)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(column);
        ArgumentOutOfRangeException.ThrowIfNegative(aisleGroup);

        return new SeatMapPosition(column, aisleGroup, isSeat);
    }

    public void MarkAsSeat() => IsSeat = true;

    public void MarkAsNonSeat() => IsSeat = false;
}