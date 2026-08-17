# Testing Strategy

This document defines the testing strategy for LedgerGuard.

Testing is a core part of the project because financial correctness depends not only on implementing business rules, but also on proving that those rules remain valid across persistence, retries, failures, and concurrent execution.

The test suite is organized into three main levels:

```text
Unit Tests
Integration Tests
End-to-End Tests
```

Each level has a specific responsibility.

The goal is not to repeat every scenario at every level. Each behavior should be tested at the lowest level capable of proving it, while critical financial flows receive additional integration and end-to-end coverage.

---

# 1. Goals

The test suite must provide confidence that LedgerGuard preserves its financial invariants.

The most important guarantees are:

- money is not created or lost during internal transfers;
- customer balances never become negative;
- ledger transactions are always balanced;
- failed operations do not leave partial financial state;
- duplicate requests do not create duplicate financial effects;
- concurrent operations do not violate business rules;
- reversals preserve immutable financial history;
- stored balances remain consistent with ledger balances.

Tests should serve as executable documentation for the rules defined in:

```text
docs/business-rules.md
```

---

# 2. Test Stack

The initial test stack is:

```text
NUnit
NUnit3TestAdapter
Microsoft.NET.Test.Sdk
Testcontainers for .NET
PostgreSQL
Microsoft.AspNetCore.Mvc.Testing
```

Recommended packages:

```xml
<PackageReference Include="NUnit" />
<PackageReference Include="NUnit3TestAdapter" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />

<PackageReference Include="Testcontainers.PostgreSql" />

<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
```

Coverage tooling may be added to CI separately.

---

# 3. Test Projects

```text
tests/
│
├── LedgerGuard.UnitTests/
│
├── LedgerGuard.IntegrationTests/
│
└── LedgerGuard.EndToEndTests/
```

Suggested structure:

```text
LedgerGuard.UnitTests/
├── Accounts/
├── Money/
├── Deposits/
├── Transfers/
├── Ledger/
└── Reversals/

LedgerGuard.IntegrationTests/
├── Persistence/
├── Deposits/
├── Transfers/
├── Reversals/
├── Idempotency/
├── Concurrency/
└── Reconciliation/

LedgerGuard.EndToEndTests/
├── Accounts/
├── Deposits/
├── Transfers/
└── Reversals/
```

---

# 4. Test Categories

NUnit categories are used to group tests.

Primary categories:

```text
Unit
Integration
E2E
```

Additional behavioral categories may be combined with them:

```text
Concurrency
Idempotency
Reconciliation
```

Example:

```csharp
[Test]
[Category("Integration")]
[Category("Concurrency")]
public async Task Concurrent_transfers_should_not_overdraw_account()
{
}
```

Run only unit tests:

```bash
dotnet test --filter TestCategory=Unit
```

Run integration tests:

```bash
dotnet test --filter TestCategory=Integration
```

Run E2E tests:

```bash
dotnet test --filter TestCategory=E2E
```

Run concurrency tests:

```bash
dotnet test --filter TestCategory=Concurrency
```

---

# 5. Test Distribution

The expected distribution is:

```text
                  ┌───────────────┐
                  │      E2E      │
                  │   few tests   │
                  └───────┬───────┘
                          │
               ┌──────────┴──────────┐
               │ Integration Tests   │
               │ persistence + DB    │
               └──────────┬──────────┘
                          │
             ┌────────────┴────────────┐
             │       Unit Tests        │
             │ business rules + domain │
             └─────────────────────────┘
```

Most business scenarios belong in unit tests.

Integration tests focus on behavior that depends on PostgreSQL, EF Core, transaction boundaries, idempotency persistence, or concurrency.

E2E tests cover only the most important workflows through the application's real HTTP boundary.

---

# 6. General Test Principles

## TST-001 — Test Business Behavior

Tests should describe observable behavior instead of implementation details.

Prefer:

```text
Blocked_account_cannot_initiate_transfer
```

Avoid:

```text
TransferService_should_call_repository_once
```

The first describes a business rule.

The second couples the test to implementation structure.

---

## TST-002 — Arrange, Act, Assert

Tests should normally follow:

```text
Arrange
Act
Assert
```

Example:

```csharp
[Test]
[Category("Unit")]
public void Transfer_with_insufficient_balance_should_be_rejected()
{
    // Arrange
    var source = AccountBuilder
        .AnActiveAccount()
        .WithBalance(100m)
        .Build();

    var destination = AccountBuilder
        .AnActiveAccount()
        .Build();

    // Act
    var result = source.TransferTo(destination, Money.Brl(100.01m));

    // Assert
    Assert.That(result.IsFailure, Is.True);
    Assert.That(result.Error.Code, Is.EqualTo("INSUFFICIENT_FUNDS"));
}
```

