namespace booking_service.Domain.ValueObjects;

public sealed record BaggageAllowance
{
    public int IncludedPieces { get; private set; }
    public decimal MaxWeightPerPieceKg { get; private set; }
    public int MaxExtraPieces { get; private set; }
    public decimal ExtraPiecePrice { get; private set; }

    private BaggageAllowance() { }

    public static BaggageAllowance Create(int includedPieces, decimal maxWeightPerPieceKg,
        int maxExtraPieces, decimal extraPiecePrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(includedPieces);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxWeightPerPieceKg);
        ArgumentOutOfRangeException.ThrowIfNegative(maxExtraPieces);
        ArgumentOutOfRangeException.ThrowIfNegative(extraPiecePrice);
        return new BaggageAllowance
        {
            IncludedPieces = includedPieces,
            MaxWeightPerPieceKg = maxWeightPerPieceKg,
            MaxExtraPieces = maxExtraPieces,
            ExtraPiecePrice = extraPiecePrice
        };
    }

    public static BaggageAllowance None { get; } = Create(0, 1m, 0, 0m);

    public int MaxPieces => IncludedPieces + MaxExtraPieces;

    /// <summary>
    /// Returns the charge for the next piece given the pieces already booked. Included pieces
    /// are free, extra pieces cost <see cref="ExtraPiecePrice"/>, and anything beyond
    /// <see cref="MaxPieces"/> or <see cref="MaxWeightPerPieceKg"/> is a hard limit.
    /// </summary>
    public decimal ChargeForNextPiece(int existingPieces, decimal weightKg)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(existingPieces);
        if (weightKg > MaxWeightPerPieceKg)
            throw new InvalidOperationException("Baggage exceeds the maximum weight allowed by the fare.");
        if (existingPieces >= MaxPieces)
            throw new InvalidOperationException("Baggage exceeds the maximum number of pieces allowed by the fare.");
        return existingPieces < IncludedPieces ? 0m : ExtraPiecePrice;
    }
}
