using seating_service.Application.Abstractions;
using seating_service.Domain.Entities;
using seating_service.Domain.ValueObjects;

namespace seating_service.Application.Features.InventoryFeatures;

internal static class InventoryProvisioning
{
    public static async Task<bool> AreProvisionedAsync(
        IInventoryRepository repository,
        HeldSelections selections,
        IReadOnlyCollection<SeatInventory> inventories,
        CancellationToken cancellationToken)
    {
        foreach (var selection in selections.Items)
        {
            if (!inventories.Any(inventory =>
                    inventory.FlightId == selection.FlightId && inventory.CabinId == selection.CabinId))
            {
                return false;
            }

            if (selection.SeatId is not null &&
                !await repository.SeatExistsAsync(
                    selection.FlightId,
                    selection.CabinId,
                    selection.SeatId,
                    cancellationToken))
            {
                return false;
            }
        }

        return true;
    }
}
