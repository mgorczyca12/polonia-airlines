using booking_service.Application.Abstractions;
using booking_service.Domain.Entities;
using booking_service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace booking_service.Infrastructure.Repositories;

public sealed class ReservationCheckoutRepository(BookingDbContext dbContext)
    : RepositoryBase<ReservationCheckout, Guid>(dbContext), IReservationCheckoutRepository
{
    public async Task<ReservationCheckout?> GetByRequestAsync(
        string guestTokenHash,
        string requestId,
        CancellationToken cancellationToken)
    {
        return await dbContext.ReservationCheckouts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                checkout => checkout.GuestTokenHash == guestTokenHash && checkout.RequestId == requestId,
                cancellationToken);
    }
}
