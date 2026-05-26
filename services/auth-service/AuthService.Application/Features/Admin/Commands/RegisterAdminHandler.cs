
// File: AuthService.Application/Features/Admin/Commands/RegisterAdminHandler.cs
// Purpose: Handles admin registration logic
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Security;

namespace AuthService.Application.Features.Admin.Commands;

public class RegisterAdminHandler : IRequestHandler<RegisterAdminCommand, AuthResponse>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAdminKeyValidator _adminKeyValidator;
    private readonly IJwtGenerator _jwtGenerator;

    public RegisterAdminHandler(
        IAdminRepository adminRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IAdminKeyValidator adminKeyValidator,
        IJwtGenerator jwtGenerator)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _adminKeyValidator = adminKeyValidator;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<AuthResponse> Handle(RegisterAdminCommand request, CancellationToken cancellationToken)
    {
        // Validate admin key
        if (!_adminKeyValidator.IsValidAdminKey(request.AdminKey))
        {
            return AuthResponse.CreateFailure("Invalid admin key provided");
        }

        // Check if username already exists
        var existingByUsername = await _adminRepository.GetByUsernameAsync(request.Username);
        if (existingByUsername != null)
        {
            return AuthResponse.CreateFailure("Username already taken");
        }

        // Check if email already exists
        var existingByEmail = await _adminRepository.GetByEmailAsync(request.Email);
        if (existingByEmail != null)
        {
            return AuthResponse.CreateFailure("Email already registered");
        }

        // Hash password
        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        // Create new admin entity - use global:: to avoid namespace conflict
        var adminEntity = new global::AuthService.Domain.Entities.Admin(
            request.Username,
            request.Email.ToLowerInvariant(),
            hashedPassword,
            UserRole.Admin
        );

        // Save to database
        await _adminRepository.AddAsync(adminEntity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate JWT token
        var token = _jwtGenerator.GenerateAdminToken(adminEntity);

        return AuthResponse.CreateSuccess(token, adminEntity.Id);
    }
}
