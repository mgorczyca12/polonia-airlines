using booking_service.Api.Extensions;
using booking_service.Application.Contracts.Responses;
using booking_service.Application.Features.CheckoutFeatures.Commands.CreateCheckout;
using DispatchR;
using Microsoft.AspNetCore.Mvc;

namespace booking_service.Api.Controllers;

[ApiController]
[Route("api/booking/checkouts")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CheckoutController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Start an anonymous guest checkout and receive its capability token.")]
    [ProducesResponseType<CheckoutResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<CheckoutResponse>> Create(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateCheckoutCommand(), cancellationToken);
        if (result.IsFailed)
        {
            return this.BookingFailure(result);
        }

        return Ok(result.Value);
    }
}