---

## TST-003 — One Main Behavior Per Test

A test should have one clear reason to fail.

Multiple assertions are acceptable when they describe the same business outcome.

Example:

After a successful transfer, it is reasonable to verify:

```text
source balance
destination balance
ledger entries
```

when those assertions together prove one transfer outcome.

---

## TST-004 — Tests Must Be Deterministic

A test must produce the same result across repeated executions.

Avoid:

- dependence on current local time;
- random data without a controlled seed;
- execution-order dependency;
- shared mutable state between tests;
- external network dependencies.

---

## TST-005 — Tests Must Be Independent

No test may depend on another test running first.

Each test must prepare its own required state.

---

## TST-006 — Business Rule IDs Should Be Traceable

When practical, tests should reference the related business rule.

Example:

```csharp
[Test]
[Category("Unit")]
[Description("TRF-008 — Source must have sufficient balance")]
public void Transfer_with_insufficient_balance_should_be_rejected()
{
}
```

This allows:

```text
business rule
    ↓
test
    ↓
implementation
```

to remain traceable.

---

# 7. Naming Convention

Use behavior-oriented names.

Recommended format:

```text
Condition_expectedBehavior
```

Examples:

```text
New_account_should_start_with_zero_balance

Blocked_account_should_not_initiate_transfer

Transfer_with_exact_available_balance_should_succeed

Transfer_exceeding_balance_by_one_cent_should_fail

Duplicate_transfer_should_not_debit_source_twice

Concurrent_transfers_should_never_overdraw_account
```

Avoid generic names:

```text
Test1
CreateTransferTest
TransferServiceWorks
RepositoryTest
```

---

# 8. Unit Tests

Unit tests validate business rules without infrastructure.

They must not require:

```text
PostgreSQL
Docker
HTTP
Entity Framework Core
file system
external services
```

They should execute quickly and make up most of the test suite.

Primary targets:

```text
Money
Account
Transfer
LedgerTransaction
LedgerEntry
Reversal
Domain services
Application logic that is independent of infrastructure
```

---

# 9. Unit Test Boundaries

Unit tests should verify:

- happy paths;
- invalid operations;
- minimum valid values;
- maximum valid values;
- values immediately outside valid boundaries;
- state transitions;
- financial invariants;
- immutable behavior;
- domain error results.

For monetary rules, boundary testing is especially important.

Example:

Business rule:

```text
balance >= transfer amount
```

Tests should include:

```text
Balance = 100

Transfer 99.99  -> success
Transfer 100.00 -> success
Transfer 100.01 -> failure
```

Not only:

```text
Transfer 50 -> success
Transfer 500 -> failure
```

---

# 10. Parameterized Tests

NUnit parameterized tests should be used when several inputs verify the same rule.

Example:

```csharp
[TestCase(0)]
[TestCase(-0.01)]
[TestCase(-1)]
[TestCase(-100)]
[Category("Unit")]
public void Non_positive_transfer_amount_should_be_rejected(decimal amount)
{
}
```

Boundary examples:

```csharp
[TestCase(0.01, true)]
[TestCase(1.00, true)]
[TestCase(100.00, true)]
[TestCase(100.01, false)]
public void Transfer_should_respect_available_balance(
    decimal amount,
    bool expectedSuccess)
{
}
```

`TestCaseSource` may be used when scenario data becomes too large for inline attributes.

---

# 11. Money Unit Tests

Rules:

```text
MON-*
```

Required scenarios:

```text
MON-U01  BRL money with valid amount should be created
MON-U02  Minimum amount 0.01 should be accepted for financial commands
MON-U03  Zero financial amount should be rejected
MON-U04  Negative amount should be rejected
MON-U05  Amount with more than two decimal places should be rejected
MON-U06  Exact maximum supported amount should be accepted
MON-U07  Amount above maximum should be rejected
MON-U08  Unsupported currency should be rejected
MON-U09  Money operations should preserve decimal precision
MON-U10  Balance overflow should be rejected
```

Important boundaries:

```text
0
0.001
0.01

99,999,999,999,999,999.98
99,999,999,999,999,999.99
100,000,000,000,000,000.00
```

---

# 12. Account Unit Tests

Rules:

```text
ACC-*
```

Required happy paths:

