using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.MoneyAggregate;
using LedgerGuard.Domain.Aggregates.TransferAggregate;

namespace LedgerGuard.Application.Transfers;

public static class TransferFunds
{
    public static TransferFundsResult Execute(
        Account source,
        Account destination,
        decimal amount,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        if (source.Id == destination.Id)
            throw new InvalidOperationException(
                "Source and destination accounts must be different.");
        
        if (createdAt == default)
            throw new ArgumentException(
                "Creation date must be provided.",
                nameof(createdAt));
        

        var money = Money.CreateBrl(amount);

        if (money.Amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Transfer amount must be greater than zero.");

        ValidateDestinationCanReceive(destination, money);

        var debitEntry = LedgerEntry.Create(
            source.Id,
            money,
            ELedgerEntryType.Debit);

        var creditEntry = LedgerEntry.Create(
            destination.Id,
            money,
            ELedgerEntryType.Credit);

        var ledgerTransaction = LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
            [debitEntry, creditEntry]);

        source.Debit(money);

        destination.Credit(money);

        var transfer = Transfer.Create(
            source.Id,
            destination.Id,
            money,
            ledgerTransaction.Id,
            createdAt);

        return new TransferFundsResult(
            transfer,
            ledgerTransaction);
    }

    private static void ValidateDestinationCanReceive(
        Account destination,
        Money amount)
    {
        if (destination.Status == EAccountStatus.Closed)
            throw new InvalidOperationException(
                "Closed accounts cannot receive funds.");

        destination.Balance.Add(amount);
    }
}