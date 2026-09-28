namespace flight_service.Domain.Entities;

using Shared.Domain.Entities;

public class AircraftType : Entity<int>
{
    public string IcaoCode { get; private set; } = null!;
    public string Model { get; private set; } = null!;
    public string Manufacturer { get; private set; } = null!;

    private AircraftType() { }

    private AircraftType(string icaoCode, string model, string manufacturer)
    {
        IcaoCode = icaoCode;
        Model = model;
        Manufacturer = manufacturer;
    }

    public static AircraftType Create(string icaoCode, string model, string manufacturer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(icaoCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        ArgumentException.ThrowIfNullOrWhiteSpace(manufacturer);

        return new AircraftType(icaoCode, model, manufacturer);
    }
}