using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using Shared.Application.Abstractions;

namespace booking_service.Application.Abstractions;

public interface IReservationRepository : IRepository<Reservation, int>
{
    Task<bool> PnrCodeExistsAsync(PnrCode pnrCode, CancellationToken cancellationToken);
}
