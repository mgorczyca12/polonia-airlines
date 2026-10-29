using FluentResults;
using Shared.Application.Commands;

namespace booking_service.Application.Features.CatalogFeatures.Commands.RetireFareProduct;

public sealed record RetireFareProductCommand(Guid ProductId)
    : ITransactionalCommand<RetireFareProductCommand, Result>;
