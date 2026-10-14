namespace flight_service.Application.Contracts.Requests;

/// <summary>Searches flights by IATA airport codes and UTC departure date.</summary>
public sealed record SearchFlightsRequest
{
    public string DepartureAirport { get; init; } = string.Empty;
    public string ArrivalAirport { get; init; } = string.Empty;
    public DateOnly DepartureDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
