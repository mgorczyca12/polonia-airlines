using seating_service.Domain.Entities;
using Shared.Application.Abstractions;

namespace seating_service.Application.Abstractions;

public interface IInventoryOperationRepository : IRepository<InventoryOperation, Guid>
{
}
