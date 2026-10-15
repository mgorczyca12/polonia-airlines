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
        public string Currency { get; private set; } = null!;
        public decimal TotalAmount => passengers.Sum(passenger => passenger.TotalAmount);
        public IReadOnlyList<ReservationFlight> Flights => flights.AsReadOnly();
        public IReadOnlyList<string> FlightIds => flights
            .OrderBy(flight => flight.SequenceNumber)
            .Select(flight => flight.FlightId)
            .ToList()
            .AsReadOnly();
        public IReadOnlyCollection<Passenger> Passengers => passengers.AsReadOnly();
        public IReadOnlyCollection<Payment> Payments => payments.AsReadOnly();

        private Reservation()
        {
        }

        public static Reservation Create(
            string code,
            int userId,
            IEnumerable<string> flightIds,
            string currency,
            DateTimeOffset reservationDate)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(code);
            ArgumentNullException.ThrowIfNull(flightIds);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userId);

            if (reservationDate == default)
            {
                throw new ArgumentException("Reservation date is required.", nameof(reservationDate));
            }

            var itinerary = flightIds.ToList();
            if (itinerary.Count == 0 ||
                itinerary.Any(string.IsNullOrWhiteSpace) ||
                itinerary.Distinct(StringComparer.Ordinal).Count() != itinerary.Count)
            {
                throw new ArgumentException("An itinerary requires distinct non-blank flight IDs.", nameof(flightIds));
            }

            var reservation = new Reservation
            {
                Code = code.Trim(),
                UserId = userId,
                Currency = CurrencyCode.Normalize(currency),
                ReservationDate = reservationDate
            };

            for (var index = 0; index < itinerary.Count; index++)
            {
                reservation.flights.Add(ReservationFlight.Create(reservation, itinerary[index], index + 1));
            }

            return reservation;
        }

        /// <summary>
        /// Adds a passenger with exactly one fare quote for every itinerary flight.
        /// </summary>
        public Passenger AddPassenger(
            string firstName,
            string lastName,
            DateOnly dateOfBirth,
            IEnumerable<FareQuote> fareQuotes)
        {
            EnsureCreated();
            ArgumentNullException.ThrowIfNull(fareQuotes);

            if (dateOfBirth > DateOnly.FromDateTime(ReservationDate.UtcDateTime))
            {
                throw new ArgumentException("Date of birth cannot be after the reservation date.", nameof(dateOfBirth));
            }

            var quotes = ValidateFareQuotes(fareQuotes);

            var passenger = Passenger.Create(this, firstName, lastName, dateOfBirth);
            foreach (var flight in flights.OrderBy(flight => flight.SequenceNumber))
            {
                passenger.AddFare(flight, quotes[flight.FlightId]);
            }

            passengers.Add(passenger);
            return passenger;
        }

        public Baggage AddBaggage(Passenger passenger, string flightId, string name, decimal weightKg)
        {
            EnsureCreated();
            ArgumentNullException.ThrowIfNull(passenger);

            if (!passengers.Any(existing => ReferenceEquals(existing, passenger)))
            {
                throw new InvalidOperationException("Passenger does not belong to this reservation.");
            }

            return passenger.AddBaggage(flightId, name, weightKg);
        }

        public void Reserve()
        {
            EnsureCreated();

            if (passengers.Count == 0)
            {
                throw new InvalidOperationException("A reservation requires at least one passenger.");
            }

            Status = ReservationStatus.Reserved;
        }

        public void Cancel()
        {
            if (Status == ReservationStatus.Cancelled)
            {
                throw new InvalidOperationException("Reservation is already cancelled.");
            }

            Status = ReservationStatus.Cancelled;
        }

        public Payment RecordPayment(string reference, decimal amount, string currency, DateTimeOffset recordedAt)
        {
            if (Status == ReservationStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot record a payment against a cancelled reservation.");
            }

            var payment = Payment.Create(this, reference, amount, currency, recordedAt);
            if (payment.Currency != Currency)
            {
                throw new InvalidOperationException("Payment currency must match the reservation currency.");
            }

            if (payments.Any(existing => existing.Reference == payment.Reference))
            {
                throw new InvalidOperationException("Payment reference is already recorded.");
            }

            if (payments.Sum(existing => existing.Amount) + payment.Amount > TotalAmount)
            {
                throw new InvalidOperationException("Recorded payments cannot exceed the reservation total.");
            }

            payments.Add(payment);
            return payment;
        }

        private Dictionary<string, FareQuote> ValidateFareQuotes(IEnumerable<FareQuote> fareQuotes)
        {
            var quotes = new Dictionary<string, FareQuote>(StringComparer.Ordinal);

            foreach (var quote in fareQuotes)
            {
                ArgumentNullException.ThrowIfNull(quote, nameof(fareQuotes));

                if (!flights.Any(flight => flight.FlightId == quote.FlightId))
                {
                    throw new ArgumentException(
                        "Fare quote references a flight outside the itinerary.",
                        nameof(fareQuotes));
                }

                if (quote.Currency != Currency)
                {
                    throw new ArgumentException(
                        "Fare quote currency must match the reservation currency.",
                        nameof(fareQuotes));
                }

                if (!quotes.TryAdd(quote.FlightId, quote))
                {
                    throw new ArgumentException("A passenger can have only one fare per flight.", nameof(fareQuotes));
                }
            }

            if (quotes.Count != flights.Count)
            {
                throw new ArgumentException(
                    "A passenger requires a fare for every itinerary flight.",
                    nameof(fareQuotes));
            }

            return quotes;
        }

        private void EnsureCreated()
        {
            if (Status != ReservationStatus.Created)
            {
                throw new InvalidOperationException("Only a created reservation can be edited or reserved.");
            }
        }
    }
}
