using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.unit.test;

public class CheckoutOfferTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 28, 12, 0, 0, TimeSpan.Zero);

    private static OfferedFare Fare(int passenger, string currency = "PLN", decimal amount = 100m)
    {
        return OfferedFare.Create(
            passenger,
            "flight-1",
            Guid.NewGuid(),
            "LIGHT",
            1,
            "cabin-1",
            "Economy",
            "Light",
            amount,
            currency,
            BaggageAllowance.None);
    }

    [Fact]
    public void Create_ValidFares_ExpiresAfterFifteenMinutesAndExposesTotals()
    {
        var offer = CheckoutOffer.Create("hash", [Fare(0), Fare(1, amount: 150m)], Now);

        Assert.NotEqual(Guid.Empty, offer.Id);
        Assert.Equal(Now.AddMinutes(15), offer.ExpiresAt);
        Assert.Equal("PLN", offer.Currency);
        Assert.Equal(250m, offer.TotalAmount);
        Assert.False(offer.Consumed);
        Assert.False(offer.IsExpiredAt(Now.AddMinutes(15).AddTicks(-1)));
        Assert.True(offer.IsExpiredAt(Now.AddMinutes(15)));
    }

    [Fact]
    public void Create_InvalidInputs_Throws()
    {
        Assert.Throws<ArgumentException>(() => CheckoutOffer.Create(" ", [Fare(0)], Now));
        Assert.Throws<ArgumentException>(() => CheckoutOffer.Create("hash", [], Now));
        Assert.Throws<ArgumentException>(() => CheckoutOffer.Create("hash", [Fare(0)], default));
        Assert.Throws<ArgumentException>(() => CheckoutOffer.Create("hash", [Fare(0), Fare(1, "EUR")], Now));
    }

    [Fact]
    public void OfferedFare_ToQuote_PreservesSnapshotAndPhysicalCabin()
    {
        var fare = Fare(0, amount: 125m);

        var quote = fare.ToQuote();

        Assert.Equal("flight-1", quote.FlightId);
        Assert.Equal("LIGHT", quote.ProductCode);
        Assert.Equal("cabin-1", quote.CabinId);
        Assert.Equal("Economy", quote.CabinClass);
        Assert.Equal("Light", quote.Brand);
        Assert.Equal(1, quote.ProductVersion);
        Assert.Equal(125m, quote.Amount);
    }
}
