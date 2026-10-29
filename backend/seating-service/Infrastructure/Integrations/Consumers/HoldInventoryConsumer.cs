using DispatchR;
using MassTransit;
using seating_service.Application.Features.InventoryFeatures.Commands.HoldInventory;
using Shared.Integration.Contracts;

namespace seating_service.Infrastructure.Integrations.Consumers;

public sealed class HoldInventoryConsumer(IMediator mediator) : IConsumer<HoldReservationInventory>
{
    public async Task Consume(ConsumeContext<HoldReservationInventory> context)
    {
        var message = context.Message;
        var command = new HoldInventoryCommand(
            message.CorrelationId,
            message.ReservationId,
            message.HoldUntil,
            message.Selections);

        var result = await mediator.Send(command, context.CancellationToken);
        result.ThrowIfFailed();
    }
}
