using System.ComponentModel.DataAnnotations;
using flight_service.Application.Contracts.Requests;
using flight_service.Application.Contracts.Responses;
using flight_service.Application.Features.FlightFeatures.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace flight_service.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FlightController(
    IFlightQueries flightQueries,
    IValidator<SearchFlightsRequest> searchValidator) : ControllerBase
{
    [HttpGet("flights")]
    [ProducesResponseType<IReadOnlyList<FlightResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<FlightResponse>>> GetFlights(
        [FromQuery] SearchFlightsRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await searchValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key,
                    group => group.Select(error => error.ErrorMessage).ToArray());
            return ValidationProblem(new ValidationProblemDetails(errors));
        }

        var result = await flightQueries.GetFlights(request, cancellationToken);
        if (result.IsFailed)
            throw new InvalidOperationException("The flight search returned an unrecognized failure.");

        return Ok(result.Value);
    }

    [HttpGet("flight/{flightId}")]
    [ProducesResponseType<FlightResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FlightResponse>> GetFlight(
        [FromRoute, Range(1, int.MaxValue)] int flightId,
        CancellationToken cancellationToken)
    {
        var result = await flightQueries.GetFlight(flightId, cancellationToken);
        if (result.IsSuccess)
            return Ok(result.Value);

        if (result.Errors.Count == 1 && result.Errors[0] is FlightNotFoundError error)
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Flight not found",
                detail: error.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = FlightNotFoundError.Code
                });

        throw new InvalidOperationException("The flight query returned an unrecognized failure.");
    }
}