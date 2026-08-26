using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;

namespace LedgerGuard.Application.Reversals;

public sealed record ExecuteTransferReversalResult(
    TransferReversal Reversal,
    LedgerTransaction LedgerTransaction);