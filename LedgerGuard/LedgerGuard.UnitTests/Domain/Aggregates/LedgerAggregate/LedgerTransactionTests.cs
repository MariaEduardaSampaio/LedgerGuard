using FluentAssertions;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.Aggregates.MoneyAggregate;

namespace LedgerGuard.UnitTests.Domain.Aggregates.LedgerAggregate;

[TestFixture]
public sealed class LedgerTransactionTests
{
    [Test]
    public void Create_WhenTransactionHasExactlyTwoBalancedEntries_ShouldCreateLedgerTransaction()
    {
        // Arrange
        var debit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Credit);

        // Act
        var transaction = LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            [debit, credit]);

        // Assert
        transaction.Id.Should().NotBeEmpty();
        transaction.Type.Should().Be(LedgerTransactionType.Transfer);
        transaction.Entries.Should().HaveCount(2);
        transaction.Entries.Should().Contain(debit);
        transaction.Entries.Should().Contain(credit);
    }

    [Test]
    public void Create_WhenTransactionHasMoreThanTwoBalancedEntries_ShouldCreateLedgerTransaction()
    {
        // Arrange
        var debit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Debit);

        var firstCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(60m),
            LedgerEntryType.Credit);

        var secondCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(40m),
            LedgerEntryType.Credit);

        // Act
        var transaction = LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            [debit, firstCredit, secondCredit]);

        // Assert
        transaction.Entries.Should().HaveCount(3);
    }

    [Test]
    public void Create_WhenTransactionHasNoEntries_ShouldRejectLedgerTransaction()
    {
        // Act
        var act = () => LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            []);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Create_WhenTransactionHasOnlyOneEntry_ShouldRejectLedgerTransaction()
    {
        // Arrange
        var entry = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Debit);

        // Act
        var act = () => LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            [entry]);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Create_WhenEntriesAreNull_ShouldRejectLedgerTransaction()
    {
        // Act
        var act = () => LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            null!);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("source");
    }

    [Test]
    public void Create_WhenDebitAndCreditTotalsAreDifferent_ShouldRejectLedgerTransaction()
    {
        // Arrange
        var debit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(99.99m),
            LedgerEntryType.Credit);

        // Act
        var act = () => LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            [debit, credit]);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Create_WhenCreditExceedsDebitByOneCent_ShouldRejectLedgerTransaction()
    {
        // Arrange
        var debit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100.01m),
            LedgerEntryType.Credit);

        // Act
        var act = () => LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            [debit, credit]);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Create_WhenDebitExceedsCreditByOneCent_ShouldRejectLedgerTransaction()
    {
        // Arrange
        var debit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100.01m),
            LedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Credit);

        // Act
        var act = () => LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            [debit, credit]);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Create_WhenAllEntriesAreDebit_ShouldRejectLedgerTransaction()
    {
        // Arrange
        var firstDebit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(50m),
            LedgerEntryType.Debit);

        var secondDebit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(50m),
            LedgerEntryType.Debit);

        // Act
        var act = () => LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            [firstDebit, secondDebit]);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Create_WhenAllEntriesAreCredit_ShouldRejectLedgerTransaction()
    {
        // Arrange
        var firstCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(50m),
            LedgerEntryType.Credit);

        var secondCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(50m),
            LedgerEntryType.Credit);

        // Act
        var act = () => LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            [firstCredit, secondCredit]);

        // Assert
        act.Should()
            .Throw<InvalidOperationException>();
    }

    [Test]
    public void Create_WhenMultipleDebitsAndCreditsAreBalanced_ShouldCreateLedgerTransaction()
    {
        // Arrange
        var firstDebit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(60m),
            LedgerEntryType.Debit);

        var secondDebit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(40m),
            LedgerEntryType.Debit);

        var firstCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(30m),
            LedgerEntryType.Credit);

        var secondCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(70m),
            LedgerEntryType.Credit);

        // Act
        var transaction = LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            [firstDebit, secondDebit, firstCredit, secondCredit]);

        // Assert
        transaction.Entries.Should().HaveCount(4);
    }

    [TestCase(LedgerTransactionType.Deposit)]
    [TestCase(LedgerTransactionType.Transfer)]
    [TestCase(LedgerTransactionType.Reversal)]
    public void Create_WhenTransactionTypeIsValid_ShouldCreateLedgerTransaction(
        LedgerTransactionType type)
    {
        // Arrange
        var debit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Credit);

        // Act
        var transaction = LedgerTransaction.Create(
            type,
            [debit, credit]);

        // Assert
        transaction.Type.Should().Be(type);
    }

    [Test]
    public void Create_WhenTransactionTypeIsUndefined_ShouldRejectLedgerTransaction()
    {
        // Arrange
        var debit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Credit);

        var undefinedType = (LedgerTransactionType)999;

        // Act
        var act = () => LedgerTransaction.Create(
            undefinedType,
            [debit, credit]);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("type");
    }

    [Test]
    public void Create_WhenTransactionIsCreated_ShouldGenerateNonEmptyId()
    {
        // Arrange
        var debit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            LedgerEntryType.Credit);

        // Act
        var transaction = LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            [debit, credit]);

        // Assert
        transaction.Id.Should().NotBeEmpty();
    }

    [Test]
    public void Create_WhenSourceEntryCollectionIsModified_ShouldNotModifyLedgerTransaction()
    {
        // Arrange
        var entries = new List<LedgerEntry>
        {
            LedgerEntry.Create(
                Guid.NewGuid(),
                Money.CreateBrl(100m),
                LedgerEntryType.Debit),

            LedgerEntry.Create(
                Guid.NewGuid(),
                Money.CreateBrl(100m),
                LedgerEntryType.Credit)
        };

        var transaction = LedgerTransaction.Create(
            LedgerTransactionType.Transfer,
            entries);

        // Act
        entries.Add(
            LedgerEntry.Create(
                Guid.NewGuid(),
                Money.CreateBrl(50m),
                LedgerEntryType.Credit));

        // Assert
        transaction.Entries.Should().HaveCount(2);
    }
}