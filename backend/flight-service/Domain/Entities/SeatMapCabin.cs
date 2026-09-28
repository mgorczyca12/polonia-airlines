namespace flight_service.Domain.Entities;

using Shared.Domain.Entities;

public class SeatMapCabin : Entity<int>
{
    public string Name { get; private set; } = null!;
    public string IataCode { get; private set; } = null!;

    private SeatMapCabin() { }

    private SeatMapCabin(string name, string iataCode)
    {
        Name = name;
        IataCode = iataCode;
    }

    public static SeatMapCabin Create(string name, string iataCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(iataCode);

        return new SeatMapCabin(name, iataCode);
    }
}