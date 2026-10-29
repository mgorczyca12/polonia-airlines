using booking_service.Api.Extensions;
using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using booking_service.Application.Features.CheckoutFeatures.Commands.CreateOffer;
using booking_service.Application.Features.CheckoutFeatures.Queries;
using DispatchR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace booking_service.Api.Controllers;

[ApiController]
[Route("api/booking/offers")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class OfferController(
    IMediator mediator,
    IOfferQueries queries,
    IValidator<GetOfferRequest> offerValidator) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a server-priced offer, valid for fifteen minutes, for the selected fare products.")]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<OfferResponse>> CreateOffer(
        OfferRequest request,
        [FromHeader(Name = "X-Guest-Token")] string guestToken,
        CancellationToken cancellationToken)
    {
        var created = await mediator.Send(new CreateOfferCommand(guestToken, request), cancellationToken);
        if (created.IsFailed)
        {
            return this.BookingFailure(created);
        }

        var offer = await queries.GetOffer(new GetOfferRequest(created.Value, guestToken), cancellationToken);
        if (offer.IsFailed)
        {
            return this.BookingFailure(offer);
        }

        return CreatedAtAction(nameof(GetOffer), new { offerId = offer.Value.Id }, offer.Value);
    }

    [HttpGet("{offerId:guid}")]
    [EndpointSummary("Get an offer created by the calling guest.")]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<OfferResponse>> GetOffer(
        Guid offerId,
        [FromHeader(Name = "X-Guest-Token")] string guestToken,
        CancellationToken cancellationToken)
    {
        var request = new GetOfferRequest(offerId, guestToken);
        await offerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var result = await queries.GetOffer(request, cancellationToken);
        if (result.IsFailed)
        {
            return this.BookingFailure(result);
        }

        return Ok(result.Value);
    }
}
