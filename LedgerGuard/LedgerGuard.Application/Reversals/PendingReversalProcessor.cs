using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;

namespace LedgerGuard.Application.Reversals;

public static class PendingReversalProcessor
{
    public static IReadOnlyCollection<ExecuteTransferReversalResult> Execute(
        Account account,
        IEnumerable<PendingReversalItem> reversals,
        DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(reversals);

        if (completedAt == default)
            throw new ArgumentException(
                "Completion date is required.",
                nameof(completedAt));

        var orderedReversals = reversals
            .Where(item => item.Reversal.Status == ReversalStatus.Pending)
            .OrderBy(item => item.Reversal.RequestedAt)
            .ThenBy(item => item.Reversal.Id)
            .ToList();

        var results = new List<ExecuteTransferReversalResult>();

        foreach (var item in orderedReversals)
        {
            ValidateItem(item);

            if (account.Balance.Amount < item.Transfer.Amount.Amount)
                continue;

            var result = ExecuteTransferReversal.Execute(
                item.Transfer,
                item.Reversal,
                item.OriginalSource,
                account,
                completedAt);

            results.Add(result);
        }

        return results.AsReadOnly();
    }

    private static void ValidateItem(PendingReversalItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(item.Transfer);
        ArgumentNullException.ThrowIfNull(item.Reversal);
        ArgumentNullException.ThrowIfNull(item.OriginalSource);
    }
}