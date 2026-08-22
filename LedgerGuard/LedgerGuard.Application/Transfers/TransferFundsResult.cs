using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.TransferAggregate;

namespace LedgerGuard.Application.Transfers;

public sealed record TransferFundsResult(
    Transfer Transfer,
    LedgerTransaction LedgerTransaction);