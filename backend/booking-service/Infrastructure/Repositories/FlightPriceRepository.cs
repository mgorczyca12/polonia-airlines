using booking_service.Application.Abstractions;
using booking_service.Domain.Entities;
using booking_service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace booking_service.Infrastructure.Repositories;

public sealed class FlightPriceRepository(BookingDbContext dbContext)
    : RepositoryBase<FlightPrice, Guid>(dbContext), IFlightPriceRepository
{
    public async Task<FlightPrice?> GetAsync(
        string flightId,
        Guid productId,
        string currency,
        CancellationToken cancellationToken)
    {
        return await dbContext.FlightPrices
            .SingleOrDefaultAsync(
                price => price.FlightId == flightId
                    && price.ProductId == productId
                    && price.Currency == currency,
                cancellationToken);
    }
}
