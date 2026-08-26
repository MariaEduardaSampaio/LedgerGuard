using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.TransferAggregate;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;

namespace LedgerGuard.Application.Reversals;

public sealed record PendingReversalItem(
    Transfer Transfer,
    TransferReversal Reversal,
    Account OriginalSource);