using booking_service.Application.Abstractions;
using booking_service.Application.Errors;
using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using DispatchR.Abstractions.Send;
using FluentResults;

namespace booking_service.Application.Features.CheckoutFeatures.Commands.CreateOffer;

public sealed class CreateOfferCommandHandler(
    IGuestCheckoutRepository checkouts,
    IFareProductRepository products,
    IFlightPriceRepository prices,
    ICheckoutOfferRepository offers,
    TimeProvider clock
) : IRequestHandler<CreateOfferCommand, ValueTask<Result<Guid>>>
{
    public async ValueTask<Result<Guid>> Handle(CreateOfferCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = GuestToken.From(request.GuestToken).Hash;
        if (await checkouts.GetByIdAsync(tokenHash, cancellationToken) is null)
        {
            return Result.Fail<Guid>(BookingError.NotFound("Checkout"));
        }

        var fares = new List<OfferedFare>();
        foreach (var selection in request.Request.Selections)
        {
            var price = await prices.GetAsync(
                selection.FlightId,
                selection.ProductId,
                request.Request.Currency,
                cancellationToken);
            var product = await products.GetByIdAsync(selection.ProductId, cancellationToken);
            if (price is null || product is null || !product.Active)
            {
                return Result.Fail<Guid>(BookingError.PriceUnavailable());
            }

            for (var index = 0; index < request.Request.PassengerCount; index++)
            {
                if (selection.PassengerIndex.HasValue && selection.PassengerIndex != index)
                {
                    continue;
                }

                fares.Add(OfferedFare.Create(
                    index,
                    selection.FlightId,
                    product.Id,
                    product.Code,
                    product.Version,
                    price.CabinId,
                    product.CabinClass,
                    product.Brand,
                    price.Amount,
                    price.Currency,
                    product.BaggageAllowance));
            }
        }

        var offer = CheckoutOffer.Create(tokenHash, fares, clock.GetUtcNow());
        offers.Add(offer);

        return Result.Ok(offer.Id);
    }
}
