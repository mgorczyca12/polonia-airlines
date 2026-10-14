using DispatchR;
using flight_service.Application.Features.FlightFeatures.Queries;
using Microsoft.AspNetCore.Mvc;

namespace flight_service.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightController(IMediator mediator, IFlightQueries flightQueries) : ControllerBase
{
    [HttpGet("flights")]
    public async Task<IActionResult> GetFlights(
      [FromQuery] string DepartureAirport,
      [FromQuery] string ArrivalAirport,
      [FromQuery] DateTime DepartureDate,
      [FromQuery] int? Page,
      [FromQuery] int? PageSize,
      CancellationToken cancellationToken
    )
    {
        var flights = await flightQueries.GetFlights(
            DepartureAirport,
            ArrivalAirport,
            DepartureDate,
            Page,
            PageSize,
            cancellationToken
        );

        return Ok(flights);
    }

    [HttpGet("flight/{flightId}")]
    public async Task<IActionResult> GetFlight(
      [FromRoute] string flightId,
      CancellationToken cancellationToken
    )
    {
        var flight = await flightQueries.GetFlight(flightId, cancellationToken);
        return Ok(flight);
    }
}