using flight_service.Domain.ValueObjects;
using Shared.Domain.Entities;

namespace flight_service.Domain.Entities;

public class Aircraft : AggregateRoot<int>
{
    public string Registration { get; private set; } = null!;
    public string Icao24Address { get; private set; } = null!;
    public int AircraftTypeId { get; private set; }
    public AircraftType AircraftType { get; private set; } = null!;
    public int AircraftConfigurationId { get; private set; }
    public AircraftConfiguration AircraftConfiguration { get; private set; } = null!;
    public AircraftOperationalState OperationalState { get; private set; } = AircraftOperationalState.Active;
    private Aircraft() { }

    private Aircraft(string registration, string icao24Address, AircraftType aircraftType, AircraftConfiguration aircraftConfiguration)
    {
        Registration = registration;
        Icao24Address = icao24Address;
        AircraftTypeId = aircraftType.Id;
        AircraftType = aircraftType;
        AircraftConfigurationId = aircraftConfiguration.Id;
        AircraftConfiguration = aircraftConfiguration;
        OperationalState = AircraftOperationalState.Active;
    }

    public static Aircraft Create(string registration, string icao24Address, AircraftType aircraftType, AircraftConfiguration aircraftConfiguration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registration);
        ArgumentException.ThrowIfNullOrWhiteSpace(icao24Address);
        ArgumentNullException.ThrowIfNull(aircraftType);
        ArgumentNullException.ThrowIfNull(aircraftConfiguration);

        return new Aircraft(registration, icao24Address, aircraftType, aircraftConfiguration);
    }

    public void Activate()
    {
        if (OperationalState == AircraftOperationalState.Decommissioned)
            throw new InvalidOperationException("A decommissioned aircraft cannot be activated.");

        OperationalState = AircraftOperationalState.Active;
    }

    public void SendToMaintenance() => OperationalState = AircraftOperationalState.Maintenance;

    public void Deactivate()
    {
        if (OperationalState == AircraftOperationalState.Decommissioned)
            throw new InvalidOperationException("A decommissioned aircraft cannot be deactivated.");

        OperationalState = AircraftOperationalState.Inactive;
    }

    public void Decommission() => OperationalState = AircraftOperationalState.Decommissioned;
}