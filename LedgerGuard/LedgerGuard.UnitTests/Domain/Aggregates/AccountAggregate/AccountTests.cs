using FluentAssertions;
using LedgerGuard.Domain.Aggregates.AccountAggregate;
using LedgerGuard.Domain.Enums;
using LedgerGuard.Domain.ValueObjects;

namespace LedgerGuard.UnitTests.Domain.Aggregates.AccountAggregate;

[TestFixture]
public sealed class Tests
{
    [Test]
    public void Create_WhenNewAccountIsCreated_ShouldStartAsActiveAndWithDefaultBalance()
    {
        var account = Account.Create("John Doe");
        
        account.OwnerName.Should().Be("John Doe");
        account.Balance.Amount.Should().Be(0);
        account.Status.Should().Be(EAccountStatus.Active);
        account.Id.Should().NotBeEmpty();
    }
    
    [Test]
    public void Block_WhenNewAccountIsCreated_ShouldBeAbleToBeBlocked()
    {
        var account = Account.Create("John Doe");
        
        account.Block();
        account.Status.Should().Be(EAccountStatus.Blocked);
    }

    [Test]
    public void Unblock_WhenAccountIsBlocked_ShouldBeAbleToBeUnblocked()
    {
        var account = Account.Create("John Doe");

        account.Block();
        account.Unblock();

        account.Status.Should().Be(EAccountStatus.Active);
    }
    
    [Test]
    public void Close_WhenNewAccountIsBlockedAndZeroBalance_ShouldBeAbleToBeClosed()
    {
        var account = Account.Create("John Doe");
        
        account.Block();
        account.Close();
        
        account.Status.Should().Be(EAccountStatus.Closed);
    }
    
    [Test]
    public void Close_WhenAccountIsActiveAndZeroBalance_ShouldBeAbleToBeClosed()
    {
        var account = Account.Create("John Doe");

        account.Close();

        account.Status.Should().Be(EAccountStatus.Closed);
    }
    
    [Test]
    public void Credit_WhenAccountIsActive_ShouldBeAbleToReceiveFunds()
    {
        var account = Account.Create("John Doe");

        account.Credit(Money.CreateBrl(100.56m));

        account.Balance.Amount.Should().Be(100.56m);
    }
    
    [Test]
    public void Credit_WhenAccountIsBlocked_ShouldBeAbleToReceiveFunds()
    {
        var account = Account.Create("John Doe");
        
        account.Block();
        account.Credit(Money.CreateBrl(233.90m));

        account.Balance.Amount.Should().Be(233.90m);
    }
    
