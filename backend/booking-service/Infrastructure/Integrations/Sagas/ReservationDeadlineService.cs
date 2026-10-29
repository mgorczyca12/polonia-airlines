using System.Data.Common;
using booking_service.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Integration.Contracts;

namespace booking_service.Infrastructure.Integrations.Sagas;

public sealed class ReservationDeadlineService(
    IServiceScopeFactory scopes,
    TimeProvider clock,
    ILogger<ReservationDeadlineService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10), clock);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await PublishDueAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is DbException or DbUpdateException or TimeoutException)
            {
                logger.LogError(exception, "Reservation deadline dispatch failed; pending deadlines will be retried.");
            }
        }
    }

    public async Task PublishDueAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var readDatabase = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var now = clock.GetUtcNow();
        var dueIds = await readDatabase.Set<ReservationSagaState>()
            .AsNoTracking()
            .Where(state => state.CurrentState != "Completed"
                && state.CurrentState != "Cancelled"
                && state.NextCheckAt <= now)
            .OrderBy(state => state.NextCheckAt)
            .Select(state => state.CorrelationId)
            .Take(100)
            .ToArrayAsync(cancellationToken);

        foreach (var id in dueIds)
        {
            await using var dispatchScope = scopes.CreateAsyncScope();
            var database = dispatchScope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var publisher = dispatchScope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
            var claimed = await database.Set<ReservationSagaState>()
                .Where(state => state.CorrelationId == id
                    && state.CurrentState != "Completed"
                    && state.CurrentState != "Cancelled"
                    && state.NextCheckAt <= now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(state => state.NextCheckAt, now.AddSeconds(15)),
                    cancellationToken);

            if (claimed == 1)
            {
                await publisher.Publish(new ReservationWorkflowExpired(id), cancellationToken);
                await database.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }
}
