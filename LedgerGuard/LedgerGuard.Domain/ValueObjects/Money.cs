using LedgerGuard.Domain.Enums;

namespace LedgerGuard.Domain.ValueObjects;

public class Money
{
    public const decimal MaxAmount = 999999999999999999.99m;

    public decimal Amount { get; set; }
    public ECurrency Currency { get; set; }

    public Money(decimal amount, ECurrency currency)
    {
        if (amount > MaxAmount)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                $"Amount cannot exceed {MaxAmount}.");
        
        if (!Enum.IsDefined(currency))
            throw new ArgumentException(
                "Invalid currency.",
                nameof(currency));
        
        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException("Monetary values cannot have more than two decimal places.");
        }

        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), 
                "Monetary values cannot be negative.");
        }

        Amount = amount;
        Currency = currency;
    }

    public static Money CreateBrl(decimal amount) => new(amount, ECurrency.Brl);
    public static Money Zero(ECurrency currency) => new(0m, currency);
    
    public Money Add(Money other)
    {
        EnsureSameCurrency(other);

        return new Money(
            Amount + other.Amount,
            Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);

        return new Money(
            Amount - other.Amount,
            Currency);
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                "Money values with different currencies cannot be operated.");
    }
}