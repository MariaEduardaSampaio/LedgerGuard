using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.TransferAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerGuard.Infrastructure.Persistence.Configurations;

public sealed class TransferConfiguration
    : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable(
            "transfers",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_transfers_different_accounts",
                    "source_account_id <> destination_account_id");

                table.HasCheckConstraint(
                    "ck_transfers_amount_positive",
                    "amount_value > 0");
            });

        builder.HasKey(transfer => transfer.Id);

        builder.Property(transfer => transfer.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(transfer => transfer.SourceAccountId)
            .HasColumnName("source_account_id")
            .IsRequired();

        builder.Property(transfer => transfer.DestinationAccountId)
            .HasColumnName("destination_account_id")
            .IsRequired();

        builder.Property(transfer => transfer.LedgerTransactionId)
            .HasColumnName("ledger_transaction_id")
            .IsRequired();

        builder.Property(transfer => transfer.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.OwnsOne(
            transfer => transfer.Amount,
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

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transfer => transfer.SourceAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transfer => transfer.DestinationAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LedgerTransaction>()
            .WithMany()
            .HasForeignKey(transfer => transfer.LedgerTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(transfer => transfer.SourceAccountId);

        builder.HasIndex(transfer => transfer.DestinationAccountId);

        builder.HasIndex(transfer => transfer.LedgerTransactionId)
            .IsUnique();
    }
}