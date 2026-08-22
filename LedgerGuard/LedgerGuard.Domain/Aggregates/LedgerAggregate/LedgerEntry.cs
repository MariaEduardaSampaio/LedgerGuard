using LedgerGuard.Domain.Aggregates.MoneyAggregate;

namespace LedgerGuard.Domain.Aggregates.LedgerAggregate;

public sealed record LedgerEntry
{
    public Guid AccountId { get; }
    public Money Amount { get; }
    public ELedgerEntryType Type { get; }

    private LedgerEntry(
        Guid accountId,
        Money amount,
        ELedgerEntryType type)
    {
        AccountId = accountId;
        Amount = amount;
        Type = type;
    }

    public static LedgerEntry Create(
        Guid accountId,
        Money amount,
        ELedgerEntryType type)
    {
        if (accountId == Guid.Empty)
            throw new ArgumentException(
                "Account id cannot be empty.",
                nameof(accountId));

        if (amount.Amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount), 
                "Ledger entry amount must be greater than zero.");
        
        if (!Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(
                nameof(type), 
                "Invalid ledger entry type.");

        return new LedgerEntry(
            accountId,
            amount,
            type);
    }
}