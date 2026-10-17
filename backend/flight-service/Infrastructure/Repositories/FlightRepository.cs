using flight_service.Application.Abstractions;
using flight_service.Infrastructure.Persistence;
using flight_service.Domain.Entities;
using Shared.Infrastructure.Repositories;

namespace flight_service.Infrastructure.Repositories;

public sealed class FlightRepository(FlightServiceDbContext dbContext)
    : RepositoryBase<Flight, int>(dbContext), IFlightRepository
{
}