```text
ACC-U01 New account starts Active
ACC-U02 New account starts with BRL 0.00
ACC-U03 Active account can be blocked
ACC-U04 Blocked account can be unblocked
ACC-U05 Zero-balance Active account can be closed
ACC-U06 Zero-balance Blocked account can be closed
ACC-U07 Blocked account can receive funds
```

Required invalid and boundary cases:

```text
ACC-U08 Empty owner name is rejected
ACC-U09 Whitespace-only owner name is rejected
ACC-U10 One-character owner name is accepted
ACC-U11 120-character owner name is accepted
ACC-U12 121-character owner name is rejected
ACC-U13 Account with BRL 0.01 cannot be closed
ACC-U14 Closed account cannot be reopened
ACC-U15 Closed account cannot be blocked
ACC-U16 Repeated block is rejected
ACC-U17 Repeated unblock is rejected
ACC-U18 Repeated close is rejected
ACC-U19 Balance cannot be directly modified
```

---

# 13. Deposit Unit Tests

Rules:

```text
DEP-*
```

Required scenarios:

```text
DEP-U01 Minimum deposit should succeed
DEP-U02 Normal deposit should increase customer balance
DEP-U03 Deposit into Blocked account should succeed
DEP-U04 Deposit into Closed account should fail
DEP-U05 Zero deposit should fail
DEP-U06 Negative deposit should fail
DEP-U07 Deposit with more than two decimal places should fail
DEP-U08 Deposit reaching maximum balance exactly should succeed
DEP-U09 Deposit overflowing maximum balance should fail
DEP-U10 Failed deposit should not change customer state
DEP-U11 Deposit should produce correct ledger entry values
DEP-U12 Deposit ledger entries should sum to zero
```

The persistence and idempotency portions of deposits belong to integration tests.

---

# 14. Transfer Unit Tests

Rules:

```text
TRF-*
```

Required happy paths:

```text
TRF-U01 Minimum transfer 0.01 succeeds
TRF-U02 Partial-balance transfer succeeds
TRF-U03 Exact-balance transfer succeeds
TRF-U04 Transfer into Blocked account succeeds
TRF-U05 Transfer reaching destination maximum exactly succeeds
TRF-U06 Source is debited by exact transfer amount
TRF-U07 Destination is credited by exact transfer amount
TRF-U08 Combined customer balance is preserved
TRF-U09 Ledger entries sum to zero
```

Required invalid cases:

```text
TRF-U10 Unknown source is rejected at application boundary
TRF-U11 Unknown destination is rejected at application boundary
TRF-U12 Self-transfer is rejected
TRF-U13 Zero amount is rejected
TRF-U14 Negative amount is rejected
TRF-U15 More than two decimal places is rejected
TRF-U16 Unsupported currency is rejected
TRF-U17 Blocked source is rejected
TRF-U18 Closed source is rejected
TRF-U19 Closed destination is rejected
TRF-U20 Insufficient funds is rejected
TRF-U21 Destination overflow is rejected
```

Required boundary cases:

```text
TRF-U22 Transfer one cent below full balance succeeds
TRF-U23 Transfer exact full balance succeeds
TRF-U24 Transfer one cent above full balance fails
TRF-U25 Transfer to destination one cent below maximum succeeds
TRF-U26 Transfer causing destination to exceed maximum by one cent fails
```

Required state guarantees:

```text
TRF-U27 Failed transfer leaves source unchanged
TRF-U28 Failed transfer leaves destination unchanged
TRF-U29 Completed transfer cannot be edited
```

---

# 15. Reversal Unit Tests

Rules:

```text
REV-*
```

Required scenarios:

```text
REV-U01 Completed transfer can be reversed
REV-U02 Reversal amount equals original amount
REV-U03 Partial reversal is not supported
REV-U04 Reversal restores original source by the correct amount
REV-U05 Reversal debits original destination by the correct amount
REV-U06 Reversal entries sum to zero
REV-U07 Original ledger entries remain unchanged
REV-U08 Second reversal is rejected
REV-U09 Unknown transfer cannot be reversed
REV-U10 Destination with exact required balance can be debited
REV-U11 Destination short by one cent causes reversal failure
REV-U12 Blocked account does not prevent valid reversal
REV-U13 Closed original source causes rejection
REV-U14 Closed original destination causes rejection
REV-U15 Reversal causing balance overflow is rejected
REV-U16 Failed reversal changes no account balance
```

---

# 16. Ledger Unit Tests

Rules:

```text
LED-*
INV-*
```

Required scenarios:

