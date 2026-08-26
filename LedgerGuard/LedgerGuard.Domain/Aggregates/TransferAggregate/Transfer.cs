using LedgerGuard.Domain.ValueObjects;

namespace LedgerGuard.Domain.Aggregates.TransferAggregate;

public sealed class Transfer
{
    public Guid Id { get; }
    public Guid SourceAccountId { get; }
    public Guid DestinationAccountId { get; }
    public Money Amount { get; }
    public Guid LedgerTransactionId { get; }
    public DateTimeOffset CreatedAt { get; }
    
    private Transfer() { }

    private Transfer(
        Guid id,
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        Guid ledgerTransactionId,
        DateTimeOffset createdAt)
    {
        Id = id;
        SourceAccountId = sourceAccountId;
        DestinationAccountId = destinationAccountId;
        Amount = amount;
        LedgerTransactionId = ledgerTransactionId;
        CreatedAt = createdAt;
    }

    public static Transfer Create(
        Guid sourceAccountId,
        Guid destinationAccountId,
        Money amount,
        Guid ledgerTransactionId,
        DateTimeOffset createdAt)
    {
        if (amount is null)
            throw new ArgumentNullException(nameof(amount),
                "Ledger entry amount cannot be null.");
        
        if (amount.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount),
                "Ledger entry amount cannot be zero or negative.");
        
        if (createdAt == default)
            throw new ArgumentException(
                "Ledger entry creation date cannot be the default value.",
                nameof(createdAt));
        
        if (destinationAccountId == Guid.Empty)
            throw new ArgumentException(
                "Destination account ID cannot be empty.",
                nameof(destinationAccountId));
        
        if (sourceAccountId == Guid.Empty)
            throw new ArgumentException(
                "Source account ID cannot be empty.",
                nameof(sourceAccountId));
        
        if (ledgerTransactionId == Guid.Empty)
            throw new ArgumentException(
                "Ledger transaction ID cannot be empty.",
                nameof(ledgerTransactionId));
        
        if (sourceAccountId == destinationAccountId)
            throw new ArgumentException(
                "Source and destination account IDs cannot be the same.",
                nameof(destinationAccountId));

        return new Transfer(
            Guid.NewGuid(),
            sourceAccountId,
            destinationAccountId,
            amount,
            ledgerTransactionId,
            createdAt);
    }
}