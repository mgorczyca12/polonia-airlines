using DispatchR.Abstractions.Send;
using FluentResults;
using booking_service.Application.Abstractions;
using booking_service.Application.Contracts;

namespace booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;

public sealed class CreateReservationHandler(
    IBookingService bookingService
) : IRequestHandler<CreateReservationCommand, ValueTask<Result<ReservationResponse>>>
{
    public async ValueTask<Result<ReservationResponse>> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        return await bookingService.CreateReservation(request.GuestToken, request.Request, cancellationToken);
    }
}
