using booking_service.Application.Contracts;
using booking_service.Application.Features.Catalog;
using booking_service.Infrastructure.Persistence;
using DispatchR;
using Microsoft.EntityFrameworkCore;

namespace booking_service.Api.Extensions;

public static class BookingInitialization
{
    public static async Task InitializeBooking(this WebApplication app, CancellationToken cancellationToken = default)
    {
        var provision = app.Configuration.GetValue<bool>("Booking:ProvisionDevelopmentDatabase");
        var seed = app.Configuration.GetValue<bool>("Booking:SeedCatalog");
        if (provision && !app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("Booking:ProvisionDevelopmentDatabase is allowed only in Development.");
        }

        if (!provision && !seed)
        {
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        if (provision)
        {
            await database.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!seed)
        {
            return;
        }

        var products = app.Configuration.GetSection("Booking:CatalogSeed").Get<CatalogSeedProduct[]>()
            ?? throw new InvalidOperationException("Booking:CatalogSeed is required when catalog seeding is enabled.");
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        foreach (var product in products)
        {
            var existing = await database.FareProducts.AsNoTracking()
                .SingleOrDefaultAsync(x => x.Code == product.Code && x.Version == product.Version, cancellationToken);
            Guid id;
            if (existing is null)
            {
                var result = await mediator.Send(new PublishFareProductCommand(
                    product.Code, product.Version, product.CabinClass, product.Brand, product.Baggage),
                    cancellationToken);
                id = result.Value;
            }
            else
            {
                var baggage = existing.BaggageAllowance;
                if (existing.CabinClass != product.CabinClass ||
                    existing.Brand != product.Brand ||
                    new BaggageSnapshot(baggage.IncludedPieces, baggage.MaxWeightPerPieceKg,
                        baggage.MaxExtraPieces, baggage.ExtraPiecePrice) != product.Baggage)
                {
                    throw new InvalidOperationException("Seeded fare terms changed: publish a new product version.");
                }

                id = existing.Id;
            }

            foreach (var price in product.Prices)
            {
                await mediator.Send(new SetFlightPriceCommand(price.FlightId, id, price.Currency, price.Amount, price.CabinId), cancellationToken);
            }
        }
    }
}

public sealed record CatalogSeedProduct(
    string Code,
    int Version,
    string CabinClass,
    string Brand,
    BaggageSnapshot Baggage,
    CatalogSeedPrice[] Prices);
public sealed record CatalogSeedPrice(string FlightId, string Currency, decimal Amount, string CabinId);
