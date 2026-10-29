namespace booking_service.Application.Contracts.Responses;

public sealed record BaggageSnapshot(
    int IncludedPieces,
    decimal MaxWeightPerPieceKg,
    int MaxExtraPieces,
    decimal ExtraPiecePrice);

/// <summary>An active fare product. Cabin class is a product attribute, not a physical cabin.</summary>
public sealed record ProductResponse(
    Guid Id,
    string Code,
    int Version,
    string CabinClass,
    string Brand,
    BaggageSnapshot Baggage);
