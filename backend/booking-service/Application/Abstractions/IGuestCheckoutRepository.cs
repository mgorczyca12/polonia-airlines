using booking_service.Domain.Entities;
using Shared.Application.Abstractions;

namespace booking_service.Application.Abstractions;

public interface IGuestCheckoutRepository : IRepository<GuestCheckout, string>
{
}
