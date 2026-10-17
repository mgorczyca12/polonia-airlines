using flight_service.Application.Abstractions;
using flight_service.Domain.Entities;
using flight_service.Domain.ValueObjects;
using DispatchR.Abstractions.Send;
using FluentResults;

namespace flight_service.Application.Features.ScheduleFeatures.Commands.CreateSchedule;

public sealed class CreateScheduleCommandHandler(
    IFlightRepository flightRepository,
    IAirportRepository airportRepository,
    IScheduleRepository scheduleRepository
) : IRequestHandler<CreateScheduleCommand, ValueTask<Result>>
{
    public async ValueTask<Result> Handle(CreateScheduleCommand request, CancellationToken cancellationToken)
    {
        var flight = await flightRepository.GetByFlightNumberAsync(
            request.FlightNumber,
            includeSchedules: false,
            cancellationToken);

        if (flight is null)
        {
            return Result.Fail("Flight not found");
        }

        var departureAirport = await airportRepository.GetByIataCodeAsync(
            request.DepartureAirportIataCode,
            includeGates: false,
            cancellationToken
        );

        if (departureAirport is null)
        {
            return Result.Fail("Departure airport not found");
        }

        var arrivalAirport = await airportRepository.GetByIataCodeAsync(
            request.ArrivalAirportIataCode,
            includeGates: false,
            cancellationToken
        );

        if (arrivalAirport is null)
        {
            return Result.Fail("Arrival airport not found");
        }

        var operatingDays = OperatingDays.FromDays(request.DaysOfWeek);

        var newSchedule = Schedule.Create(
            flight,
            departureAirport,
            arrivalAirport,
            request.DepartureTime,
            request.ArrivalTime,
            request.effectiveFrom,
            request.effectiveUntil,
            operatingDays
        );

        scheduleRepository.Add(newSchedule);

        return Result.Ok();
    }
}
