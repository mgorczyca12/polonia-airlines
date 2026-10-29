using Microsoft.EntityFrameworkCore;
using seating_service.Application.Abstractions;
using seating_service.Infrastructure.Persistence;

namespace seating_service.Infrastructure;

public sealed class ConfirmedInventoryCleanup(
    SeatingDbContext database,
    IInventoryHolds holds)
{
    public async Task Cleanup(Guid correlationId, CancellationToken cancellationToken)
    {
        await database.LockMutations(cancellationToken);
        var operation = await database.Operations
            .AsNoTracking()
            .SingleOrDefaultAsync(value => value.CorrelationId == correlationId, cancellationToken);

        if (operation?.State != InventoryOperationState.Confirmed)
        {
            return;
        }

        await holds.Release(correlationId, cancellationToken);
    }
}
