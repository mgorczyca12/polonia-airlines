using System.Linq.Expressions;
using flight_service.Application.Contracts.Requests;
using flight_service.Application.Contracts.Responses;
using flight_service.Application.Features.FlightFeatures.Queries;
using flight_service.Application.Features.FlightFeatures.Queries.Errors;
using flight_service.Domain.Entities;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace flight_service.Infrastructure.Persistence.Queries;

public sealed class FlightQueries(FlightServiceDbContext context) : IFlightQueries
{
    private static readonly Expression<Func<Flight, FlightResponse>> Projection = flight =>
        new FlightResponse(
            flight.Id,
            flight.FlightNumber,
            new DateTimeOffset(DateTime.SpecifyKind(flight.DepartureTime, DateTimeKind.Utc)),
            new DateTimeOffset(DateTime.SpecifyKind(flight.ArrivalTime, DateTimeKind.Utc)),
            flight.DepartureAirport.IataCode,
            flight.ArrivalAirport.IataCode);

    public async Task<Result<FlightResponse>> GetFlight(int flightId, CancellationToken cancellationToken)
    {
        var flight = await context.Flights
            .AsNoTracking()
            .Where(flight => flight.Id == flightId)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return flight is null
            ? Result.Fail<FlightResponse>(new FlightNotFoundError(flightId))
            : Result.Ok(flight);
    }

    public async Task<Result<IReadOnlyList<FlightResponse>>> GetFlights(
        SearchFlightsRequest request,
        CancellationToken cancellationToken)
    {
        var start = request.DepartureDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var end = start.AddDays(1);
        var skip = checked((request.Page - 1) * request.PageSize);
        var flights = await context.Flights
            .AsNoTracking()
            .Where(flight => flight.DepartureAirport.IataCode == request.DepartureAirport
                && flight.ArrivalAirport.IataCode == request.ArrivalAirport
                && flight.DepartureTime >= start
                && flight.DepartureTime < end)
            .OrderBy(flight => flight.DepartureTime)
            .ThenBy(flight => flight.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return Result.Ok<IReadOnlyList<FlightResponse>>(flights);
    }
}
