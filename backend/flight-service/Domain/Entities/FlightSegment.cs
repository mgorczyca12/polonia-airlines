namespace flight_service.Domain.Entities;

using Shared.Domain.Entities;

public class FlightSegment : Entity<int>
{
    public int FlightId { get; private set; }
    public Flight Flight { get; private set; } = null!;
    public int SequenceNumber { get; private set; }
    public int DepartureAirportId { get; private set; }
    public Airport DepartureAirport { get; private set; } = null!;
    public int ArrivalAirportId { get; private set; }
    public Airport ArrivalAirport { get; private set; } = null!;
    private FlightSegment() { }

    private FlightSegment(int sequenceNumber, Airport departureAirport, Airport arrivalAirport)
    {
        SequenceNumber = sequenceNumber;
        DepartureAirportId = departureAirport.Id;
        DepartureAirport = departureAirport;
        ArrivalAirportId = arrivalAirport.Id;
        ArrivalAirport = arrivalAirport;
    }

    public static FlightSegment Create(int sequenceNumber, Airport departureAirport, Airport arrivalAirport)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequenceNumber);
        ArgumentNullException.ThrowIfNull(departureAirport);
        ArgumentNullException.ThrowIfNull(arrivalAirport);

        return new FlightSegment(sequenceNumber, departureAirport, arrivalAirport);
    }

}