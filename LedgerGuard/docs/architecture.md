# Architecture

## Overview

LedgerGuard is implemented as a **modular monolith** using a layered architecture.

The application is separated into four main projects:

```text
LedgerGuard.Api
LedgerGuard.Application
LedgerGuard.Domain
LedgerGuard.Infrastructure
```

The goal is to keep business rules independent from HTTP, persistence, and framework-specific concerns while maintaining a simple deployment model.

---

## Dependency Flow

```text
┌───────────────────────┐
│    LedgerGuard.Api    │
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│ LedgerGuard.Application│
└───────────┬───────────┘
            │
            ▼
┌───────────────────────┐
│  LedgerGuard.Domain   │
└───────────────────────┘

LedgerGuard.Infrastructure
            │
            ├────► Application abstractions
            └────► Domain
```

Dependencies must always point toward the business core.

The Domain layer must not depend on:

- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- HTTP
- Docker
- Infrastructure implementations

---

# Solution Structure

```text
src/
│
├── LedgerGuard.Api/
│   ├── Endpoints/
│   ├── Contracts/
│   ├── Middleware/
│   └── Program.cs
│
├── LedgerGuard.Application/
│   ├── Abstractions/
│   │   ├── Persistence/
│   │   └── Time/
│   │
│   ├── Accounts/
│   ├── Deposits/
│   ├── Transfers/
│   └── Reversals/
│
├── LedgerGuard.Domain/
│   ├── Accounts/
│   ├── Money/
│   ├── Transfers/
│   ├── Ledger/
│   └── Shared/
│
└── LedgerGuard.Infrastructure/
    ├── Persistence/
    │   ├── Configurations/
    │   ├── Migrations/
    │   └── LedgerGuardDbContext.cs
    │
    ├── Repositories/
    └── Idempotency/
```

Tests live separately:

```text
tests/
├── LedgerGuard.UnitTests/
├── LedgerGuard.IntegrationTests/
└── LedgerGuard.EndToEndTests/
```

---

# Domain Layer

`LedgerGuard.Domain` contains the financial model and its business rules.

Main concepts:

```text
Account
Money
Transfer
LedgerTransaction
LedgerEntry
Reversal
```

Typical responsibilities:

- validate account state transitions;
- validate whether an account can send or receive money;
- prevent invalid monetary values;
- enforce transfer rules;
- represent transfer and reversal state;
- protect ledger invariants;
- expose explicit domain errors.

Business behavior should be expressed through methods instead of public property mutation.

Example:

```csharp
account.Debit(amount);
account.Credit(amount);
account.Block();
account.Close();
```

Instead of:

```csharp
account.Balance -= amount;
account.Status = AccountStatus.Closed;
```

Entity state should only change through operations that enforce the corresponding business rules.

---

# Application Layer

`LedgerGuard.Application` coordinates use cases.

Main use cases include:

```text
CreateAccount
BlockAccount
CloseAccount

DepositFunds

CreateTransfer
GetTransfer

ReverseTransfer

GetAccountLedger
ReconcileAccount
```

The Application layer is responsible for:

- loading domain objects;
- coordinating multiple domain objects;
- defining transaction boundaries;
- calling persistence abstractions;
- coordinating idempotency;
- handling concurrency conflicts;
- returning application results.

It should not contain rules that naturally belong to domain entities or value objects.

Example flow:

```text
CreateTransfer
      │
      ├── Load source account
      ├── Load destination account
      │
      ├── Execute domain validations
      │
      ├── Create transfer
      ├── Create ledger transaction
      │
      └── Commit everything atomically
```

---

# Infrastructure Layer

`LedgerGuard.Infrastructure` contains technical implementations required by the application.

Responsibilities include:

- PostgreSQL persistence;
- Entity Framework Core configuration;
- repository implementations;
- database migrations;
- idempotency storage;
- database transaction management;
- concurrency-token configuration.

Infrastructure implements abstractions defined by the Application layer.

Example:

```text
Application
    │
    └── IAccountRepository

Infrastructure
    │
    └── AccountRepository
```

The Domain layer does not know which database is being used.

---

# API Layer

`LedgerGuard.Api` exposes the application through HTTP.

Responsibilities:

