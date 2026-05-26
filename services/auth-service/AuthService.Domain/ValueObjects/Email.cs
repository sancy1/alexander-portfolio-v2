// File: AuthService.Domain/ValueObjects/Email.cs
// Purpose: Immutable email value object with validation
// Layer: Domain

using AuthService.Domain.Exceptions;

namespace AuthService.Domain.ValueObjects;

public class Email : IEquatable<Email>
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email cannot be empty");

        if (!IsValidEmail(value))
            throw new DomainException($"Invalid email format: {value}");

        Value = value.ToLowerInvariant();
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public bool Equals(Email? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj is not Email email) return false;
        return Equals(email);
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}