```text
LED-U01 Ledger transaction requires at least two entries
LED-U02 Ledger transaction with sum zero is valid
LED-U03 Unbalanced ledger transaction is rejected
LED-U04 Zero-value ledger entry is rejected
LED-U05 Entries must share transaction currency
LED-U06 Deposit creates expected debit and credit
LED-U07 Transfer creates expected debit and credit
LED-U08 Reversal creates opposite entries
LED-U09 Ledger entries cannot be modified after creation
LED-U10 Reversal does not modify original entries
```

---

# 17. Integration Tests

Integration tests prove behavior that cannot be trusted based only on in-memory domain execution.

They use real infrastructure.

The database for integration tests must be:

```text
PostgreSQL
```

started through:

```text
Testcontainers for .NET
```

Integration tests should not replace PostgreSQL with:

```text
EF Core InMemory provider
SQLite
mock repository
```

when the behavior being tested depends on PostgreSQL semantics.

---

# 18. PostgreSQL Testcontainer

A PostgreSQL container should be created for integration tests.

Example fixture concept:

```csharp
public sealed class PostgreSqlFixture
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:17")
            .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public Task StartAsync() =>
        _container.StartAsync();

    public Task DisposeAsync() =>
        _container.DisposeAsync().AsTask();
}
```

The exact fixture implementation may change according to the selected NUnit lifecycle strategy.

The important requirement is:

```text
tests execute against a real PostgreSQL instance
```

---

# 19. Database Lifecycle

The PostgreSQL container may be shared across a test fixture or integration test assembly for performance.

However, database state must be isolated between tests.

Before every test, the database must have a known state.

Possible strategy:

```text
Start PostgreSQL container once
        │
        ▼
Apply migrations
        │
        ▼
Before each test
reset application tables
        │
        ▼
Seed only data required by that test
```

Tests must never rely on records created by another test.

---

# 20. Database Reset

Resetting state must preserve schema and migrations while removing test data.

Conceptually:

```text
ledger_entries
ledger_transactions
transfers
idempotency_records
accounts
```

are cleared in a safe dependency order.

System-defined records such as the internal settlement account may be recreated by the test fixture after cleanup.

Concurrency tests must not run inside an outer test transaction that prevents multiple database connections from observing committed state correctly.

---

# 21. Migration Tests

Database migrations are part of the application behavior.

At minimum, integration setup must prove that:

```text
an empty PostgreSQL database
        │
        ▼
apply migrations
        │
        ▼
application schema becomes usable
```

A migration failure should fail the integration suite.

---

# 22. Persistence Integration Tests

Required scenarios:

```text
PER-I01 Account can be persisted and retrieved
PER-I02 Account status persists correctly
PER-I03 Monetary precision is preserved
PER-I04 Maximum valid balance can be persisted
PER-I05 Database rejects invalid negative customer balance
PER-I06 Transfer can be persisted with source and destination
PER-I07 Ledger transaction persists its entries
PER-I08 Ledger foreign keys prevent orphan entries
PER-I09 Unique identifiers are enforced
PER-I10 Idempotency uniqueness is enforced
```

---

# 23. Transaction Atomicity Tests

Atomicity must be tested against PostgreSQL.

Required scenarios:

```text
ATM-I01 Successful deposit commits balance and ledger together

ATM-I02 Successful transfer commits:
        source balance
        destination balance
        transfer
        ledger transaction
        ledger entries
        idempotency result

ATM-I03 Successful reversal commits all compensating changes together
```

Failure scenarios:

```text
ATM-I04 Failure before commit leaves no financial changes

ATM-I05 Ledger persistence failure rolls back account balance changes

ATM-I06 Transfer persistence failure rolls back account updates

ATM-I07 Idempotency persistence failure prevents partial successful financial state

ATM-I08 Reversal persistence failure leaves original transfer unchanged
```

---

# 24. Failure Injection

Some atomicity scenarios require intentionally causing infrastructure failure.

Failure injection should occur through controlled test-only mechanisms.

Examples:

```text
forced repository failure before commit
database constraint violation
forced exception during application transaction
```

The test is successful only if the database remains in the expected pre-operation state.

Example:

```text
Before

A = 100
B = 0

Failure occurs while persisting transfer

After

A = 100
B = 0

transfer rows = 0
ledger rows = 0
```

---

# 25. Idempotency Integration Tests

Rules:

```text
IDE-*
```

Idempotency must be validated against PostgreSQL because database uniqueness participates in the guarantee.

Required scenarios:

