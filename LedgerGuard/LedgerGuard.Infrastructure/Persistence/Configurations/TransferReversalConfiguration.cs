using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.TransferAggregate;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerGuard.Infrastructure.Persistence.Configurations;

public sealed class TransferReversalConfiguration
    : IEntityTypeConfiguration<TransferReversal>
{
    public void Configure(EntityTypeBuilder<TransferReversal> builder)
    {
        builder.ToTable(
            "transfer_reversals",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_transfer_reversals_completed_after_requested",
                    "completed_at IS NULL OR completed_at >= requested_at");
            });

        builder.HasKey(reversal => reversal.Id);

        builder.Property(reversal => reversal.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(reversal => reversal.TransferId)
            .HasColumnName("transfer_id")
            .IsRequired();

        builder.Property(reversal => reversal.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(reversal => reversal.RequestedAt)
            .HasColumnName("requested_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(reversal => reversal.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(reversal => reversal.LedgerTransactionId)
            .HasColumnName("ledger_transaction_id");

        builder.HasOne<Transfer>()
            .WithMany()
            .HasForeignKey(reversal => reversal.TransferId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LedgerTransaction>()
            .WithMany()
            .HasForeignKey(reversal => reversal.LedgerTransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(reversal => reversal.TransferId)
            .IsUnique();

        builder.HasIndex(reversal => reversal.LedgerTransactionId)
            .IsUnique();

        builder.HasIndex(reversal => new
        {
            reversal.Status,
            reversal.RequestedAt
        });
    }
}