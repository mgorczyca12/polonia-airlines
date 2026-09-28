using flight_service.Domain.ValueObjects;
using Shared.Domain.Entities;

namespace flight_service.Domain.Entities;

public class Airport : AggregateRoot<int>
{
    public string Name { get; private set; } = null!;
    public string IcaoCode { get; private set; } = null!;
    public string IataCode { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public ICollection<AirportGate> Gates { get; private set; } = new List<AirportGate>();

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
}