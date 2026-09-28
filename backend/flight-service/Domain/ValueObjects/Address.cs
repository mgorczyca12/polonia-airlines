namespace flight_service.Domain.ValueObjects;

public sealed class Address : IEquatable<Address>
{
    public static Address Create(string city, string state, string country)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(state);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        return new Address(city, state, country);
    }

    public string City { get; }
    public string State { get; }
    public string Country { get; }

    public Address(string city, string state, string country)
    {
        City = city;
        State = state;
        Country = country;
    }

    public bool Equals(Address? other) =>
        other is not null &&
        City == other.City &&
        State == other.State &&
        Country == other.Country;

    public override bool Equals(object? obj) => Equals(obj as Address);

    public override int GetHashCode() => HashCode.Combine(City, State, Country);
}
