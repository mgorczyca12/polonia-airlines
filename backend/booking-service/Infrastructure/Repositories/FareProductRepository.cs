using booking_service.Application.Abstractions;
using booking_service.Domain.Entities;
using booking_service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace booking_service.Infrastructure.Repositories;

public sealed class FareProductRepository(BookingDbContext dbContext)
    : RepositoryBase<FareProduct, Guid>(dbContext), IFareProductRepository
{
    public async Task<bool> ExistsAsync(string code, int version, CancellationToken cancellationToken)
    {
        return await dbContext.FareProducts
            .AnyAsync(product => product.Code == code && product.Version == version, cancellationToken);
    }
}
