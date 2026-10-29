using booking_service.Application.Contracts.Responses;
using booking_service.Domain.ValueObjects;

namespace booking_service.Infrastructure.Persistence.Queries;

internal static class ResponseMapper
{
    internal static BaggageSnapshot ToSnapshot(BaggageAllowance allowance)
    {
        return new BaggageSnapshot(
            allowance.IncludedPieces,
            allowance.MaxWeightPerPieceKg,
            allowance.MaxExtraPieces,
            allowance.ExtraPiecePrice);
    }

    internal static FareSnapshot ToSnapshot(OfferedFare fare)
    {
        return new FareSnapshot(
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
            ToSnapshot(fare.BaggageAllowance));
    }
}
