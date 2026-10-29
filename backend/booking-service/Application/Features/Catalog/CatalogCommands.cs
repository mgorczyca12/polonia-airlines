using booking_service.Application.Contracts;
using DispatchR.Abstractions.Send;
using FluentResults;
using Shared.Application.Commands;

namespace booking_service.Application.Features.Catalog;

public interface ICatalogAdministration
{
    Task<Guid> PublishProduct(PublishFareProductCommand request, CancellationToken cancellationToken);
    Task SetPrice(SetFlightPriceCommand request, CancellationToken cancellationToken);
    Task RetireProduct(Guid id, CancellationToken cancellationToken);
}

public sealed record PublishFareProductCommand(
    string Code,
    int Version,
    string CabinClass,
    string Brand,
    BaggageSnapshot Baggage) : ICommand<PublishFareProductCommand, Result<Guid>>;
public sealed record SetFlightPriceCommand(string FlightId, Guid ProductId, string Currency, decimal Amount, string CabinId)
    : ICommand<SetFlightPriceCommand, Result>;
public sealed record RetireFareProductCommand(Guid Id) : ICommand<RetireFareProductCommand, Result>;

public sealed class PublishFareProductHandler(ICatalogAdministration administration)
    : IRequestHandler<PublishFareProductCommand, ValueTask<Result<Guid>>>
{
    public async ValueTask<Result<Guid>> Handle(PublishFareProductCommand request, CancellationToken cancellationToken)
    {
        return Result.Ok(await administration.PublishProduct(request, cancellationToken));
    }
}

public sealed class SetFlightPriceHandler(ICatalogAdministration administration)
    : IRequestHandler<SetFlightPriceCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(SetFlightPriceCommand request, CancellationToken cancellationToken)
    {
        await administration.SetPrice(request, cancellationToken);
        return Result.Ok();
    }
}

public sealed class RetireFareProductHandler(ICatalogAdministration administration)
    : IRequestHandler<RetireFareProductCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(RetireFareProductCommand request, CancellationToken cancellationToken)
    {
        await administration.RetireProduct(request.Id, cancellationToken);
        return Result.Ok();
    }
}
