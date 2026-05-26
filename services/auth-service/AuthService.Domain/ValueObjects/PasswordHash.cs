// File: AuthService.Domain/ValueObjects/PasswordHash.cs
// Purpose: Immutable password hash value object
// Layer: Domain

namespace AuthService.Domain.ValueObjects;

public class PasswordHash : IEquatable<PasswordHash>
{
    public string Value { get; }

    public PasswordHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
            throw new ArgumentException("Password hash cannot be empty", nameof(hash));

        Value = hash;
    }

    public bool Equals(PasswordHash? other)
    {
        if (other is null) return false;
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj is not PasswordHash hash) return false;
        return Equals(hash);
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value;
}