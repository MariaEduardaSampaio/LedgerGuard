using FluentAssertions;
using LedgerGuard.Domain.Aggregates.MoneyAggregate;

namespace LedgerGuard.UnitTests.Domain.Aggregates.MoneyAggregate;

[TestFixture]
public sealed class MoneyTests
{
    [Test]
    public void Constructor_WhenValidParametersAreProvided_ShouldCreateValidBrlMoney()
    {
        // Arrange
        var amount = 100.00m;
        var currency = ECurrency.Brl;

        // Act
        var money = new Money(amount, currency);

        // Assert
        money.Amount.Should().Be(amount);
        money.Currency.Should().Be(currency);
    }
    
    [TestCase(100.123)]
    [TestCase(5.00001)]
    [TestCase(39.0687)]
    [TestCase(12.999)]
    [TestCase(10.4689)]
    public void Constructor_WhenAmountHasMoreThanTwoDecimalPlaces_ShouldThrowArgumentException(decimal invalidAmount)
    {
        // Arrange
        var currency = ECurrency.Brl;

        // Act
        Action act = () => new Money(invalidAmount, currency);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("Monetary values cannot have more than two decimal places.");
    }
    
    
    [TestCase(100.12)]
    [TestCase(5.00)]
    [TestCase(39.06)]
    [TestCase(12.99)]
    [TestCase(10.46)]
    public void Constructor_WhenAmountHasExactlyTwoDecimalPlaces_ShouldSetAmount(decimal validAmount)
    {
        // Arrange
        var currency = ECurrency.Brl;

        // Act
        var money = new Money(validAmount, currency);

        // Assert
        money.Amount.Should().Be(validAmount);
    }
    
    [TestCase(-100.72)]
    [TestCase(-56.80)]
    [TestCase(-39.81)]
    [TestCase(-24.99)]
    [TestCase(-0.01)]
    public void Constructor_WhenAmountIsNegative_ShouldThrowArgumentException(decimal invalidAmount)
    {
        // Arrange
        var currency = ECurrency.Brl;

        // Act
        Action act = () => new Money(invalidAmount, currency);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("Monetary values cannot be negative. (Parameter 'amount')");
    }
    
    [Test]
    public void CreateBrl_WhenAmountIsExactlyMaximumSupported_ShouldCreateMoneyEntity()
    {
        // Arrange & Act
        var money = Money.CreateBrl(Money.MaxAmount);
        
        // Assert
        money.Amount.Should().Be(Money.MaxAmount);
    }
    
    [Test]
    public void Constructor_WhenAmountIsAboveMaximum_ShouldBeRejected()
    {
        // Arrange
        var amount = Money.MaxAmount + 0.01m;

        // Act
        Action act = () => Money.CreateBrl(amount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage($"Amount cannot exceed {Money.MaxAmount}. (Parameter 'amount')");
    }
    
    [Test]
    public void Create_WhenCurrencyIsUndefined_ShouldRejectMoney()
    {
        // Arrange
        var invalidCurrency = (ECurrency)999;

        Action act = () => new Money(100m, invalidCurrency);

        // Act & Assert
        act.Should().Throw<ArgumentException>().WithMessage("Invalid currency. (Parameter 'currency')");
    }
    
    [Test]
    public void Add_WhenAddingDecimalAmounts_ShouldPreservePrecision()
    {
        // Arrange
        var first = new Money(0.10m, ECurrency.Brl);
        var second = new Money(0.20m, ECurrency.Brl);

        // Act
        var result = first.Add(second);

        // Assert
        (decimal.Round(result.Amount, 2) == result.Amount).Should().BeTrue();
    }

    [Test]
    public void Add_WhenAmountsAreValid_ShouldReturnSum()
    {
        var first = new Money(0.31m, ECurrency.Brl);
        var second = new Money(0.45m, ECurrency.Brl);

        // Act
        var result = first.Add(second);

        // Assert
        Assert.That(result.Amount, Is.EqualTo(0.76m));
    }

    [Test]
    public void Add_WhenResultExceedsMaximum_ShouldRejectOperation()
    {
        var first = new Money(Money.MaxAmount, ECurrency.Brl);
        var second = new Money(0.01m, ECurrency.Brl);

        // Act
        Action act = () => first.Add(second);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage($"Amount cannot exceed {Money.MaxAmount}. (Parameter 'amount')");
    }
    
    [Test]
    public void Subtract_WhenResultWouldBeNegative_ShouldRejectOperation()
    {
        // Arrange
        var balance = new Money(50m, ECurrency.Brl);
        var amount = new Money(100m, ECurrency.Brl);
        
        Action act = () => balance.Subtract(amount);
        
        // Act & Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("Monetary values cannot be negative. (Parameter 'amount')");
    }

    [Test]
    public void Subtract_WhenAmountsAreValid_ShouldReturnDifference()
    {
        // Arrange
        var balance = new Money(123.45m, ECurrency.Brl);
        var amount = new Money(67.89m, ECurrency.Brl);
        
        // Act
        var result = balance.Subtract(amount);
        
        // Assert
        result.Amount.Should().Be(55.56m);
    }
}