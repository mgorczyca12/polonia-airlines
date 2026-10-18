using flight_service.Application.Contracts.Responses;
using flight_service.Application.Features.ConfigurationFeatures.Errors;
using flight_service.Application.Features.ConfigurationFeatures.Queries;
using flight_service.Domain.Entities;
using FluentValidation;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace flight_service.Infrastructure.Persistence.Queries;

public sealed class ConfigurationQueries(FlightServiceDbContext context, IValidator<int> idValidator) : IConfigurationQueries
{
    public async Task<Result<ConfigurationResponse>> GetConfiguration(
        int configurationId,
        CancellationToken cancellationToken)
    {
        await idValidator.ValidateAndThrowAsync(configurationId, cancellationToken);
        var configuration = await context.AircraftConfigurations
            .AsNoTracking()
            .Include(value => value.SeatMap)
                .ThenInclude(value => value.SeatMapRows)
                .ThenInclude(value => value.Cabin)
            .Include(value => value.SeatMap)
                .ThenInclude(value => value.SeatMapRows)
                .ThenInclude(value => value.SeatMapPositions)
            .SingleOrDefaultAsync(value => value.Id == configurationId, cancellationToken);
        if (configuration is null)
        {
            return Result.Fail<ConfigurationResponse>(ConfigurationError.Missing("configuration"));
        }

        return Result.Ok(new ConfigurationResponse(
            configuration.Id,
            configuration.AircraftTypeId,
            configuration.Code,
            configuration.Name,
            configuration.Version,
            configuration.IsActive,
            Map(configuration.SeatMap)));
    }

    public async Task<Result<FlightSeatMapResponse>> GetFlightSeatMap(
        int flightId,
        CancellationToken cancellationToken)
    {
        await idValidator.ValidateAndThrowAsync(flightId, cancellationToken);
        var flight = await context.Flights
            .AsNoTracking()
            .Include(value => value.Aircraft!)
                .ThenInclude(value => value.AircraftConfiguration)
                .ThenInclude(value => value.SeatMap)
                .ThenInclude(value => value.SeatMapRows)
                .ThenInclude(value => value.Cabin)
            .Include(value => value.Aircraft!)
                .ThenInclude(value => value.AircraftConfiguration)
                .ThenInclude(value => value.SeatMap)
                .ThenInclude(value => value.SeatMapRows)
                .ThenInclude(value => value.SeatMapPositions)
            .SingleOrDefaultAsync(value => value.Id == flightId, cancellationToken);
        if (flight is null)
        {
            return Result.Fail<FlightSeatMapResponse>(ConfigurationError.Missing("flight"));
        }

        if (flight.Aircraft is null)
        {
            return Result.Fail<FlightSeatMapResponse>(ConfigurationError.Conflict(
                "flight_aircraft_unassigned", "A seat map is not available until an aircraft is assigned."));
        }

        var aircraft = flight.Aircraft;
        var configuration = aircraft.AircraftConfiguration;
        return Result.Ok(new FlightSeatMapResponse(
            flight.Id,
            aircraft.Id,
            aircraft.Registration,
            configuration.Id,
            configuration.Code,
            configuration.Version,
            Map(configuration.SeatMap)));
    }

    private static SeatMapResponse Map(SeatMap seatMap)
    {
        var rows = seatMap.SeatMapRows
            .OrderBy(row => row.Number)
            .Select(row => new SeatMapRowResponse(
                row.Id,
                row.Number,
                row.CabinId,
                row.Cabin.Name,
                row.Cabin.IataCode,
                row.Type.Name,
                row.SeatMapPositions
                    .OrderBy(position => position.AisleGroup)
                    .ThenBy(position => position.Column, StringComparer.Ordinal)
                    .Select(position => new SeatMapPositionResponse(
                        position.Id,
                        position.Column,
                        position.AisleGroup,
                        position.IsSeat))
                    .ToArray()))
            .ToArray();

        return new SeatMapResponse(seatMap.Id, rows);
    }
}
