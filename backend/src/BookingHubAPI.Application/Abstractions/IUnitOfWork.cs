namespace BookingHubAPI.Application.Abstractions;

/// <summary>Groups several repository writes into one atomic database transaction.</summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Runs <paramref name="work"/> in a transaction: it commits when the delegate completes and rolls
    /// everything back when it throws (the exception then propagates). When the connection fails
    /// transiently the whole delegate is run again, so it must be safe to repeat: build entities inside
    /// it and keep expensive or non-database side effects outside.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> work);
}
