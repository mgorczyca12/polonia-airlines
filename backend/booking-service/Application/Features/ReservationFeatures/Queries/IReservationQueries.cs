using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using FluentResults;

namespace booking_service.Application.Features.ReservationFeatures.Queries;

public interface IReservationQueries
{
    Task<Result<ReservationResponse>> GetReservation(
        GetReservationRequest request,
        CancellationToken cancellationToken);
}
