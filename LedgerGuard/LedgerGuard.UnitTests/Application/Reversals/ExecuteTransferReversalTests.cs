using FluentAssertions;
using LedgerGuard.Application.Reversals;
using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.MoneyAggregate;
using LedgerGuard.Domain.Aggregates.TransferAggregate;
using LedgerGuard.Domain.Aggregates.TransferReversalAggregate;

namespace LedgerGuard.UnitTests.Application.Reversals;

[TestFixture]
public sealed class ExecuteTransferReversalTests
{
    private static readonly DateTimeOffset TransferCreatedAt =
        new(2026, 8, 25, 10, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ReversalRequestedAt =
        new(2026, 8, 25, 11, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ReversalCompletedAt =
        new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    // --------------------------------------------------
    // HAPPY PATH
    // --------------------------------------------------

    [Test]
    public void Execute_WhenReversalIsValid_ShouldCompleteReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        // Act
        var result = ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        result.Reversal.Status.Should().Be(ReversalStatus.Completed);
        result.Reversal.CompletedAt.Should().Be(ReversalCompletedAt);
        result.Reversal.LedgerTransactionId
            .Should().Be(result.LedgerTransaction.Id);
    }

    [Test]
    public void Execute_WhenReversalSucceeds_ShouldRestoreOriginalSourceBalance()
    {
        // Arrange
        var scenario = CreateScenario(
            sourceBalance: 20m,
            destinationBalance: 100m);

        // Act
        ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        scenario.Source.Balance.Amount.Should().Be(120m);
    }

    [Test]
    public void Execute_WhenReversalSucceeds_ShouldDebitOriginalDestination()
    {
        // Arrange
        var scenario = CreateScenario(
            destinationBalance: 150m);

        // Act
        ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        scenario.Destination.Balance.Amount.Should().Be(50m);
    }

    [Test]
    public void Execute_WhenDestinationHasExactRequiredBalance_ShouldCompleteReversal()
    {
        // Arrange
        var scenario = CreateScenario(
            destinationBalance: 100m);

        // Act
        ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        scenario.Destination.Balance.Amount.Should().Be(0m);
        scenario.Source.Balance.Amount.Should().Be(100m);
        scenario.Reversal.Status.Should().Be(ReversalStatus.Completed);
    }

    [Test]
    public void Execute_WhenCompletedAtEqualsRequestedAt_ShouldCompleteReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        // Act
        ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalRequestedAt);