- receive HTTP requests;
- deserialize and validate request format;
- extract headers such as `Idempotency-Key`;
- invoke application use cases;
- map application/domain results to HTTP responses;
- expose OpenAPI documentation.

The API must not implement financial business rules.

Avoid:

```csharp
if (account.Balance < request.Amount)
{
    return UnprocessableEntity();
}
```

The API should only translate a business result such as:

```text
InsufficientFunds
```

into the appropriate HTTP response.

---

# Persistence Model

PostgreSQL is the primary database.

Main tables:

```text
accounts
transfers
ledger_transactions
ledger_entries
idempotency_records
```

A simplified relationship model:

```text
accounts
   │
   ├──────────────┐
   │              │
   ▼              ▼
transfers     ledger_entries
                  │
                  ▼
          ledger_transactions

idempotency_records
        │
        └────► financial operation
```

Important constraints should also be enforced at the database level when possible.

Examples:

```text
account balance >= 0
transfer amount > 0
unique transfer identifier
unique idempotency key within its operation scope
valid foreign keys
```

Database constraints reinforce domain rules but do not replace domain validation.

---

# Balance and Ledger

The ledger is the immutable source of financial history.

`Account.CurrentBalance` is stored as a persisted projection to allow efficient balance checks and transaction processing.

Both must remain consistent:

```text
Account.CurrentBalance
        ==
SUM(Account Ledger Entries)
```

Whenever a financial operation changes a balance, the balance update and the corresponding ledger entries must be committed in the same database transaction.

A reconciliation use case verifies this invariant independently.

```text
Stored Balance
      │
      ├──── compare ──── Ledger Balance
      │
      ▼
   Consistent?
```

---

# Double-Entry Ledger

Every financial operation creates a `LedgerTransaction`.

A ledger transaction contains at least two `LedgerEntry` records.

Example transfer:

```text
Transfer BRL 100

Account A    -100
Account B    +100
-----------------
Total           0
```

The following invariant must always hold:

```text
SUM(entries.Amount) == 0
```

Ledger entries are append-only.

They cannot be modified or deleted after being committed.

Corrections are represented by new compensating entries.

---

# Transaction Boundaries

Financial operations that modify multiple records must be atomic.

A transfer is executed inside a single database transaction.

```text
BEGIN TRANSACTION

1. Load source account
2. Load destination account
3. Validate transfer
4. Update source balance
5. Update destination balance
6. Persist transfer
7. Persist ledger transaction
8. Persist ledger entries
9. Persist idempotency result

COMMIT
```

If any step fails:

```text
ROLLBACK
```

No partial financial state may remain.

The same principle applies to:

- deposits;
- reversals.

---

# Idempotency Architecture

Financial commands require an `Idempotency-Key`.

Supported operations:

```text
Deposit
Transfer
Reversal
```

An idempotency record stores enough information to identify the original request and result.

Suggested model:

```text
IdempotencyRecord
├── Key
├── Operation
├── RequestHash
├── ResourceId
├── ResponseStatus
└── CreatedAt
```

Uniqueness should be enforced using:

```text
(Operation, Key)
```

Processing flow:

```text
Request
   │
   ▼
Read Idempotency-Key
   │
   ▼
Look for existing record
   │
   ├── Same key + same request
   │       └── Return original result
   │
   ├── Same key + different request
   │       └── Reject with conflict
   │
   └── New key
           │
           ▼
      Execute operation
           │
           ▼
      Persist operation
      + idempotency result
      in the same transaction
```

The database unique constraint is part of the concurrency guarantee.

Application-level checks alone are not sufficient.

---

# Concurrency Control

LedgerGuard uses **optimistic concurrency control** for accounts.

Each account contains a version field:

```text
Account
├── Id
├── CurrentBalance
└── Version
```

The version is configured as a concurrency token.

Conceptually, an update behaves like:

```sql
UPDATE accounts
SET
    current_balance = @newBalance,
    version = @newVersion
WHERE
    id = @accountId
    AND version = @expectedVersion;
```

If another operation changed the account first, no row matches the expected version.

The application must then treat the operation as a concurrency conflict.

---

## Concurrent Transfer Example

Initial state:

```text
Account A = BRL 100
```

Two transfers execute concurrently:

