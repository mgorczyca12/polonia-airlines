namespace flight_service.Application.Contracts.Requests;

[Flags]
public enum SeatMapRowKind
{
    Seats = 1,
    ExitRow = 2,
    Bulkhead = 4,
    Galley = 8,
    Lavatory = 16
}

/// <summary>Full editable contents of one numbered seat-map row.</summary>
public sealed record SeatMapRowRequest(
    int CabinId,
    SeatMapRowKind Type,
    IReadOnlyList<SeatMapPositionRequest> Positions);

/// <summary>A column's aisle group and whether it represents a seat.</summary>
public sealed record SeatMapPositionRequest(string Column, int AisleGroup, bool IsSeat);

/// <summary>The desired active state of an aircraft configuration.</summary>
public sealed record ConfigurationActiveRequest(bool IsActive);

/// <summary>The aircraft to assign to a flight.</summary>
public sealed record AircraftAssignmentRequest(int AircraftId);
