using FluentResults;
using Shared.Application.Commands;

namespace flight_service.Application.Features.ScheduleFeatures.Commands.CreateSchedule;

public sealed record CreateScheduleCommand(
    string FlightNumber,
    string DepartureAirportIataCode,
    string ArrivalAirportIataCode,
    TimeOnly DepartureTime,
    TimeOnly ArrivalTime,
    DateOnly effectiveFrom,
    DateOnly effectiveUntil,
    IReadOnlyCollection<DayOfWeek> DaysOfWeek
    )
    : IRetryableCommand<CreateScheduleCommand, Result>;
