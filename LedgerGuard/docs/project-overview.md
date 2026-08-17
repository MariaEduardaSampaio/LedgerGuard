# Project Overview

## Overview

LedgerGuard is a financial transaction engine focused on correctness, consistency, and business-rule enforcement.

The system manages accounts, deposits, transfers, reversals, and an immutable double-entry ledger. Its main objective is to ensure that financial operations remain correct when requests are retried, executed concurrently, or fail during processing.

## Core Problem

Financial operations must preserve a set of guarantees regardless of how or when they are executed.

A transfer must never:

- debit or credit an account more than once for the same operation;
- create or destroy money;
- leave only one side of the transaction committed;
- produce a negative balance;
- create duplicate ledger entries;
- leave partial financial state after a failure.

LedgerGuard is designed around preventing these conditions.

## Scope

The initial version supports:

- Account creation and status management
- Deposits
- Account-to-account transfers
- Transfer reversals
- Double-entry ledger
- Idempotent financial operations
- Concurrent transaction handling
- Ledger reconciliation

## Core Domain

### Account

Represents an account that can hold and transfer funds.

An account has a status that determines whether it can send or receive money.

### Money

Represents a monetary amount associated with a currency.

The initial version supports BRL only.

### Deposit

Represents money entering the system from an external source.

Every deposit must create the corresponding ledger entries.

### Transfer

Represents the movement of money between two accounts.

A transfer must debit the source and credit the destination atomically.

### Reversal

Represents a compensating operation for a previously completed transfer.

The original transaction is never modified or deleted.

### Ledger Transaction

Represents a complete financial event such as a deposit, transfer, or reversal.

### Ledger Entry

Represents one side of a financial transaction.

Every ledger transaction must contain balanced entries.

## Financial Model

LedgerGuard uses double-entry accounting.

For a transfer of BRL 100 from Account A to Account B:

```text
Account A    -100
Account B    +100
-----------------
Total           0
```

The sum of all entries belonging to the same financial transaction must always be zero.

## Example Flow

Initial state:

```text
Alice: BRL 1,000
Bob:   BRL   300
```

Operation:

```text
Alice transfers BRL 250 to Bob
```

Result:

```text
Alice: BRL 750
Bob:   BRL 550
```

Ledger:

```text
Alice    -250
Bob      +250
--------------
Total       0
```

If the same request is retried with the same idempotency key, no additional financial effect should occur.

## Core Guarantees

LedgerGuard is built around the following guarantees:

1. **Money conservation**  
   Internal transfers cannot create or destroy money.

2. **Non-negative balances**  
   Account balances cannot become negative.

3. **Balanced ledger**  
   Every financial transaction must have ledger entries whose sum is zero.

4. **Atomicity**  
   A financial operation must either complete entirely or have no financial effect.

5. **Idempotency**  
   Retrying the same logical request must not duplicate financial effects.

6. **Immutable financial history**  
   Committed ledger entries cannot be edited or deleted.

7. **Concurrency safety**  
   Concurrent operations must preserve all business invariants.

8. **Consistent balances**  
   The stored account balance must match the balance represented by its ledger entries.

## Design Principles

- Business rules belong to the domain.
- Financial history is append-only.
- Invalid operations must not modify financial state.
- Infrastructure should reinforce domain invariants, not define them.
- Database transactions protect multi-step financial operations.
- Concurrency must be treated as part of the business problem.
- Tests should describe and validate expected business behavior.
- Simplicity is preferred over unnecessary infrastructure complexity.

## Documentation

Detailed specifications are available in:

- [`business-rules.md`](./business-rules.md) — domain rules and invariants
- [`architecture.md`](./architecture.md) — solution structure and technical decisions
- [`testing-strategy.md`](./testing-strategy.md) — unit, integration, concurrency, and end-to-end testing strategy
