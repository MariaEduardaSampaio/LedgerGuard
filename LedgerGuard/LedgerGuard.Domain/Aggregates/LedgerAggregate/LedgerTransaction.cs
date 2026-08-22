namespace LedgerGuard.Domain.Aggregates.LedgerAggregate;

public sealed class LedgerTransaction
{
    private readonly List<LedgerEntry> _entries;

    public Guid Id { get; }
    public LedgerTransactionType Type { get; }
    public IReadOnlyCollection<LedgerEntry> Entries => _entries.AsReadOnly();

    private LedgerTransaction(
        Guid id,
        LedgerTransactionType type,
        IEnumerable<LedgerEntry> entries)
    {
        Id = id;
        Type = type;
        _entries = entries.ToList();
    }

    public static LedgerTransaction Create(
        LedgerTransactionType type,
        IEnumerable<LedgerEntry> entries)
    {
        var entryList = entries.ToList();

        if (entryList.Count < 2)
            throw new InvalidOperationException(
                "A ledger transaction must have at least two entries.");
        
        if (!Enum.IsDefined(typeof(LedgerTransactionType), type))
            throw new ArgumentOutOfRangeException(nameof(type),
                "Invalid ledger transaction type.");
        
        ValidateCurrencies(entryList);
        ValidateBalance(entryList);

        return new LedgerTransaction(
            Guid.NewGuid(),
            type,
            entryList);
    }

    private static void ValidateCurrencies(
        IReadOnlyCollection<LedgerEntry> entries)
    {
        var currency = entries.First().Amount.Currency;

        if (entries.Any(entry => entry.Amount.Currency != currency))
            throw new InvalidOperationException(
                "All ledger entries must use the same currency.");
    }

    private static void ValidateBalance(
        IReadOnlyCollection<LedgerEntry> entries)
    {
        var totalDebit = entries
            .Where(entry => entry.Type == LedgerEntryType.Debit)
            .Sum(entry => entry.Amount.Amount);

        var totalCredit = entries
            .Where(entry => entry.Type == LedgerEntryType.Credit)
            .Sum(entry => entry.Amount.Amount);

        if (totalDebit != totalCredit)
            throw new InvalidOperationException(
                "Ledger transaction must be balanced.");
    }
}