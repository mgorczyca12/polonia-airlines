using Shared.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.Domain.Entities
{
    public class Reservation : AggregateRoot<int>
    {
        private readonly List<ReservationFlight> flights = new();
        private readonly List<Passenger> passengers = new();
        private readonly List<Payment> payments = new();

        public string Code { get; private set; } = null!;
        public DateTimeOffset ReservationDate { get; private set; }
        public int UserId { get; private set; }
        public ReservationStatus Status { get; private set; } = ReservationStatus.Created;
        public Fare Fare { get; private set; } = null!;
        public IReadOnlyList<ReservationFlight> Flights => flights.AsReadOnly();
        public IReadOnlyList<string> FlightIds => flights.OrderBy(flight => flight.SequenceNumber)
            .Select(flight => flight.FlightId).ToList().AsReadOnly();
        public IReadOnlyCollection<Passenger> Passengers => passengers.AsReadOnly();
        public IReadOnlyCollection<Payment> Payments => payments.AsReadOnly();

        private Reservation() { }

        public static Reservation Create(string code, int userId, IEnumerable<string> flightIds,
            Fare fare, DateTimeOffset reservationDate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);
            ArgumentNullException.ThrowIfNull(flightIds);
            ArgumentNullException.ThrowIfNull(fare);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);
            if (reservationDate == default)
                throw new ArgumentException("Reservation date is required.", nameof(reservationDate));

            var itinerary = flightIds.ToList();
            if (itinerary.Count == 0 || itinerary.Any(string.IsNullOrWhiteSpace) ||
                itinerary.Distinct(StringComparer.Ordinal).Count() != itinerary.Count)
                throw new ArgumentException("An itinerary requires distinct non-blank flight IDs.", nameof(flightIds));

            var reservation = new Reservation
            {
                Code = code.Trim(),
                UserId = userId,
                ReservationDate = reservationDate
            };
            reservation.Fare = Fare.CreateForReservation(reservation, fare);
            for (var index = 0; index < itinerary.Count; index++)
                reservation.flights.Add(ReservationFlight.Create(reservation, itinerary[index], index + 1));
            return reservation;
        }

        public Passenger AddPassenger(string firstName, string lastName, DateOnly dateOfBirth)
        {
            EnsureCreated();
            if (dateOfBirth > DateOnly.FromDateTime(ReservationDate.UtcDateTime))
                throw new ArgumentException("Date of birth cannot be after the reservation date.", nameof(dateOfBirth));
            var passenger = Passenger.Create(this, firstName, lastName, dateOfBirth);
            passengers.Add(passenger);
            return passenger;
        }

        public Baggage AddBaggage(Passenger passenger, string name, decimal weightKg)
        {
            EnsureCreated();
            ArgumentNullException.ThrowIfNull(passenger);
            if (!passengers.Any(existing => ReferenceEquals(existing, passenger)))
                throw new InvalidOperationException("Passenger does not belong to this reservation.");
            return passenger.AddBaggage(name, weightKg);
        }

        public void Reserve()
        {
            EnsureCreated();
            if (passengers.Count == 0)
                throw new InvalidOperationException("A reservation requires at least one passenger.");
            Status = ReservationStatus.Reserved;
        }

        public void Cancel()
        {
            if (Status == ReservationStatus.Cancelled)
                throw new InvalidOperationException("Reservation is already cancelled.");
            Status = ReservationStatus.Cancelled;
        }

        public Payment RecordPayment(string reference, decimal amount, string currency, DateTimeOffset recordedAt)
        {
            if (Status == ReservationStatus.Cancelled)
                throw new InvalidOperationException("Cannot record a payment against a cancelled reservation.");
            var payment = Payment.Create(this, reference, amount, currency, recordedAt);
            if (payment.Currency != Fare.Currency)
                throw new InvalidOperationException("Payment currency must match the fare currency.");
            if (payments.Any(existing => existing.Reference == payment.Reference))
                throw new InvalidOperationException("Payment reference is already recorded.");
            if (payments.Sum(existing => existing.Amount) + payment.Amount > Fare.Amount)
                throw new InvalidOperationException("Recorded payments cannot exceed the quoted fare.");
            payments.Add(payment);
            return payment;
        }

        private void EnsureCreated()
        {
            if (Status != ReservationStatus.Created)
                throw new InvalidOperationException("Only a created reservation can be edited or reserved.");
        }
    }
}