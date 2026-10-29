using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using FluentResults;

namespace booking_service.Application.Features.CheckoutFeatures.Queries;

public interface IOfferQueries
{
    Task<Result<OfferResponse>> GetOffer(GetOfferRequest request, CancellationToken cancellationToken);
}
