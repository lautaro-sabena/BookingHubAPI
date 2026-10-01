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

    /// <summary>
    /// Same as <see cref="ExecuteInTransactionAsync(Func{Task})"/>, for work that is not naturally idempotent.
    /// A connection can drop after the server committed, so a transient failure is ambiguous: before re-running
    /// the delegate, <paramref name="verifySucceeded"/> is asked whether the work is already committed
    /// (<c>true</c> = do not run it again). The delegate must build its entities from ids fixed outside it.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> work, Func<Task<bool>> verifySucceeded);
}
