namespace flight_service.Application.Contracts.Responses;

/// <summary>Passenger layout of the aircraft currently assigned to a flight, not seat availability.</summary>
public sealed record FlightSeatMapResponse(
    int FlightId,
    int AircraftId,
    string Registration,
    int ConfigurationId,
    string ConfigurationCode,
    int ConfigurationVersion,
    SeatMapResponse SeatMap);

/// <summary>A passenger seat map with rows in display order.</summary>
public sealed record SeatMapResponse(int Id, IReadOnlyList<SeatMapRowResponse> Rows);

/// <summary>A numbered layout row with cabin, descriptive types, and ordered positions.</summary>
public sealed record SeatMapRowResponse(
    int Id,
    int Number,
    int CabinId,
    string CabinName,
    string CabinCode,
    string Type,
    IReadOnlyList<SeatMapPositionResponse> Positions);

/// <summary>A seat or non-seat position; column and aisle group describe its placement.</summary>
public sealed record SeatMapPositionResponse(int Id, string Column, int AisleGroup, bool IsSeat);

/// <summary>An aircraft configuration and its current passenger layout.</summary>
public sealed record ConfigurationResponse(
    int Id,
    int AircraftTypeId,
    string Code,
    string Name,
    int Version,
    bool IsActive,
    SeatMapResponse SeatMap);
