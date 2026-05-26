// File: AuthService.Domain/Interfaces/IPasswordHasher.cs
// Purpose: Interface for password hashing operations
// Layer: Domain

namespace AuthService.Domain.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
}