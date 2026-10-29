using booking_service.Application.Abstractions;
using booking_service.Application.Contracts.Responses;
using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using DispatchR.Abstractions.Send;
using FluentResults;

namespace booking_service.Application.Features.CheckoutFeatures.Commands.CreateCheckout;

public sealed class CreateCheckoutCommandHandler(
    IGuestCheckoutRepository checkouts,
    TimeProvider clock
) : IRequestHandler<CreateCheckoutCommand, ValueTask<Result<CheckoutResponse>>>
{
    public ValueTask<Result<CheckoutResponse>> Handle(
        CreateCheckoutCommand request,
        CancellationToken cancellationToken)
    {
        var token = GuestToken.Generate();
        checkouts.Add(GuestCheckout.Start(token, clock.GetUtcNow()));

        return ValueTask.FromResult(Result.Ok(new CheckoutResponse(token.Value)));
    }
}
