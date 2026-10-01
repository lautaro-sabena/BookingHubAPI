using Xunit;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BookingHubAPI.IntegrationTests.Infrastructure;

/// <summary>InMemory has no transactions, so these cover what does not depend on rollback: running the work, propagating failures and dropping pending entities. Rollback itself needs PostgreSQL.</summary>
public class EfUnitOfWorkTests
{
    private readonly BookingDbContext _context;
    private readonly EfUnitOfWork _sut;

    public EfUnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase("UnitOfWork_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _context = new BookingDbContext(options);
        _sut = new EfUnitOfWork(_context);
    }

    [Fact]
    public async Task ExecuteInTransaction_ShouldRunTheWork()
    {
        await _sut.ExecuteInTransactionAsync(async () =>
        {
            _context.Users.Add(new User { Id = Guid.NewGuid(), Email = "a@test.com", PasswordHash = "h" });
            await _context.SaveChangesAsync();
        });

        (await _context.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ExecuteInTransaction_WhenTheWorkThrows_ShouldPropagateAndForgetPendingEntities()
    {
        var act = () => _sut.ExecuteInTransactionAsync(() =>
        {
            _context.Users.Add(new User { Id = Guid.NewGuid(), Email = "a@test.com", PasswordHash = "h" });
            throw new InvalidOperationException("boom");
        });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");
        _context.ChangeTracker.Entries().Should().BeEmpty();
    }
}