```text
IDE-I01 Successful transfer followed by identical retry returns same operation

IDE-I02 Duplicate transfer does not debit source twice

IDE-I03 Duplicate transfer does not credit destination twice

IDE-I04 Duplicate transfer creates one transfer row

IDE-I05 Duplicate transfer creates one ledger transaction

IDE-I06 Duplicate transfer creates one pair of ledger entries

IDE-I07 Same key + different amount returns conflict

IDE-I08 Same key + different destination returns conflict

IDE-I09 Same key + different source returns conflict

IDE-I10 Same key in different operation scope is allowed

IDE-I11 Stable insufficient-funds result is replayed

IDE-I12 Balance changing later does not change previously stored stable result

IDE-I13 Malformed pre-execution request does not reserve key

IDE-I14 Transient failure without committed outcome allows safe retry
```

---

# 26. Concurrency Integration Tests

Concurrency tests are critical.

They must use:

```text
real PostgreSQL
multiple database contexts/connections
real concurrent Tasks
```

A mocked repository cannot prove concurrency correctness.

---

# 27. Critical Concurrency Scenario

Initial state:

```text
Account A = BRL 100
```

Two distinct transfers begin concurrently:

```text
T1 = BRL 80
T2 = BRL 80
```

Expected:

```text
one transfer succeeds
one transfer fails after revalidation

Account A = BRL 20
```

Must never produce:

```text
both successful

or

Account A = BRL -60
```

Required test:

```text
CON-I01 Concurrent_transfers_should_not_overdraw_account
```

---

# 28. Sufficient-Funds Concurrency

Initial:

```text
A = 200
```

Concurrent:

```text
T1 = 80
T2 = 80
```

Expected:

```text
both eventually succeed
A = 40
```

Required:

```text
CON-I02 Concurrent_affordable_transfers_should_both_be_applied
```

This verifies that concurrency protection does not incorrectly reject all conflicts.

---

# 29. Concurrent Deposits

Initial:

```text
A = 0
```

Distinct concurrent deposits:

```text
+50
+70
```

Expected:

```text
A = 120
```

and both ledger transactions exist.

Required:

```text
CON-I03 Concurrent_deposits_should_not_lose_updates
```

---

# 30. Duplicate Request Concurrency

Send multiple simultaneous requests using:

```text
same operation
same payload
same Idempotency-Key
```

Expected:

```text
one business operation
one financial effect
one ledger transaction
```

Required:

```text
CON-I04 Concurrent_duplicate_transfers_should_execute_once

CON-I05 Concurrent_duplicate_deposits_should_execute_once

CON-I06 Concurrent_duplicate_reversals_should_execute_once
```

A stronger stress variation may issue:

```text
10
25
50
```

simultaneous duplicate requests.

The invariant remains:

```text
financial effect count = 1
```

---

# 31. Transfer vs Reversal Race

Scenario:

```text
Bob balance = 100
```

Concurrent operations:

```text
A: reverse previous transfer requiring Bob -100
B: Bob transfers 80 elsewhere
```

Allowed final outcomes:

```text
Reversal wins:
Bob = 0
outgoing transfer fails
```

or:

```text
Outgoing transfer wins:
Bob = 20
reversal fails
```

Forbidden:

```text
both succeed
Bob < 0
```

Required:

```text
CON-I07 Transfer_and_reversal_race_should_preserve_valid_state
```

---

# 32. Transfer vs Close Race

Given a zero-balance destination that is being closed while another account transfers money to it:

Expected result must match one valid ordering.

Either:

```text
close commits first
transfer fails
account remains Closed with 0 balance
```

or:

```text
transfer commits first
balance becomes positive
close fails
```

Forbidden:

```text
account = Closed
balance > 0
```

Required:

```text
CON-I08 Transfer_and_close_race_should_preserve_account_invariants
```

---

# 33. Transfer vs Block Race

Two valid serial outcomes:

```text
transfer commits first
then account becomes Blocked
```

or:

```text
block commits first
transfer is rejected
```

Required:

```text
CON-I09 Transfer_and_block_race_should_produce_valid_serial_outcome
```

---

# 34. Concurrency Test Assertions

Concurrency tests must assert final state, not only individual task results.

After concurrent execution, verify:

```text
final balances
successful operation count
failed operation count
transfer rows
ledger transaction count
ledger entry count
ledger balance
stored balance
non-negative balance invariant
```

Example:

```csharp
Assert.Multiple(() =>
{
    Assert.That(successfulTransfers, Is.EqualTo(1));
    Assert.That(account.CurrentBalance, Is.EqualTo(20m));
    Assert.That(account.CurrentBalance, Is.GreaterThanOrEqualTo(0m));
    Assert.That(ledgerBalance, Is.EqualTo(account.CurrentBalance));
});
```

