using Shared.Domain.Entities;

namespace booking_service.Domain.Entities;

public class User : Entity<int>
{
    public string DisplayName { get; private set; } = null!;
    public string Email { get; private set; } = null!;

    private User() { }

    private User(int id, string displayName, string email) : base(id)
    {
        UpdateContactDetails(displayName, email);
    }

    public static User Create(int id, string displayName, string email)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        return new User(id, displayName, email);
    }

    public void UpdateContactDetails(string displayName, string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        if (!System.Net.Mail.MailAddress.TryCreate(email.Trim(), out var address) ||
            address.Address != email.Trim())
            throw new ArgumentException("A valid email address is required.", nameof(email));
        DisplayName = displayName.Trim();
        Email = address.Address;
    }
}