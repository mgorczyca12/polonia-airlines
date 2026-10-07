namespace booking_service.Domain.ValueObjects;

internal static class CurrencyCode
{
    internal static string Normalize(string currency)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        var code = currency.Trim().ToUpperInvariant();
        if (code.Length != 3 || code.Any(character => character < 'A' || character > 'Z'))
            throw new ArgumentException("Currency must be a three-letter code.", nameof(currency));
        return code;
    }
}
