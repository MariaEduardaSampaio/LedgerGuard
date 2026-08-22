using FluentAssertions;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;

namespace LedgerGuard.UnitTests.Domain.Aggregates.TransferReversalAggregate;

[TestFixture]
public sealed class TransferReversalTests
{
    private static readonly DateTimeOffset ValidRequestedAt =
        new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ValidCompletedAt =
        new(2026, 8, 22, 13, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_WhenDataIsValid_ShouldCreatePendingReversal()
    {
        // Arrange
        var transferId = Guid.NewGuid();

        // Act
        var reversal = TransferReversal.Create(
            transferId,
            ValidRequestedAt);

        // Assert
        reversal.Id.Should().NotBeEmpty();
        reversal.TransferId.Should().Be(transferId);
        reversal.Status.Should().Be(ReversalStatus.Pending);
        reversal.RequestedAt.Should().Be(ValidRequestedAt);
    }

    [Test]
    public void Create_WhenReversalIsCreated_ShouldNotHaveCompletionData()
    {
        // Act
        var reversal = TransferReversal.Create(
            Guid.NewGuid(),
            ValidRequestedAt);

        // Assert
        reversal.Status.Should().Be(ReversalStatus.Pending);
        reversal.CompletedAt.Should().BeNull();
        reversal.LedgerTransactionId.Should().BeNull();
    }

    [Test]
    public void Create_WhenTransferIdIsEmpty_ShouldRejectReversal()
    {
        // Act
        var act = () => TransferReversal.Create(
            Guid.Empty,
            ValidRequestedAt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("transferId");
    }

    [Test]
    public void Create_WhenRequestedAtIsDefault_ShouldRejectReversal()
    {
        // Act
        var act = () => TransferReversal.Create(
            Guid.NewGuid(),
            default);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("requestedAt");
    }

    [Test]
    public void Complete_WhenReversalIsPending_ShouldCompleteReversal()
    {
        // Arrange
        var reversal = TransferReversal.Create(
            Guid.NewGuid(),
            ValidRequestedAt);

        var ledgerTransactionId = Guid.NewGuid();

        // Act
        reversal.Complete(
            ledgerTransactionId,
            ValidCompletedAt);

        // Assert
        reversal.Status.Should().Be(ReversalStatus.Completed);
        reversal.CompletedAt.Should().Be(ValidCompletedAt);
        reversal.LedgerTransactionId.Should().Be(ledgerTransactionId);
    }

    [Test]
    public void Complete_WhenCompletedAtEqualsRequestedAt_ShouldCompleteReversal()
    {
        // Arrange
        var reversal = TransferReversal.Create(
            Guid.NewGuid(),
            ValidRequestedAt);

        var ledgerTransactionId = Guid.NewGuid();

        // Act
        reversal.Complete(
            ledgerTransactionId,
            ValidRequestedAt);

        // Assert
        reversal.Status.Should().Be(ReversalStatus.Completed);
        reversal.CompletedAt.Should().Be(ValidRequestedAt);
        reversal.LedgerTransactionId.Should().Be(ledgerTransactionId);
    }

    [Test]
    public void Complete_WhenLedgerTransactionIdIsEmpty_ShouldRejectCompletion()
    {
        // Arrange
        var reversal = TransferReversal.Create(
            Guid.NewGuid(),
            ValidRequestedAt);

        // Act
        var act = () => reversal.Complete(
            Guid.Empty,
            ValidCompletedAt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("ledgerTransactionId");
    }

    [Test]
    public void Complete_WhenCompletedAtIsDefault_ShouldRejectCompletion()
    {
        // Arrange
        var reversal = TransferReversal.Create(
            Guid.NewGuid(),
            ValidRequestedAt);

        // Act
        var act = () => reversal.Complete(
            Guid.NewGuid(),
            default);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("completedAt");
    }

    [Test]
    public void Complete_WhenCompletedAtIsBeforeRequestedAt_ShouldRejectCompletion()
    {
        // Arrange
        var reversal = TransferReversal.Create(
            Guid.NewGuid(),
            ValidRequestedAt);

        var completedAt = ValidRequestedAt.AddTicks(-1);

        // Act
        var act = () => reversal.Complete(
            Guid.NewGuid(),
            completedAt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("completedAt");
    }

    [Test]
    public void Complete_WhenReversalIsAlreadyCompleted_ShouldRejectCompletion()
    {
        // Arrange
        var reversal = TransferReversal.Create(
            Guid.NewGuid(),
            ValidRequestedAt);

        reversal.Complete(
            Guid.NewGuid(),
            ValidCompletedAt);

        // Act
        var act = () => reversal.Complete(
            Guid.NewGuid(),
            ValidCompletedAt.AddMinutes(1));

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Complete_WhenLedgerTransactionIdIsEmpty_ShouldKeepReversalPending()
    {
        // Arrange
        var reversal = TransferReversal.Create(
            Guid.NewGuid(),
            ValidRequestedAt);

        // Act
        var act = () => reversal.Complete(
            Guid.Empty,
            ValidCompletedAt);

        act.Should().Throw<ArgumentException>();

        // Assert
        reversal.Status.Should().Be(ReversalStatus.Pending);
        reversal.CompletedAt.Should().BeNull();
        reversal.LedgerTransactionId.Should().BeNull();
    }

    [Test]
    public void Complete_WhenCompletedAtIsInvalid_ShouldKeepReversalPending()
    {
        // Arrange
        var reversal = TransferReversal.Create(
            Guid.NewGuid(),
            ValidRequestedAt);

        var invalidCompletedAt =
            ValidRequestedAt.AddTicks(-1);

        // Act
        var act = () => reversal.Complete(
            Guid.NewGuid(),
            invalidCompletedAt);

        act.Should().Throw<ArgumentException>();

        // Assert
        reversal.Status.Should().Be(ReversalStatus.Pending);
        reversal.CompletedAt.Should().BeNull();
        reversal.LedgerTransactionId.Should().BeNull();
    }

    [Test]
    public void Complete_WhenReversalIsCompleted_ShouldPreserveOriginalRequestData()
    {
        // Arrange
        var transferId = Guid.NewGuid();

        var reversal = TransferReversal.Create(
            transferId,
            ValidRequestedAt);

        // Act
        reversal.Complete(
            Guid.NewGuid(),
            ValidCompletedAt);

        // Assert
        reversal.TransferId.Should().Be(transferId);
        reversal.RequestedAt.Should().Be(ValidRequestedAt);
    }

    [Test]
    public void TransferReversal_WhenCreated_ShouldNotAllowImmutablePropertiesToBeModified()
    {
        // Arrange
        var immutableProperties = new[]
        {
            nameof(TransferReversal.Id),
            nameof(TransferReversal.TransferId),
            nameof(TransferReversal.RequestedAt)
        };

        // Assert
        foreach (var propertyName in immutableProperties)
        {
            var property =
                typeof(TransferReversal).GetProperty(propertyName);

            property.Should().NotBeNull();
            property!.SetMethod?.IsPublic.Should().BeFalse();
        }
    }

    [Test]
    public void TransferReversal_WhenCreated_ShouldNotAllowStateToBeDirectlyModified()
    {
        // Arrange
        var stateProperties = new[]
        {
            nameof(TransferReversal.Status),
            nameof(TransferReversal.CompletedAt),
            nameof(TransferReversal.LedgerTransactionId)
        };

        // Assert
        foreach (var propertyName in stateProperties)
        {
            var property =
                typeof(TransferReversal).GetProperty(propertyName);

            property.Should().NotBeNull();
            property!.SetMethod?.IsPublic.Should().BeFalse();
        }
    }
}