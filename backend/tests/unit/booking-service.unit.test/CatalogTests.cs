using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;

namespace booking_service.unit.test;

public class CatalogTests
{
    [Fact]
    public void FareProduct_Create_IsActiveUntilRetiredAndCopiesTerms()
    {
        var allowance = BaggageAllowance.Create(1, 23m, 2, 50m);

        var product = FareProduct.Create("LIGHT", 1, "Economy", "Light", allowance);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.True(product.Active);
        Assert.Equal(allowance, product.BaggageAllowance);
        Assert.NotSame(allowance, product.BaggageAllowance);

        product.Retire();

        Assert.False(product.Active);
    }

    [Fact]
    public void FareProduct_Create_InvalidInputs_Throws()
    {
        Assert.Throws<ArgumentException>(() => FareProduct.Create(" ", 1, "Economy", "Light", BaggageAllowance.None));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FareProduct.Create("LIGHT", 0, "Economy", "Light", BaggageAllowance.None));
        Assert.Throws<ArgumentException>(() => FareProduct.Create("LIGHT", 1, " ", "Light", BaggageAllowance.None));
        Assert.Throws<ArgumentException>(() => FareProduct.Create("LIGHT", 1, "Economy", " ", BaggageAllowance.None));
        Assert.Throws<ArgumentNullException>(() => FareProduct.Create("LIGHT", 1, "Economy", "Light", null!));
    }

    [Fact]
    public void FlightPrice_CreateAndReprice_NormalizesCurrencyAndPreservesOpaqueIds()
    {
        var productId = Guid.NewGuid();

        var price = FlightPrice.Create(" flight ", productId, " pln ", 125m, " cabin ");

        Assert.Equal(" flight ", price.FlightId);
        Assert.Equal(" cabin ", price.CabinId);
        Assert.Equal("PLN", price.Currency);
        Assert.Equal(productId, price.ProductId);

        price.Reprice(99m, "cabin-2");

        Assert.Equal(99m, price.Amount);
        Assert.Equal("cabin-2", price.CabinId);
        Assert.Throws<ArgumentOutOfRangeException>(() => price.Reprice(-1m, "cabin"));
    }

    [Fact]
    public void FlightPrice_Create_InvalidInputs_Throws()
    {
        var productId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => FlightPrice.Create(" ", productId, "PLN", 1m, "cabin"));
        Assert.Throws<ArgumentException>(() => FlightPrice.Create("flight", Guid.Empty, "PLN", 1m, "cabin"));
        Assert.Throws<ArgumentOutOfRangeException>(() => FlightPrice.Create("flight", productId, "PLN", -1m, "cabin"));
        Assert.Throws<ArgumentException>(() => FlightPrice.Create("flight", productId, "PLN", 1m, " "));
        Assert.Throws<ArgumentException>(() => FlightPrice.Create("flight", productId, "PL", 1m, "cabin"));
    }

    [Fact]
    public void ReservationCheckout_IsReplayOf_ComparesPayloadHashesOrdinally()
    {
        var checkout = ReservationCheckout.Create("token", "request", "ABC", Guid.NewGuid(), 7, Guid.NewGuid());

        Assert.True(checkout.IsReplayOf("ABC"));
        Assert.False(checkout.IsReplayOf("abc"));
        Assert.Throws<ArgumentException>(() =>
            ReservationCheckout.Create("token", "request", "ABC", Guid.Empty, 7, Guid.NewGuid()));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ReservationCheckout.Create("token", "request", "ABC", Guid.NewGuid(), 0, Guid.NewGuid()));
    }
}
