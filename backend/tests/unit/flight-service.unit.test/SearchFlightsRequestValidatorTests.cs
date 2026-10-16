using flight_service.Application.Contracts.Requests;
using flight_service.Application.Features.FlightFeatures.Queries;

namespace flight_service.unit.test;

public class SearchFlightsRequestValidatorTests
{
    private readonly SearchFlightsRequestValidator validator = new();
    private static SearchFlightsRequest ValidRequest => new()
    {
        DepartureAirport = "WAW",
        ArrivalAirport = "KRK",
        DepartureDate = new DateOnly(2026, 10, 13)
    };

    [Fact]
    public void Validate_DefaultPaginationOrMaximumPageSize_Succeeds()
    {
        Assert.True(validator.Validate(ValidRequest).IsValid);
        Assert.Equal(1, ValidRequest.Page);
        Assert.Equal(20, ValidRequest.PageSize);
        Assert.True(validator.Validate(ValidRequest with
        {
            PageSize = 100
        }).IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("waw")]
    [InlineData(" WAW")]
    [InlineData("WAW ")]
    [InlineData("WAW\n")]
    [InlineData("EPWA")]
    [InlineData("WA1")]
    public void Validate_NonExactUppercaseIataCodes_ReturnsAirportFieldErrors(string code)
    {
        Assert.Contains(validator.Validate(ValidRequest with
        {
            DepartureAirport = code
        }).Errors,
            error => error.PropertyName == nameof(SearchFlightsRequest.DepartureAirport));
        Assert.Contains(validator.Validate(ValidRequest with
        {
            ArrivalAirport = code
        }).Errors,
            error => error.PropertyName == nameof(SearchFlightsRequest.ArrivalAirport));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, 101)]
    [InlineData(int.MaxValue, 100)]
    public void Validate_InvalidPagination_ReturnsValidationFailure(int page, int pageSize)
    {
        Assert.False(validator.Validate(ValidRequest with
        {
            Page = page,
            PageSize = pageSize
        }).IsValid);
    }

    [Fact]
    public void Validate_MaximumPageWithSingleItemPageSize_SucceedsWithoutOverflow()
    {
        Assert.True(validator.Validate(ValidRequest with
        {
            Page = int.MaxValue,
            PageSize = 1
        }).IsValid);
    }

    [Fact]
    public void Validate_MissingDateOrUnrepresentableEndOfDay_ReturnsValidationFailure()
    {
        Assert.False(validator.Validate(ValidRequest with
        {
            DepartureDate = default
        }).IsValid);
        Assert.False(validator.Validate(ValidRequest with
        {
            DepartureDate = DateOnly.MaxValue
        }).IsValid);
    }
}
