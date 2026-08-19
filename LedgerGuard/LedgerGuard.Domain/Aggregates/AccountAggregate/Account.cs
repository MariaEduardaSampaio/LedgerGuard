using LedgerGuard.Domain.Aggregates.MoneyAggregate;

namespace LedgerGuard.Domain.Aggregates.AccountAggregate;

public sealed class Account
{
    public Guid Id { get; }
    public string OwnerName { get; }
    public AccountStatus Status { get; private set; }
    public Money Balance { get; private set; }

    private Account(
        Guid id,
        string ownerName,
        AccountStatus status,
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
            AccountStatus.Active,
            Money.Zero(Currency.Brl));
    }

    public void Block()
    {
        if (Status != AccountStatus.Active)
            throw new InvalidOperationException(
                "Only active accounts can be blocked.");

        Status = AccountStatus.Blocked;
    }

    public void Unblock()
    {
        if (Status != AccountStatus.Blocked)
            throw new InvalidOperationException(
                "Only blocked accounts can be unblocked.");

        Status = AccountStatus.Active;
    }

    public void Close()
    {
        if (Status == AccountStatus.Closed)
            throw new InvalidOperationException(
                "Account is already closed.");

        if (Balance.Amount != 0m)
            throw new InvalidOperationException(
                "Only accounts with zero balance can be closed.");

        Status = AccountStatus.Closed;
    }

    public void Credit(Money amount)
    {
        if (Status == AccountStatus.Closed)
            throw new InvalidOperationException(
                "Closed accounts cannot receive funds.");

        Balance = Balance.Add(amount);
    }
}