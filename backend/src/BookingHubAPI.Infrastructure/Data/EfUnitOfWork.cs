using BookingHubAPI.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace BookingHubAPI.Infrastructure.Data;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly BookingDbContext _context;

    public EfUnitOfWork(BookingDbContext context)
    {
        _context = context;
    }

    public Task ExecuteInTransactionAsync(Func<Task> work) => ExecuteAsync(work, null);

    public Task ExecuteInTransactionAsync(Func<Task> work, Func<Task<bool>> verifySucceeded) =>
        ExecuteAsync(work, verifySucceeded ?? throw new ArgumentNullException(nameof(verifySucceeded)));

    private async Task ExecuteAsync(Func<Task> work, Func<Task<bool>>? verifySucceeded)
    {
        // Already inside a transaction: join it, the outermost call commits.
        if (_context.Database.CurrentTransaction != null)
        {
            await work();
            return;
        }

        // A retrying execution strategy (EnableRetryOnFailure) rejects user transactions unless the
        // whole transaction runs inside the strategy, so a transient failure re-runs it from the start.
        var strategy = _context.Database.CreateExecutionStrategy();
        // If the commit succeeded server-side but the connection dropped before the answer arrived, the strategy
        // would re-run the work; verifySucceeded lets it detect that and stop instead.
        await strategy.ExecuteAsync<object?, bool>(
            null,
            async (_, _) =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await work();
                    await transaction.CommitAsync();
                    return true;
                }
                catch
                {
                    // Disposing the transaction rolls it back; forget the entities of the failed attempt so a retry
                    // (or later use of this scoped context) does not save them again.
                    _context.ChangeTracker.Clear();
                    throw;
                }
            },
            verifySucceeded == null
                ? null
                : async (_, _) => new ExecutionResult<bool>(await verifySucceeded(), true),
            default);
    }
}
