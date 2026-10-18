using Shared.Domain.Entities;

namespace flight_service.Domain.Entities;

// TODO: Implement configuration-owned cargo layouts for main/lower decks, container positions,
// bulk holds, and capacity restrictions. Keep actual flight loads separate from layout definitions.
public class CargoLayout : Entity<int>
{
    private CargoLayout()
    {
    }
}