```text
Transfer 1 = BRL 80
Transfer 2 = BRL 80
```

Both may initially read:

```text
balance = BRL 100
```

Only one can successfully commit against the original account version.

The other operation must reload the current state and execute the business validation again.

After revalidation:

```text
Account A = BRL 20
```

The second BRL 80 transfer is rejected for insufficient funds.

Expected result:

```text
1 transfer completed
1 transfer rejected

final balance = BRL 20
```

A concurrency retry must never bypass business validation.

---

# Concurrency Conflict Handling

A concurrency conflict follows this flow:

```text
Load state
    │
    ▼
Execute business rules
    │
    ▼
Attempt commit
    │
    ├── Success
    │      └── Complete operation
    │
    └── Concurrency conflict
           │
           ▼
       Reload state
           │
           ▼
       Re-run business rules
           │
           ├── Still valid
           │      └── Retry commit
           │
           └── No longer valid
                  └── Return business failure
```

Retries must be bounded.

If the operation cannot be safely completed after retrying, the application should return a conflict instead of retrying indefinitely.

---

# Repository Strategy

Use repositories only around meaningful persistence boundaries.

Suggested abstractions:

```text
IAccountRepository
ITransferRepository
ILedgerRepository
IIdempotencyRepository
```

Avoid introducing a generic repository only to wrap every Entity Framework Core operation.

Repository interfaces should represent operations required by application use cases.

---

# Unit of Work

A single financial command may modify multiple persistence objects.

The transaction boundary should therefore be controlled through a shared unit of work.

For the initial implementation, `LedgerGuardDbContext` can provide this behavior internally.

Application code should be able to express:

```text
perform changes
       │
       ▼
commit once
```

rather than independently saving each repository operation.

---

# Error Flow

Expected business failures should be represented explicitly.

Examples:

```text
InsufficientFunds
AccountBlocked
AccountClosed
InvalidAmount
TransferAlreadyReversed
IdempotencyConflict
ConcurrencyConflict
```

Expected business conditions should not rely on generic exceptions for control flow.

Infrastructure exceptions may still be translated into appropriate application failures where necessary.

---

# Request Flow

A typical transfer request follows:

```text
HTTP Request
     │
     ▼
API Endpoint
     │
     ▼
Application Use Case
     │
     ├──── Idempotency
     │
     ├──── Load Accounts
     │
     ▼
Domain Rules
     │
     ▼
Create Transfer + Ledger Entries
     │
     ▼
Infrastructure
     │
     ▼
PostgreSQL Transaction
     │
     ▼
Application Result
     │
     ▼
HTTP Response
```

---

# Initial API Boundaries

Suggested endpoints:

```http
POST /accounts
GET  /accounts/{accountId}

POST /accounts/{accountId}/deposits

POST /transfers
GET  /transfers/{transferId}

POST /transfers/{transferId}/reversal

GET  /accounts/{accountId}/ledger
GET  /accounts/{accountId}/reconciliation
```

The exact HTTP contracts are defined separately from the Domain model.

API request/response DTOs should not be reused as domain entities.

---

# Architectural Decisions

## Modular Monolith

The first version is deployed as a single application.

This keeps transaction handling and development simple while preserving clear internal boundaries.

---

## Single Database

All core financial data is stored in PostgreSQL.

This allows deposits, transfers, reversals, ledger entries, balances, and idempotency records to participate in the same database transaction.

---

## Domain-Centered Design

Financial rules live in the Domain layer and can be tested without infrastructure.

---

## Immutable Ledger

Ledger entries are append-only.

Historical financial data is never rewritten to represent corrections.

---

## Optimistic Concurrency

Concurrent account changes are detected using explicit concurrency tokens.

Conflicting operations must reload state and revalidate business rules.

---

## No Message Broker in the Initial Version

Core financial operations are executed synchronously inside the database transaction.

Asynchronous messaging may be added later for non-critical side effects or integration events, but it is not required for the initial implementation.

---

# Future Architecture Extensions

Possible future additions:

```text
Outbox Pattern
Integration Events
Message Broker
Fraud/Risk Module
Authentication
Multiple Currencies
Observability
```

These should only be introduced when the core financial domain and its tests are stable.
