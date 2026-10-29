using booking_service.Domain.Entities;
using Shared.Application.Abstractions;

namespace booking_service.Application.Abstractions;

public interface IReservationCheckoutRepository : IRepository<ReservationCheckout, Guid>
{
    Task<ReservationCheckout?> GetByRequestAsync(
        string guestTokenHash,
        string requestId,
        CancellationToken cancellationToken);
}
