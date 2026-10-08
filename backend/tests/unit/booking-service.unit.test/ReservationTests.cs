using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.unit.test;

public class ReservationTests
{
    private static readonly DateTimeOffset BookingDate = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    private static Reservation Create(params string[] flightIds) =>
        Reservation.Create(" ABC123 ", 1, flightIds, Fare.Create("Economy", 200m, " pln "), BookingDate);

    public static TheoryData<string[]> ValidItineraries => new()
    {
        new[] { "1" },
        new[] { "1", "2", "3" }
    };

    public static TheoryData<string[]> InvalidItineraries => new()
    {
        Array.Empty<string>(),
        new[] { "" },
        new[] { " " },
        new[] { "1", "1" }
    };

    [Theory]
    [MemberData(nameof(ValidItineraries))]
    public void CreatesSingleFlightOrItinerary(string[] flights)
    {
        var reservation = Create(flights);
        flights[0] = "99";

        Assert.Equal("ABC123", reservation.Code);
        Assert.Equal(1, reservation.UserId);
        Assert.Equal(BookingDate, reservation.ReservationDate);
        Assert.Equal(ReservationStatus.Created, reservation.Status);
        Assert.Equal("1", reservation.FlightIds[0]);
        Assert.Equal(flights.Length, reservation.FlightIds.Count);
        Assert.Equal("PLN", reservation.Fare.Currency);
    }

    [Theory]
    [MemberData(nameof(InvalidItineraries))]
    public void RejectsInvalidItineraries(string[] flights) =>
        Assert.Throws<ArgumentException>(() => Create(flights));

