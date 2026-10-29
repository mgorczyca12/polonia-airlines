using Microsoft.EntityFrameworkCore;
using seating_service.Application.Abstractions;
using seating_service.Infrastructure.Persistence;

namespace seating_service.Infrastructure.Repositories;

public sealed class SqlInventoryMutationLock(SeatingDbContext context) : IInventoryMutationLock
{
    public async Task AcquireAsync(CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("Inventory mutations require the consumer outbox SQL transaction.");
        }

        var guard = await context.Set<InventoryMutationLock>()
            .FromSqlRaw("SELECT * FROM \"InventoryMutationLock\" WHERE \"Id\" = 1 FOR UPDATE")
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (guard is null)
        {
            throw new InvalidOperationException("Inventory mutation lock is missing; provision the database first.");
        }
    }
}
