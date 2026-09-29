using BookingHubAPI.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BookingHubAPI.Infrastructure.Data;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly BookingDbContext _context;

    public EfUnitOfWork(BookingDbContext context)
    {
        _context = context;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> work)
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
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await work();
                await transaction.CommitAsync();
            }
            catch
            {
                // Disposing the transaction rolls it back; forget the entities of the failed attempt so a retry
                // (or later use of this scoped context) does not save them again.
                _context.ChangeTracker.Clear();
                throw;
            }
        });
    }
}
