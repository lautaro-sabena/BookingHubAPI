namespace BookingHubAPI.Domain.Exceptions;

/// <summary>
/// An entity was asked to break one of its own invariants (e.g. an illegal status transition).
/// Application services check the corresponding rule first and return an expected error, so
/// reaching this exception means a caller skipped that check: it is a programming error, not user input.
/// </summary>
public class DomainException : InvalidOperationException
{
    public DomainException(string message) : base(message)
    {
    }
}
