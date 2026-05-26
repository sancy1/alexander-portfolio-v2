// File: AuthService.Infrastructure/Security/PasswordHasher.cs
// Purpose: BCrypt implementation of password hashing
// Layer: Infrastructure

using AuthService.Domain.Interfaces;
using BCrypt.Net;

namespace AuthService.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}