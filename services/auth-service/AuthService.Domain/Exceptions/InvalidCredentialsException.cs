
// File: AuthService.Domain/Exceptions/InvalidCredentialsException.cs
// Purpose: Thrown when invalid credentials are provided
// Layer: Domain

namespace AuthService.Domain.Exceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException() 
        : base("Invalid email/username or password") { }
    
    public InvalidCredentialsException(string message) 
        : base(message) { }
    
    public InvalidCredentialsException(string message, Exception innerException) 
        : base(message, innerException) { }
}
