namespace flight_service.Domain.ValueObjects;

public sealed class AircraftOperationalState
{
    public static readonly AircraftOperationalState Active = new AircraftOperationalState("ACTIVE");
    public static readonly AircraftOperationalState Inactive = new AircraftOperationalState("INACTIVE");
    public static readonly AircraftOperationalState Maintenance = new AircraftOperationalState("MAINTENANCE");
    public static readonly AircraftOperationalState Decommissioned = new AircraftOperationalState("DECOMMISSIONED");


    public string Code { get; }

    public AircraftOperationalState(string code)
    {
        Code = code;
    }
}