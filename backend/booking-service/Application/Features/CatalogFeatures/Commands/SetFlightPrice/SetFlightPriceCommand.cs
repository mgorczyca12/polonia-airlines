using FluentResults;
using Shared.Application.Commands;

namespace booking_service.Application.Features.CatalogFeatures.Commands.SetFlightPrice;

public sealed record SetFlightPriceCommand(
    string FlightId,
    Guid ProductId,
    string Currency,
    decimal Amount,
    string CabinId)
    : ITransactionalCommand<SetFlightPriceCommand, Result>;
