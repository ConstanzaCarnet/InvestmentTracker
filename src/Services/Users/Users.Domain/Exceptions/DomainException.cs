namespace Users.Domain.Exceptions;

/// <summary>Business rule violation — maps to HTTP 4xx, not 500.</summary>
public class DomainException : Exception
{
    public int StatusCode { get; }

    public DomainException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }
}
