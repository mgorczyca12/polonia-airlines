using System.Security.Cryptography;
using System.Text;

namespace booking_service.Domain.ValueObjects;

/// <summary>
/// An unguessable capability that lets a guest return to their own checkout. Only its hash is persisted.
/// </summary>
public sealed record GuestToken
{
    private const int ByteLength = 32;
    private const int TextLength = ByteLength * 2;

    public string Value { get; }
    public string Hash { get; }

    private GuestToken(string value)
    {
        Value = value;
        Hash = HashOf(value);
    }

    public static GuestToken Generate()
    {
        return new GuestToken(Convert.ToHexString(RandomNumberGenerator.GetBytes(ByteLength)));
    }

    public static GuestToken From(string value)
    {
        if (!IsWellFormed(value))
        {
            throw new ArgumentException("A guest token must be 64 uppercase hexadecimal characters.", nameof(value));
        }

        return new GuestToken(value);
    }

    public static bool IsWellFormed(string? value)
    {
        return value is { Length: TextLength } && value.All(IsUpperHexadecimal);
    }

    public override string ToString()
    {
        return "GuestToken { }";
    }

    private static bool IsUpperHexadecimal(char character)
    {
        return character is >= '0' and <= '9' or >= 'A' and <= 'F';
    }

    private static string HashOf(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}
