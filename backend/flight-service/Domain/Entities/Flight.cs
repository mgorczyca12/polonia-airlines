namespace flight_service.Domain.Entities;

using Shared.Domain.Entities;

public class Flight : AggregateRoot<int>
{
    private readonly List<FlightSegment> flightSegments = new();
    public string FlightNumber { get; private set; } = null!;
    public DateTime DepartureTime { get; private set; }
    public DateTime ArrivalTime { get; private set; }

    public int DepartureAirportId { get; private set; }
    public Airport DepartureAirport { get; private set; } = null!;

    public int ArrivalAirportId { get; private set; }
    public Airport ArrivalAirport { get; private set; } = null!;

    public IReadOnlyCollection<FlightSegment> FlightSegments => flightSegments.AsReadOnly();

    private Flight() { }

    private Flight(string flightNumber, DateTime departureTime, DateTime arrivalTime, Airport departureAirport, Airport arrivalAirport)
    {
        FlightNumber = flightNumber;
        DepartureTime = departureTime;
        ArrivalTime = arrivalTime;
        DepartureAirportId = departureAirport.Id;
        DepartureAirport = departureAirport;
        ArrivalAirportId = arrivalAirport.Id;
        ArrivalAirport = arrivalAirport;
    }

    public static Flight Create(string flightNumber, DateTime departureTime, DateTime arrivalTime, Airport departureAirport, Airport arrivalAirport)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(flightNumber);
        ArgumentNullException.ThrowIfNull(departureAirport);
        ArgumentNullException.ThrowIfNull(arrivalAirport);

        if (arrivalTime <= departureTime)
            throw new ArgumentException("Arrival time must be after departure time.", nameof(arrivalTime));

        return new Flight(flightNumber, departureTime, arrivalTime, departureAirport, arrivalAirport);
    }

    public void AddSegment(FlightSegment segment)
    {
        ArgumentNullException.ThrowIfNull(segment);
        if (flightSegments.Any(existing => ReferenceEquals(existing, segment)))
            throw new InvalidOperationException("Segment is already part of this flight.");
        segment.AttachTo(this);
        flightSegments.Add(segment);
    }
}