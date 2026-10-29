using System.Security.Cryptography;
using System.Text.Json;
using booking_service.Application.Contracts.Requests;

namespace booking_service.Application.Features.ReservationFeatures.Commands.CreateReservation;

/// <summary>Identifies a reservation request's content so a reused request ID with other content is detected.</summary>
internal static class ReservationRequestFingerprint
{
    internal static string Of(ReservationRequest request)
    {
        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(request)));
    }
}
