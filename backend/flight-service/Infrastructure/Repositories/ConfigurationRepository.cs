using flight_service.Application.Abstractions;
using flight_service.Domain.Entities;
using flight_service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace flight_service.Infrastructure.Repositories;

public sealed class ConfigurationRepository(FlightServiceDbContext context)
    : RepositoryBase<AircraftConfiguration, int>(context), IConfigurationRepository
{
    public async Task<AircraftConfiguration?> GetWithSeatMapAsync(int id, CancellationToken cancellationToken)
    {
        return await Entities
            .Include(value => value.SeatMap)
                .ThenInclude(value => value.SeatMapRows)
                .ThenInclude(value => value.SeatMapPositions)
            .SingleOrDefaultAsync(value => value.Id == id, cancellationToken);
    }

    public async Task<AircraftType?> GetAircraftTypeAsync(int id, CancellationToken cancellationToken)
    {
        return await context.AircraftTypes.FindAsync(new object[] { id }, cancellationToken);
    }

    public async Task<SeatMapCabin?> GetCabinAsync(int id, CancellationToken cancellationToken)
    {
        return await context.SeatMapCabins.FindAsync(new object[] { id }, cancellationToken);
    }

    public Task<bool> ExistsAsync(
        int aircraftTypeId,
        string code,
        int version,
        CancellationToken cancellationToken)
    {
        return Entities.AnyAsync(value =>
            value.AircraftTypeId == aircraftTypeId
                && value.Code == code
                && value.Version == version,
            cancellationToken);
    }
}