        // Assert
        scenario.Reversal.Status.Should().Be(ReversalStatus.Completed);
        scenario.Reversal.CompletedAt.Should().Be(ReversalRequestedAt);
    }

    // --------------------------------------------------
    // BLOCKED ACCOUNTS
    // --------------------------------------------------

    [Test]
    public void Execute_WhenOriginalDestinationIsBlocked_ShouldCompleteReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        scenario.Destination.Block();

        // Act
        ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        scenario.Destination.Balance.Amount.Should().Be(0m);
        scenario.Destination.Status.Should().Be(EAccountStatus.Blocked);

        scenario.Source.Balance.Amount.Should().Be(100m);

        scenario.Reversal.Status.Should().Be(ReversalStatus.Completed);
    }

    [Test]
    public void Execute_WhenOriginalSourceIsBlocked_ShouldCompleteReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        scenario.Source.Block();

        // Act
        ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        scenario.Source.Balance.Amount.Should().Be(100m);
        scenario.Source.Status.Should().Be(EAccountStatus.Blocked);

        scenario.Reversal.Status.Should().Be(ReversalStatus.Completed);
    }

    // --------------------------------------------------
    // LEDGER
    // --------------------------------------------------

    [Test]
    public void Execute_WhenReversalSucceeds_ShouldCreateReversalLedgerTransaction()
    {
        // Arrange
        var scenario = CreateScenario();

        // Act
        var result = ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        result.LedgerTransaction.Type
            .Should().Be(ELedgerTransactionType.Reversal);
    }

    [Test]
    public void Execute_WhenReversalSucceeds_ShouldCreateExactlyTwoLedgerEntries()
    {
        // Arrange
        var scenario = CreateScenario();

        // Act
        var result = ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        result.LedgerTransaction.Entries
            .Should().HaveCount(2);
    }

    [Test]
    public void Execute_WhenReversalSucceeds_ShouldCreateDestinationDebitEntry()
    {
        // Arrange
        var scenario = CreateScenario();

        // Act
        var result = ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        result.LedgerTransaction.Entries.Should().ContainSingle(entry =>
            entry.AccountId == scenario.Destination.Id &&
            entry.Type == ELedgerEntryType.Debit &&
            entry.Amount.Amount == 100m);
    }

    [Test]
    public void Execute_WhenReversalSucceeds_ShouldCreateSourceCreditEntry()
    {
        // Arrange
        var scenario = CreateScenario();

        // Act
        var result = ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        result.LedgerTransaction.Entries.Should().ContainSingle(entry =>
            entry.AccountId == scenario.Source.Id &&
            entry.Type == ELedgerEntryType.Credit &&
            entry.Amount.Amount == 100m);
    }

    [Test]
    public void Execute_WhenReversalSucceeds_ShouldCreateBalancedLedgerTransaction()
    {
        // Arrange
        var scenario = CreateScenario();

        // Act
        var result = ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        var debitTotal = result.LedgerTransaction.Entries
            .Where(entry => entry.Type == ELedgerEntryType.Debit)
            .Sum(entry => entry.Amount.Amount);

        var creditTotal = result.LedgerTransaction.Entries
            .Where(entry => entry.Type == ELedgerEntryType.Credit)
            .Sum(entry => entry.Amount.Amount);

        // Assert
        debitTotal.Should().Be(100m);
        creditTotal.Should().Be(100m);
        debitTotal.Should().Be(creditTotal);
    }

    [Test]
    public void Execute_WhenReversalSucceeds_ShouldPreserveCombinedAccountBalance()
    {
        // Arrange
        var scenario = CreateScenario(
            sourceBalance: 25m,
            destinationBalance: 150m);

        var combinedBefore =
            scenario.Source.Balance.Amount +
            scenario.Destination.Balance.Amount;

        // Act
        ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        var combinedAfter =
            scenario.Source.Balance.Amount +
            scenario.Destination.Balance.Amount;

        // Assert
        combinedAfter.Should().Be(combinedBefore);
    }

    // --------------------------------------------------
    // INSUFFICIENT FUNDS
    // --------------------------------------------------

    [Test]
    public void Execute_WhenDestinationIsOneCentShort_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario(
            destinationBalance: 99.99m);

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Execute_WhenDestinationHasNoBalance_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario(
            destinationBalance: 0m);

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Execute_WhenDestinationHasInsufficientFunds_ShouldNotChangeBalances()
    {
        // Arrange
        var scenario = CreateScenario(
            sourceBalance: 20m,
            destinationBalance: 99.99m);

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        act.Should().Throw<InvalidOperationException>();

        // Assert
        scenario.Source.Balance.Amount.Should().Be(20m);
        scenario.Destination.Balance.Amount.Should().Be(99.99m);
    }

    [Test]
    public void Execute_WhenDestinationHasInsufficientFunds_ShouldKeepReversalPending()
    {
        // Arrange
        var scenario = CreateScenario(
            destinationBalance: 99.99m);

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        act.Should().Throw<InvalidOperationException>();

        // Assert
        scenario.Reversal.Status.Should().Be(ReversalStatus.Pending);
        scenario.Reversal.CompletedAt.Should().BeNull();
        scenario.Reversal.LedgerTransactionId.Should().BeNull();
    }

    // --------------------------------------------------
    // CLOSED ACCOUNTS
    // --------------------------------------------------

    [Test]
    public void Execute_WhenOriginalSourceIsClosed_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        scenario.Source.Close();

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        act.Should().Throw<InvalidOperationException>();

        scenario.Destination.Balance.Amount.Should().Be(100m);
        scenario.Reversal.Status.Should().Be(ReversalStatus.Pending);
    }

    [Test]
    public void Execute_WhenOriginalDestinationIsClosed_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario(
            destinationBalance: 0m);

        scenario.Destination.Close();

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        act.Should().Throw<InvalidOperationException>();

        scenario.Source.Balance.Amount.Should().Be(0m);
        scenario.Destination.Balance.Amount.Should().Be(0m);
        scenario.Reversal.Status.Should().Be(ReversalStatus.Pending);
    }

    // --------------------------------------------------
    // SOURCE OVERFLOW
    // --------------------------------------------------

    [Test]
    public void Execute_WhenSourceBalanceWouldOverflow_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario(
            sourceBalance: Money.MaxAmount,
            destinationBalance: 100m);

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Execute_WhenSourceBalanceWouldOverflow_ShouldNotChangeBalances()
    {
        // Arrange
        var scenario = CreateScenario(
            sourceBalance: Money.MaxAmount,
            destinationBalance: 100m);

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        act.Should().Throw<ArgumentOutOfRangeException>();

        // Assert
        scenario.Source.Balance.Amount.Should().Be(Money.MaxAmount);
        scenario.Destination.Balance.Amount.Should().Be(100m);
        scenario.Reversal.Status.Should().Be(ReversalStatus.Pending);
    }

    // --------------------------------------------------
    // RELATIONSHIP VALIDATIONS
    // --------------------------------------------------

    [Test]
    public void Execute_WhenReversalBelongsToDifferentTransfer_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        var anotherReversal = TransferReversal.Create(
            Guid.NewGuid(),
            ReversalRequestedAt);

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            anotherReversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Execute_WhenSourceDoesNotMatchTransfer_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        var wrongSource = Account.Create("Wrong Source");

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            wrongSource,
            scenario.Destination,
            ReversalCompletedAt);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void Execute_WhenDestinationDoesNotMatchTransfer_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        var wrongDestination = Account.Create("Wrong Destination");

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            wrongDestination,
            ReversalCompletedAt);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    // --------------------------------------------------
    // REVERSAL STATE
    // --------------------------------------------------

    [Test]
    public void Execute_WhenReversalIsAlreadyCompleted_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        var sourceBalance = scenario.Source.Balance.Amount;
        var destinationBalance = scenario.Destination.Balance.Amount;

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt.AddMinutes(1));

        // Assert
        act.Should().Throw<InvalidOperationException>();

        scenario.Source.Balance.Amount.Should().Be(sourceBalance);
        scenario.Destination.Balance.Amount.Should().Be(destinationBalance);
    }

    // --------------------------------------------------
    // DATE VALIDATION
    // --------------------------------------------------

    [Test]
    public void Execute_WhenCompletedAtIsDefault_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            default);

        // Assert
        act.Should().Throw<ArgumentException>();

        scenario.Source.Balance.Amount.Should().Be(0m);
        scenario.Destination.Balance.Amount.Should().Be(100m);
        scenario.Reversal.Status.Should().Be(ReversalStatus.Pending);
    }

    [Test]
    public void Execute_WhenCompletedAtIsBeforeRequestedAt_ShouldRejectReversal()
    {
        // Arrange
        var scenario = CreateScenario();

        var completedAt =
            ReversalRequestedAt.AddTicks(-1);

        // Act
        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            completedAt);

        // Assert
        act.Should().Throw<ArgumentException>();

        scenario.Source.Balance.Amount.Should().Be(0m);
        scenario.Destination.Balance.Amount.Should().Be(100m);
        scenario.Reversal.Status.Should().Be(ReversalStatus.Pending);
    }

    // --------------------------------------------------
    // NULLS
    // --------------------------------------------------

    [Test]
    public void Execute_WhenTransferIsNull_ShouldRejectReversal()
    {
        var scenario = CreateScenario();

        var act = () => ExecuteTransferReversal.Execute(
            null!,
            scenario.Reversal,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("transfer");
    }

    [Test]
    public void Execute_WhenReversalIsNull_ShouldRejectReversal()
    {
        var scenario = CreateScenario();

        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            null!,
            scenario.Source,
            scenario.Destination,
            ReversalCompletedAt);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("reversal");
    }

    [Test]
    public void Execute_WhenSourceIsNull_ShouldRejectReversal()
    {
        var scenario = CreateScenario();

        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            null!,
            scenario.Destination,
            ReversalCompletedAt);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("source");
    }

    [Test]
    public void Execute_WhenDestinationIsNull_ShouldRejectReversal()
    {
        var scenario = CreateScenario();

        var act = () => ExecuteTransferReversal.Execute(
            scenario.Transfer,
            scenario.Reversal,
            scenario.Source,
            null!,
            ReversalCompletedAt);

        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("destination");
    }

    // --------------------------------------------------
    // HELPERS
    // --------------------------------------------------

    private static ReversalScenario CreateScenario(
        decimal sourceBalance = 0m,
        decimal destinationBalance = 100m,
        decimal transferAmount = 100m)
    {
        var source = Account.Create("Maria");
        var destination = Account.Create("Alice");

        if (sourceBalance > 0)
            source.Credit(Money.CreateBrl(sourceBalance));

        if (destinationBalance > 0)
            destination.Credit(Money.CreateBrl(destinationBalance));

        var transfer = Transfer.Create(
            source.Id,
            destination.Id,
            Money.CreateBrl(transferAmount),
            Guid.NewGuid(),
            TransferCreatedAt);

        var reversal = TransferReversal.Create(
            transfer.Id,
            ReversalRequestedAt);

        return new ReversalScenario(
            source,
            destination,
            transfer,
            reversal);
    }

    private sealed record ReversalScenario(
        Account Source,
        Account Destination,
        Transfer Transfer,
        TransferReversal Reversal);
}