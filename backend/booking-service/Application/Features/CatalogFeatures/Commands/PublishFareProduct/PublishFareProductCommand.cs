using booking_service.Application.Contracts.Requests;
using FluentResults;
using Shared.Application.Commands;

namespace booking_service.Application.Features.CatalogFeatures.Commands.PublishFareProduct;

public sealed record PublishFareProductCommand(
    string Code,
    int Version,
    string CabinClass,
    string Brand,
    BaggageAllowanceRequest Baggage)
    : ITransactionalCommand<PublishFareProductCommand, Result<Guid>>;
