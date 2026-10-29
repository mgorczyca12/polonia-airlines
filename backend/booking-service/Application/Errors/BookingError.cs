using FluentResults;

namespace booking_service.Application.Errors;

public enum BookingErrorKind
{
    NotFound,
    Invalid,
    Conflict,
    Unavailable
}

public sealed class BookingError(string code, string message, BookingErrorKind kind) : Error(message)
{
    public string Code { get; } = code;
    public BookingErrorKind Kind { get; } = kind;

    public static BookingError NotFound(string resource)
    {
        return new BookingError("not_found", $"{resource} was not found.", BookingErrorKind.NotFound);
    }

    public static BookingError PriceUnavailable()
    {
        return new BookingError(
            "price_unavailable",
            "No active fare price exists for the selection.",
            BookingErrorKind.Invalid);
    }

    public static BookingError PassengerMismatch()
    {
        return new BookingError(
            "passenger_mismatch",
            "Passenger count must match the offer.",
            BookingErrorKind.Invalid);
    }

    public static BookingError OfferExpired()
    {
        return new BookingError("offer_expired", "Offer has expired.", BookingErrorKind.Conflict);
    }

    public static BookingError OfferConsumed()
    {
        return new BookingError("offer_consumed", "Offer has already been consumed.", BookingErrorKind.Conflict);
    }

    public static BookingError IdempotencyConflict()
    {
        return new BookingError(
            "idempotency_conflict",
            "Request ID was used with a different payload.",
            BookingErrorKind.Conflict);
    }

    public static BookingError FareProductExists()
    {
        return new BookingError(
            "fare_product_exists",
            "A fare product with this code and version already exists.",
            BookingErrorKind.Conflict);
    }

    public static BookingError PnrUnavailable()
    {
        return new BookingError(
            "pnr_unavailable",
            "Unable to allocate a reservation code. Retry the request.",
            BookingErrorKind.Unavailable);
    }
}
