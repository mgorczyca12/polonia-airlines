using booking_service.Application.Abstractions;
using booking_service.Application.Errors;
using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using DispatchR.Abstractions.Send;
using FluentResults;

namespace booking_service.Application.Features.CatalogFeatures.Commands.PublishFareProduct;

public sealed class PublishFareProductCommandHandler(IFareProductRepository products)
    : IRequestHandler<PublishFareProductCommand, ValueTask<Result<Guid>>>
{
    public async ValueTask<Result<Guid>> Handle(
        PublishFareProductCommand request,
        CancellationToken cancellationToken)
    {
        if (await products.ExistsAsync(request.Code, request.Version, cancellationToken))
        {
            return Result.Fail<Guid>(BookingError.FareProductExists());
        }

        var baggage = request.Baggage;
        var product = FareProduct.Create(
            request.Code,
            request.Version,
            request.CabinClass,
            request.Brand,
            BaggageAllowance.Create(
                baggage.IncludedPieces,
                baggage.MaxWeightPerPieceKg,
                baggage.MaxExtraPieces,
                baggage.ExtraPiecePrice));

        products.Add(product);

        return Result.Ok(product.Id);
    }
}
