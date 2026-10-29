using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using seating_service.Application.Abstractions;
using seating_service.Domain.ValueObjects;

namespace seating_service.Application.Features.InventoryFeatures;

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
        IEnumerable<HeldSelection> selections)
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

    private static string Encode(params string[] parts)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(parts))));
    }
}
