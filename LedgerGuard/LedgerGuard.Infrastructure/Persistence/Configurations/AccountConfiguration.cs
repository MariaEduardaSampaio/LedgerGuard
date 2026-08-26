using LedgerGuard.Domain.Aggregates.AccountAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerGuard.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration
    : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(account => account.OwnerName)
            .HasColumnName("owner_name")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(account => account.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsOne(
            account => account.Balance,
            balance =>
            {
                balance.Property(money => money.Amount)
                    .HasColumnName("balance_amount")
                    .HasPrecision(20, 2)
                    .IsRequired();

                balance.Property(money => money.Currency)
                    .HasColumnName("balance_currency")
                    .HasConversion<string>()
                    .HasMaxLength(3)
                    .IsRequired();
            });

        builder.ToTable(
            table => table.HasCheckConstraint(
                "ck_accounts_balance_non_negative",
                "balance_amount >= 0"));
    }
}