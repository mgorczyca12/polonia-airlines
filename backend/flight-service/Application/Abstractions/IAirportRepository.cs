using flight_service.Domain.Entities;
using Shared.Application.Abstractions;

namespace flight_service.Application.Abstractions;

public interface IAirportRepository : IRepository<Airport, int>
{
    Task<Airport?> GetByIataCodeAsync(string code, bool includeGates, CancellationToken cancellationToken);
}