    [Test]
    public void Create_WhenOwnerNameIsEmpty_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Account.Create(string.Empty);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("ownerName");
    }

    [Test]
    public void Create_WhenOwnerNameContainsOnlyWhitespace_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Account.Create("   ");

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("ownerName");
    }

    [Test]
    public void Create_WhenOwnerNameHasOneCharacter_ShouldCreateAccount()
    {
        // Act
        var account = Account.Create("A");

        // Assert
        account.OwnerName.Should().Be("A");
    }

    [Test]
    public void Create_WhenOwnerNameHasExactly120Characters_ShouldCreateAccount()
    {
        // Arrange
        var ownerName = new string('A', 120);

        // Act
        var account = Account.Create(ownerName);

        // Assert
        account.OwnerName.Should().Be(ownerName);
        account.OwnerName.Should().HaveLength(120);
    }

    [Test]
    public void Create_WhenOwnerNameExceeds120Characters_ShouldThrowArgumentException()
    {
        // Arrange
        var ownerName = new string('A', 121);

        // Act
        var act = () => Account.Create(ownerName);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("ownerName");
    }
    
    [Test]
    public void Create_WhenOwnerNameHasWhiteSpaces_ShouldCreateAccountWithTrimmedName()
    {
        // Act
        var account = Account.Create("        John Doe      ");

        // Assert
        account.OwnerName.Should().Be("John Doe");
    }

    [Test]
    public void Close_WhenAccountHasMinimumPositiveBalance_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(0.01m));

        // Act
        var act = () => account.Close();

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Only accounts with zero balance can be closed.");

        account.Status.Should().Be(EAccountStatus.Active);
        account.Balance.Amount.Should().Be(0.01m);
    }

    [Test]
    public void Unblock_WhenAccountIsClosed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Close();

        // Act
        var act = () => account.Unblock();

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Only blocked accounts can be unblocked.");

        account.Status.Should().Be(EAccountStatus.Closed);
    }

    [Test]
    public void Block_WhenAccountIsClosed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Close();

        // Act
        var act = () => account.Block();

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Only active accounts can be blocked.");

        account.Status.Should().Be(EAccountStatus.Closed);
    }

    [Test]
    public void Block_WhenAccountIsAlreadyBlocked_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Block();

        // Act
        var act = () => account.Block();

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Only active accounts can be blocked.");

        account.Status.Should().Be(EAccountStatus.Blocked);
    }

    [Test]
    public void Unblock_WhenAccountWasAlreadyUnblocked_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var account = Account.Create("John Doe");

        account.Block();
        account.Unblock();

        // Act
        var act = () => account.Unblock();

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Only blocked accounts can be unblocked.");

        account.Status.Should().Be(EAccountStatus.Active);
    }

    [Test]
    public void Close_WhenAccountIsAlreadyClosed_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Close();

        // Act
        var act = () => account.Close();

        // Assert
        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("Account is already closed.");

        account.Status.Should().Be(EAccountStatus.Closed);
    }

    [Test]
    public void Balance_WhenAccountIsCreated_ShouldNotHavePublicSetter()
    {
        // Arrange
        var balanceProperty = typeof(Account)
            .GetProperty(nameof(Account.Balance));

        // Assert
        balanceProperty.Should().NotBeNull();
        balanceProperty!.SetMethod.Should().NotBeNull();
        balanceProperty.SetMethod!.IsPublic.Should().BeFalse();
    }
    
    [Test]
    public void Debit_WhenAccountIsActive_ShouldDecreaseBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));

        // Act
        account.Debit(Money.CreateBrl(40m));

        // Assert
        account.Balance.Amount.Should().Be(60m);
    }

    [Test]
    public void Debit_WhenAmountIsMinimumValidValue_ShouldDecreaseBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(1m));

        // Act
        account.Debit(Money.CreateBrl(0.01m));

        // Assert
        account.Balance.Amount.Should().Be(0.99m);
    }

    [Test]
    public void Debit_WhenAmountIsOneCentBelowBalance_ShouldDecreaseBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));

        // Act
        account.Debit(Money.CreateBrl(99.99m));

        // Assert
        account.Balance.Amount.Should().Be(0.01m);
    }

    [Test]
    public void Debit_WhenAmountEqualsBalance_ShouldReduceBalanceToZero()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));

        // Act
        account.Debit(Money.CreateBrl(100m));

        // Assert
        account.Balance.Amount.Should().Be(0m);
    }
    
    [Test]
    public void Debit_WhenAmountExceedsBalanceByOneCent_ShouldRejectDebit()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));

        // Act
        var act = () =>
            account.Debit(Money.CreateBrl(100.01m));

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Debit_WhenAccountHasZeroBalance_ShouldRejectDebit()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var act = () =>
            account.Debit(Money.CreateBrl(0.01m));

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Debit_WhenAccountIsBlocked_ShouldRejectDebit()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));
        account.Block();

        // Act
        var act = () =>
            account.Debit(Money.CreateBrl(50m));

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Debit_WhenAccountIsClosed_ShouldRejectDebit()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Close();

        // Act
        var act = () =>
            account.Debit(Money.CreateBrl(0.01m));

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Debit_WhenAmountIsZero_ShouldRejectDebit()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));

        var amount = Money.Zero(ECurrency.Brl);

        // Act
        var act = () =>
            account.Debit(amount);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("amount");
    }

    [Test]
    public void Debit_WhenAmountIsNull_ShouldRejectDebit()
    {
        // Arrange
        var account = Account.Create("John Doe");

        // Act
        var act = () =>
            account.Debit(null!);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("amount");
    }
    
    [Test]
    public void Debit_WhenAccountHasInsufficientFunds_ShouldNotChangeBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));

        // Act
        var act = () =>
            account.Debit(Money.CreateBrl(100.01m));

        act.Should().Throw<InvalidOperationException>();

        // Assert
        account.Balance.Amount.Should().Be(100m);
    }

    [Test]
    public void Debit_WhenAccountIsBlocked_ShouldNotChangeBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));
        account.Block();

        // Act
        var act = () =>
            account.Debit(Money.CreateBrl(50m));

        act.Should().Throw<InvalidOperationException>();

        // Assert
        account.Balance.Amount.Should().Be(100m);
        account.Status.Should().Be(EAccountStatus.Blocked);
    }

    [Test]
    public void Debit_WhenAccountIsClosed_ShouldNotChangeBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Close();

        // Act
        var act = () =>
            account.Debit(Money.CreateBrl(10m));

        act.Should().Throw<InvalidOperationException>();

        // Assert
        account.Balance.Amount.Should().Be(0m);
        account.Status.Should().Be(EAccountStatus.Closed);
    }

    [Test]
    public void Debit_WhenDebitSucceeds_ShouldKeepAccountActive()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));

        // Act
        account.Debit(Money.CreateBrl(30m));

        // Assert
        account.Balance.Amount.Should().Be(70m);
        account.Status.Should().Be(EAccountStatus.Active);
    }
    
    
    [Test]
    public void DebitForReversal_WhenAccountIsActive_ShouldDecreaseBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));

        // Act
        account.DebitForReversal(Money.CreateBrl(40m));

        // Assert
        account.Balance.Amount.Should().Be(60m);
        account.Status.Should().Be(EAccountStatus.Active);
    }

    [Test]
    public void DebitForReversal_WhenAccountIsBlocked_ShouldDecreaseBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));
        account.Block();

        // Act
        account.DebitForReversal(Money.CreateBrl(40m));

        // Assert
        account.Balance.Amount.Should().Be(60m);
        account.Status.Should().Be(EAccountStatus.Blocked);
    }

    [Test]
    public void DebitForReversal_WhenAccountIsClosed_ShouldRejectDebit()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Close();

        // Act
        var act = () =>
            account.DebitForReversal(Money.CreateBrl(10m));

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();

        account.Balance.Amount.Should().Be(0m);
        account.Status.Should().Be(EAccountStatus.Closed);
    }

    [Test]
    public void DebitForReversal_WhenBalanceIsInsufficient_ShouldNotChangeBalance()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));

        // Act
        var act = () =>
            account.DebitForReversal(Money.CreateBrl(100.01m));

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();

        account.Balance.Amount.Should().Be(100m);
        account.Status.Should().Be(EAccountStatus.Active);
    }
    
    [Test]
    public void DebitForReversal_WhenAmountEqualsBalance_ShouldReduceBalanceToZero()
    {
        // Arrange
        var account = Account.Create("John Doe");
        account.Credit(Money.CreateBrl(100m));

        // Act
        account.DebitForReversal(Money.CreateBrl(100m));

        // Assert
        account.Balance.Amount.Should().Be(0m);
    }
}