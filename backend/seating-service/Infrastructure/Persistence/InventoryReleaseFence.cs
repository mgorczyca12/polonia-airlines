using Microsoft.EntityFrameworkCore;
using seating_service.Application.Abstractions;
using seating_service.Application.Features.Inventory;

namespace seating_service.Infrastructure.Persistence;

public sealed class InventoryReleaseFence(
    DbContextOptions<SeatingDbContext> options) : IInventoryReleaseFence
{
    public async Task EnsureReleased(Guid correlationId, CancellationToken cancellationToken)
    {
        // Independent commit fences late Hold/Confirm even if Redis cleanup or the consumer transaction fails.
        await using var database = new SeatingDbContext(options);
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        await database.LockMutations(cancellationToken);
        var operation = await database.Operations.FindAsync([correlationId], cancellationToken);
        if (operation is null)
        {
            operation = new InventoryOperation { CorrelationId = correlationId };
            database.Operations.Add(operation);
        }

        operation.AllocationsPendingRelease |= operation.State == InventoryOperationState.Confirmed;
        operation.State = InventoryOperationState.Released;
        operation.RejectionCode = InventoryRejectionCode.Cancelled.ToContractCode();
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