---

# 35. Reconciliation Integration Tests

Required scenarios:

```text
REC-I01 Stored balance equals ledger balance after deposit

REC-I02 Stored balance equals ledger balance after transfer

REC-I03 Stored balance equals ledger balance after reversal

REC-I04 Reconciliation handles zero-balance account with financial history

REC-I05 Reconciliation detects intentionally corrupted stored balance

REC-I06 Reconciliation does not automatically change corrupted data
```

The corruption in `REC-I05` should be introduced directly by test infrastructure because normal application behavior must not allow it.

---

# 36. API Integration Tests

The API should also receive integration coverage using:

```text
WebApplicationFactory<Program>
```

with application services wired to the PostgreSQL Testcontainer.

These tests verify:

```text
HTTP
routing
serialization
request validation
application wiring
error mapping
database integration
```

They are broader than repository integration tests but do not replace the small external E2E suite.

---

# 37. HTTP Contract Tests

Required response behavior includes:

```text
400 Bad Request
404 Not Found
409 Conflict
422 Unprocessable Entity
```

Examples:

```text
API-I01 Invalid JSON returns 400

API-I02 Missing required field returns 400

API-I03 Unknown source account returns 404

API-I04 Unknown transfer returns 404

API-I05 Insufficient funds returns 422

API-I06 Blocked source returns 422

API-I07 Closed destination returns 422

API-I08 Idempotency payload conflict returns 409

API-I09 Successful transfer returns expected response model
```

Error responses should also verify:

```text
error code
message
relevant details
```

not only HTTP status.

---

# 38. End-to-End Tests

E2E tests validate the application from the external HTTP boundary against a real running application and PostgreSQL database.

The environment should be close to:

```text
HTTP Client
    │
    ▼
LedgerGuard API
    │
    ▼
Application
    │
    ▼
Domain
    │
    ▼
EF Core
    │
    ▼
PostgreSQL
```

The E2E suite should remain small.

It exists to prove that the entire application is correctly connected, not to repeat every business rule already covered by unit and integration tests.

---

# 39. E2E Environment

Preferred environment:

```text
Docker Compose

LedgerGuard API
+
PostgreSQL
```

Tests send real HTTP requests to the running API.

The E2E project should not:

- directly call repositories;
- directly modify domain objects;
- bypass the HTTP interface for assertions about user-visible flows.

Database inspection may be used only when a behavior cannot be observed through public APIs and the test explicitly targets persistence integrity.

---

# 40. Required E2E Flows

## E2E-001 — Complete Transfer

```text
Create Alice
        ↓
Create Bob
        ↓
Deposit BRL 1,000 into Alice
        ↓
Transfer BRL 250 Alice -> Bob
        ↓
Get Alice
        ↓
Get Bob
        ↓
Get ledger
```

Expected:

```text
Alice = 750
Bob   = 250
```

Every financial transaction must be balanced.

---

## E2E-002 — Insufficient Funds

```text
Create Alice
Create Bob
Deposit 100 into Alice
Attempt transfer 100.01
```

Expected:

```text
422
Alice = 100
Bob = 0
no transfer committed
no transfer ledger entries
```

---

## E2E-003 — Idempotent Transfer Retry

```text
Create valid transfer with Key ABC
        ↓
receive success
        ↓
repeat exact HTTP request with Key ABC
```

Expected:

```text
same logical transfer
balances changed once
ledger written once
```

---

## E2E-004 — Idempotency Conflict

First:

```text
Key = ABC
A -> B = 100
```

Then:

```text
Key = ABC
A -> B = 200
```

Expected:

```text
409 Conflict
```

Only the original operation exists.

---

## E2E-005 — Complete Reversal

```text
Create transfer
        ↓
Reverse transfer
        ↓
Verify balances
        ↓
Verify original ledger
        ↓
Verify reversal ledger
```

Original financial history must remain visible.

---

## E2E-006 — Failed Reversal

```text
A -> B = 100
B -> C = 80

B = 20

Attempt reversal of A -> B
```

Expected:

```text
reversal rejected
balances unchanged by reversal
original transfer preserved
no compensating ledger transaction
```

---

# 41. Test Data Builders

Tests should avoid large repetitive object initialization.

Use test builders when they improve readability.

Example:

```csharp
var account = AccountBuilder
    .AnActiveAccount()
    .WithBalance(100m)
    .Build();
```

Useful builders may include:

```text
AccountBuilder
TransferBuilder
LedgerTransactionBuilder
```

Builders should only exist in test projects.

They must not leak into production code.

---

