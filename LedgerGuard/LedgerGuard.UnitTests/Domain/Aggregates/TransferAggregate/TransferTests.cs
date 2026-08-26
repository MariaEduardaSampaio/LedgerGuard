using FluentAssertions;
using LedgerGuard.Domain.Aggregates.TransferAggregate;
using LedgerGuard.Domain.Enums;
using LedgerGuard.Domain.ValueObjects;

namespace LedgerGuard.UnitTests.Domain.Aggregates.TransferAggregate;

[TestFixture]
public sealed class TransferTests
{
    private static readonly DateTimeOffset ValidCreatedAt =
        new(2026, 8, 22, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void Create_WhenDataIsValid_ShouldCreateTransfer()
    {
        // Arrange
        var sourceAccountId = Guid.NewGuid();
        var destinationAccountId = Guid.NewGuid();
        var amount = Money.CreateBrl(100m);
        var ledgerTransactionId = Guid.NewGuid();

        // Act
        var transfer = Transfer.Create(
            sourceAccountId,
            destinationAccountId,
            amount,
            ledgerTransactionId,
            ValidCreatedAt);

        // Assert
        transfer.Id.Should().NotBeEmpty();
        transfer.SourceAccountId.Should().Be(sourceAccountId);
        transfer.DestinationAccountId.Should().Be(destinationAccountId);
        transfer.Amount.Should().Be(amount);
        transfer.LedgerTransactionId.Should().Be(ledgerTransactionId);
        transfer.CreatedAt.Should().Be(ValidCreatedAt);
    }

    [Test]
    public void Create_WhenAmountIsMinimumValidValue_ShouldCreateTransfer()
    {
        // Arrange
        var amount = Money.CreateBrl(0.01m);

        // Act
        var transfer = Transfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            Guid.NewGuid(),
            ValidCreatedAt);

        // Assert
        transfer.Amount.Amount.Should().Be(0.01m);
    }

    [Test]
    public void Create_WhenAmountIsMaximumValidValue_ShouldCreateTransfer()
    {
        // Arrange
        var amount = Money.CreateBrl(Money.MaxAmount);

        // Act
        var transfer = Transfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            Guid.NewGuid(),
            ValidCreatedAt);

        // Assert
        transfer.Amount.Amount.Should().Be(Money.MaxAmount);
    }

    [Test]
    public void Create_WhenSourceAccountIdIsEmpty_ShouldRejectTransfer()
    {
        // Arrange
        var amount = Money.CreateBrl(100m);

        // Act
        var act = () => Transfer.Create(
            Guid.Empty,
            Guid.NewGuid(),
            amount,
            Guid.NewGuid(),
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("sourceAccountId");
    }

    [Test]
    public void Create_WhenDestinationAccountIdIsEmpty_ShouldRejectTransfer()
    {
        // Arrange
        var amount = Money.CreateBrl(100m);

        // Act
        var act = () => Transfer.Create(
            Guid.NewGuid(),
            Guid.Empty,
            amount,
            Guid.NewGuid(),
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("destinationAccountId");
    }

    [Test]
    public void Create_WhenSourceAndDestinationAreTheSame_ShouldRejectTransfer()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var amount = Money.CreateBrl(100m);

        // Act
        var act = () => Transfer.Create(
            accountId,
            accountId,
            amount,
            Guid.NewGuid(),
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentException>();
    }

    [Test]
    public void Create_WhenAmountIsNull_ShouldRejectTransfer()
    {
        // Act
        var act = () => Transfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null!,
            Guid.NewGuid(),
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>()
            .WithParameterName("amount");
    }

    [Test]
    public void Create_WhenAmountIsZero_ShouldRejectTransfer()
    {
        // Arrange
        var amount = Money.Zero(ECurrency.Brl);

        // Act
        var act = () => Transfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            Guid.NewGuid(),
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("amount");
    }

    [Test]
    public void Create_WhenLedgerTransactionIdIsEmpty_ShouldRejectTransfer()
    {
        // Arrange
        var amount = Money.CreateBrl(100m);

        // Act
        var act = () => Transfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            Guid.Empty,
            ValidCreatedAt);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("ledgerTransactionId");
    }

    [Test]
    public void Create_WhenCreatedAtIsDefault_ShouldRejectTransfer()
    {
        // Arrange
        var amount = Money.CreateBrl(100m);

        // Act
        var act = () => Transfer.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            amount,
            Guid.NewGuid(),
            default);

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("createdAt");
    }

    [Test]
    public void Transfer_WhenCreated_ShouldNotAllowPropertiesToBeModified()
    {
        // Arrange
        var properties = new[]
        {
            nameof(Transfer.Id),
            nameof(Transfer.SourceAccountId),
            nameof(Transfer.DestinationAccountId),
            nameof(Transfer.Amount),
            nameof(Transfer.LedgerTransactionId),
            nameof(Transfer.CreatedAt)
        };

        // Assert
        foreach (var propertyName in properties)
        {
            var property = typeof(Transfer).GetProperty(propertyName);

            property.Should().NotBeNull();
            property!.SetMethod?.IsPublic.Should().BeFalse();
        }
    }
}