using DispatchR;
using Microsoft.AspNetCore.Mvc;

namespace flight_service.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AircraftController(IMediator mediator) : ControllerBase
{
    [HttpGet("aircrafts")]
    public Task<IActionResult> GetAircrafts(
      [FromQuery] int? Page,
      [FromQuery] int? PageSize,
      CancellationToken cancellationToken
    )
    {
        return Task.FromResult<IActionResult>(Ok());
    }

    [HttpGet("aircraft/{aircraftId}")]
    public Task<IActionResult> GetAircraft(
      [FromRoute] string aircraftId,
      CancellationToken cancellationToken
    )
    {
        return Task.FromResult<IActionResult>(Ok());
    }
}
