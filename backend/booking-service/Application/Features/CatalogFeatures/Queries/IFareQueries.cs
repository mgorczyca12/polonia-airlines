using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using FluentResults;

namespace booking_service.Application.Features.CatalogFeatures.Queries;

public interface IFareQueries
{
    Task<Result<IReadOnlyList<FlightFareResponse>>> GetFares(
        SearchFaresRequest request,
        CancellationToken cancellationToken);
}
