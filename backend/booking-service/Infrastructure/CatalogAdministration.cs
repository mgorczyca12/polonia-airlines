using booking_service.Application.Features.Catalog;
using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace booking_service.Infrastructure;

public sealed class CatalogAdministration(BookingDbContext database) : ICatalogAdministration
{
    public async Task<Guid> PublishProduct(PublishFareProductCommand request, CancellationToken cancellationToken)
    {
        var baggage = request.Baggage;
        var product = FareProduct.Create(
            request.Code, request.Version, request.CabinClass, request.Brand,
            BaggageAllowance.Create(baggage.IncludedPieces, baggage.MaxWeightPerPieceKg,
                baggage.MaxExtraPieces, baggage.ExtraPiecePrice));
        database.FareProducts.Add(product);
        await database.SaveChangesAsync(cancellationToken);
        return product.Id;
    }

    public async Task SetPrice(SetFlightPriceCommand request, CancellationToken cancellationToken)
    {
        var candidate = FlightPrice.Create(request.FlightId, request.ProductId, request.Currency, request.Amount, request.CabinId);
        var existing = await database.FlightPrices.SingleOrDefaultAsync(x =>
            x.FlightId == candidate.FlightId && x.ProductId == candidate.ProductId &&
            x.Currency == candidate.Currency, cancellationToken);
        if (existing is null)
        {
            database.FlightPrices.Add(candidate);
        }
        else
        {
            existing.Reprice(candidate.Amount, candidate.CabinId);
        }

        await database.SaveChangesAsync(cancellationToken);
    }

    public async Task RetireProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await database.FareProducts.SingleAsync(x => x.Id == id, cancellationToken);
        product.Retire();
        await database.SaveChangesAsync(cancellationToken);
    }
}
