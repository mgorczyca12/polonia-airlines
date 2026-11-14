using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using booking_service.Application.Features.CatalogFeatures.Queries;
using FluentResults;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace booking_service.Infrastructure.Persistence.Queries;

public sealed class FareQueries(
    BookingDbContext context,
    IValidator<SearchFaresRequest> validator) : IFareQueries
{
    public async Task<Result<IReadOnlyList<FlightFareResponse>>> GetFares(
        SearchFaresRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var fares = await (
            from price in context.FlightPrices.AsNoTracking()
            join product in context.FareProducts.AsNoTracking() on price.ProductId equals product.Id
            where request.FlightIds.Contains(price.FlightId)
                && price.Currency == request.Currency
                && product.Active
            select new { Price = price, Product = product })
            .ToListAsync(cancellationToken);

        var responses = fares
            .OrderBy(fare => fare.Price.FlightId, StringComparer.Ordinal)
            .ThenBy(fare => fare.Product.Code, StringComparer.Ordinal)
            .ThenBy(fare => fare.Product.Version)
            .ThenBy(fare => fare.Product.Id)
            .Select(fare => new FlightFareResponse(
                fare.Price.FlightId,
                fare.Product.Id,
                fare.Product.Code,
                fare.Product.Version,
                fare.Price.CabinId,
                fare.Product.CabinClass,
                fare.Product.Brand,
                fare.Price.Amount,
                fare.Price.Currency,
                ResponseMapper.ToSnapshot(fare.Product.BaggageAllowance)))
            .ToArray();

        return Result.Ok<IReadOnlyList<FlightFareResponse>>(responses);
    }
}
