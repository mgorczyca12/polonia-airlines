namespace flight_service.Application.Contracts.Responses;

/// <summary>Represents a flight with UTC timestamps and IATA airport codes.</summary>
public sealed record FlightResponse(
    int Id,
    string FlightNumber,
    DateTimeOffset DepartureTime,
    DateTimeOffset ArrivalTime,
    string DepartureAirport,
    string ArrivalAirport);
