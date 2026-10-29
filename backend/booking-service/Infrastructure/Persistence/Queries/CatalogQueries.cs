using booking_service.Application.Contracts.Responses;
using booking_service.Application.Features.CatalogFeatures.Queries;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace booking_service.Infrastructure.Persistence.Queries;

public sealed class CatalogQueries(BookingDbContext context) : ICatalogQueries
{
    public async Task<Result<IReadOnlyList<ProductResponse>>> GetProducts(CancellationToken cancellationToken)
    {
        var products = await context.FareProducts
            .AsNoTracking()
            .Where(product => product.Active)
            .OrderBy(product => product.Code)
            .ThenBy(product => product.Version)
            .ToListAsync(cancellationToken);

        var responses = products
            .Select(product => new ProductResponse(
                product.Id,
                product.Code,
                product.Version,
                product.CabinClass,
                product.Brand,
                ResponseMapper.ToSnapshot(product.BaggageAllowance)))
            .ToList();

        return Result.Ok<IReadOnlyList<ProductResponse>>(responses);
    }
}
