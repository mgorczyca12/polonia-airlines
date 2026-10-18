using flight_service.Domain.Entities;
using Shared.Application.Abstractions;

namespace flight_service.Application.Abstractions;

public interface IAircraftRepository : IRepository<Aircraft, int>
{
    Task<Aircraft?> GetForAssignmentAsync(int id, CancellationToken cancellationToken);
}
