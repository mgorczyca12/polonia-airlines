using booking_service.Domain.Entities;
using Shared.Application.Abstractions;

namespace booking_service.Application.Abstractions;

public interface IFlightPriceRepository : IRepository<FlightPrice, Guid>
{
    Task<FlightPrice?> GetAsync(
        string flightId,
        Guid productId,
        string currency,
        CancellationToken cancellationToken);
}
