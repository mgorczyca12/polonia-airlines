using booking_service.Application.Abstractions;
using booking_service.Application.Errors;
using booking_service.Domain.Entities;
using DispatchR.Abstractions.Send;
using FluentResults;

namespace booking_service.Application.Features.CatalogFeatures.Commands.SetFlightPrice;

public sealed class SetFlightPriceCommandHandler(
    IFareProductRepository products,
    IFlightPriceRepository prices
) : IRequestHandler<SetFlightPriceCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(SetFlightPriceCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Fail(BookingError.NotFound("Fare product"));
        }

        var price = await prices.GetAsync(
            request.FlightId,
            request.ProductId,
            request.Currency,
            cancellationToken);

        if (price is null)
        {
            prices.Add(FlightPrice.Create(
                request.FlightId,
                request.ProductId,
                request.Currency,
                request.Amount,
                request.CabinId));
        }
        else
        {
            price.Reprice(request.Amount, request.CabinId);
        }

        return Result.Ok();
    }
}
