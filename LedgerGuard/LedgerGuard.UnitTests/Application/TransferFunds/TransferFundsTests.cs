using FluentAssertions;
using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.MoneyAggregate;

namespace LedgerGuard.UnitTests.Application.TransferFunds;

[TestFixture]
public sealed class TransferFundsTests
{
    private static readonly DateTimeOffset ValidCreatedAt =
        new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);

    private static Account CreateAccountWithBalance(
        string ownerName,
        decimal balance)
    {
        var account = Account.Create(ownerName);

        if (balance > 0)
            account.Credit(Money.CreateBrl(balance));

        return account;
    }

    [Test]
    public void Execute_WhenAmountIsMinimumValidValue_ShouldTransferFunds()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 1m);
        var destination = Account.Create("Alice");

        // Act
        LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            0.01m,
            ValidCreatedAt);

        // Assert
        source.Balance.Amount.Should().Be(0.99m);
        destination.Balance.Amount.Should().Be(0.01m);
    }

    [Test]
    public void Execute_WhenAmountIsPartialBalance_ShouldTransferFunds()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            40m,
            ValidCreatedAt);

        // Assert
        source.Balance.Amount.Should().Be(60m);
        destination.Balance.Amount.Should().Be(40m);
    }

    [Test]
    public void Execute_WhenAmountEqualsFullBalance_ShouldTransferFunds()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            100m,
            ValidCreatedAt);

        // Assert
        source.Balance.Amount.Should().Be(0m);
        destination.Balance.Amount.Should().Be(100m);
    }

    [Test]
    public void Execute_WhenAmountIsOneCentBelowFullBalance_ShouldTransferFunds()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            99.99m,
            ValidCreatedAt);

        // Assert
        source.Balance.Amount.Should().Be(0.01m);
        destination.Balance.Amount.Should().Be(99.99m);
    }

    [Test]
    public void Execute_WhenDestinationIsBlocked_ShouldTransferFunds()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);

        var destination = Account.Create("Alice");
        destination.Block();

        // Act
        LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            50m,
            ValidCreatedAt);

        // Assert
        source.Balance.Amount.Should().Be(50m);
        destination.Balance.Amount.Should().Be(50m);
        destination.Status.Should().Be(EAccountStatus.Blocked);
    }

    [Test]
    public void Execute_WhenDestinationReachesMaximumBalanceExactly_ShouldTransferFunds()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 1m);

        var destination = CreateAccountWithBalance(
            "Alice",
            Money.MaxAmount - 0.01m);

        // Act
        LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            0.01m,
            ValidCreatedAt);

        // Assert
        source.Balance.Amount.Should().Be(0.99m);
        destination.Balance.Amount.Should().Be(Money.MaxAmount);
    }

    [Test]
    public void Execute_WhenAmountIsMaximumValidValue_ShouldTransferFunds()
    {
        // Arrange
        var source = CreateAccountWithBalance(
            "Maria",
            Money.MaxAmount);

        var destination = Account.Create("Alice");

        // Act
        LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            Money.MaxAmount,
            ValidCreatedAt);

        // Assert
        source.Balance.Amount.Should().Be(0m);
        destination.Balance.Amount.Should().Be(Money.MaxAmount);
    }

    [Test]
    public void Execute_WhenTransferSucceeds_ShouldDebitSourceByExactAmount()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 150m);
        var destination = Account.Create("Alice");

        // Act
        LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            37.56m,
            ValidCreatedAt);

        // Assert
        source.Balance.Amount.Should().Be(112.44m);
    }

    [Test]
    public void Execute_WhenTransferSucceeds_ShouldCreditDestinationByExactAmount()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 150m);
        var destination = CreateAccountWithBalance("Alice", 20m);

        // Act
        LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            37.56m,
            ValidCreatedAt);

        // Assert
        destination.Balance.Amount.Should().Be(57.56m);
    }

    [Test]
    public void Execute_WhenTransferSucceeds_ShouldPreserveCombinedBalance()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 150m);
        var destination = CreateAccountWithBalance("Alice", 50m);

        var combinedBalanceBefore =
            source.Balance.Amount +
            destination.Balance.Amount;

        // Act
        LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            70m,
            ValidCreatedAt);

        var combinedBalanceAfter =
            source.Balance.Amount +
            destination.Balance.Amount;

        // Assert
        combinedBalanceAfter.Should().Be(combinedBalanceBefore);
        combinedBalanceAfter.Should().Be(200m);
    }

    [Test]
    public void Execute_WhenTransferSucceeds_ShouldCreateTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var result = LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            25m,
            ValidCreatedAt);

        // Assert
        result.Transfer.Id.Should().NotBeEmpty();

        result.Transfer.SourceAccountId
            .Should().Be(source.Id);

        result.Transfer.DestinationAccountId
            .Should().Be(destination.Id);

        result.Transfer.Amount.Amount
            .Should().Be(25m);

        result.Transfer.CreatedAt
            .Should().Be(ValidCreatedAt);
    }

    [Test]
    public void Execute_WhenTransferSucceeds_ShouldReferenceCreatedLedgerTransaction()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var result = LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            25m,
            ValidCreatedAt);

        // Assert
        result.Transfer.LedgerTransactionId
            .Should().Be(result.LedgerTransaction.Id);
    }

    [Test]
    public void Execute_WhenTransferSucceeds_ShouldCreateTransferLedgerTransaction()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var result = LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            30m,
            ValidCreatedAt);

        // Assert
        result.LedgerTransaction.Type
            .Should().Be(ELedgerTransactionType.Transfer);
    }

    [Test]
    public void Execute_WhenTransferSucceeds_ShouldCreateExactlyTwoLedgerEntries()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var result = LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            30m,
            ValidCreatedAt);

        // Assert
        result.LedgerTransaction.Entries
            .Should().HaveCount(2);
    }

    [Test]
    public void Execute_WhenTransferSucceeds_ShouldCreateSourceDebitEntry()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var result = LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            30m,
            ValidCreatedAt);

        // Assert
        result.LedgerTransaction.Entries.Should().ContainSingle(entry =>
            entry.AccountId == source.Id &&
            entry.Type == ELedgerEntryType.Debit &&
            entry.Amount.Amount == 30m);
    }

    [Test]
    public void Execute_WhenTransferSucceeds_ShouldCreateDestinationCreditEntry()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var result = LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            30m,
            ValidCreatedAt);

        // Assert
        result.LedgerTransaction.Entries.Should().ContainSingle(entry =>
            entry.AccountId == destination.Id &&
            entry.Type == ELedgerEntryType.Credit &&
            entry.Amount.Amount == 30m);
    }

    [Test]
    public void Execute_WhenTransferSucceeds_ShouldCreateBalancedLedgerTransaction()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var result = LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            30m,
            ValidCreatedAt);

        var debitTotal = result.LedgerTransaction.Entries
            .Where(entry => entry.Type == ELedgerEntryType.Debit)
            .Sum(entry => entry.Amount.Amount);

        var creditTotal = result.LedgerTransaction.Entries
            .Where(entry => entry.Type == ELedgerEntryType.Credit)
            .Sum(entry => entry.Amount.Amount);

        // Assert
        debitTotal.Should().Be(30m);
        creditTotal.Should().Be(30m);
        debitTotal.Should().Be(creditTotal);
    }

    [Test]
    public void Execute_WhenAmountIsZero_ShouldRejectTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            0m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Execute_WhenAmountIsNegative_ShouldRejectTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            -0.01m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Execute_WhenAmountHasMoreThanTwoDecimalPlaces_ShouldRejectTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            10.001m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Test]
    public void Execute_WhenAmountExceedsMaximum_ShouldRejectTransfer()
    {
        // Arrange
        var source = Account.Create("Maria");
        var destination = Account.Create("Alice");

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            Money.MaxAmount + 0.01m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Execute_WhenSourceIsBlocked_ShouldRejectTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        source.Block();

        var destination = Account.Create("Alice");

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            50m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Execute_WhenSourceIsClosed_ShouldRejectTransfer()
    {
        // Arrange
        var source = Account.Create("Maria");
        source.Close();

        var destination = Account.Create("Alice");

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            0.01m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Execute_WhenSourceHasInsufficientFunds_ShouldRejectTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            150m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Execute_WhenAmountExceedsSourceBalanceByOneCent_ShouldRejectTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            100.01m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Execute_WhenDestinationIsClosed_ShouldRejectTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);

        var destination = Account.Create("Alice");
        destination.Close();

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            50m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Execute_WhenDestinationWouldExceedMaximumByOneCent_ShouldRejectTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);

        var destination = CreateAccountWithBalance(
            "Alice",
            Money.MaxAmount);

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            0.01m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public void Execute_WhenSourceAndDestinationAreTheSame_ShouldRejectTransfer()
    {
        // Arrange
        var account = CreateAccountWithBalance("Maria", 100m);

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            account,
            account,
            50m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();

        account.Balance.Amount.Should().Be(100m);
    }

    [Test]
    public void Execute_WhenSourceIsNull_ShouldRejectTransfer()
    {
        // Arrange
        var destination = Account.Create("Alice");

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            null!,
            destination,
            10m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("source");
    }

    [Test]
    public void Execute_WhenDestinationIsNull_ShouldRejectTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            null!,
            10m,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("destination");
    }

    [Test]
    public void Execute_WhenCreatedAtIsDefault_ShouldRejectTransfer()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = Account.Create("Alice");

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            50m,
            default);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Test]
    public void Execute_WhenSourceHasInsufficientFunds_ShouldNotChangeEitherBalance()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = CreateAccountWithBalance("Alice", 50m);

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            100.01m,
            ValidCreatedAt);

        act.Should().Throw<InvalidOperationException>();

        // Assert
        source.Balance.Amount.Should().Be(100m);
        destination.Balance.Amount.Should().Be(50m);
    }

    [Test]
    public void Execute_WhenSourceIsBlocked_ShouldNotChangeEitherBalance()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        source.Block();

        var destination = CreateAccountWithBalance("Alice", 20m);

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            50m,
            ValidCreatedAt);

        act.Should().Throw<InvalidOperationException>();

        // Assert
        source.Balance.Amount.Should().Be(100m);
        destination.Balance.Amount.Should().Be(20m);
        source.Status.Should().Be(EAccountStatus.Blocked);
    }

    [Test]
    public void Execute_WhenDestinationIsClosed_ShouldNotChangeEitherBalance()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);

        var destination = Account.Create("Alice");
        destination.Close();

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            50m,
            ValidCreatedAt);

        act.Should().Throw<InvalidOperationException>();

        // Assert
        source.Balance.Amount.Should().Be(100m);
        destination.Balance.Amount.Should().Be(0m);
        destination.Status.Should().Be(EAccountStatus.Closed);
    }

    [Test]
    public void Execute_WhenDestinationWouldOverflow_ShouldNotChangeEitherBalance()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);

        var destination = CreateAccountWithBalance(
            "Alice",
            Money.MaxAmount);

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            0.01m,
            ValidCreatedAt);

        act.Should().Throw<ArgumentOutOfRangeException>();

        // Assert
        source.Balance.Amount.Should().Be(100m);
        destination.Balance.Amount.Should().Be(Money.MaxAmount);
    }

    [Test]
    public void Execute_WhenCreatedAtIsInvalid_ShouldNotChangeEitherBalance()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = CreateAccountWithBalance("Alice", 20m);

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            50m,
            default);

        act.Should().Throw<ArgumentException>();

        // Assert
        source.Balance.Amount.Should().Be(100m);
        destination.Balance.Amount.Should().Be(20m);
    }

    [Test]
    public void Execute_WhenAmountIsInvalid_ShouldNotChangeEitherBalance()
    {
        // Arrange
        var source = CreateAccountWithBalance("Maria", 100m);
        var destination = CreateAccountWithBalance("Alice", 20m);

        // Act
        var act = () => LedgerGuard.Application.Transfers.TransferFunds.Execute(
            source,
            destination,
            -10m,
            ValidCreatedAt);

        act.Should().Throw<ArgumentOutOfRangeException>();

        // Assert
        source.Balance.Amount.Should().Be(100m);
        destination.Balance.Amount.Should().Be(20m);
    }
}