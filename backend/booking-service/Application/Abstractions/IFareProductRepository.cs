using booking_service.Domain.Entities;
using Shared.Application.Abstractions;

namespace booking_service.Application.Abstractions;

public interface IFareProductRepository : IRepository<FareProduct, Guid>
{
    Task<bool> ExistsAsync(string code, int version, CancellationToken cancellationToken);
}
