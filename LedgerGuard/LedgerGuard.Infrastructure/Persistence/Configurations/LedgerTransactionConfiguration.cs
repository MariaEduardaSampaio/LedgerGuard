using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerGuard.Infrastructure.Persistence.Configurations;

public sealed class LedgerTransactionConfiguration
    : IEntityTypeConfiguration<LedgerTransaction>
{
    public void Configure(
        EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.ToTable("ledger_transactions");

        builder.HasKey(transaction => transaction.Id);

        builder.Property(transaction => transaction.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.OwnsMany(
            transaction => transaction.Entries,
            entry =>
            {
                entry.ToTable("ledger_entries");

                entry.Property<Guid>("id")
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();

                entry.HasKey("id");

                entry.WithOwner()
                    .HasForeignKey("ledger_transaction_id");

                entry.Property(ledgerEntry => ledgerEntry.AccountId)
                    .HasColumnName("account_id")
                    .IsRequired();

                entry.Property(ledgerEntry => ledgerEntry.Type)
                    .HasColumnName("entry_type")
                    .HasConversion<string>()
                    .HasMaxLength(10)
                    .IsRequired();

                entry.OwnsOne(
                    ledgerEntry => ledgerEntry.Amount,
                    amount =>
                    {
                        amount.Property(money => money.Amount)
                            .HasColumnName("amount_value")
                            .HasPrecision(20, 2)
                            .IsRequired();

                        amount.Property(money => money.Currency)
                            .HasColumnName("amount_currency")
                            .HasConversion<string>()
                            .HasMaxLength(3)
                            .IsRequired();
                    });

                entry.HasOne<Account>()
                    .WithMany()
                    .HasForeignKey(ledgerEntry => ledgerEntry.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entry.HasIndex("ledger_transaction_id");

                entry.HasIndex(ledgerEntry => ledgerEntry.AccountId);
            });

        builder.Navigation(transaction => transaction.Entries)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}