using booking_service.Api.Extensions;
using booking_service.Application.Contracts.Responses;
using booking_service.Application.Features.CatalogFeatures.Queries;
using Microsoft.AspNetCore.Mvc;

namespace booking_service.Api.Controllers;

[ApiController]
[Route("api/booking/catalog")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CatalogController(ICatalogQueries queries) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List the active fare products.")]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetCatalog(CancellationToken cancellationToken)
    {
        var result = await queries.GetProducts(cancellationToken);
        if (result.IsFailed)
        {
            return this.BookingFailure(result);
        }

        return Ok(result.Value);
    }
}
