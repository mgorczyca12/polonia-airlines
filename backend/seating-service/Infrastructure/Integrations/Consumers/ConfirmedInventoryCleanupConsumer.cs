using DispatchR;
using MassTransit;
using seating_service.Application.Features.InventoryFeatures.Commands.CleanupConfirmedInventory;
using Shared.Integration.Contracts;

namespace seating_service.Infrastructure.Integrations.Consumers;

public sealed class ConfirmedInventoryCleanupConsumer(
    IMediator mediator,
    ILogger<ConfirmedInventoryCleanupConsumer> logger) : IConsumer<ReservationInventoryConfirmed>
{
    public async Task Consume(ConsumeContext<ReservationInventoryConfirmed> context)
    {
        try
        {
            var command = new CleanupConfirmedInventoryCommand(context.Message.CorrelationId);

            var result = await mediator.Send(command, context.CancellationToken);
            result.ThrowIfFailed();
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
