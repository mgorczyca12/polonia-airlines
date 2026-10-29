using FluentValidation;

namespace seating_service.Application.Features.InventoryFeatures.Commands.CleanupConfirmedInventory;

public sealed class CleanupConfirmedInventoryCommandValidator : AbstractValidator<CleanupConfirmedInventoryCommand>
{
    public CleanupConfirmedInventoryCommandValidator()
    {
        RuleFor(command => command.CorrelationId).NotEqual(Guid.Empty);
    }
}
