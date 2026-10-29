using booking_service.Application.Contracts.Requests;
using FluentResults;
using Shared.Application.Commands;

namespace booking_service.Application.Features.CheckoutFeatures.Commands.CreateOffer;

public sealed record CreateOfferCommand(string GuestToken, OfferRequest Request)
    : ITransactionalCommand<CreateOfferCommand, Result<Guid>>;
