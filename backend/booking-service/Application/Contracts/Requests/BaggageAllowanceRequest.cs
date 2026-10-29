namespace booking_service.Application.Contracts.Requests;

/// <summary>Baggage terms of a fare product: included pieces, extra-piece limit and price, and weight limit.</summary>
public sealed record BaggageAllowanceRequest(
    int IncludedPieces,
    decimal MaxWeightPerPieceKg,
    int MaxExtraPieces,
    decimal ExtraPiecePrice);
