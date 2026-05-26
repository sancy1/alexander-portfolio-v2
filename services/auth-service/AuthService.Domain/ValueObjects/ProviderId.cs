// File: AuthService.Domain/ValueObjects/ProviderId.cs
// Purpose: Provider ID value object for OAuth providers (Google/GitHub user IDs)
// Layer: Domain

namespace AuthService.Domain.ValueObjects;

public class ProviderId : IEquatable<ProviderId>
{
    public string Value { get; }

    public ProviderId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Provider ID cannot be empty", nameof(value));

        Value = value;
    }

    public bool Equals(ProviderId? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj is not ProviderId providerId) return false;
        return Equals(providerId);
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}