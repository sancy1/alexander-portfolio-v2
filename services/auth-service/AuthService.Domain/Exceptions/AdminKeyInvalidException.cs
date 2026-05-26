
// File: AuthService.Domain/Exceptions/AdminKeyInvalidException.cs
// Purpose: Thrown when admin key is invalid
// Layer: Domain

namespace AuthService.Domain.Exceptions;

public class AdminKeyInvalidException : DomainException
{
    public AdminKeyInvalidException() 
        : base("Invalid admin key provided") { }
    
    public AdminKeyInvalidException(string message) 
        : base(message) { }
    
    public AdminKeyInvalidException(string message, Exception innerException) 
        : base(message, innerException) { }
}
