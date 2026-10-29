using booking_service.Application.Abstractions;
using booking_service.Application.Contracts;
using DispatchR.Abstractions.Send;
using FluentResults;
using Shared.Application.Commands;

namespace booking_service.Application.Features.Checkout;

public sealed record CreateCheckoutCommand : ICommand<CreateCheckoutCommand, Result<CheckoutResponse>>;
public sealed record CreateOfferCommand(string GuestToken, OfferRequest Request)
    : ICommand<CreateOfferCommand, Result<OfferResponse>>;

public sealed class CreateCheckoutHandler(IBookingService service)
    : IRequestHandler<CreateCheckoutCommand, ValueTask<Result<CheckoutResponse>>>
{
    public async ValueTask<Result<CheckoutResponse>> Handle(CreateCheckoutCommand request, CancellationToken cancellationToken)
    {
        return Result.Ok(await service.CreateCheckout(cancellationToken));
    }
}

public sealed class CreateOfferHandler(IBookingService service)
    : IRequestHandler<CreateOfferCommand, ValueTask<Result<OfferResponse>>>
{
    public async ValueTask<Result<OfferResponse>> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
    {
        return await service.CreateOffer(request.GuestToken, request.Request, cancellationToken);
    }
}