# 42. Test Data Rules

Use explicit values when the value matters to the scenario.

Good:

```text
Balance = 100
Transfer = 100.01
```

because the one-cent boundary is meaningful.

Random generated values should only be used when randomness is part of the test strategy.

Do not use random values merely to make test data look realistic.

---

# 43. Time in Tests

Business code should not depend directly on:

```csharp
DateTime.UtcNow
```

when the exact timestamp is part of behavior being tested.

Prefer an application abstraction such as:

```csharp
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
```

Tests can then use a deterministic time.

Example:

```text
2026-08-17T12:00:00Z
```

This allows assertions for:

```text
CreatedAt
ReversedAt
```

without flaky timing checks.

---

# 44. Assertions

Assertions should describe the relevant business outcome.

Prefer:

```csharp
Assert.That(result.IsSuccess, Is.True);
Assert.That(source.Balance, Is.EqualTo(750m));
Assert.That(destination.Balance, Is.EqualTo(250m));
```

For related assertions, NUnit's multiple-assert pattern may be used:

```csharp
Assert.Multiple(() =>
{
    Assert.That(source.Balance, Is.EqualTo(750m));
    Assert.That(destination.Balance, Is.EqualTo(250m));
    Assert.That(entries.Sum(x => x.Amount), Is.EqualTo(0m));
});
```

Avoid assertions about private implementation details.

---

# 45. Mocking Strategy

Mocking should be limited.

Do not mock:

```text
Money
Account
Transfer
LedgerEntry
LedgerTransaction
```

These are real domain objects and should be tested directly.

Do not mock PostgreSQL when validating:

```text
transactions
constraints
idempotency uniqueness
optimistic concurrency
rollback
```

Mocks or fakes may be useful for true application boundaries such as:

```text
IClock
```

The project should prefer real domain behavior over heavily mocked interaction tests.

---

# 46. Parallel Test Execution

Pure unit tests should be safe to execute in parallel.

Database tests require more care.

Integration tests that share database state must either:

- use isolated databases/schemas; or
- control parallel execution; or
- guarantee unique data with safe cleanup.

Tests must not become flaky because another test modified shared database state.

Concurrency tests intentionally create parallel business operations internally, but the test fixture itself should still be isolated from unrelated tests.

---

# 47. Flaky Tests

A flaky test is considered a defect.

Do not solve flaky tests by adding arbitrary delays such as:

```csharp
await Task.Delay(1000);
```

Concurrency synchronization should use explicit coordination when necessary.

Examples:

```text
TaskCompletionSource
Barrier
SemaphoreSlim
controlled transaction points
```

Tests must wait for known state transitions, not guessed timing.

---

# 48. Concurrency Test Repetition

Concurrency bugs may depend on scheduling.

Critical concurrency scenarios should be capable of repeated execution.

Example development run:

```bash
dotnet test --filter TestCategory=Concurrency
```

Repeated local or CI execution may be used to increase confidence.

However, repetition is not a substitute for deterministic synchronization.

A test should intentionally cause the competing operations to overlap whenever possible.

---

# 49. Coverage

Coverage is a supporting metric, not the objective of the test suite.

Suggested goals:

```text
Domain line coverage: >= 90%
Domain branch coverage: >= 90%

Overall line coverage: >= 80%
```

Critical financial rules should aim for complete meaningful branch coverage even if the repository-wide percentage is lower.

A high coverage percentage does not compensate for missing scenarios such as:

```text
concurrency
idempotency
rollback
boundary values
```

---

# 50. Required Rule Coverage

Every rule identified in:

```text
business-rules.md
```

must have at least one automated test unless the rule is explicitly architectural or documentation-only.

Critical rules should have more than one test when they contain multiple boundaries or failure modes.

Example:

```text
TRF-008 Source must have sufficient balance
```

requires at least:

```text
below balance
exact balance
one cent above balance
```

---

# 51. Test Traceability Matrix

The project should maintain traceability using test names and descriptions rather than a large manually maintained spreadsheet.

Example:

```text
TRF-008 — Source must have sufficient funds

Unit
├── Transfer_below_available_balance_should_succeed
├── Transfer_with_exact_available_balance_should_succeed
└── Transfer_exceeding_balance_by_one_cent_should_fail

Integration
└── Failed_transfer_should_not_persist_any_financial_change

E2E
└── Insufficient_funds_should_return_422_and_preserve_balances
```

This pattern should be applied to important invariants.

---

# 52. Critical Invariant Coverage

The following invariants require coverage at more than one test level.

## INV-001 — Conservation of Money

Unit:

