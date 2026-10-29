using System.Security.Cryptography;

namespace booking_service.Domain.ValueObjects;

public sealed record PnrCode
{
    private const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    public string Value { get; }

    private PnrCode(string value)
    {
        Value = value;
    }

    public static PnrCode From(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length != 6 ||
            value.Any(character => !char.IsAsciiLetterOrDigit(character)))
        {
            throw new ArgumentException("PNR code must contain exactly 6 ASCII letters or digits.", nameof(value));
        }

        return new PnrCode(value.ToUpperInvariant());
    }

    public static PnrCode Generate()
    {
        return new PnrCode(RandomNumberGenerator.GetString(Charset, 6));
    }

    public override string ToString()
    {
        return Value;
    }
}
