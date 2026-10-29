using seating_service.Application.Abstractions;
using seating_service.Domain.Entities;
using seating_service.Infrastructure.Persistence;
using Shared.Infrastructure.Repositories;

namespace seating_service.Infrastructure.Repositories;

public sealed class InventoryOperationRepository(SeatingDbContext context)
    : RepositoryBase<InventoryOperation, Guid>(context), IInventoryOperationRepository
{
}
