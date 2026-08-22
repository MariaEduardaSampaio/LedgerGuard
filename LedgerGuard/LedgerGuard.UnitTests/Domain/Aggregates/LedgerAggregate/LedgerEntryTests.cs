using FluentAssertions;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.MoneyAggregate;

namespace LedgerGuard.UnitTests.Domain.Aggregates.LedgerAggregate;

[TestFixture]
public sealed class LedgerEntryTests
{
    [Test]
    public void Create_WhenDebitEntryIsValid_ShouldCreateLedgerEntry()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = Money.CreateBrl(100m);

        // Act
        var entry = LedgerEntry.Create(
            accountId,
            amount,
            ELedgerEntryType.Debit);

        // Assert
        entry.AccountId.Should().Be(accountId);
        entry.Amount.Should().Be(amount);
        entry.Type.Should().Be(ELedgerEntryType.Debit);
    }

    [Test]
    public void Create_WhenCreditEntryIsValid_ShouldCreateLedgerEntry()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = Money.CreateBrl(100m);

        // Act
        var entry = LedgerEntry.Create(
            accountId,
            amount,
            ELedgerEntryType.Credit);

        // Assert
        entry.AccountId.Should().Be(accountId);
        entry.Amount.Should().Be(amount);
        entry.Type.Should().Be(ELedgerEntryType.Credit);
    }

    [Test]
    public void Create_WhenAccountIdIsEmpty_ShouldRejectLedgerEntry()
    {
        // Arrange
        var amount = Money.CreateBrl(100m);

        // Act
        var act = () => LedgerEntry.Create(
            Guid.Empty,
            amount,
            ELedgerEntryType.Credit);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("accountId");
    }

    [Test]
    public void Create_WhenAmountIsZero_ShouldRejectLedgerEntry()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = Money.Zero(ECurrency.Brl);

        // Act
        var act = () => LedgerEntry.Create(
            accountId,
            amount,
            ELedgerEntryType.Credit);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("amount");
    }

    [Test]
    public void Create_WhenEntryTypeIsUndefined_ShouldRejectLedgerEntry()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = Money.CreateBrl(100m);
        var undefinedType = (ELedgerEntryType)999;

        // Act
        var act = () => LedgerEntry.Create(
            accountId,
            amount,
            undefinedType);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("type");
    }

    [Test]
    public void Create_WhenAmountIsMinimumValidValue_ShouldCreateLedgerEntry()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = Money.CreateBrl(0.01m);

        // Act
        var entry = LedgerEntry.Create(
            accountId,
            amount,
            ELedgerEntryType.Credit);

        // Assert
        entry.Amount.Should().Be(amount);
        entry.Amount.Amount.Should().Be(0.01m);
    }

    [Test]
    public void Create_WhenAmountIsMaximumValidValue_ShouldCreateLedgerEntry()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = Money.CreateBrl(Money.MaxAmount);

        // Act
        var entry = LedgerEntry.Create(
            accountId,
            amount,
            ELedgerEntryType.Debit);

        // Assert
        entry.Amount.Should().Be(amount);
        entry.Amount.Amount.Should().Be(Money.MaxAmount);
    }

    [Test]
    public void LedgerEntry_WhenCreated_ShouldNotAllowPropertiesToBeModified()
    {
        // Arrange
        var accountIdProperty =
            typeof(LedgerEntry).GetProperty(nameof(LedgerEntry.AccountId));

        var amountProperty =
            typeof(LedgerEntry).GetProperty(nameof(LedgerEntry.Amount));

        var typeProperty =
            typeof(LedgerEntry).GetProperty(nameof(LedgerEntry.Type));

        // Assert
        accountIdProperty.Should().NotBeNull();
        amountProperty.Should().NotBeNull();
        typeProperty.Should().NotBeNull();

        accountIdProperty!.SetMethod?.IsPublic.Should().BeFalse();
        amountProperty!.SetMethod?.IsPublic.Should().BeFalse();
        typeProperty!.SetMethod?.IsPublic.Should().BeFalse();
    }
}