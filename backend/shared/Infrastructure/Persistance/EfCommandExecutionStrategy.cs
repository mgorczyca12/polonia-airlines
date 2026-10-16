using Microsoft.EntityFrameworkCore;
using Shared.Application.Abstractions;

namespace Shared.Infrastructure.Persistence;

public sealed class EfCommandExecutionStrategy<TContext>(TContext dbContext) : ICommandExecutionStrategy
    where TContext : DbContext
{
    public Task<TResponse> ExecuteAsync<TResponse>(
        Func<CancellationToken, Task<TResponse>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(operation, cancellationToken);
    }
}
