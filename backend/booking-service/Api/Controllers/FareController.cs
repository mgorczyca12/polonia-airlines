using booking_service.Api.Extensions;
using booking_service.Application.Contracts.Requests;
using booking_service.Application.Contracts.Responses;
using booking_service.Application.Features.CatalogFeatures.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace booking_service.Api.Controllers;

[ApiController]
[Route("api/booking/fares")]
public sealed class FareController(
    IFareQueries queries,
    IValidator<SearchFaresRequest> validator) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Find active flight-specific fare prices without reserving inventory.")]
    [ProducesResponseType<IReadOnlyList<FlightFareResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<FlightFareResponse>>> GetFares(
        [FromQuery] SearchFaresRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var result = await queries.GetFares(request, cancellationToken);
        if (result.IsFailed)
        {
            return this.BookingFailure(result);
        }

        return Ok(result.Value);
    }
}
