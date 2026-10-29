using MassTransit;
using Shared.Integration.Contracts;

namespace seating_service.Infrastructure.Consumers;

public sealed class HoldInventoryConsumer(InventoryParticipant participant) : IConsumer<HoldReservationInventory>
{
    public Task Consume(ConsumeContext<HoldReservationInventory> context)
    {
        return participant.Hold(context.Message, context.CancellationToken);
    }
}

public sealed class ConfirmInventoryConsumer(InventoryParticipant participant) : IConsumer<ConfirmReservationInventory>
{
    public Task Consume(ConsumeContext<ConfirmReservationInventory> context)
    {
        return participant.Confirm(context.Message, context.CancellationToken);
    }
}

public sealed class ReleaseInventoryConsumer(InventoryParticipant participant) : IConsumer<ReleaseReservationInventory>
{
    public Task Consume(ConsumeContext<ReleaseReservationInventory> context)
    {
        return participant.Release(context.Message, context.CancellationToken);
    }
}

public sealed class ConfirmedInventoryCleanupConsumer(
    ConfirmedInventoryCleanup cleanup,
    ILogger<ConfirmedInventoryCleanupConsumer> logger) : IConsumer<ReservationInventoryConfirmed>
{
    public async Task Consume(ConsumeContext<ReservationInventoryConfirmed> context)
    {
        try
        {
            await cleanup.Cleanup(context.Message.CorrelationId, context.CancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Post-commit inventory hold cleanup failed for {CorrelationId}; retry/error queue and expiry retain safety.",
                context.Message.CorrelationId);
            throw;
        }
    }
}
