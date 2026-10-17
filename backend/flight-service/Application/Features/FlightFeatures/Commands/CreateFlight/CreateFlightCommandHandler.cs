using flight_service.Application.Abstractions;
using flight_service.Domain.Entities;
using DispatchR.Abstractions.Send;
using FluentResults;
using Shared.Application.Abstractions;

namespace flight_service.Application.Features.FlightFeatures.Commands.CreateFlight;

public sealed class CreateFlightCommandHandler(
    IAirportRepository airportRepository,
    IFlightRepository flightRepository
) : IRequestHandler<CreateFlightCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(
        CreateFlightCommand request,
        CancellationToken cancellationToken)
    {
        var departureAirport = await airportRepository.GetByIataCodeAsync(
            request.DepartureAirport,
            includeGates: true,
            cancellationToken);

        if (departureAirport is null)
        {
            return Result.Fail("Departure airport not found");
        }

        var arrivalAirport = await airportRepository.GetByIataCodeAsync(
            request.ArrivalAirport,
            includeGates: true,
            cancellationToken);

        if (arrivalAirport is null)
        {
            return Result.Fail("Arrival airport not found");
        }

        var newFlight = Flight.Create(
            request.FlightNumber,
            request.DepartureTime,
            request.ArrivalTime,
            departureAirport,
            arrivalAirport
        );

        flightRepository.Add(newFlight);

        return Result.Ok();
    }
}