```text
transfer preserves combined balance
```

Integration:

```text
persisted source + destination balances preserve total
```

E2E:

```text
critical transfer flow preserves expected balances
```

---

## INV-002 — Non-Negative Customer Balance

Unit:

```text
insufficient balance rejected
```

Integration:

```text
concurrent transfer race cannot overdraw
database constraint rejects negative balance
```

E2E:

```text
insufficient-funds flow
```

---

## INV-003 — Balanced Ledger

Unit:

```text
ledger entries sum to zero
```

Integration:

```text
persisted entries sum to zero
```

E2E:

```text
critical financial flows expose balanced ledger history
```

---

## INV-006 — Atomicity

Unit:

```text
domain state unchanged after rejected command
```

Integration:

```text
database failures rollback every persisted change
```

E2E:

```text
failed business flow exposes no partial result
```

---

## INV-008 — One Logical Request, One Effect

Unit:

```text
where pure application logic can be tested
```

Integration:

```text
database-backed idempotency
concurrent duplicates
```

E2E:

```text
HTTP retry using same Idempotency-Key
```

---

# 53. CI Strategy

Pull requests should run:

```text
Restore
   ↓
Build
   ↓
Unit Tests
   ↓
Integration Tests
   ↓
E2E Tests
   ↓
Coverage
```

A failing test blocks the pipeline.

Warnings related to test discovery or skipped tests should be visible in CI output.

---

# 54. CI Test Commands

All tests:

```bash
dotnet test
```

Unit:

```bash
dotnet test --filter TestCategory=Unit
```

Integration:

```bash
dotnet test --filter TestCategory=Integration
```

E2E:

```bash
dotnet test --filter TestCategory=E2E
```

Concurrency:

```bash
dotnet test --filter TestCategory=Concurrency
```

---

# 55. Definition of Done for a Business Rule

A business rule is not considered implemented simply because production code exists.

A rule is complete when:

- the rule is documented in `business-rules.md`;
- relevant happy paths are tested;
- relevant invalid paths are tested;
- relevant boundary values are tested;
- domain tests pass;
- persistence-dependent behavior has integration coverage;
- concurrency-sensitive behavior has concurrency integration tests;
- critical public flows have E2E coverage where appropriate;
- no test relies on execution order;
- no known flaky test exists.

---

# 56. Definition of Done for a Financial Operation

A Deposit, Transfer, or Reversal feature is complete only when the test suite proves:

```text
valid operation succeeds

invalid operation fails

minimum valid amount works

maximum relevant boundary works

failed operation changes nothing

ledger entries are correct

ledger transaction balances to zero

stored balance matches ledger balance

idempotent retry produces one effect

database transaction is atomic

concurrent execution preserves invariants
```

For Transfers and Reversals, relevant E2E workflows must also pass.

---

# 57. Initial Testing Implementation Order

Testing should grow alongside implementation.

Recommended order:

## Phase 1 — Money

```text
Money unit tests
```

Then implement `Money`.

---

## Phase 2 — Accounts

```text
Account state and boundary unit tests
```

Then implement account behavior.

---

## Phase 3 — Ledger

```text
Ledger invariant unit tests
```

Then implement double-entry structures.

---

## Phase 4 — Deposits

```text
Deposit domain tests
```

Then persistence tests.

---

## Phase 5 — Transfers

Start with:

```text
happy path
insufficient funds
exact balance
one cent over balance
blocked account
closed account
self-transfer
ledger balance
```

Then implement the transfer use case.

---

## Phase 6 — PostgreSQL

Introduce Testcontainers and verify:

```text
migrations
repositories
constraints
transactions
```

---

## Phase 7 — Atomicity

Add controlled failures and prove rollback.

---

## Phase 8 — Idempotency

Add sequential duplicate scenarios first.

Then add concurrent duplicates.

---

## Phase 9 — Concurrency

Implement and test:

```text
transfer vs transfer
deposit vs deposit
transfer vs reversal
transfer vs close
transfer vs block
```

---

## Phase 10 — Reversals

Complete domain, integration, and concurrency scenarios.

---

## Phase 11 — E2E

Add only the critical external workflows after the underlying behavior is already well tested.

---

# 58. Final Principle

The LedgerGuard test suite should make the following statement defensible:

> The financial rules are not only implemented; they are continuously verified under normal execution, boundary conditions, retries, failures, persistence, and concurrency.

A test is valuable when it increases confidence in a real business guarantee.

The objective is not to maximize the number of tests.

The objective is to make incorrect financial behavior difficult to introduce without the test suite detecting it.
