using flight_service.Domain.Entities;
using Shared.Application.Abstractions;

namespace flight_service.Application.Abstractions;

public interface IConfigurationRepository : IRepository<AircraftConfiguration, int>
{
    Task<AircraftConfiguration?> GetWithSeatMapAsync(int id, CancellationToken cancellationToken);
    Task<AircraftType?> GetAircraftTypeAsync(int id, CancellationToken cancellationToken);
    Task<SeatMapCabin?> GetCabinAsync(int id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int aircraftTypeId, string code, int version, CancellationToken cancellationToken);
}
