using booking_service.Application.Contracts.Responses;
using FluentResults;
using Shared.Application.Commands;

namespace booking_service.Application.Features.CheckoutFeatures.Commands.CreateCheckout;

public sealed record CreateCheckoutCommand
    : ITransactionalCommand<CreateCheckoutCommand, Result<CheckoutResponse>>;
