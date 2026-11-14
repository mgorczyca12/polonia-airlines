namespace booking_service.Application.Contracts.Responses;

/// <summary>A stored passenger name without unnecessary travel-document information.</summary>
public sealed record PassengerSummaryResponse(int Id, string FirstName, string LastName);
