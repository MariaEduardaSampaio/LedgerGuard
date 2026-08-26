using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.TransferAggregate;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;
using Microsoft.EntityFrameworkCore;

namespace LedgerGuard.Infrastructure.Persistence;

public sealed class LedgerGuardDbContext(DbContextOptions<LedgerGuardDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transfer> Transfers => Set<Transfer>();
    public DbSet<TransferReversal> TransferReversals => Set<TransferReversal>();
    public DbSet<LedgerTransaction> LedgerTransactions => Set<LedgerTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(LedgerGuardDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}