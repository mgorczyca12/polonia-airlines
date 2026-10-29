using System.Text.Json;
using booking_service.Domain.ValueObjects;

namespace booking_service.Infrastructure.Persistence.Converters;

/// <summary>Stores an offer's fares as JSON through a flat DTO so the domain types stay encapsulated.</summary>
internal static class OfferedFareJson
{
    internal static string Serialize(IReadOnlyList<OfferedFare> fares)
    {
        return JsonSerializer.Serialize(fares.Select(ToStored).ToArray());
    }

    internal static IReadOnlyList<OfferedFare> Deserialize(string json)
    {
        var stored = JsonSerializer.Deserialize<StoredFare[]>(json)
            ?? throw new InvalidOperationException("The persisted offer fares are invalid.");

        return stored.Select(ToDomain).ToArray();
    }

    private static StoredFare ToStored(OfferedFare fare)
    {
        return new StoredFare(
            fare.PassengerIndex,
            fare.FlightId,
            fare.ProductId,
            fare.ProductCode,
            fare.ProductVersion,
            fare.CabinId,
            fare.CabinClass,
            fare.Brand,
            fare.Amount,
            fare.Currency,
            fare.BaggageAllowance.IncludedPieces,
            fare.BaggageAllowance.MaxWeightPerPieceKg,
            fare.BaggageAllowance.MaxExtraPieces,
            fare.BaggageAllowance.ExtraPiecePrice);
    }

    private static OfferedFare ToDomain(StoredFare fare)
    {
        return OfferedFare.Create(
            fare.PassengerIndex,
            fare.FlightId,
            fare.ProductId,
            fare.ProductCode,
            fare.ProductVersion,
            fare.CabinId,
            fare.CabinClass,
            fare.Brand,
            fare.Amount,
            fare.Currency,
            BaggageAllowance.Create(
                fare.IncludedPieces,
                fare.MaxWeightPerPieceKg,
                fare.MaxExtraPieces,
                fare.ExtraPiecePrice));
    }

    internal sealed record StoredFare(
        int PassengerIndex,
        string FlightId,
        Guid ProductId,
        string ProductCode,
        int ProductVersion,
        string CabinId,
        string CabinClass,
        string Brand,
        decimal Amount,
        string Currency,
        int IncludedPieces,
        decimal MaxWeightPerPieceKg,
        int MaxExtraPieces,
        decimal ExtraPiecePrice);
}
