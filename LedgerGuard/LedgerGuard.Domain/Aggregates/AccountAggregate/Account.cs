using LedgerGuard.Domain.Aggregates.MoneyAggregate;

namespace LedgerGuard.Domain.Aggregates.AccountAggregate;

public sealed class Account
{
    public Guid Id { get; }
    public string OwnerName { get; }
    public EAccountStatus Status { get; private set; }
    public Money Balance { get; private set; }

    private Account(
        Guid id,
        string ownerName,
        EAccountStatus status,
        Money balance)
    {
        Id = id;
        
        if (string.IsNullOrWhiteSpace(ownerName))
            throw new ArgumentException(
                "Owner name cannot be null, empty or whitespace.",
                nameof(ownerName));
        
        if (ownerName.Length > 120)
            throw new ArgumentException(
                "Owner name cannot exceed 120 characters.",
                nameof(ownerName));
        
        OwnerName = ownerName.Trim();
        Status = status;
        Balance = balance;
    }

    public static Account Create(string ownerName)
    {
        return new Account(
            Guid.NewGuid(),
            ownerName,
            EAccountStatus.Active,
            Money.Zero(ECurrency.Brl));
    }

    public void Block()
    {
        if (Status != EAccountStatus.Active)
            throw new InvalidOperationException(
                "Only active accounts can be blocked.");

        Status = EAccountStatus.Blocked;
    }

    public void Unblock()
    {
        if (Status != EAccountStatus.Blocked)
            throw new InvalidOperationException(
                "Only blocked accounts can be unblocked.");

        Status = EAccountStatus.Active;
    }

    public void Close()
    {
        if (Status == EAccountStatus.Closed)
            throw new InvalidOperationException(
                "Account is already closed.");

        if (Balance.Amount != 0m)
            throw new InvalidOperationException(
                "Only accounts with zero balance can be closed.");

        Status = EAccountStatus.Closed;
    }

    public void Credit(Money amount)
    {
        if (Status == EAccountStatus.Closed)
            throw new InvalidOperationException(
                "Closed accounts cannot receive funds.");

        Balance = Balance.Add(amount);
    }
    
    public void Debit(Money amount)
    {
        ArgumentNullException.ThrowIfNull(amount);

        if (Status != EAccountStatus.Active)
            throw new InvalidOperationException(
                "Only active accounts can send funds.");

        if (amount.Amount <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "Debit amount must be greater than zero.");

        if (Balance.Amount < amount.Amount)
            throw new InvalidOperationException(
                "Account has insufficient funds.");

        Balance = Balance.Subtract(amount);
    }
}