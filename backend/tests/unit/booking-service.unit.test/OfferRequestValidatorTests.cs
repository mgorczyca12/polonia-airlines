using booking_service.Application.Contracts.Requests;
using booking_service.Application.Features.CheckoutFeatures.Commands.CreateOffer;

namespace booking_service.unit.test;

public class OfferRequestValidatorTests
{
    private static readonly Guid Product = Guid.NewGuid();
    private readonly OfferRequestValidator validator = new();

    [Fact]
    public void Validate_OneProductPerFlightForEveryPassenger_IsValid()
    {
        var request = new OfferRequest("PLN", 2, [new("a", Product), new("b", Product)]);

        Assert.True(validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_PerPassengerSelections_MustCoverEveryPassengerOnce()
    {
        var complete = new OfferRequest("PLN", 2, [new("a", Product, 0), new("a", Product, 1)]);
        var missing = new OfferRequest("PLN", 2, [new("a", Product, 0)]);
        var duplicate = new OfferRequest("PLN", 2, [new("a", Product), new("a", Product, 1)]);
        var outOfRange = new OfferRequest("PLN", 1, [new("a", Product, 1)]);

        Assert.True(validator.Validate(complete).IsValid);
        Assert.False(validator.Validate(missing).IsValid);
        Assert.False(validator.Validate(duplicate).IsValid);
        Assert.False(validator.Validate(outOfRange).IsValid);
    }

    [Theory]
    [InlineData("pln", 1)]
    [InlineData("PL", 1)]
    [InlineData("", 1)]
    [InlineData("PLN", 0)]
    [InlineData("PLN", 10)]
    public void Validate_InvalidCurrencyOrPassengerCount_IsInvalid(string currency, int passengers)
    {
        var request = new OfferRequest(currency, passengers, [new("a", Product)]);

        Assert.False(validator.Validate(request).IsValid);
    }

    [Fact]
    public void Validate_EmptyOrOversizedSelections_IsInvalid()
    {
        var tooManyFlights = Enumerable.Range(0, 9).Select(index => new OfferSelection($"f{index}", Product)).ToArray();

        Assert.False(validator.Validate(new OfferRequest("PLN", 1, [])).IsValid);
        Assert.False(validator.Validate(new OfferRequest("PLN", 1, tooManyFlights)).IsValid);
        Assert.False(validator.Validate(new OfferRequest("PLN", 1, [new(" ", Product)])).IsValid);
        Assert.False(validator.Validate(new OfferRequest("PLN", 1, [new("a", Guid.Empty)])).IsValid);
    }
}
