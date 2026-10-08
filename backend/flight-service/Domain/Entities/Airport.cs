using flight_service.Domain.ValueObjects;
using Shared.Domain.Entities;

namespace flight_service.Domain.Entities;

public class Airport : AggregateRoot<int>
{
    private readonly List<AirportGate> gates = new();
    public string Name { get; private set; } = null!;
    public string IcaoCode { get; private set; } = null!;
    public string IataCode { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public IReadOnlyCollection<AirportGate> Gates => gates.AsReadOnly();

    private Airport() { }

    private Airport(string name, string icaoCode, string iataCode, Address address)
    {
        Name = name;
        IcaoCode = icaoCode;
        IataCode = iataCode;
        Address = address;
    }

    public static Airport Create(string name, string icaoCode, string iataCode, Address address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(icaoCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(iataCode);
        ArgumentNullException.ThrowIfNull(address);

        return new Airport(name, icaoCode, iataCode, address);
    }

    public void AddGate(AirportGate gate)
    {
        ArgumentNullException.ThrowIfNull(gate);
        if (!ReferenceEquals(gate.Airport, this))
            throw new InvalidOperationException("Gate belongs to another airport.");
        if (gates.Any(existing => ReferenceEquals(existing, gate)))
            throw new InvalidOperationException("Gate is already part of this airport.");
        gates.Add(gate);
    }
}