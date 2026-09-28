namespace flight_service.Domain.Entities;

using Shared.Domain.Entities;

public class AirportGate : Entity<int>
{
    public string Terminal { get; private set; } = null!;
    public string GateNumber { get; private set; } = null!;
    public int AirportId { get; private set; }
    public Airport Airport { get; private set; } = null!;

    private AirportGate() { }

    private AirportGate(Airport airport, string terminal, string gateNumber)
    {
        AirportId = airport.Id;
        Airport = airport;
        Terminal = terminal;
        GateNumber = gateNumber;
    }

    public static AirportGate Create(Airport airport, string terminal, string gateNumber)
    {
        ArgumentNullException.ThrowIfNull(airport);
        ArgumentException.ThrowIfNullOrWhiteSpace(terminal);
        ArgumentException.ThrowIfNullOrWhiteSpace(gateNumber);

        return new AirportGate(airport, terminal, gateNumber);
    }
}