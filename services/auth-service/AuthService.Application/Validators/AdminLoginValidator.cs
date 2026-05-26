
// File: AuthService.Application/Validators/AdminLoginValidator.cs
// Purpose: Validation rules for admin login
// Layer: Application

using FluentValidation;
using AuthService.Application.DTOs.Requests;

namespace AuthService.Application.Validators;

public class AdminLoginValidator : AbstractValidator<AdminLoginRequest>
{
    public AdminLoginValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username or email is required");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}
