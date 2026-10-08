using Shared.Domain.Entities;

namespace booking_service.Domain.Entities;

public class ReservationFlight : Entity<int>
{
    public int ReservationId { get; private set; }
    public Reservation Reservation { get; private set; } = null!;
    public int FlightId { get; private set; }
    public int SequenceNumber { get; private set; }

    private ReservationFlight() { }

    internal static ReservationFlight Create(Reservation reservation, int flightId, int sequenceNumber)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(flightId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sequenceNumber);
        return new ReservationFlight
        {
            ReservationId = reservation.Id,
            Reservation = reservation,
            FlightId = flightId,
            SequenceNumber = sequenceNumber
        };
    }
}
