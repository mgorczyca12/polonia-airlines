using booking_service.Application.Abstractions;
using booking_service.Domain.Entities;
using booking_service.Infrastructure.Persistence;
using Shared.Infrastructure.Repositories;

namespace booking_service.Infrastructure.Repositories;

public sealed class GuestCheckoutRepository(BookingDbContext dbContext)
    : RepositoryBase<GuestCheckout, string>(dbContext), IGuestCheckoutRepository
{
}
