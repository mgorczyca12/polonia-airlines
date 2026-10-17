using flight_service.Domain.Entities;
using Shared.Application.Abstractions;

namespace flight_service.Application.Abstractions;

public interface IScheduleRepository : IRepository<Schedule, int>
{
}
