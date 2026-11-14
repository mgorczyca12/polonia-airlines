namespace booking_service.Application.Contracts.Requests;

/// <summary>Finds active, priced fare products for up to one hundred opaque flight references.</summary>
public sealed record SearchFaresRequest
{
    public string[] FlightIds { get; init; } = [];
    public string Currency { get; init; } = string.Empty;
}
