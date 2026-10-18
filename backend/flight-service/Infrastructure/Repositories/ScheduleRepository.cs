using flight_service.Application.Abstractions;
using flight_service.Domain.Entities;
using flight_service.Infrastructure.Persistence;
using Shared.Infrastructure.Repositories;

namespace flight_service.Infrastructure.Repositories;

public sealed class ScheduleRepository(FlightServiceDbContext context)
    : RepositoryBase<Schedule, int>(context), IScheduleRepository
{
}
