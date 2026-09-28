namespace flight_service.Domain.Entities;

using flight_service.Domain.ValueObjects;
using Shared.Domain.Entities;

public class FlightSchedule : AggregateRoot<int>
{
    public int FlightId { get; private set; }
    public Flight Flight { get; private set; } = null!;
    public int DepartureAirportId { get; private set; }
    public Airport DepartureAirport { get; private set; } = null!;

    public int ArrivalAirportId { get; private set; }
    public Airport ArrivalAirport { get; private set; } = null!;

    public TimeOnly ScheduledDepartureTime { get; private set; }
    public TimeOnly ScheduledArrivalTime { get; private set; }

    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveUntil { get; private set; }

    public OperatingDays OperatingDays { get; private set; } = OperatingDays.None;
    public bool IsActive { get; private set; }

    private FlightSchedule() { }

    private FlightSchedule(
        Flight flight,
        Airport departureAirport,
        Airport arrivalAirport,
        TimeOnly scheduledDepartureTime,
        TimeOnly scheduledArrivalTime,
        DateOnly effectiveFrom,
        DateOnly? effectiveUntil,
        OperatingDays operatingDays)
    {
        FlightId = flight.Id;
        Flight = flight;
        DepartureAirportId = departureAirport.Id;
        DepartureAirport = departureAirport;
        ArrivalAirportId = arrivalAirport.Id;
        ArrivalAirport = arrivalAirport;
        ScheduledDepartureTime = scheduledDepartureTime;
        ScheduledArrivalTime = scheduledArrivalTime;
        EffectiveFrom = effectiveFrom;
        EffectiveUntil = effectiveUntil;
        OperatingDays = operatingDays;
        IsActive = true;
    }

    public static FlightSchedule Create(
        Flight flight,
        Airport departureAirport,
        Airport arrivalAirport,
        TimeOnly scheduledDepartureTime,
        TimeOnly scheduledArrivalTime,
        DateOnly effectiveFrom,
        DateOnly? effectiveUntil,
        OperatingDays operatingDays)
    {
        ArgumentNullException.ThrowIfNull(flight);
        ArgumentNullException.ThrowIfNull(departureAirport);
        ArgumentNullException.ThrowIfNull(arrivalAirport);

        if (scheduledArrivalTime <= scheduledDepartureTime)
            throw new ArgumentException("Scheduled arrival time must be after scheduled departure time.", nameof(scheduledArrivalTime));

        if (effectiveUntil < effectiveFrom)
            throw new ArgumentException("The schedule end date must not be before its start date.", nameof(effectiveUntil));

        if (operatingDays == OperatingDays.None)
            throw new ArgumentException("At least one operating day is required.", nameof(operatingDays));

        return new FlightSchedule(
            flight,
            departureAirport,
            arrivalAirport,
            scheduledDepartureTime,
            scheduledArrivalTime,
            effectiveFrom,
            effectiveUntil,
            operatingDays);
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}