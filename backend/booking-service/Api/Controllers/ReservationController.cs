using booking_service.Application.Abstractions;
using booking_service.Application.Contracts;
using booking_service.Application.Features.Checkout;
using booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;
using booking_service.Application.Validation;
using DispatchR;
using FluentResults;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace booking_service.Api.Controllers;

[ApiController]
[Route("api/booking")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReservationController(
    IMediator mediator,
    IBookingQueries queries,
    BookingAccessValidator accessValidator) : ControllerBase
{
    [HttpPost("checkouts")]
    public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateCheckoutCommand(), cancellationToken);
        return Ok(result.Value);
    }

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken cancellationToken)
    {
        var result = await queries.Catalog(cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Failure(result.Errors);
    }

    [HttpPost("offers")]
    public async Task<IActionResult> CreateOffer(
        OfferRequest request,
        [FromHeader(Name = "X-Guest-Token")] string guestToken,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateOfferCommand(guestToken, request), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(Offer), new { offerId = result.Value.Id }, result.Value)
            : Failure(result.Errors);
    }

    [HttpGet("offers/{offerId:guid}")]
    public async Task<IActionResult> Offer(
        Guid offerId,
        [FromHeader(Name = "X-Guest-Token")] string guestToken,
        CancellationToken cancellationToken)
    {
        await accessValidator.ValidateAndThrowAsync(new BookingAccessRequest(guestToken, OfferId: offerId), cancellationToken);
        var result = await queries.Offer(offerId, guestToken, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Failure(result.Errors);
    }

    [HttpPost("reservations")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReservation(
        ReservationRequest request,
        [FromHeader(Name = "X-Guest-Token")] string guestToken,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateReservationCommand(guestToken, request), cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetReservation), new { reservationId = result.Value.Id }, result.Value)
            : Failure(result.Errors);
    }

    [HttpGet("reservations/{reservationId:int}")]
    public async Task<IActionResult> GetReservation(
        int reservationId,
        [FromHeader(Name = "X-Guest-Token")] string guestToken,
        CancellationToken cancellationToken)
    {
        await accessValidator.ValidateAndThrowAsync(new BookingAccessRequest(guestToken, ReservationId: reservationId), cancellationToken);
        var result = await queries.Reservation(reservationId, guestToken, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Failure(result.Errors);
    }

    private ObjectResult Failure(IReadOnlyList<IError> errors)
    {
        if (errors.Count != 1 || errors[0] is not BookingError error)
        {
            throw new InvalidOperationException("An unrecognized booking result failure occurred.");
        }

        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "price_unavailable" or "passenger_mismatch" => StatusCodes.Status400BadRequest,
            "offer_expired" or "offer_consumed" or "idempotency_conflict" => StatusCodes.Status409Conflict,
            "pnr_unavailable" => StatusCodes.Status503ServiceUnavailable,
            _ => throw new InvalidOperationException("An unrecognized booking error occurred.")
        };
        var problem = new ProblemDetails { Status = status, Title = error.Message };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
    }
}
