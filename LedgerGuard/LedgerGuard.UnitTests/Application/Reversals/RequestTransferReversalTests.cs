using FluentAssertions;
using LedgerGuard.Application.Reversals;
using LedgerGuard.Domain.Aggregates.TransferAggregate;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;
using LedgerGuard.Domain.ValueObjects;

namespace LedgerGuard.UnitTests.Application.Reversals;

[TestFixture]
public sealed class RequestTransferReversalTests
{
    private static readonly DateTimeOffset TransferCreatedAt =
        new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ReversalRequestedAt =
        new(2026, 8, 25, 13, 0, 0, TimeSpan.Zero);

    [Test]
    public void Execute_WhenTransferIsValid_ShouldCreatePendingReversal()
    {
        // Arrange
        var transfer = CreateValidTransfer();

        // Act
        var reversal = RequestTransferReversal.Execute(
            transfer,
            ReversalRequestedAt);

        // Assert
        reversal.Id.Should().NotBeEmpty();
        reversal.TransferId.Should().Be(transfer.Id);
        reversal.Status.Should().Be(ReversalStatus.Pending);
        reversal.RequestedAt.Should().Be(ReversalRequestedAt);
        reversal.CompletedAt.Should().BeNull();
        reversal.LedgerTransactionId.Should().BeNull();
    }

    [Test]
    public void Execute_WhenTransferIsNull_ShouldRejectRequest()
    {
        // Act
        var act = () => RequestTransferReversal.Execute(
            null!,
            ReversalRequestedAt);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("transfer");
    }

    [Test]
    public void Execute_WhenRequestedAtIsInvalid_ShouldRejectRequest()
    {
        // Arrange
        var transfer = CreateValidTransfer();

        // Act
        var act = () => RequestTransferReversal.Execute(
            transfer,
            default);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("requestedAt");
    }

    private static Transfer CreateValidTransfer()
    {
        return Transfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            Guid.NewGuid(),
            TransferCreatedAt);
    }
}