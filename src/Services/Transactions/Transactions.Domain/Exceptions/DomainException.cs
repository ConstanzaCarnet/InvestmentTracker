namespace Transactions.Domain.Exceptions;

/// <summary>
/// Represents a business rule violation. Maps to HTTP 400 Bad Request,
/// as opposed to unexpected server errors which map to HTTP 500.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
