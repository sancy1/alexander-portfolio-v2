
// File: AuthService.Domain/Exceptions/DomainException.cs
// Purpose: Base domain exception
// Layer: Domain

namespace AuthService.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException() : base() { }
    
    public DomainException(string message) : base(message) { }
    
    public DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}
