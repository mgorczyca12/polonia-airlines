using System.ComponentModel.DataAnnotations;
using DispatchR;
using flight_service.Api.Extensions;
using flight_service.Application.Contracts.Requests;
using flight_service.Application.Contracts.Responses;
using flight_service.Application.Features.ConfigurationFeatures.Commands.CreateConfiguration;
using flight_service.Application.Features.ConfigurationFeatures.Commands.RemoveSeatMapRow;
using flight_service.Application.Features.ConfigurationFeatures.Commands.SetConfigurationActive;
using flight_service.Application.Features.ConfigurationFeatures.Commands.SetSeatMapRow;
using flight_service.Application.Features.ConfigurationFeatures.Queries;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace flight_service.Api.Controllers;

[ApiController]
[Route("api/configurations")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ConfigurationController(
    IMediator mediator,
    IConfigurationQueries queries,
    IValidator<int> idValidator) : ControllerBase
{
    [HttpGet("{configurationId}")]
    [EndpointSummary("Get an aircraft configuration and its current seat map.")]
    [ProducesResponseType<ConfigurationResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ConfigurationResponse>> GetConfiguration(
        [Range(1, int.MaxValue)] int configurationId,
        CancellationToken cancellationToken)
    {
        await idValidator.ValidateAndThrowAsync(configurationId, cancellationToken);
        var result = await queries.GetConfiguration(configurationId, cancellationToken);
        if (result.IsFailed)
        {
            return this.ConfigurationFailure(result);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    [EndpointSummary("Create an aircraft configuration with an empty seat map.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateConfigurationCommand request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        if (result.IsFailed)
        {
            return this.ConfigurationFailure(result);
        }

        return CreatedAtAction(nameof(GetConfiguration), new { configurationId = result.Value }, new { id = result.Value });
    }

    [HttpPut("{configurationId}/active")]
    [EndpointSummary("Activate or deactivate a configuration.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetActive(
        [Range(1, int.MaxValue)] int configurationId,
        ConfigurationActiveRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetConfigurationActiveCommand(configurationId, request.IsActive), cancellationToken);
        if (result.IsFailed)
        {
            return this.ConfigurationFailure(result);
        }

        return NoContent();
    }

    [HttpPut("{configurationId}/seat-map/rows/{rowNumber}")]
    [EndpointSummary("Add or replace a numbered row's cabin, types and complete position list.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetRow(
        [Range(1, int.MaxValue)] int configurationId,
        [Range(1, int.MaxValue)] int rowNumber,
        SeatMapRowRequest request,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SetSeatMapRowCommand(configurationId, rowNumber, request), cancellationToken);
        if (result.IsFailed)
        {
            return this.ConfigurationFailure(result);
        }

        return NoContent();
    }

    [HttpDelete("{configurationId}/seat-map/rows/{rowNumber}")]
    [EndpointSummary("Remove a numbered row and its positions.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveRow(
        [Range(1, int.MaxValue)] int configurationId,
        [Range(1, int.MaxValue)] int rowNumber,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveSeatMapRowCommand(configurationId, rowNumber), cancellationToken);
        if (result.IsFailed)
        {
            return this.ConfigurationFailure(result);
        }

        return NoContent();
    }
}
