namespace seating_service.Application.Features.InventoryFeatures.Commands.HoldInventory;

public static class HoldInventoryRequestRule
{
    private const int MaxSelections = 100;
    private static readonly TimeSpan MaxHoldDuration = TimeSpan.FromMinutes(10);

    public static bool IsSatisfiedBy(HoldInventoryCommand command, DateTimeOffset now)
    {
        if (command.CorrelationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ReservationId) ||
            command.HoldUntil <= now ||
            command.HoldUntil > now.Add(MaxHoldDuration) ||
            command.Selections is not { Length: > 0 and <= MaxSelections })
        {
            return false;
        }

        var passengers = new HashSet<string>(StringComparer.Ordinal);
        var seats = new HashSet<string>(StringComparer.Ordinal);

        foreach (var selection in command.Selections)
        {
            if (selection is null ||
                string.IsNullOrWhiteSpace(selection.PassengerId) ||
                string.IsNullOrWhiteSpace(selection.FlightId) ||
                string.IsNullOrWhiteSpace(selection.CabinId) ||
                (selection.SeatId is not null && string.IsNullOrWhiteSpace(selection.SeatId)))
            {
                return false;
            }

            var passengerKey = InventoryKeys.Passenger(selection.FlightId, selection.CabinId, selection.PassengerId);

            if (!passengers.Add(passengerKey) ||
                (selection.SeatId is not null && !seats.Add(InventoryKeys.Seat(selection.FlightId, selection.SeatId))))
            {
                return false;
            }
        }

        return true;
    }
}
