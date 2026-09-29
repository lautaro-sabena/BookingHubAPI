namespace BookingHubAPI.Application.Abstractions;

/// <summary>One-way password hashing; implemented by the infrastructure layer.</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
