using booking_service.Api.Extensions;
using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;
using booking_service.Application.Features.ReservationFeatures.Queries;
using DispatchR;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace booking_service.Api.Controllers;

[ApiController]
[Route("api/booking/reservations")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ReservationController(
    IMediator mediator,
    IReservationQueries queries,
    IValidator<GetReservationRequest> reservationValidator) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Turn an offer into a pending reservation; its inventory workflow completes asynchronously.")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReservationResponse>> CreateReservation(
        ReservationRequest request,
        [FromHeader(Name = "X-Guest-Token")] string guestToken,
        CancellationToken cancellationToken)
    {
        var created = await mediator.Send(new CreateReservationCommand(guestToken, request), cancellationToken);
        if (created.IsFailed)
        {
            return this.BookingFailure(created);
        }

        var reservation = await queries.GetReservation(
            new GetReservationRequest(created.Value, guestToken),
            cancellationToken);
        if (reservation.IsFailed)
        {
            return this.BookingFailure(reservation);
        }

        return CreatedAtAction(
            nameof(GetReservation),
            new { reservationId = reservation.Value.Id },
            reservation.Value);
    }

    [HttpGet("{reservationId:int}")]
    [EndpointSummary("Get a reservation created by the calling guest, with its workflow progress.")]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReservationResponse>> GetReservation(
        int reservationId,
        [FromHeader(Name = "X-Guest-Token")] string guestToken,
        CancellationToken cancellationToken)
    {
        var request = new GetReservationRequest(reservationId, guestToken);
        await reservationValidator.ValidateAndThrowAsync(request, cancellationToken);

        var result = await queries.GetReservation(request, cancellationToken);
        if (result.IsFailed)
        {
            return this.BookingFailure(result);
        }

        return Ok(result.Value);
    }
}
