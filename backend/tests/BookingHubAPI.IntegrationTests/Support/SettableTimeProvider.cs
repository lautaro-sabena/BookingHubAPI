namespace BookingHubAPI.IntegrationTests.Support;

/// <summary>
/// The API clock in integration tests: real time until a test pins it with <see cref="Pin"/>.
/// A pinned clock is shared by every request of the factory, so only tests that need it should pin it
/// (and run in a fixture of their own).
/// </summary>
public sealed class SettableTimeProvider : TimeProvider
{
    private DateTimeOffset? _pinned;

    public void Pin(DateTimeOffset now) => _pinned = now;

    public void Unpin() => _pinned = null;

    public override DateTimeOffset GetUtcNow() => _pinned ?? base.GetUtcNow();
}
