using booking_service.Application.Contracts.Responses;
using FluentResults;

namespace booking_service.Application.Features.CatalogFeatures.Queries;

public interface ICatalogQueries
{
    Task<Result<IReadOnlyList<ProductResponse>>> GetProducts(CancellationToken cancellationToken);
}
