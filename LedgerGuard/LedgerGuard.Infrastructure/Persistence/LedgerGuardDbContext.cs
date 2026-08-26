using LedgerGuard.Domain.Aggregates.AccountAggregate;
using Microsoft.EntityFrameworkCore;

namespace LedgerGuard.Infrastructure.Persistence;

public sealed class LedgerGuardDbContext(DbContextOptions<LedgerGuardDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(LedgerGuardDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}