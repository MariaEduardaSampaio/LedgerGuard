using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.TransferAggregate;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;

namespace LedgerGuard.Application.Reversals;

public static class ExecuteTransferReversal
{
    public static ExecuteTransferReversalResult Execute(
        Transfer transfer,
        TransferReversal reversal,
        Account source,
        Account destination,
        DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(transfer);
        ArgumentNullException.ThrowIfNull(reversal);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        if (reversal.TransferId != transfer.Id)
            throw new InvalidOperationException(
                "Reversal does not belong to the provided transfer.");

        if (source.Id != transfer.SourceAccountId)
            throw new InvalidOperationException(
                "Source account does not match the original transfer.");

        if (destination.Id != transfer.DestinationAccountId)
            throw new InvalidOperationException(
                "Destination account does not match the original transfer.");

        if (reversal.Status != ReversalStatus.Pending)
            throw new InvalidOperationException(
                "Only pending reversals can be executed.");

        if (completedAt == default)
            throw new ArgumentException(
                "Completion date is required.",
                nameof(completedAt));

        if (completedAt < reversal.RequestedAt)
            throw new ArgumentException(
                "Completion date cannot be before the reversal request.",
                nameof(completedAt));

        if (source.Status == EAccountStatus.Closed)
            throw new InvalidOperationException(
                "Closed source accounts cannot receive reversals.");

        if (destination.Status == EAccountStatus.Closed)
            throw new InvalidOperationException(
                "Closed destination accounts cannot be debited for reversals.");

        var amount = transfer.Amount;

        if (destination.Balance.Amount < amount.Amount)
            throw new InvalidOperationException(
                "Destination account has insufficient funds for reversal.");

        source.Balance.Add(amount);

        var debitEntry = LedgerEntry.Create(
            destination.Id,
            amount,
            ELedgerEntryType.Debit);

        var creditEntry = LedgerEntry.Create(
            source.Id,
            amount,
            ELedgerEntryType.Credit);

        var ledgerTransaction = LedgerTransaction.Create(
            ELedgerTransactionType.Reversal,
            [debitEntry, creditEntry]);

        destination.DebitForReversal(amount);
        source.Credit(amount);

        reversal.Complete(
            ledgerTransaction.Id,
            completedAt);

        return new ExecuteTransferReversalResult(
            reversal,
            ledgerTransaction);
    }
}