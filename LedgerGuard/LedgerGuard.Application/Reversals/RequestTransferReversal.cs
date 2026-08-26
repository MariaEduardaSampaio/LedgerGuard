using LedgerGuard.Domain.Aggregates.TransferAggregate;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;

namespace LedgerGuard.Application.Reversals;

public static class RequestTransferReversal
{
    public static TransferReversal Execute(
        Transfer transfer,
        DateTimeOffset requestedAt)
    {
        ArgumentNullException.ThrowIfNull(transfer);

        return TransferReversal.Create(
            transfer.Id,
            requestedAt);
    }
}