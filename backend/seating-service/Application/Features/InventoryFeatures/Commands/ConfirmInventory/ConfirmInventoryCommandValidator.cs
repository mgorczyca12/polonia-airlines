using FluentValidation;

namespace seating_service.Application.Features.InventoryFeatures.Commands.ConfirmInventory;

public sealed class ConfirmInventoryCommandValidator : AbstractValidator<ConfirmInventoryCommand>
{
    public ConfirmInventoryCommandValidator()
    {
        RuleFor(command => command.CorrelationId).NotEqual(Guid.Empty);
    }
}
