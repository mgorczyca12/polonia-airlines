using booking_service.Application.Abstractions;
using booking_service.Application.Errors;
using DispatchR.Abstractions.Send;
using FluentResults;

namespace booking_service.Application.Features.CatalogFeatures.Commands.RetireFareProduct;

public sealed class RetireFareProductCommandHandler(IFareProductRepository products)
    : IRequestHandler<RetireFareProductCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(RetireFareProductCommand request, CancellationToken cancellationToken)
    {
        var product = await products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
        {
            return Result.Fail(BookingError.NotFound("Fare product"));
        }

        product.Retire();

        return Result.Ok();
    }
}
