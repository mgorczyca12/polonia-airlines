namespace flight_service.Domain.Entities;

using Shared.Domain.Entities;

public class AircraftConfiguration : AggregateRoot<int>
{
    public int AircraftTypeId { get; private set; }
    public AircraftType AircraftType { get; private set; } = null!;
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int Version { get; private set; }
    public bool IsActive { get; private set; }
    public SeatMap SeatMap { get; private set; } = null!;

    private AircraftConfiguration()
    {
    }

    private AircraftConfiguration(AircraftType aircraftType, string code, string name, int version)
    {
        AircraftTypeId = aircraftType.Id;
        AircraftType = aircraftType;
        Code = code;
        Name = name;
        Version = version;
        IsActive = true;
        SeatMap = new SeatMap(this);
    }

    public static AircraftConfiguration Create(AircraftType aircraftType, string code, string name, int version)
    {
        ArgumentNullException.ThrowIfNull(aircraftType);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(version);

        return new AircraftConfiguration(aircraftType, code, name, version);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
