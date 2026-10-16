using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.unit.test;

public class ReservationTests
{
    private static readonly DateTimeOffset BookingDate = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Adult = new(1990, 1, 1);

    // 1 included piece, 23 kg per piece, up to 2 paid extra pieces at 50 each.
    private static readonly BaggageAllowance Allowance = BaggageAllowance.Create(1, 23m, 2, 50m);

    private static Reservation Create(params string[] flightIds)
    {
        return Reservation.Create(" ABC123 ", 1, flightIds, " pln ", BookingDate);
    }

    private static FareQuote Quote(
        string flightId,
        decimal amount = 200m,
        string currency = "PLN",
        string cabin = "Economy",
        BaggageAllowance? allowance = null)
    {
        return FareQuote.Create(flightId, "ECO-BASIC", cabin, amount, currency, allowance ?? Allowance);
    }

    private static Passenger AddPassenger(
        Reservation reservation,
        string firstName = "Jan",
        decimal amount = 200m,
        BaggageAllowance? allowance = null)
    {
        return reservation.AddPassenger(
            firstName,
            "Kowalski",
            Adult,
            reservation.FlightIds.Select(id => Quote(id, amount, allowance: allowance)));
    }

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
    public void Create_ValidItinerary_InitializesReservationAndCopiesFlightIds(string[] flights)
    {
        var reservation = Create(flights);
        flights[0] = "99";

        Assert.Equal("ABC123", reservation.Code);
        Assert.Equal(1, reservation.UserId);
        Assert.Equal(BookingDate, reservation.ReservationDate);
        Assert.Equal(ReservationStatus.Created, reservation.Status);
        Assert.Equal("1", reservation.FlightIds[0]);
        Assert.Equal(flights.Length, reservation.FlightIds.Count);
        Assert.Equal("PLN", reservation.Currency);
        Assert.Equal(0m, reservation.TotalAmount);
    }

    [Theory]
    [MemberData(nameof(InvalidItineraries))]
    public void Create_InvalidItinerary_ThrowsArgumentException(string[] flights)
    {
        Assert.Throws<ArgumentException>(() => Create(flights));
    }

