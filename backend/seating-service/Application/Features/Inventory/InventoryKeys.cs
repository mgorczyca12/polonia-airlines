using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Shared.Integration.Contracts;
using seating_service.Application.Abstractions;

namespace seating_service.Application.Features.Inventory;

public static class InventoryKeys
{
    public static string Bucket(string flightId, string cabinId)
    {
        return Encode(flightId, cabinId);
    }

    public static string Passenger(string flightId, string cabinId, string passengerId)
    {
        return Encode(flightId, cabinId, passengerId);
    }

    public static string Seat(string flightId, string seatId)
    {
        return Encode(flightId, seatId);
    }

    public static InventoryHold Hold(
        Guid correlationId,
        DateTimeOffset expiresAt,
        IEnumerable<InventorySelection> selections)
    {
        var items = selections
            .Select(selection => new HoldItem(
                Bucket(selection.FlightId, selection.CabinId),
                Passenger(selection.FlightId, selection.CabinId, selection.PassengerId),
                selection.SeatId is null ? null : Seat(selection.FlightId, selection.SeatId)))
            .OrderBy(item => item.Bucket, StringComparer.Ordinal)
            .ThenBy(item => item.Passenger, StringComparer.Ordinal)
            .ToArray();

        var normalizedExpiry = DateTimeOffset.FromUnixTimeMilliseconds(expiresAt.ToUnixTimeMilliseconds());
        return new InventoryHold(correlationId, normalizedExpiry, items);
    }

    public static bool IsValid(HoldReservationInventory command, DateTimeOffset now)
    {
        if (command.CorrelationId == Guid.Empty ||
            string.IsNullOrWhiteSpace(command.ReservationId) ||
            command.HoldUntil <= now ||
            command.HoldUntil > now.AddMinutes(10) ||
            command.Selections is not { Length: > 0 and <= 100 })
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

            if (!passengers.Add(Passenger(selection.FlightId, selection.CabinId, selection.PassengerId)) ||
                (selection.SeatId is not null && !seats.Add(Seat(selection.FlightId, selection.SeatId))))
            {
                return false;
            }
        }

        return true;
    }

    private static string Encode(params string[] parts)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(parts))));
    }
}
