using flight_service.Application.Abstractions;
using flight_service.Domain.Entities;
using flight_service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace flight_service.Infrastructure.Repositories;

public sealed class AircraftRepository(FlightServiceDbContext context)
    : RepositoryBase<Aircraft, int>(context), IAircraftRepository
{
    public Task<Aircraft?> GetForAssignmentAsync(int id, CancellationToken cancellationToken)
    {
        return Entities
            .Include(value => value.AircraftType)
            .Include(value => value.AircraftConfiguration)
                .ThenInclude(value => value.AircraftType)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
    }
}