    [Fact]
    public void AddBaggage_ValidPassenger_ExposesReadOnlyAggregateCollections()
    {
        var reservation = Create("1");
        var passenger = reservation.AddPassenger(" Jan ", " Kowalski ", Adult, [Quote("1")]);
        var baggage = reservation.AddBaggage(passenger, "1", " Checked bag ", 20m);

        Assert.Equal("Jan", passenger.FirstName);
        Assert.Equal("Kowalski", passenger.LastName);
        Assert.Equal("Checked bag", baggage.Name);
        Assert.Equal(20m, baggage.WeightKg);
        Assert.Equal(0m, baggage.Charge);
        Assert.Same(passenger, Assert.Single(reservation.Passengers));
        Assert.Same(baggage, Assert.Single(passenger.Baggage));
        Assert.Throws<NotSupportedException>(() => ((ICollection<Passenger>)reservation.Passengers).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<Baggage>)passenger.Baggage).Clear());
        Assert.Throws<NotSupportedException>(() => ((ICollection<PassengerFare>)passenger.Fares).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)reservation.FlightIds).Add("2"));
    }

    [Fact]
    public void AddPassenger_QuotesForEachFlight_SnapshotsFaresInItineraryOrder()
    {
        var reservation = Create("1", "2");
        var passenger = reservation.AddPassenger("Jan", "Kowalski", Adult,
            [Quote("2", 300m, cabin: "Business"), Quote("1", 100m)]);

        Assert.Equal(new[] { "1", "2" }, passenger.Fares.Select(fare => fare.ReservationFlight.FlightId));
        var business = passenger.Fares.Single(fare => fare.ReservationFlight.FlightId == "2");
        Assert.Equal("Business", business.CabinClass);
        Assert.Equal("ECO-BASIC", business.ProductCode);
        Assert.Equal(300m, business.Amount);
        Assert.Equal("PLN", business.Currency);
        Assert.Same(Allowance, business.BaggageAllowance);
        Assert.Same(passenger, business.Passenger);
        Assert.Equal(400m, passenger.TotalAmount);
        Assert.Equal(400m, reservation.TotalAmount);
    }

    [Fact]
    public void AddPassenger_MultiplePassengers_KeepsFaresIndependentAndTotalsAmounts()
    {
        var reservation = Create("1");
        var economy = reservation.AddPassenger("Jan", "Kowalski", Adult, [Quote("1", 100m)]);
        var business = reservation.AddPassenger("Anna", "Kowalska", Adult, [Quote("1", 500m, cabin: "Business")]);

        Assert.NotSame(economy.Fares.Single(), business.Fares.Single());
        Assert.Equal(600m, reservation.TotalAmount);
    }

    [Fact]
    public void AddPassenger_InvalidFareQuotes_ThrowsWithoutAddingPassenger()
    {
        var reservation = Create("1", "2");

        Assert.Throws<ArgumentNullException>(() => reservation.AddPassenger("Jan", "Kowalski", Adult, null!));
        Assert.Throws<ArgumentNullException>(() => reservation.AddPassenger("Jan", "Kowalski", Adult, [null!]));
        Assert.Throws<ArgumentException>(() => reservation.AddPassenger("Jan", "Kowalski", Adult, []));
        Assert.Throws<ArgumentException>(() => reservation.AddPassenger("Jan", "Kowalski", Adult, [Quote("1")]));
        Assert.Throws<ArgumentException>(() =>
            reservation.AddPassenger("Jan", "Kowalski", Adult, [Quote("1"), Quote("1")]));
        Assert.Throws<ArgumentException>(() =>
            reservation.AddPassenger("Jan", "Kowalski", Adult, [Quote("1"), Quote("3")]));
        Assert.Throws<ArgumentException>(() => reservation.AddPassenger("Jan", "Kowalski", Adult,
            [Quote("1"), Quote("2", currency: "EUR")]));
        Assert.Empty(reservation.Passengers);
    }

    [Fact]
    public void AddBaggage_IncludedAndExtraPieces_AppliesChargesAndEnforcesPieceLimit()
    {
        var reservation = Create("1");
        var passenger = AddPassenger(reservation);

        Assert.Equal(0m, reservation.AddBaggage(passenger, "1", "Included", 23m).Charge);
        Assert.Equal(50m, reservation.AddBaggage(passenger, "1", "Extra 1", 10m).Charge);
        Assert.Equal(50m, reservation.AddBaggage(passenger, "1", "Extra 2", 10m).Charge);
        Assert.Equal(300m, passenger.TotalAmount);

        Assert.Throws<InvalidOperationException>(() => reservation.AddBaggage(passenger, "1", "Too many", 5m));
        Assert.Equal(3, passenger.Baggage.Count);
        Assert.Equal(300m, reservation.TotalAmount);
    }

    [Fact]
    public void AddBaggage_MultiplePassengersAndFlights_AppliesAllowanceIndependently()
    {
        var reservation = Create("1", "2");
        var first = AddPassenger(reservation, "Jan");
        var second = AddPassenger(reservation, "Anna");

        Assert.Equal(0m, reservation.AddBaggage(first, "1", "Bag", 10m).Charge);
        Assert.Equal(0m, reservation.AddBaggage(first, "2", "Bag", 10m).Charge);
        Assert.Equal(0m, reservation.AddBaggage(second, "1", "Bag", 10m).Charge);
        Assert.Equal(50m, reservation.AddBaggage(first, "1", "Extra", 10m).Charge);
    }

    [Fact]
    public void AddBaggage_WeightExceedsAllowance_ThrowsWithoutAddingBaggage()
    {
        var reservation = Create("1");
        var passenger = AddPassenger(reservation);

        Assert.Throws<InvalidOperationException>(() => reservation.AddBaggage(passenger, "1", "Heavy", 23.1m));
        Assert.Empty(passenger.Baggage);
    }

    [Fact]
    public void AddBaggage_FareHasNoAllowance_ThrowsInvalidOperationException()
    {
        var reservation = Create("1");
        var passenger = AddPassenger(reservation, allowance: BaggageAllowance.None);

        Assert.Throws<InvalidOperationException>(() => reservation.AddBaggage(passenger, "1", "Bag", 1m));
    }

    [Fact]
    public void AddBaggage_MissingOrUnmatchedFlightId_ThrowsWithoutAddingBaggage()
    {
        var reservation = Create("1");
        var passenger = AddPassenger(reservation);

        Assert.Throws<InvalidOperationException>(() => reservation.AddBaggage(passenger, "2", "Bag", 10m));
        Assert.Throws<ArgumentException>(() => reservation.AddBaggage(passenger, " ", "Bag", 10m));
        Assert.Empty(passenger.Baggage);
    }

    [Fact]
    public void AddBaggage_ForeignPassengerWithSameTransientId_ThrowsInvalidOperationException()
    {
        var reservation = Create("1");
        AddPassenger(reservation);
        var foreign = AddPassenger(Create("1"), "Anna");

        Assert.Throws<InvalidOperationException>(() => reservation.AddBaggage(foreign, "1", "Bag", 10m));
    }

    [Fact]
    public void Reserve_CreatedReservation_RequiresPassengerAndFreezesBookingDetails()
    {
        var reservation = Create("1");
        Assert.Throws<InvalidOperationException>(reservation.Reserve);
        var passenger = AddPassenger(reservation);
        reservation.Reserve();

        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
        Assert.Throws<InvalidOperationException>(reservation.Reserve);
        Assert.Throws<InvalidOperationException>(() => AddPassenger(reservation, "Anna"));
        Assert.Throws<InvalidOperationException>(() => reservation.AddBaggage(passenger, "1", "Bag", 10m));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancel_CreatedOrReservedReservation_RetainsHistoryAndRejectsFurtherChanges(bool reserve)
    {
        var reservation = Create("1");
        var passenger = AddPassenger(reservation);
        reservation.RecordPayment("payment-1", 50m, "PLN", BookingDate);
        if (reserve)
        {
            reservation.Reserve();
        }

        reservation.Cancel();

        Assert.Equal(ReservationStatus.Cancelled, reservation.Status);
        Assert.Single(reservation.Passengers);
        Assert.Single(reservation.Payments);
        Assert.Throws<InvalidOperationException>(reservation.Reserve);
        Assert.Throws<InvalidOperationException>(reservation.Cancel);
        Assert.Throws<InvalidOperationException>(() => reservation.AddBaggage(passenger, "1", "Bag", 10m));
        Assert.Throws<InvalidOperationException>(() => reservation.RecordPayment("payment-2", 50m, "PLN", BookingDate));
    }

    [Fact]
    public void RecordPayment_PartialPayments_EnforcesReferenceCurrencyAndOutstandingBalance()
    {
        var reservation = Create("1");
        AddPassenger(reservation);
        var payment = reservation.RecordPayment(" payment-1 ", 50m, "pln", BookingDate);
        Assert.Equal("payment-1", payment.Reference);
        Assert.Equal("PLN", payment.Currency);
        Assert.Equal(BookingDate, payment.RecordedAt);
        Assert.Throws<InvalidOperationException>(() => reservation.RecordPayment("payment-1", 50m, "PLN", BookingDate));
        Assert.Throws<InvalidOperationException>(() => reservation.RecordPayment("payment-2", 50m, "EUR", BookingDate));
        Assert.Throws<InvalidOperationException>(() =>
            reservation.RecordPayment("payment-2", 151m, "PLN", BookingDate));
        reservation.RecordPayment("payment-2", 150m, "PLN", BookingDate);
        Assert.Equal(200m, reservation.Payments.Sum(item => item.Amount));
        Assert.Throws<NotSupportedException>(() => ((ICollection<Payment>)reservation.Payments).Clear());
    }

    [Fact]
    public void RecordPayment_ReservationHasBaggageCharges_IncludesChargesInPaymentLimit()
    {
        var reservation = Create("1");
        var passenger = AddPassenger(reservation);
        reservation.AddBaggage(passenger, "1", "Included", 10m);
        reservation.AddBaggage(passenger, "1", "Extra", 10m);

        Assert.Equal(250m, reservation.TotalAmount);
        Assert.Throws<InvalidOperationException>(() =>
            reservation.RecordPayment("payment-1", 251m, "PLN", BookingDate));
        reservation.RecordPayment("payment-2", 250m, "PLN", BookingDate);
    }

    [Fact]
    public void AddPassengerAndAddBaggageAndRecordPayment_InvalidInputs_ThrowsWithoutMutation()
    {
        var reservation = Create("1");
        Assert.Throws<ArgumentException>(() => reservation.AddPassenger("", "Name", Adult, [Quote("1")]));
        Assert.Throws<ArgumentException>(() => reservation.AddPassenger("Name", "Name", default, [Quote("1")]));
        Assert.Throws<ArgumentException>(() =>
            reservation.AddPassenger("Name", "Name", new DateOnly(2027, 1, 1), [Quote("1")]));
        Assert.Empty(reservation.Passengers);
        var passenger = AddPassenger(reservation);
        Assert.Throws<ArgumentOutOfRangeException>(() => reservation.AddBaggage(passenger, "1", "Bag", 0));
        Assert.Throws<ArgumentException>(() => reservation.AddBaggage(passenger, "1", "", 10));
        Assert.Empty(passenger.Baggage);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            reservation.RecordPayment("reference", 0m, "PLN", BookingDate));
        Assert.Throws<ArgumentException>(() => reservation.RecordPayment("reference", 10m, "PLN", default));
        Assert.Empty(reservation.Payments);
    }

    [Theory]
    [InlineData("")]
    [InlineData("PL")]
    [InlineData("PLNN")]
    [InlineData("P1N")]
    public void Create_MalformedCurrency_ThrowsArgumentException(string currency)
    {
        Assert.Throws<ArgumentException>(() => Quote("1", currency: currency));
        Assert.Throws<ArgumentException>(() => Reservation.Create("Code", 1, ["1"], currency, BookingDate));
    }

    [Fact]
    public void Create_NegativeOrZeroFareAmount_RejectsNegativeAndAcceptsZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Quote("1", -1m));
        Assert.Equal(0m, Quote("1", 0m).Amount);
    }

    [Fact]
    public void Create_FareQuoteInputs_RequiresIdentifiersAndAllowanceAndPreservesRepresentation()
    {
        Assert.Throws<ArgumentException>(() => FareQuote.Create(" ", "P", "C", 1m, "PLN", Allowance));
        Assert.Throws<ArgumentException>(() => FareQuote.Create("1", "", "C", 1m, "PLN", Allowance));
        Assert.Throws<ArgumentException>(() => FareQuote.Create("1", "P", " ", 1m, "PLN", Allowance));
        Assert.Throws<ArgumentNullException>(() => FareQuote.Create("1", "P", "C", 1m, "PLN", null!));
        var quote = FareQuote.Create(" 1 ", " P ", " Premium ", 1m, "PLN", Allowance);
        Assert.Equal(" 1 ", quote.FlightId);
        Assert.Equal(" P ", quote.ProductCode);
        Assert.Equal(" Premium ", quote.CabinClass);
    }

    [Fact]
    public void Create_BaggageAllowanceLimits_RejectsInvalidLimitsAndCalculatesMaxPieces()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BaggageAllowance.Create(-1, 23m, 0, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => BaggageAllowance.Create(1, 0m, 0, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => BaggageAllowance.Create(1, 23m, -1, 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => BaggageAllowance.Create(1, 23m, 0, -1m));
        Assert.Equal(3, Allowance.MaxPieces);
    }

    [Fact]
    public void UpdateContactDetails_InvalidThenValidInputs_ValidatesBeforeChangingState()
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
    public void Create_MissingReservationIdentityCurrencyOrDate_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Reservation.Create("", 1, ["1"], "PLN", BookingDate));
        Assert.Throws<ArgumentOutOfRangeException>(() => Reservation.Create("Code", 0, ["1"], "PLN", BookingDate));
        Assert.Throws<ArgumentException>(() => Reservation.Create("Code", 1, ["1"], " ", BookingDate));
        Assert.Throws<ArgumentException>(() => Reservation.Create("Code", 1, ["1"], "PLN", default));
    }

    [Fact]
    public void Create_MultiFlightReservationWithChildren_PreservesParentReferencesAndItinerarySequence()
    {
        var reservation = Create("3", "1", "2");
        var passenger = AddPassenger(reservation);
        var baggage = reservation.AddBaggage(passenger, "1", "Bag", 10m);
        var payment = reservation.RecordPayment("reference", 50m, "PLN", BookingDate);

        Assert.Same(reservation, passenger.Reservation);
        Assert.Equal(reservation.Id, passenger.ReservationId);
        Assert.Same(passenger, baggage.Passenger);
        Assert.Equal(passenger.Id, baggage.PassengerId);
        Assert.Same(passenger.Fares.Single(fare => fare.ReservationFlight.FlightId == "1"), baggage.PassengerFare);
        Assert.Same(reservation, payment.Reservation);
        Assert.Equal(reservation.Id, payment.ReservationId);
        Assert.Equal(new[] { "3", "1", "2" }, reservation.Flights.Select(flight => flight.FlightId));
        Assert.Equal(new[] { 1, 2, 3 }, reservation.Flights.Select(flight => flight.SequenceNumber));
        Assert.Equal(new[] { "3", "1", "2" }, reservation.FlightIds);
        Assert.All(reservation.Flights, flight =>
        {
            Assert.Same(reservation, flight.Reservation);
            Assert.Equal(reservation.Id, flight.ReservationId);
        });
        Assert.All(passenger.Fares, fare =>
        {
            Assert.Same(passenger, fare.Passenger);
            Assert.Equal(passenger.Id, fare.PassengerId);
            Assert.Contains(fare.ReservationFlight, reservation.Flights);
        });
        Assert.Throws<NotSupportedException>(() => ((IList<ReservationFlight>)reservation.Flights).Clear());
    }

    [Theory]
    [InlineData("Created")]
    [InlineData("Reserved")]
    [InlineData("Cancelled")]
    public void From_KnownReservationStatus_ReconstructsStatus(string status)
    {
        Assert.Equal(status, ReservationStatus.From(status).Status);
    }

    [Theory]
    [InlineData("")]
    [InlineData("created")]
    [InlineData("Unknown")]
    [InlineData(null)]
    public void From_UnknownReservationStatus_ThrowsArgumentException(string? status)
    {
        Assert.Throws<ArgumentException>(() => ReservationStatus.From(status!));
    }

    [Fact]
    public void Create_OpaqueExternalFlightIds_PreservesRepresentationAndUsesOrdinalComparison()
    {
        var reservation = Create("Flight-A", "flight-a", "  flight-b  ", "0");

        Assert.Equal(new[] { "Flight-A", "flight-a", "  flight-b  ", "0" }, reservation.FlightIds);
        Assert.Throws<ArgumentException>(() => Create("Flight-A", "Flight-A"));
        Assert.Throws<ArgumentNullException>(() => Create(null!));
        Assert.Throws<ArgumentException>(() => Create(new string[] { null! }));

        var passenger = AddPassenger(reservation);
        Assert.Equal(4, passenger.Fares.Count);
        Assert.Throws<ArgumentException>(() => reservation.AddPassenger("Jan", "Kowalski", Adult,
            reservation.FlightIds.Select(id => Quote(id.ToUpperInvariant()))));
    }
}