    [Fact]
    public void AggregateOwnsPassengersAndBaggage()
    {
        var reservation = Create("1");
        var passenger = reservation.AddPassenger(" Jan ", " Kowalski ", new DateOnly(1990, 1, 1));
        var baggage = reservation.AddBaggage(passenger, " Checked bag ", 20m);

        Assert.Equal("Jan", passenger.FirstName);
        Assert.Equal("Kowalski", passenger.LastName);
        Assert.Equal("Checked bag", baggage.Name);
        Assert.Equal(20m, baggage.WeightKg);
        Assert.Same(passenger, Assert.Single(reservation.Passengers));
        Assert.Same(baggage, Assert.Single(passenger.Baggage));
        Assert.Throws<NotSupportedException>(() => ((ICollection<Passenger>)reservation.Passengers).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<Baggage>)passenger.Baggage).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)reservation.FlightIds).Add("2"));
    }

    [Fact]
    public void RejectsForeignPassengerEvenWithSameTransientId()
    {
        var reservation = Create("1");
        reservation.AddPassenger("Jan", "Kowalski", new DateOnly(1990, 1, 1));
        var foreign = Create("2").AddPassenger("Anna", "Kowalska", new DateOnly(1990, 1, 1));

        Assert.Throws<InvalidOperationException>(() => reservation.AddBaggage(foreign, "Bag", 10m));
    }

    [Fact]
    public void ReserveRequiresPassengerAndFreezesBookingDetails()
    {
        var reservation = Create("1");
        Assert.Throws<InvalidOperationException>(reservation.Reserve);
        var passenger = reservation.AddPassenger("Jan", "Kowalski", new DateOnly(1990, 1, 1));
        reservation.Reserve();

        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
        Assert.Throws<InvalidOperationException>(reservation.Reserve);
        Assert.Throws<InvalidOperationException>(() => reservation.AddPassenger("Anna", "Kowalska", new DateOnly(1990, 1, 1)));
        Assert.Throws<InvalidOperationException>(() => reservation.AddBaggage(passenger, "Bag", 10m));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CancelIsTerminalAndRetainsHistory(bool reserve)
    {
        var reservation = Create("1");
        var passenger = reservation.AddPassenger("Jan", "Kowalski", new DateOnly(1990, 1, 1));
        reservation.RecordPayment("payment-1", 50m, "PLN", BookingDate);
        if (reserve)
            reservation.Reserve();
        reservation.Cancel();

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Single(reservation.Passengers);
        Assert.Single(reservation.Payments);
        Assert.Throws<InvalidOperationException>(reservation.Reserve);
        Assert.Throws<InvalidOperationException>(reservation.Cancel);
        Assert.Throws<InvalidOperationException>(() => reservation.AddBaggage(passenger, "Bag", 10m));
        Assert.Throws<InvalidOperationException>(() => reservation.RecordPayment("payment-2", 50m, "PLN", BookingDate));
    }

    [Fact]
    public void PaymentsSupportPartialAmountsAndRejectDuplicatesMismatchAndOverpayment()
    {
        var reservation = Create("1");
        var payment = reservation.RecordPayment(" payment-1 ", 50m, "pln", BookingDate);
        Assert.Equal("payment-1", payment.Reference);
        Assert.Equal("PLN", payment.Currency);
        Assert.Equal(BookingDate, payment.RecordedAt);
        Assert.Throws<InvalidOperationException>(() => reservation.RecordPayment("payment-1", 50m, "PLN", BookingDate));
        Assert.Throws<InvalidOperationException>(() => reservation.RecordPayment("payment-2", 50m, "EUR", BookingDate));
        Assert.Throws<InvalidOperationException>(() => reservation.RecordPayment("payment-2", 151m, "PLN", BookingDate));
        reservation.RecordPayment("payment-2", 150m, "PLN", BookingDate);
        Assert.Equal(200m, reservation.Payments.Sum(item => item.Amount));
        Assert.Throws<NotSupportedException>(() => ((ICollection<Payment>)reservation.Payments).Clear());
    }

    [Fact]
    public void RejectsInvalidPassengerBaggageAndPaymentInputsWithoutMutation()
    {
        var reservation = Create("1");
        Assert.Throws<ArgumentException>(() => reservation.AddPassenger("", "Name", new DateOnly(1990, 1, 1)));
        Assert.Throws<ArgumentException>(() => reservation.AddPassenger("Name", "Name", default));
        Assert.Throws<ArgumentException>(() => reservation.AddPassenger("Name", "Name", new DateOnly(2027, 1, 1)));
        Assert.Empty(reservation.Passengers);
        var passenger = reservation.AddPassenger("Jan", "Kowalski", new DateOnly(1990, 1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => reservation.AddBaggage(passenger, "Bag", 0));
        Assert.Throws<ArgumentException>(() => reservation.AddBaggage(passenger, "", 10));
        Assert.Empty(passenger.Baggage);
        Assert.Throws<ArgumentOutOfRangeException>(() => reservation.RecordPayment("reference", 0m, "PLN", BookingDate));
        Assert.Throws<ArgumentException>(() => reservation.RecordPayment("reference", 10m, "PLN", default));
        Assert.Empty(reservation.Payments);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PL")]
    [InlineData("PLNN")]
    [InlineData("P1N")]
    public void RejectsMalformedCurrencies(string currency) =>
        Assert.Throws<ArgumentException>(() => Fare.Create("Economy", 100m, currency));

    [Fact]
    public void FareRejectsNegativeAmountsButAllowsZeroQuote()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Fare.Create("Economy", -1m, "PLN"));
        Assert.Equal(0m, Fare.Create("Economy", 0m, "PLN").Amount);
    }

    [Fact]
    public void UserContactUpdatesValidateBeforeChangingState()
    {
        var user = User.Create(1, " Jan ", " jan@example.com ");
        Assert.Equal(1, user.Id);
        Assert.Equal("Jan", user.DisplayName);
        Assert.Equal("jan@example.com", user.Email);
        Assert.Throws<ArgumentException>(() => user.UpdateContactDetails("New name", "invalid"));
        Assert.Equal("Jan", user.DisplayName);
        user.UpdateContactDetails("Anna", "anna@example.com");
        Assert.Equal("Anna", user.DisplayName);
        Assert.Throws<ArgumentOutOfRangeException>(() => User.Create(0, "Name", "name@example.com"));
    }

    [Fact]
    public void ReservationRequiresIdentityQuoteAndDate()
    {
        Assert.Throws<ArgumentException>(() => Reservation.Create("", 1, ["1"], Fare.Create("Fare", 1m, "PLN"), BookingDate));
        Assert.Throws<ArgumentOutOfRangeException>(() => Reservation.Create("Code", 0, ["1"], Fare.Create("Fare", 1m, "PLN"), BookingDate));
        Assert.Throws<ArgumentNullException>(() => Reservation.Create("Code", 1, ["1"], null!, BookingDate));
        Assert.Throws<ArgumentException>(() => Reservation.Create("Code", 1, ["1"], Fare.Create("Fare", 1m, "PLN"), default));
    }

    [Fact]
    public void ChildrenReferenceTheirParentAndItineraryHasExplicitSequence()
    {
        var reservation = Create("3", "1", "2");
        var passenger = reservation.AddPassenger("Jan", "Kowalski", new DateOnly(1990, 1, 1));
        var baggage = reservation.AddBaggage(passenger, "Bag", 10m);
        var payment = reservation.RecordPayment("reference", 50m, "PLN", BookingDate);

        Assert.Same(reservation, passenger.Reservation);
        Assert.Equal(reservation.Id, passenger.ReservationId);
        Assert.Same(passenger, baggage.Passenger);
        Assert.Equal(passenger.Id, baggage.PassengerId);
        Assert.Same(reservation, payment.Reservation);
        Assert.Equal(reservation.Id, payment.ReservationId);
        Assert.Same(reservation, reservation.Fare.Reservation);
        Assert.Equal(reservation.Id, reservation.Fare.ReservationId);
        Assert.Equal(new[] { "3", "1", "2" }, reservation.Flights.Select(flight => flight.FlightId));
        Assert.Equal(new[] { 1, 2, 3 }, reservation.Flights.Select(flight => flight.SequenceNumber));
        Assert.Equal(new[] { "3", "1", "2" }, reservation.FlightIds);
        Assert.All(reservation.Flights, flight =>
        {
            Assert.Same(reservation, flight.Reservation);
            Assert.Equal(reservation.Id, flight.ReservationId);
        });
        Assert.Throws<NotSupportedException>(() => ((IList<ReservationFlight>)reservation.Flights).Clear());
    }

    [Fact]
    public void EachReservationHasItsOwnFareSnapshot()
    {
        var quote = Fare.Create("Economy", 100m, "PLN");
        var first = Reservation.Create("First", 1, ["1"], quote, BookingDate);
        var second = Reservation.Create("Second", 1, ["2"], quote, BookingDate);

        Assert.NotSame(quote, first.Fare);
        Assert.NotSame(first.Fare, second.Fare);
        Assert.Same(first, first.Fare.Reservation);
        Assert.Same(second, second.Fare.Reservation);
        Assert.Equal(quote.Amount, first.Fare.Amount);
        Assert.Equal(quote.Code, first.Fare.Code);
        Assert.Equal(quote.Currency, first.Fare.Currency);
    }

    [Theory]
    [InlineData("Created")]
    [InlineData("Reserved")]
    [InlineData("Cancelled")]
    public void KnownStatusesCanBeReconstructed(string status) =>
        Assert.Equal(status, ReservationStatus.From(status).Status);

    [Theory]
    [InlineData("")]
    [InlineData("created")]
    [InlineData("Unknown")]
    [InlineData(null)]
    public void UnknownStatusesFailExplicitly(string? status) =>
        Assert.Throws<ArgumentException>(() => ReservationStatus.From(status!));

    [Fact]
    public void ExternalFlightIdsAreOpaqueAndComparedOrdinally()
    {
        var reservation = Create("Flight-A", "flight-a", "  flight-b  ", "0");

        Assert.Equal(new[] { "Flight-A", "flight-a", "  flight-b  ", "0" }, reservation.FlightIds);
        Assert.Throws<ArgumentException>(() => Create("Flight-A", "Flight-A"));
        Assert.Throws<ArgumentNullException>(() => Create(null!));
        Assert.Throws<ArgumentException>(() => Create(new string[] { null! }));
    }
}
