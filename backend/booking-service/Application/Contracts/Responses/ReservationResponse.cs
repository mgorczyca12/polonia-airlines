namespace booking_service.Application.Contracts.Responses;

/// <summary>A reservation with its reservation status and the progress of its inventory workflow.</summary>
public sealed record ReservationResponse(
    int Id,
    string Code,
    string Status,
    DateTimeOffset ReservationDate,
    string Currency,
    decimal TotalAmount,
    string GuestAccessToken,
    FareSnapshot[] Fares,
    Guid WorkflowId,
    string WorkflowStatus,
    string? FailureCode);
