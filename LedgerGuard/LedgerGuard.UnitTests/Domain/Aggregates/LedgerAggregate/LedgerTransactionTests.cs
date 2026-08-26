using FluentAssertions;
using LedgerGuard.Domain.Aggregates.LedgerAggregate;
using LedgerGuard.Domain.ValueObjects;

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
            ELedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            ELedgerEntryType.Credit);

        // Act
        var transaction = LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
            [debit, credit]);

        // Assert
        transaction.Id.Should().NotBeEmpty();
        transaction.Type.Should().Be(ELedgerTransactionType.Transfer);
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
            ELedgerEntryType.Debit);

        var firstCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(60m),
            ELedgerEntryType.Credit);

        var secondCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(40m),
            ELedgerEntryType.Credit);

        // Act
        var transaction = LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
            [debit, firstCredit, secondCredit]);

        // Assert
        transaction.Entries.Should().HaveCount(3);
    }

    [Test]
    public void Create_WhenTransactionHasNoEntries_ShouldRejectLedgerTransaction()
    {
        // Act
        var act = () => LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
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
            ELedgerEntryType.Debit);

        // Act
        var act = () => LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
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
            ELedgerTransactionType.Transfer,
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
            ELedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(99.99m),
            ELedgerEntryType.Credit);

        // Act
        var act = () => LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
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
            ELedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100.01m),
            ELedgerEntryType.Credit);

        // Act
        var act = () => LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
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
            ELedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            ELedgerEntryType.Credit);

        // Act
        var act = () => LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
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
            ELedgerEntryType.Debit);

        var secondDebit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(50m),
            ELedgerEntryType.Debit);

        // Act
        var act = () => LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
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
            ELedgerEntryType.Credit);

        var secondCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(50m),
            ELedgerEntryType.Credit);

        // Act
        var act = () => LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
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
            ELedgerEntryType.Debit);

        var secondDebit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(40m),
            ELedgerEntryType.Debit);

        var firstCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(30m),
            ELedgerEntryType.Credit);

        var secondCredit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(70m),
            ELedgerEntryType.Credit);

        // Act
        var transaction = LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
            [firstDebit, secondDebit, firstCredit, secondCredit]);

        // Assert
        transaction.Entries.Should().HaveCount(4);
    }

    [TestCase(ELedgerTransactionType.Deposit)]
    [TestCase(ELedgerTransactionType.Transfer)]
    [TestCase(ELedgerTransactionType.Reversal)]
    public void Create_WhenTransactionTypeIsValid_ShouldCreateLedgerTransaction(
        ELedgerTransactionType type)
    {
        // Arrange
        var debit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            ELedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            ELedgerEntryType.Credit);

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
            ELedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            ELedgerEntryType.Credit);

        var undefinedType = (ELedgerTransactionType)999;

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
            ELedgerEntryType.Debit);

        var credit = LedgerEntry.Create(
            Guid.NewGuid(),
            Money.CreateBrl(100m),
            ELedgerEntryType.Credit);

        // Act
        var transaction = LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
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
                ELedgerEntryType.Debit),

            LedgerEntry.Create(
                Guid.NewGuid(),
                Money.CreateBrl(100m),
                ELedgerEntryType.Credit)
        };

        var transaction = LedgerTransaction.Create(
            ELedgerTransactionType.Transfer,
            entries);

        // Act
        entries.Add(
            LedgerEntry.Create(
                Guid.NewGuid(),
                Money.CreateBrl(50m),
                ELedgerEntryType.Credit));

        // Assert
        transaction.Entries.Should().HaveCount(2);
    }
}