using flight_service.Application.Abstractions;
using flight_service.Infrastructure.Persistence;
using flight_service.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace flight_service.Infrastructure.Repositories;

public sealed class AirportRepository(FlightServiceDbContext dbContext)
    : RepositoryBase<Airport, int>(dbContext), IAirportRepository
{
    public async Task<Airport?> GetByIataCodeAsync(
        string code,
        bool includeGates,
        CancellationToken cancellationToken)
    {
        var query = Entities.AsQueryable();
        if (includeGates)
        {
            query = query.Include(x => x.Gates);
        }

        return await query
            .Where(x => x.IataCode == code)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
