using FluentAssertions;
using LedgerGuard.Application.Reversals;
using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.MoneyAggregate;
using LedgerGuard.Domain.Aggregates.TransferAggregate;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;

namespace LedgerGuard.UnitTests.Application.Reversals;

[TestFixture]
public sealed class PendingReversalProcessorTests
{
    private static readonly DateTimeOffset TransferCreatedAt =
        new(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset CompletedAt =
        new(2026, 8, 25, 15, 0, 0, TimeSpan.Zero);

    [Test]
    public void Execute_WhenNoReversalsExist_ShouldReturnEmptyResult()
    {
        // Arrange
        var destination = Account.Create("Alice");

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            [],
            CompletedAt);

        // Assert
        results.Should().BeEmpty();
    }

    [Test]
    public void Execute_WhenSinglePendingReversalHasSufficientBalance_ShouldCompleteReversal()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            100m);

        var item = CreatePendingReversal(
            destination,
            100m,
            requestedAt: CompletedAt.AddHours(-1));

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            [item],
            CompletedAt);

        // Assert
        results.Should().HaveCount(1);

        item.Reversal.Status
            .Should().Be(ReversalStatus.Completed);

        destination.Balance.Amount
            .Should().Be(0m);

        item.OriginalSource.Balance.Amount
            .Should().Be(100m);
    }

    [Test]
    public void Execute_WhenSinglePendingReversalHasInsufficientBalance_ShouldKeepReversalPending()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            99.99m);

        var item = CreatePendingReversal(
            destination,
            100m,
            requestedAt: CompletedAt.AddHours(-1));

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            [item],
            CompletedAt);

        // Assert
        results.Should().BeEmpty();

        item.Reversal.Status
            .Should().Be(ReversalStatus.Pending);

        destination.Balance.Amount
            .Should().Be(99.99m);

        item.OriginalSource.Balance.Amount
            .Should().Be(0m);
    }

    [Test]
    public void Execute_WhenBalanceExactlyMatchesReversalAmount_ShouldCompleteReversal()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            100m);

        var item = CreatePendingReversal(
            destination,
            100m,
            CompletedAt.AddHours(-1));

        // Act
        PendingReversalProcessor.Execute(
            destination,
            [item],
            CompletedAt);

        // Assert
        destination.Balance.Amount.Should().Be(0m);
        item.Reversal.Status.Should().Be(ReversalStatus.Completed);
    }

    [Test]
    public void Execute_WhenBalanceIsOneCentBelowReversalAmount_ShouldKeepReversalPending()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            99.99m);

        var item = CreatePendingReversal(
            destination,
            100m,
            CompletedAt.AddHours(-1));

        // Act
        PendingReversalProcessor.Execute(
            destination,
            [item],
            CompletedAt);

        // Assert
        destination.Balance.Amount.Should().Be(99.99m);
        item.Reversal.Status.Should().Be(ReversalStatus.Pending);
    }

    [Test]
    public void Execute_WhenMultipleReversalsCanBeCompleted_ShouldProcessOldestFirst()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            100m);

        var oldest = CreatePendingReversal(
            destination,
            40m,
            CompletedAt.AddHours(-3));

        var middle = CreatePendingReversal(
            destination,
            30m,
            CompletedAt.AddHours(-2));

        var newest = CreatePendingReversal(
            destination,
            20m,
            CompletedAt.AddHours(-1));

        // Passamos propositalmente fora de ordem.
        var reversals = new[]
        {
            newest,
            oldest,
            middle
        };

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            reversals,
            CompletedAt);

        // Assert
        results.Should().HaveCount(3);

        results.Select(result => result.Reversal.Id)
            .Should()
            .ContainInOrder(
                oldest.Reversal.Id,
                middle.Reversal.Id,
                newest.Reversal.Id);

        destination.Balance.Amount.Should().Be(10m);
    }

    [Test]
    public void Execute_WhenOldestReversalHasInsufficientBalance_ShouldSkipAndProcessNext()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            60m);

        var oldest = CreatePendingReversal(
            destination,
            100m,
            CompletedAt.AddHours(-2));

        var newest = CreatePendingReversal(
            destination,
            50m,
            CompletedAt.AddHours(-1));

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            [oldest, newest],
            CompletedAt);

        // Assert
        results.Should().HaveCount(1);

        oldest.Reversal.Status
            .Should().Be(ReversalStatus.Pending);

        newest.Reversal.Status
            .Should().Be(ReversalStatus.Completed);

        destination.Balance.Amount
            .Should().Be(10m);

        oldest.OriginalSource.Balance.Amount
            .Should().Be(0m);

        newest.OriginalSource.Balance.Amount
            .Should().Be(50m);
    }

    [Test]
    public void Execute_WhenMiddleReversalCannotBeCompleted_ShouldContinueProcessingNext()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            100m);

        var first = CreatePendingReversal(
            destination,
            70m,
            CompletedAt.AddHours(-3));

        var second = CreatePendingReversal(
            destination,
            40m,
            CompletedAt.AddHours(-2));

        var third = CreatePendingReversal(
            destination,
            30m,
            CompletedAt.AddHours(-1));

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            [first, second, third],
            CompletedAt);

        // Assert
        results.Should().HaveCount(2);

        first.Reversal.Status
            .Should().Be(ReversalStatus.Completed);

        second.Reversal.Status
            .Should().Be(ReversalStatus.Pending);

        third.Reversal.Status
            .Should().Be(ReversalStatus.Completed);

        destination.Balance.Amount.Should().Be(0m);
    }

    [Test]
    public void Execute_WhenEarlierReversalConsumesAvailableBalance_ShouldKeepLaterReversalPending()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            100m);

        var first = CreatePendingReversal(
            destination,
            80m,
            CompletedAt.AddHours(-2));

        var second = CreatePendingReversal(
            destination,
            30m,
            CompletedAt.AddHours(-1));

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            [first, second],
            CompletedAt);

        // Assert
        results.Should().HaveCount(1);

        first.Reversal.Status
            .Should().Be(ReversalStatus.Completed);

        second.Reversal.Status
            .Should().Be(ReversalStatus.Pending);

        destination.Balance.Amount
            .Should().Be(20m);
    }

    [Test]
    public void Execute_WhenAllReversalsHaveInsufficientBalance_ShouldKeepAllPending()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            20m);

        var first = CreatePendingReversal(
            destination,
            100m,
            CompletedAt.AddHours(-2));

        var second = CreatePendingReversal(
            destination,
            50m,
            CompletedAt.AddHours(-1));

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            [first, second],
            CompletedAt);

        // Assert
        results.Should().BeEmpty();

        first.Reversal.Status
            .Should().Be(ReversalStatus.Pending);

        second.Reversal.Status
            .Should().Be(ReversalStatus.Pending);

        destination.Balance.Amount
            .Should().Be(20m);
    }

    [Test]
    public void Execute_WhenCompletedReversalIsProvided_ShouldIgnoreCompletedReversal()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            200m);

        var completed = CreatePendingReversal(
            destination,
            50m,
            CompletedAt.AddHours(-2));

        ExecuteTransferReversal.Execute(
            completed.Transfer,
            completed.Reversal,
            completed.OriginalSource,
            destination,
            CompletedAt.AddHours(-1));

        var balanceBeforeProcessing =
            destination.Balance.Amount;

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            [completed],
            CompletedAt);

        // Assert
        results.Should().BeEmpty();

        completed.Reversal.Status
            .Should().Be(ReversalStatus.Completed);

        destination.Balance.Amount
            .Should().Be(balanceBeforeProcessing);
    }

    [Test]
    public void Execute_WhenPendingAndCompletedReversalsExist_ShouldProcessOnlyPendingReversals()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            200m);

        var completed = CreatePendingReversal(
            destination,
            50m,
            CompletedAt.AddHours(-3));

        ExecuteTransferReversal.Execute(
            completed.Transfer,
            completed.Reversal,
            completed.OriginalSource,
            destination,
            CompletedAt.AddHours(-2));

        var pending = CreatePendingReversal(
            destination,
            40m,
            CompletedAt.AddHours(-1));

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            [completed, pending],
            CompletedAt);

        // Assert
        results.Should().HaveCount(1);

        results.Single().Reversal.Id
            .Should().Be(pending.Reversal.Id);

        pending.Reversal.Status
            .Should().Be(ReversalStatus.Completed);
    }

    [Test]
    public void Execute_WhenProcessingMultipleReversals_ShouldReturnOnlyCompletedReversals()
    {
        // Arrange
        var destination = CreateAccountWithBalance(
            "Alice",
            100m);

        var first = CreatePendingReversal(
            destination,
            70m,
            CompletedAt.AddHours(-3));

        var second = CreatePendingReversal(
            destination,
            50m,
            CompletedAt.AddHours(-2));

        var third = CreatePendingReversal(
            destination,
            30m,
            CompletedAt.AddHours(-1));

        // Act
        var results = PendingReversalProcessor.Execute(
            destination,
            [first, second, third],
            CompletedAt);

        // Assert
        results.Select(result => result.Reversal.Id)
            .Should()
            .BeEquivalentTo(
                [
                    first.Reversal.Id,
                    third.Reversal.Id
                ]);

        second.Reversal.Status
            .Should().Be(ReversalStatus.Pending);
    }

    [Test]
    public void Execute_WhenAccountIsNull_ShouldRejectProcessing()
    {
        // Act
        var act = () => PendingReversalProcessor.Execute(
            null!,
            [],
            CompletedAt);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("account");
    }

    [Test]
    public void Execute_WhenReversalsAreNull_ShouldRejectProcessing()
    {
        // Arrange
        var account = Account.Create("Alice");

        // Act
        var act = () => PendingReversalProcessor.Execute(
            account,
            null!,
            CompletedAt);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("reversals");
    }

    [Test]
    public void Execute_WhenCompletedAtIsDefault_ShouldRejectProcessing()
    {
        // Arrange
        var account = Account.Create("Alice");

        // Act
        var act = () => PendingReversalProcessor.Execute(
            account,
            [],
            default);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("completedAt");
    }

    private static PendingReversalItem CreatePendingReversal(
        Account destination,
        decimal amount,
        DateTimeOffset requestedAt)
    {
        var source = Account.Create(
            $"Source {Guid.NewGuid()}");

        var transfer = Transfer.Create(
            source.Id,
            destination.Id,
            Money.CreateBrl(amount),
            Guid.NewGuid(),
            TransferCreatedAt);

        var reversal = TransferReversal.Create(
            transfer.Id,
            requestedAt);

        return new PendingReversalItem(
            transfer,
            reversal,
            source);
    }

    private static Account CreateAccountWithBalance(
        string ownerName,
        decimal balance)
    {
        var account = Account.Create(ownerName);

        if (balance > 0)
            account.Credit(Money.CreateBrl(balance));

        return account;
    }
}