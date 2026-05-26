// File: AuthService.Domain/Interfaces/IJwtGenerator.cs
// Purpose: Interface for JWT token generation
// Layer: Domain

using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IJwtGenerator
{
    string GenerateAdminToken(Admin admin);
    string GenerateUserToken(SocialUser user);
}