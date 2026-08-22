using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.MoneyAggregate;

namespace LedgerGuard.Application.Deposits;

public static class DepositFunds
{
    public static LedgerTransaction Execute(
        Account account,
        decimal amount,
        Guid settlementAccountId)
    {
        ArgumentNullException.ThrowIfNull(account);

        if (settlementAccountId == Guid.Empty)
            throw new ArgumentException(
                "Settlement account id cannot be empty.",
                nameof(settlementAccountId));

        var money = Money.CreateBrl(amount);

        var debitEntry = LedgerEntry.Create(
            settlementAccountId,
            money,
            ELedgerEntryType.Debit);

        var creditEntry = LedgerEntry.Create(
            account.Id,
            money,
            ELedgerEntryType.Credit);

        var ledgerTransaction = LedgerTransaction.Create(
            ELedgerTransactionType.Deposit,
            [debitEntry, creditEntry]);

        account.Credit(money);

        return ledgerTransaction;
    }
}