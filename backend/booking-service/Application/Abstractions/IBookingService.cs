using booking_service.Application.Contracts;
using FluentResults;

namespace booking_service.Application.Abstractions;

public interface IBookingService
{
    Task<CheckoutResponse> CreateCheckout(CancellationToken cancellationToken);
    Task<Result<OfferResponse>> CreateOffer(string guestToken, OfferRequest request, CancellationToken cancellationToken);
    Task<Result<ReservationResponse>> CreateReservation(string guestToken, ReservationRequest request, CancellationToken cancellationToken);
}

public interface IBookingQueries
{
    Task<Result<ProductResponse[]>> Catalog(CancellationToken cancellationToken);
    Task<Result<OfferResponse>> Offer(Guid id, string guestToken, CancellationToken cancellationToken);
    Task<Result<ReservationResponse>> Reservation(int id, string guestToken, CancellationToken cancellationToken);
}

public sealed class BookingError(string code, string message) : Error(message)
{
    public string Code { get; } = code;
}
