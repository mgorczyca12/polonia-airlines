using flight_service.Application.Abstractions;
using flight_service.Infrastructure.Persistence;
using flight_service.Domain.Entities;
using Shared.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace flight_service.Infrastructure.Repositories;

public sealed class FlightRepository(FlightServiceDbContext dbContext)
    : RepositoryBase<Flight, int>(dbContext), IFlightRepository
{
    public async Task<Flight?> GetByFlightNumberAsync(string flightNumber, bool includeSchedules, CancellationToken cancellationToken)
    {
        return await dbContext.Flights
            .Where(f => f.FlightNumber == flightNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
