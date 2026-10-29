using booking_service.Application.Abstractions;
using booking_service.Domain.Entities;
using booking_service.Domain.ValueObjects;
using booking_service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Repositories;

namespace booking_service.Infrastructure.Repositories;

public sealed class ReservationRepository(BookingDbContext dbContext)
    : RepositoryBase<Reservation, int>(dbContext), IReservationRepository
{
    public async Task<bool> PnrCodeExistsAsync(PnrCode pnrCode, CancellationToken cancellationToken)
    {
        return await dbContext.Reservations.AnyAsync(reservation => reservation.Code == pnrCode, cancellationToken);
    }
}
