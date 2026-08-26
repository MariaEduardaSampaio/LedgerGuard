using FluentAssertions;
using LedgerGuard.Application.Deposits;
using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.ValueObjects;

namespace LedgerGuard.UnitTests.Application.Deposits;

[TestFixture]
public sealed class DepositFundsTests
{
    private Guid _settlementAccountId;

    [SetUp]
    public void SetUp()
    {
        _settlementAccountId = Guid.NewGuid();
    }

    [Test]
    public void Execute_WhenAmountIsMinimumValidValue_ShouldDepositFunds()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var transaction = DepositFunds.Execute(
            account,
            0.01m,
            _settlementAccountId);

        // Assert
        account.Balance.Amount.Should().Be(0.01m);
        transaction.Should().NotBeNull();
    }

    [Test]
    public void Execute_WhenAccountIsActive_ShouldIncreaseBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        DepositFunds.Execute(
            account,
            100m,
            _settlementAccountId);

        // Assert
        account.Balance.Amount.Should().Be(100m);
    }

    [Test]
    public void Execute_WhenAccountAlreadyHasBalance_ShouldAddDepositToCurrentBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(40m));

        // Act
        DepositFunds.Execute(
            account,
            60m,
            _settlementAccountId);

        // Assert
        account.Balance.Amount.Should().Be(100m);
    }

    [Test]
    public void Execute_WhenAccountIsBlocked_ShouldIncreaseBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Block();

        // Act
        DepositFunds.Execute(
            account,
            100m,
            _settlementAccountId);

        // Assert
        account.Balance.Amount.Should().Be(100m);
        account.Status.Should().Be(EAccountStatus.Blocked);
    }

    [Test]
    public void Execute_WhenAccountIsClosed_ShouldRejectDeposit()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Close();

        // Act
        var act = () => DepositFunds.Execute(
            account,
            100m,
            _settlementAccountId);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();

        account.Balance.Amount.Should().Be(0m);
        account.Status.Should().Be(EAccountStatus.Closed);
    }

    [Test]
    public void Execute_WhenAmountIsZero_ShouldRejectDeposit()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var act = () => DepositFunds.Execute(
            account,
            0m,
            _settlementAccountId);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();

        account.Balance.Amount.Should().Be(0m);
    }

    [Test]
    public void Execute_WhenAmountIsNegative_ShouldRejectDeposit()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var act = () => DepositFunds.Execute(
            account,
            -0.01m,
            _settlementAccountId);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();

        account.Balance.Amount.Should().Be(0m);
    }

    [Test]
    public void Execute_WhenAmountHasMoreThanTwoDecimalPlaces_ShouldRejectDeposit()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var act = () => DepositFunds.Execute(
            account,
            10.001m,
            _settlementAccountId);

        // Assert
        act.Should().Throw<ArgumentException>();

        account.Balance.Amount.Should().Be(0m);
    }

    [Test]
    public void Execute_WhenDepositReachesMaximumBalanceExactly_ShouldDepositFunds()
    {
        // Arrange
        var account = Account.Create("John Doe");

        account.Credit(
            Money.CreateBrl(Money.MaxAmount - 0.01m));

        // Act
        DepositFunds.Execute(
            account,
            0.01m,
            _settlementAccountId);

        // Assert
        account.Balance.Amount.Should().Be(Money.MaxAmount);
    }

    [Test]
    public void Execute_WhenDepositExceedsMaximumBalanceByOneCent_ShouldRejectDeposit()
    {
        // Arrange
        var account = Account.Create("John Doe");

        account.Credit(
            Money.CreateBrl(Money.MaxAmount));

        // Act
        var act = () => DepositFunds.Execute(
            account,
            0.01m,
            _settlementAccountId);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();

        account.Balance.Amount.Should().Be(Money.MaxAmount);
    }

    [Test]
    public void Execute_WhenDepositWouldOverflowBalance_ShouldNotChangeAccountBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");

        var initialBalance = Money.MaxAmount - 0.50m;

        account.Credit(
            Money.CreateBrl(initialBalance));

        // Act
        var act = () => DepositFunds.Execute(
            account,
            1m,
            _settlementAccountId);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();

        account.Balance.Amount.Should().Be(initialBalance);
    }

    [Test]
    public void Execute_WhenDepositSucceeds_ShouldCreateSettlementDebitEntry()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var transaction = DepositFunds.Execute(
            account,
            100m,
            _settlementAccountId);

        // Assert
        transaction.Entries.Should().ContainSingle(entry =>
            entry.AccountId == _settlementAccountId &&
            entry.Type == ELedgerEntryType.Debit &&
            entry.Amount.Amount == 100m);
    }

    [Test]
    public void Execute_WhenDepositSucceeds_ShouldCreateCustomerCreditEntry()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var transaction = DepositFunds.Execute(
            account,
            100m,
            _settlementAccountId);

        // Assert
        transaction.Entries.Should().ContainSingle(entry =>
            entry.AccountId == account.Id &&
            entry.Type == ELedgerEntryType.Credit &&
            entry.Amount.Amount == 100m);
    }

    [Test]
    public void Execute_WhenDepositSucceeds_ShouldCreateExactlyTwoLedgerEntries()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var transaction = DepositFunds.Execute(
            account,
            100m,
            _settlementAccountId);

        // Assert
        transaction.Entries.Should().HaveCount(2);
    }

    [Test]
    public void Execute_WhenDepositSucceeds_ShouldCreateBalancedLedgerTransaction()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var transaction = DepositFunds.Execute(
            account,
            100m,
            _settlementAccountId);

        // Assert
        var debitTotal = transaction.Entries
            .Where(entry => entry.Type == ELedgerEntryType.Debit)
            .Sum(entry => entry.Amount.Amount);

        var creditTotal = transaction.Entries
            .Where(entry => entry.Type == ELedgerEntryType.Credit)
            .Sum(entry => entry.Amount.Amount);

        debitTotal.Should().Be(creditTotal);
        debitTotal.Should().Be(100m);
    }

    [Test]
    public void Execute_WhenDepositSucceeds_ShouldCreateDepositLedgerTransaction()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var transaction = DepositFunds.Execute(
            account,
            100m,
            _settlementAccountId);

        // Assert
        transaction.Type.Should().Be(
            ELedgerTransactionType.Deposit);
    }

    [Test]
    public void Execute_WhenAccountIsNull_ShouldRejectDeposit()
    {
        // Act
        var act = () => DepositFunds.Execute(
            null!,
            100m,
            _settlementAccountId);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("account");
    }

    [Test]
    public void Execute_WhenSettlementAccountIdIsEmpty_ShouldRejectDeposit()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var act = () => DepositFunds.Execute(
            account,
            100m,
            Guid.Empty);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("settlementAccountId");

        account.Balance.Amount.Should().Be(0m);
    }
}