using seating_service.Domain.Entities;
using seating_service.Domain.ValueObjects;

namespace seating_service.Application.Features.InventoryFeatures;

internal static class InventoryCollectionExtensions
{
    public static SeatInventory FindFor(this IEnumerable<SeatInventory> inventories, HeldSelection selection)
    {
        return inventories.Single(inventory =>
            inventory.FlightId == selection.FlightId && inventory.CabinId == selection.CabinId);
    }
}
