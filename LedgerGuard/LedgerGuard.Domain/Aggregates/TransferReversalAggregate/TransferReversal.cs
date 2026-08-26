namespace LedgerGuard.Domain.Aggregates.TransferReversalAggregate;

public sealed class TransferReversal
{
    public Guid Id { get; }
    public Guid TransferId { get; }
    public ReversalStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? LedgerTransactionId { get; private set; }
    
    private TransferReversal() { }

    private TransferReversal(Guid id, Guid transferId, DateTimeOffset requestedAt)
    {
        Id = id;
        TransferId = transferId;
        RequestedAt = requestedAt;
        Status = ReversalStatus.Pending;
    }

    public static TransferReversal Create(Guid transferId, DateTimeOffset requestedAt)
    {
        if (transferId == Guid.Empty)
            throw new ArgumentException(
                "Transfer id cannot be empty.",
                nameof(transferId));
        
        if (requestedAt == default)
            throw new ArgumentException(
                "Request date is required.",
                nameof(requestedAt));
        
        return new TransferReversal(Guid.NewGuid(), transferId, requestedAt);
    }
    
    public void Complete(
        Guid ledgerTransactionId,
        DateTimeOffset completedAt)
    {
        if (Status != ReversalStatus.Pending)
            throw new InvalidOperationException(
                "Only pending reversals can be completed.");

        if (ledgerTransactionId == Guid.Empty)
            throw new ArgumentException(
                "Ledger transaction id cannot be empty.",
                nameof(ledgerTransactionId));

        if (completedAt == default)
            throw new ArgumentException(
                "Completion date is required.",
                nameof(completedAt));

        if (completedAt < RequestedAt)
            throw new ArgumentException(
                "Completion date cannot be before request date.",
                nameof(completedAt));

        Status = ReversalStatus.Completed;
        LedgerTransactionId = ledgerTransactionId;
        CompletedAt = completedAt;
    }
}