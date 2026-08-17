# Business Rules

This document defines the business behavior and financial invariants of LedgerGuard.

The rules in this file are the source of truth for the initial implementation. Each rule has an identifier so it can be referenced by automated tests and future documentation.

---

# 1. Scope and Definitions

## 1.1 Supported Operations

The initial version supports:

- customer account creation;
- account blocking and unblocking;
- account closing;
- deposits;
- transfers between customer accounts;
- full transfer reversals;
- immutable double-entry ledger records;
- idempotent financial commands;
- concurrent financial operations;
- balance reconciliation.

## 1.2 Supported Currency

The initial version supports only:

```text
BRL
```

Multi-currency transfers and foreign exchange are outside the initial scope.

## 1.3 Account Types

LedgerGuard distinguishes two kinds of ledger accounts:

### Customer Account

An account exposed to customers.

Customer accounts:

- cannot have a negative balance;
- can be Active, Blocked, or Closed;
- participate in deposits, transfers, and reversals.

### System Settlement Account

An internal technical ledger account used as the counterparty for money entering the system through deposits.

It is not exposed as a customer account.

Unlike customer accounts, it may carry a negative ledger balance because it represents the external source of deposited funds.

Example:

```text
Deposit BRL 100

System Settlement    -100
Customer Account     +100
-------------------------
Total                    0
```

---

# 2. Monetary Limits

To keep application and persistence behavior explicit, the MVP uses the following monetary range:

```text
Minimum positive amount: BRL 0.01

Maximum monetary value:
BRL 99,999,999,999,999,999.99
```

The persistence layer should use an exact decimal representation compatible with this range, such as:

```text
NUMERIC(20, 2)
```

The domain must reject values outside the supported range before attempting to persist them.

---

# 3. Global Financial Invariants

These rules must remain true regardless of which operation is executed.

## INV-001 — Conservation of Money

An internal transfer cannot create or destroy money.

For any transfer:

```text
total customer balance before
==
total customer balance after
```

Example:

```text
Before

A = 1,000
B =   500
---------
  = 1,500

Transfer A -> B = 300

After

A =   700
B =   800
---------
  = 1,500
```

Deposits are different because money enters LedgerGuard from an external source. Their corresponding system settlement entry preserves double-entry balance.

---

## INV-002 — Non-Negative Customer Balance

A customer account must never have:

```text
balance < 0
```

This must remain true under sequential and concurrent execution.

The internal System Settlement Account is excluded from this rule.

---

## INV-003 — Balanced Ledger

Every posted ledger transaction must satisfy:

```text
SUM(entries.Amount) == 0
```

The invariant applies independently per currency.

Since the MVP supports only BRL:

```text
SUM(BRL entries) == 0
```

---

## INV-004 — At Least Two Ledger Entries

Every financial transaction must contain at least two entries.

A single-sided financial transaction is invalid.

---

## INV-005 — Financial History Is Immutable

Once committed:

- a ledger transaction cannot be deleted;
- a ledger entry cannot be deleted;
- a ledger entry amount cannot be changed;
- a ledger entry account cannot be changed;
- the financial meaning of a historical transaction cannot be rewritten.

Corrections must be represented through new compensating transactions.

---

## INV-006 — Atomic Financial Operations

A financial operation must be all-or-nothing.

A transfer cannot leave:

```text
source debited
destination unchanged
```

or:

```text
destination credited
source unchanged
```

A deposit cannot update the customer balance without creating its ledger transaction.

A reversal cannot partially undo the original transfer.

---

## INV-007 — Failed Operations Have No Financial Effect

If an operation fails before completion:

- customer balances remain unchanged;
- no partial ledger entries remain;
- no partial financial transaction remains;
- no partially completed transfer or reversal remains.

Stable idempotency records for rejected operations are allowed because they do not represent a financial effect.

---

## INV-008 — One Logical Request, At Most One Financial Effect

Retries, duplicated HTTP requests, concurrent duplicate requests, or client timeouts must not cause the same logical financial operation to be executed twice.

---

## INV-009 — Stored Balance Matches Ledger Balance

For every customer account:

```text
Account.CurrentBalance
==
SUM(posted LedgerEntry.Amount for account)
```

This must be true after every successful financial transaction.

---

## INV-010 — Concurrency Cannot Weaken Business Rules

Any business rule that is valid sequentially must remain valid when requests execute concurrently.

Concurrency is never a valid reason for:

- negative balances;
- duplicated operations;
- lost ledger entries;
- inconsistent balances;
- broken double-entry transactions.

---

# 4. Money Rules

## MON-001 — Currency Is Required

Every monetary value must have a currency.

A money value without a currency is invalid.

---

## MON-002 — Only BRL Is Supported

Valid:

```text
BRL 100.00
```

Invalid:

```text
USD 100.00
EUR 100.00
```

---

## MON-003 — Decimal Arithmetic Is Required

Monetary values must use exact decimal arithmetic.

Floating-point types must not be used to represent financial values.

---

## MON-004 — Maximum Two Decimal Places

Valid:

```text
1
1.0
1.00
100.25
0.01
```

Invalid:

```text
1.001
10.999
0.001
```

Values with more than two decimal places must be rejected rather than silently rounded.

---

## MON-005 — Positive Financial Commands

Deposits, transfers, and reversal amounts must be strictly positive.

Minimum valid amount:

```text
0.01
```

Invalid:

```text
0
-0.01
-100
```

---

## MON-006 — Maximum Supported Value

A single money value cannot exceed:

```text
99,999,999,999,999,999.99
```

The exact maximum is valid.

Any greater value is rejected.

---

## MON-007 — No Silent Overflow

If applying a valid operation would make an account balance exceed the supported maximum monetary value, the operation must be rejected without changing state.

Example:

```text
Destination balance:
99,999,999,999,999,999.99

Incoming transfer:
0.01
```

Result:

```text
rejected
```

---

# 5. Customer Account Rules

Customer account statuses:

```text
Active
Blocked
Closed
```

## ACC-001 — Account Identifier

Every account receives a unique system-generated identifier.

Clients cannot choose or overwrite the identifier.

---

## ACC-002 — Owner Name Is Required

`OwnerName` must contain at least one non-whitespace character.

Whitespace around the name is trimmed.

Valid:

```text
Alice
Maria Silva
```

Invalid:

```text
""
"   "
```

---

## ACC-003 — Owner Name Length

After trimming:

```text
1 <= OwnerName.Length <= 120
```

Exactly 120 characters is valid.

121 characters is invalid.

Different accounts may have the same owner name.

Owner name is not a unique identifier.

---

## ACC-004 — New Accounts Start With Zero Balance

A newly created customer account must have:

```text
CurrentBalance = BRL 0.00
```

Initial funds cannot be supplied through account creation.

Funds must enter through a deposit.

---

## ACC-005 — New Accounts Start Active

A newly created customer account starts with:

```text
Status = Active
```

---

## ACC-006 — Account Currency

Customer accounts use:

```text
BRL
```

The account currency cannot be changed after creation.

---

## ACC-007 — Active Accounts Can Send Funds

An Active account may be the source of a transfer when all other rules are satisfied.

---

## ACC-008 — Active Accounts Can Receive Funds

An Active account may receive:

- deposits;
- transfers;
- reversal credits.

---

## ACC-009 — Blocked Accounts Cannot Initiate Customer Transfers

A Blocked customer account cannot be the source of a regular transfer.

Blocking affects customer-initiated outgoing transfers.

---

## ACC-010 — Blocked Accounts Can Receive Funds

A Blocked account may still receive:

- deposits;
- transfers;
- reversal credits.

---

## ACC-011 — Blocked Accounts Can Be Unblocked

Valid transition:

```text
Blocked -> Active
```

Unblocking does not alter balance or ledger history.

---

## ACC-012 — Active Accounts Can Be Blocked

Valid transition:

```text
Active -> Blocked
```

Blocking does not alter balance or ledger history.

---

## ACC-013 — Repeated Block Is Rejected

Invalid transition:

```text
Blocked -> Blocked
```

The request produces no state change.

---

## ACC-014 — Repeated Unblock Is Rejected

Invalid transition:

```text
Active -> Active
```

when the requested operation is specifically "unblock".

---

## ACC-015 — Closed Accounts Cannot Send Funds

A Closed customer account cannot initiate transfers.

---

## ACC-016 — Closed Accounts Cannot Receive Funds

A Closed customer account cannot receive:

- deposits;
- transfers;
- normal financial credits.

---

## ACC-017 — Account Must Have Zero Balance Before Closing

Valid:

```text
Balance = 0.00
Active -> Closed
```

Valid:

```text
Balance = 0.00
Blocked -> Closed
```

Invalid:

```text
Balance = 0.01
Active -> Closed
```

Invalid:

```text
Balance > 0
Blocked -> Closed
```

---

## ACC-018 — Closed Is a Terminal State

After:

```text
Active -> Closed
```

or:

```text
Blocked -> Closed
```

the account cannot transition back to Active or Blocked.

---

## ACC-019 — Repeated Close Is Rejected

Attempting to close an already Closed account has no state effect and must be reported as an invalid state transition.

---

## ACC-020 — Balance Cannot Be Set Directly

There is no business operation equivalent to:

```text
SetBalance
```

or:

```text
EditBalance
```

Every balance change must be explained by ledger entries.

---

## ACC-021 — Account Cannot Be Deleted

Customer accounts with financial history cannot be deleted.

The MVP does not support physical account deletion.

---

# 6. Deposit Rules

A deposit represents funds entering LedgerGuard from an external source.

Each deposit uses the internal System Settlement Account as its counterparty.

## DEP-001 — Destination Account Must Exist

Depositing into a nonexistent customer account is rejected.

---

## DEP-002 — Deposit Amount Must Be Positive

The valid range is:

```text
0.01
through
99,999,999,999,999,999.99
```

subject to the destination balance limit.

---

## DEP-003 — Destination Cannot Be Closed

Deposits into Closed accounts are rejected.

---

## DEP-004 — Deposits Into Active Accounts Are Allowed

Happy path:

```text
Account balance = 100
Deposit = 50
Final balance = 150
```

---

## DEP-005 — Deposits Into Blocked Accounts Are Allowed

Blocking prevents customer-initiated outgoing transfers but does not prevent incoming funds.

---

## DEP-006 — Deposit Can Reach Maximum Balance Exactly

Given:

```text
Current balance = 99,999,999,999,999,999.98
Deposit         = 0.01
```

Final balance:

```text
99,999,999,999,999,999.99
```

This is valid.

---

## DEP-007 — Deposit Cannot Overflow Maximum Balance

Given:

```text
Current balance = 99,999,999,999,999,999.99
Deposit         = 0.01
```

The deposit is rejected.

No balance or ledger change occurs.

---

## DEP-008 — Deposit Creates Balanced Ledger Entries

For:

```text
Deposit BRL 100
```

the ledger must contain:

```text
System Settlement    -100
Customer Account     +100
-------------------------
Total                    0
```

---

## DEP-009 — Deposit Is Atomic

The following must be committed together:

- customer balance increase;
- ledger transaction;
- settlement ledger entry;
- customer ledger entry;
- successful idempotency result.

---

## DEP-010 — Deposit Requires Idempotency

Every deposit command requires an idempotency key.

Duplicate retries cannot credit the customer twice.

---

# 7. Transfer Rules

A transfer moves funds from one customer account to another customer account.

## TRF-001 — Source Account Must Exist

A transfer with an unknown source account is rejected.

---

## TRF-002 — Destination Account Must Exist

A transfer with an unknown destination account is rejected.

---

## TRF-003 — Source and Destination Must Be Different

Invalid:

```text
A -> A
```

No self-transfer is allowed.

---

## TRF-004 — Transfer Amount Must Be Positive

Minimum:

```text
BRL 0.01
```

Zero and negative values are rejected.

---

## TRF-005 — Transfer Must Use BRL

Any unsupported currency is rejected.

---

## TRF-006 — Source Must Be Active

Allowed:

```text
Active -> Active
Active -> Blocked
```

Not allowed:

```text
Blocked -> Active
Blocked -> Blocked
Closed -> any
```

for a regular customer transfer.

---

## TRF-007 — Destination Cannot Be Closed

An Active or Blocked destination may receive a transfer.

A Closed destination may not.

---

## TRF-008 — Source Must Have Sufficient Balance

Given:

```text
Balance = 100.00
```

Valid:

```text
Transfer = 0.01
Transfer = 50.00
Transfer = 100.00
```

Invalid:

```text
Transfer = 100.01
```

---

## TRF-009 — Exact-Balance Transfer Is Valid

A customer may transfer the entire available balance.

Example:

```text
Balance before = 100
Transfer       = 100
Balance after  = 0
```

---

## TRF-010 — Destination Balance Cannot Overflow

A transfer is rejected if:

```text
destination balance + transfer amount
>
maximum supported balance
```

The source must not be debited.

---

## TRF-011 — Transfer Debits Source Exactly Once

For a BRL 100 transfer:

```text
source balance decreases by exactly 100
```

No duplicate debit may occur.

---

## TRF-012 — Transfer Credits Destination Exactly Once

For a BRL 100 transfer:

```text
destination balance increases by exactly 100
```

No duplicate credit may occur.

---

## TRF-013 — Debit and Credit Amounts Must Match

For every successful transfer:

```text
abs(source ledger entry)
==
destination ledger entry
==
transfer amount
```

---

## TRF-014 — Transfer Preserves Combined Customer Balance

For source `A` and destination `B`:

```text
A_before + B_before
==
A_after + B_after
```

---

## TRF-015 — Transfer Creates One Ledger Transaction

A single successful transfer produces exactly one logical ledger transaction.

---

## TRF-016 — Transfer Creates Exactly Two Customer Ledger Entries

For the initial model:

```text
Source       -Amount
Destination  +Amount
```

No fee or third-party account exists in the MVP.

---

## TRF-017 — Successful Transfer Has a Unique Identifier

Two distinct transfers cannot share the same transfer identifier.

---

## TRF-018 — Completed Transfer Cannot Be Edited

After completion:

- source cannot be changed;
- destination cannot be changed;
- amount cannot be changed;
- currency cannot be changed.

Corrections require reversal plus a new transfer.

---

## TRF-019 — Completed Transfer Cannot Be Deleted

Financial history is retained.

---

## TRF-020 — Transfer Is Atomic

The following belong to the same atomic outcome:

- source balance update;
- destination balance update;
- transfer creation;
- ledger transaction creation;
- source ledger entry;
- destination ledger entry;
- successful idempotency record.

---

# 8. Transfer Reversal Rules

A reversal is a full compensating transaction for a previously completed transfer.

Partial reversals are outside the MVP.

Original:

```text
Alice   -100
Bob     +100
```

Reversal:

```text
Alice   +100
Bob     -100
```

## REV-001 — Original Transfer Must Exist

A reversal referencing an unknown transfer is rejected.

---

## REV-002 — Only Completed Transfers Can Be Reversed

A transfer that never completed cannot be reversed.

---

## REV-003 — Reversal Is Full

The reversal amount is always exactly equal to the original transfer amount.

Clients do not provide a custom reversal amount.

---

## REV-004 — Partial Reversal Is Not Supported

Examples such as:

```text
Original = 100
Reverse  = 25
```

are not valid in the MVP.

---

## REV-005 — A Transfer Can Be Reversed Only Once

After a successful reversal, another reversal of the same transfer is rejected.

---

## REV-006 — Reversal Preserves Original Financial History

The original transfer and its ledger entries remain unchanged.

---

## REV-007 — Reversal Creates New Ledger Entries

The reversal is represented as a new ledger transaction with opposite financial effects.

---

## REV-008 — Reversal Must Balance

For original:

```text
A -100
B +100
```

reversal:

```text
A +100
B -100
```

must satisfy:

```text
SUM(reversal entries) == 0
```

---

## REV-009 — Original Destination Must Have Sufficient Funds

The original destination becomes the debited account during reversal.

Example:

```text
Original transfer:
Alice -> Bob = 100

Bob current balance = 100
```

Reversal is valid.

If:

```text
Bob current balance = 99.99
```

the reversal is rejected.

No account may become negative.

---

## REV-010 — Exact-Balance Reversal Is Valid

If the original destination has exactly the amount required for reversal, the reversal is allowed and may leave the account at zero.

---

## REV-011 — Blocked Accounts Do Not Prevent System Reversal

A reversal is a system compensating operation, not a new customer-initiated transfer.

Therefore a Blocked account may be:

- debited as the original destination, if it has sufficient balance;
- credited as the original source.

Blocking does not prevent a valid reversal.

---

## REV-012 — Closed Accounts Cannot Participate in Reversal

A reversal involving a Closed customer account is rejected.

This preserves the rule that Closed accounts do not receive or send funds.

---

## REV-013 — Reversal Cannot Overflow Original Source Balance

If crediting the original source would exceed the maximum supported account balance, the reversal is rejected.

No partial debit from the original destination may occur.

---

## REV-014 — Reversal Is Atomic

The following must commit together:

- debit original destination;
- credit original source;
- create reversal record;
- create reversal ledger transaction;
- create compensating ledger entries;
- mark original transfer as reversed;
- persist successful idempotency result.

---

## REV-015 — Reversal Requires Idempotency

Retries of the same reversal command cannot reverse the same transfer multiple times.

---

# 9. Idempotency Rules

Idempotency protects financial commands from duplicate execution.

It applies to:

```text
Deposit
Transfer
Reversal
```

## IDE-001 — Idempotency Key Is Required

A financial command without an idempotency key is rejected before financial execution.

---

## IDE-002 — Key Cannot Be Empty

Invalid:

```text
""
"   "
```

Leading and trailing whitespace should not be accepted as meaningful key content.

---

## IDE-003 — Maximum Key Length

Maximum:

```text
255 characters
```

Exactly 255 characters is valid.

256 characters is invalid.

---

## IDE-004 — Keys Are Case-Sensitive

These are different keys:

```text
transfer-abc
Transfer-ABC
```

---

## IDE-005 — Idempotency Scope

Uniqueness is scoped by:

```text
(OperationType, IdempotencyKey)
```

Therefore:

```text
Transfer + ABC
```

and:

```text
Deposit + ABC
```

are different idempotency identities.

---

## IDE-006 — Same Key + Same Payload Returns Same Stable Result

Once a stable result has been recorded, replaying the same operation with:

- the same operation type;
- the same idempotency key;
- the same canonical payload;

must return the original result without executing the financial operation again.

---

## IDE-007 — Same Key + Different Payload Is a Conflict

Example:

First request:

```text
Key: ABC
Transfer: A -> B, BRL 100
```

Second request:

```text
Key: ABC
Transfer: A -> B, BRL 200
```

The second request is rejected.

No second transfer is created.

---

## IDE-008 — Same Key + Different Accounts Is a Conflict

First:

```text
Key: ABC
A -> B, BRL 100
```

Second:

```text
Key: ABC
A -> C, BRL 100
```

The second request is rejected.

---

## IDE-009 — Duplicate Successful Request Has No New Financial Effect

For a successfully completed request, retries must not:

- change balances again;
- create another transfer;
- create another ledger transaction;
- create duplicate ledger entries.

---

## IDE-010 — Stable Business Rejections Are Remembered

Once execution has started and a deterministic business result is produced, the result may be stored under the idempotency key.

Example:

```text
Balance = 50

Key = ABC
Transfer = 100
Result = Insufficient Funds
```

If funds are deposited later and the exact request is retried with:

```text
Key = ABC
```

the original rejected result is returned.

To attempt a new logical transfer, the client must use a new idempotency key.

---

## IDE-011 — Pre-Execution Validation Failures Are Not Stored

Requests rejected before business execution because of malformed input do not reserve the idempotency key.

Examples:

- missing required field;
- malformed identifier;
- invalid JSON;
- unsupported request structure;
- missing idempotency key itself.

After correcting the request, the client may reuse the previously unused key.

---

## IDE-012 — Transient Infrastructure Failure Is Not a Stable Result

If the application cannot determine a committed final outcome because of a transient infrastructure failure, the idempotency key must remain safe for retry.

The retry must never duplicate a committed financial operation.

The financial operation and successful idempotency result therefore must share the same atomic persistence boundary.

---

## IDE-013 — Concurrent Duplicate Requests Produce One Outcome

If multiple requests with the same idempotency identity arrive simultaneously:

```text
exactly one logical operation
```

may execute successfully.

Other callers receive the same stable result once available.

---

# 10. Concurrency Rules

Concurrency must preserve the same outcome constraints as valid sequential execution.

## CON-001 — Concurrent Withdrawals Cannot Overdraw

Given:

```text
Source balance = 100
```

Concurrent:

```text
Transfer A = 80
Transfer B = 80
```

Expected:

```text
one succeeds
one is rejected after current state is reevaluated
final balance = 20
```

Invalid:

```text
both succeed
final balance = -60
```

---

## CON-002 — Concurrent Transfers May Both Succeed When Funds Are Sufficient

Given:

```text
Source balance = 200
```

Concurrent:

```text
Transfer A = 80
Transfer B = 80
```

Expected final balance:

```text
40
```

Both transfers may succeed.

---

## CON-003 — Concurrency Conflict Requires Fresh Business Validation

After a detected concurrent update, the operation must not simply replay the previous write.

It must reevaluate the current domain state.

Example:

```text
Initial balance = 100

T1 validates transfer 80
T2 validates transfer 80

T1 commits
balance = 20
```

T2 must validate again against:

```text
balance = 20
```

and fail for insufficient funds.

---

## CON-004 — No Lost Updates

If multiple valid operations update the same account, successful operations must all be reflected in the final balance and ledger.

---

## CON-005 — Concurrent Deposits Must Not Lose Credits

Given:

```text
Initial balance = 0
```

Concurrent deposits:

```text
+50
+70
```

Expected final balance:

```text
120
```

if both are distinct valid operations.

---

## CON-006 — Concurrent Duplicate Deposits Credit Once

Given two concurrent deposit requests with the same idempotency identity:

```text
Deposit = 50
```

Expected final balance increase:

```text
+50
```

not:

```text
+100
```

---

## CON-007 — Concurrent Duplicate Transfers Execute Once

Two simultaneous identical transfer requests using the same idempotency identity produce one transfer and one set of ledger entries.

---

## CON-008 — Concurrent Reversals Execute Once

If two requests attempt to reverse the same completed transfer simultaneously:

```text
at most one reversal succeeds
```

Only one compensating ledger transaction may exist.

---

## CON-009 — Transfer and Reversal Race Must Preserve Valid State

If a transfer reversal and another outgoing operation from the original destination race with each other, the final state must correspond to a valid serialization of operations.

Example:

```text
Bob balance = 100

Operation A:
reverse original transfer, requiring Bob -100

Operation B:
Bob transfers 80 elsewhere
```

Valid outcomes include:

### Outcome 1

Reversal commits first:

```text
Bob = 0
Operation B fails
```

### Outcome 2

Transfer B commits first:

```text
Bob = 20
Reversal fails for insufficient funds
```

Invalid:

```text
both succeed
Bob becomes negative
```

---

## CON-010 — Account Close and Incoming Transfer Race

If closing an account and transferring funds into it occur concurrently, the result must be equivalent to one valid ordering.

Possible outcomes:

### Transfer commits first

```text
balance > 0
close fails
```

### Close commits first

```text
status = Closed
transfer fails
```

Invalid:

```text
status = Closed
balance > 0 because transfer also committed
```

---

## CON-011 — Block and Outgoing Transfer Race

If blocking an account races with an outgoing transfer, the result must represent a valid order.

Possible outcomes:

### Transfer commits first

Transfer succeeds, then account becomes Blocked.

### Block commits first

Transfer is rejected because source is Blocked.

No partial transaction is allowed.

---

## CON-012 — Bounded Retry

Concurrency conflicts may be retried only a bounded number of times.

Retries must never continue indefinitely.

If a safe outcome cannot be reached, the command ends with a concurrency conflict and no partial financial state.

---

# 11. Ledger Rules

## LED-001 — Every Financial Operation Has a Ledger Transaction

Successful:

- deposit;
- transfer;
- reversal;

must each have a corresponding ledger transaction.

---

## LED-002 — Ledger Entry Amount Cannot Be Zero

Financial ledger entries created by the MVP must represent an actual movement.

Invalid:

```text
Account A  0.00
Account B  0.00
```

---

## LED-003 — Entry Currency Matches Transaction Currency

All entries belonging to a ledger transaction must use the same currency.

---

## LED-004 — Entries Reference Existing Ledger Accounts

A ledger entry cannot reference a nonexistent account.

---

## LED-005 — Entries Reference One Existing Ledger Transaction

Every ledger entry belongs to exactly one ledger transaction.

---

## LED-006 — Deposit Ledger Shape

A deposit creates exactly two entries:

```text
System Settlement    -Amount
Customer Account     +Amount
```

---

## LED-007 — Transfer Ledger Shape

A transfer creates exactly two entries:

```text
Source       -Amount
Destination  +Amount
```

---

## LED-008 — Reversal Ledger Shape

A reversal creates exactly two entries opposite to the original transfer:

```text
Original Source        +Amount
Original Destination   -Amount
```

---

## LED-009 — Ledger Transaction Is Immutable After Commit

Once committed, it cannot be repurposed or edited into another financial event.

---

## LED-010 — Historical Entries Remain Queryable

Reversing an operation does not hide or replace the original entries.

Both original and compensating entries remain part of financial history.

---

## LED-011 — No Orphan Financial Operation

A completed Deposit, Transfer, or Reversal cannot exist without its corresponding ledger transaction.

---

## LED-012 — No Orphan Ledger Transaction

A ledger transaction representing a Deposit, Transfer, or Reversal must reference the corresponding business operation.

---

# 12. Reconciliation Rules

Reconciliation verifies that the persisted account projection matches the ledger.

## REC-001 — Reconciliation Is Read-Only

Running reconciliation must never modify balances or ledger history automatically.

---

## REC-002 — Customer Balance Formula

For a customer account:

```text
LedgerBalance = SUM(all committed ledger entries for account)
```

---

## REC-003 — Healthy Account

If:

```text
CurrentBalance == LedgerBalance
```

the account is reconciled.

---

## REC-004 — Mismatch Is Reported

If:

```text
CurrentBalance != LedgerBalance
```

the system reports the discrepancy.

It does not silently overwrite either value.

---

## REC-005 — Zero-Balance Account Can Have History

An account may have:

```text
CurrentBalance = 0
```

while still containing many historical ledger entries.

Zero balance does not imply empty ledger history.

---

## REC-006 — Reversed Transfer Remains Reconciled

Original and reversal entries must naturally offset each other when no other movements exist.

Example:

```text
Original: -100
Reversal: +100
Net:         0
```

---

# 13. Time and Audit Rules

## AUD-001 — Financial Timestamps Are Generated by the System

Clients cannot choose authoritative creation timestamps for financial operations.

---

## AUD-002 — Timestamps Are Stored in UTC

Financial timestamps use UTC internally.

---

## AUD-003 — Historical CreatedAt Is Immutable

The creation timestamp of a committed financial operation cannot be rewritten.

---

## AUD-004 — Reversal References Original Transfer

A reversal must retain a direct reference to the transfer it compensates.

---

# 14. Failure Behavior

## FAI-001 — Domain Failure

Expected business failures include:

- insufficient funds;
- blocked source account;
- closed account;
- invalid account state transition;
- destination balance overflow;
- transfer already reversed;
- idempotency conflict.

They must not leave partial financial state.

---

## FAI-002 — Persistence Failure Before Commit

If persistence fails before transaction commit:

```text
no financial change is considered successful
```

All changes are rolled back.

---

## FAI-003 — Client Disconnect Does Not Define Financial Outcome

If a client disconnects after sending a request, the server-side transaction may still complete.

The client must be able to retry safely using the same idempotency key.

---

## FAI-004 — Timeout Does Not Permit Blind Re-execution

An unknown client-side outcome must be resolved through idempotent retry rather than by creating a new logical operation automatically.

---

# 15. Scenario Catalog

This section defines the minimum behavior set expected from the implementation.

---

## 15.1 Account — Happy Paths

### ACC-H01 — Create Account

Input:

```text
OwnerName = Alice
```

Expected:

```text
unique ID
OwnerName = Alice
Status = Active
Balance = BRL 0.00
```

### ACC-H02 — Trim Owner Name

Input:

```text
"  Alice  "
```

Expected stored name:

```text
Alice
```

### ACC-H03 — Block Active Account

```text
Active -> Blocked
```

No financial state changes.

### ACC-H04 — Unblock Blocked Account

```text
Blocked -> Active
```

### ACC-H05 — Close Zero-Balance Active Account

```text
Active + 0.00 -> Closed
```

### ACC-H06 — Close Zero-Balance Blocked Account

```text
Blocked + 0.00 -> Closed
```

---

## 15.2 Account — Boundary and Invalid Cases

### ACC-B01 — Owner Name Length 1

Valid.

### ACC-B02 — Owner Name Length 120

Valid.

### ACC-B03 — Owner Name Length 121

Rejected.

### ACC-B04 — Empty Owner Name

Rejected.

### ACC-B05 — Whitespace-Only Owner Name

Rejected.

### ACC-B06 — Close Balance 0.01

Rejected.

### ACC-B07 — Close Maximum Balance

Rejected.

### ACC-B08 — Reopen Closed Account

Rejected.

### ACC-B09 — Block Closed Account

Rejected.

### ACC-B10 — Unblock Closed Account

Rejected.

### ACC-B11 — Close Already Closed Account

Rejected without state change.

---

## 15.3 Deposit — Happy Paths

### DEP-H01 — Minimum Deposit

```text
Deposit = 0.01
```

Succeeds.

### DEP-H02 — Normal Deposit

```text
Deposit = 100.00
```

Succeeds.

### DEP-H03 — Deposit Into Blocked Account

Succeeds.

### DEP-H04 — Deposit Reaching Maximum Balance Exactly

Succeeds.

### DEP-H05 — Replay Successful Deposit

Same idempotency key and payload returns original result.

Balance changes once.

---

## 15.4 Deposit — Boundary and Extreme Cases

### DEP-B01 — Zero Deposit

Rejected.

### DEP-B02 — Negative Deposit

Rejected.

### DEP-B03 — Deposit With 3 Decimal Places

Rejected.

### DEP-B04 — Maximum Single Deposit Into Zero Balance

Valid:

```text
99,999,999,999,999,999.99
```

### DEP-B05 — Amount Greater Than Maximum

Rejected.

### DEP-B06 — Destination Balance Overflow by 0.01

Rejected.

### DEP-B07 — Deposit Into Closed Account

Rejected.

### DEP-B08 — Deposit Into Unknown Account

Rejected.

### DEP-B09 — Same Idempotency Key, Different Amount

Conflict.

### DEP-B10 — Same Idempotency Key, Different Destination

Conflict.

---

## 15.5 Transfer — Happy Paths

### TRF-H01 — Minimum Transfer

```text
0.01
```

between valid accounts.

### TRF-H02 — Partial Balance Transfer

```text
Source = 100
Transfer = 40
Final source = 60
```

### TRF-H03 — Exact Balance Transfer

```text
Source = 100
Transfer = 100
Final source = 0
```

### TRF-H04 — Transfer Into Blocked Account

Valid.

### TRF-H05 — Transfer Reaching Destination Maximum Exactly

Valid.

### TRF-H06 — Retry Successful Transfer

Balances and ledger change once.

---

## 15.6 Transfer — Boundary, Invalid, and Extreme Cases

### TRF-B01 — Amount 0

Rejected.

### TRF-B02 — Amount -0.01

Rejected.

### TRF-B03 — Amount With More Than Two Decimal Places

Rejected.

### TRF-B04 — Amount 0.01 Greater Than Available Balance

Rejected.

Example:

```text
Balance = 100
Transfer = 100.01
```

### TRF-B05 — Self Transfer

Rejected.

### TRF-B06 — Unknown Source

Rejected.

### TRF-B07 — Unknown Destination

Rejected.

### TRF-B08 — Blocked Source

Rejected.

### TRF-B09 — Closed Source

Rejected.

### TRF-B10 — Closed Destination

Rejected.

### TRF-B11 — Destination Overflow

Rejected atomically.

### TRF-B12 — Unsupported Currency

Rejected.

### TRF-B13 — Maximum Transfer From Maximum Balance to Zero Balance

Valid if the destination can hold the resulting amount.

### TRF-B14 — Same Idempotency Key With Amount Changed by 0.01

Conflict.

### TRF-B15 — Same Idempotency Key With Source and Destination Swapped

Conflict.

---

## 15.7 Reversal — Happy Paths

### REV-H01 — Reverse Completed Transfer

Original:

```text
A -> B = 100
```

Reversal restores:

```text
A +100
B -100
```

### REV-H02 — Reverse When Original Destination Has Exact Required Balance

Valid.

### REV-H03 — Reverse While Original Destination Is Blocked

Valid if balance is sufficient and neither account is Closed.

### REV-H04 — Retry Successful Reversal

One reversal and one compensating ledger transaction exist.

---

## 15.8 Reversal — Boundary and Invalid Cases

### REV-B01 — Reverse Unknown Transfer

Rejected.

### REV-B02 — Reverse Already Reversed Transfer

Rejected.

### REV-B03 — Reverse When Destination Is Short by 0.01

Rejected.

### REV-B04 — Reverse When Original Source Is Closed

Rejected.

### REV-B05 — Reverse When Original Destination Is Closed

Rejected.

### REV-B06 — Reversal Would Overflow Original Source

Rejected.

### REV-B07 — Same Idempotency Key Used for Different Original Transfer

Conflict.

### REV-B08 — Concurrent Double Reversal

Only one succeeds.

---

## 15.9 Idempotency — Happy Paths

### IDE-H01 — Same Successful Request Repeated Sequentially

Returns original result.

### IDE-H02 — Same Successful Request Repeated Concurrently

One financial operation.

### IDE-H03 — Stable Rejection Repeated

Returns original rejection.

### IDE-H04 — Exactly 255-Character Key

Valid.

---

## 15.10 Idempotency — Invalid and Failure Cases

### IDE-B01 — Missing Key

Rejected.

### IDE-B02 — Empty Key

Rejected.

### IDE-B03 — Whitespace Key

Rejected.

### IDE-B04 — 256-Character Key

Rejected.

### IDE-B05 — Same Key and Different Amount

Conflict.

### IDE-B06 — Same Key and Different Account

Conflict.

### IDE-B07 — Same Key With Different Operation Type

Allowed because operation type is part of the scope.

### IDE-B08 — Malformed Request Then Correct Request With Same Key

Correct request may execute because pre-execution malformed requests do not reserve the key.

### IDE-B09 — Transient Failure Then Retry

Retry is allowed when no stable final outcome was committed.

---

## 15.11 Concurrency — Critical Scenarios

### CON-H01 — Two Transfers, Both Affordable

```text
Balance = 200

T1 = 80
T2 = 80
```

Expected:

```text
both succeed
balance = 40
```

### CON-H02 — Two Transfers, Only One Affordable

```text
Balance = 100

T1 = 80
T2 = 80
```

Expected:

```text
one succeeds
one fails
balance = 20
```

### CON-H03 — Many Distinct Concurrent Deposits

If all deposits are valid and no maximum balance is exceeded:

```text
final balance
=
initial balance + sum(successful deposits)
```

### CON-H04 — Many Concurrent Transfers

For any set of successful outgoing transfers:

```text
final balance >= 0
```

and:

```text
initial balance
- SUM(successful outgoing)
+ SUM(successful incoming)
=
final balance
```

### CON-H05 — Duplicate Storm

Many simultaneous requests with one idempotency identity still create:

```text
1 logical financial operation
```

### CON-H06 — Transfer vs Close

Final state must match one valid ordering.

### CON-H07 — Transfer vs Block

Final state must match one valid ordering.

### CON-H08 — Transfer vs Reversal

Final state must match one valid ordering and preserve non-negative balances.

---

# 16. Cross-Operation Invariants

These cases validate interactions between different features.

## XOP-001 — Deposit Then Transfer

```text
Create A
Deposit 100 into A
Transfer 40 A -> B
```

Expected:

```text
A = 60
B = 40
```

All ledger transactions individually balance.

---

## XOP-002 — Transfer Then Reversal

```text
A = 100
B = 0

Transfer 100 A -> B
Reverse transfer
```

Expected:

```text
A = 100
B = 0
```

Original and reversal history both remain.

---

## XOP-003 — Transfer, Spend, Then Failed Reversal

```text
A -> B = 100
B -> C = 80

B = 20
```

Reversal of the first transfer requires:

```text
B -100
```

Result:

```text
reversal rejected
```

Balances remain:

```text
A = original post-transfer state
B = 20
C = 80
```

---

## XOP-004 — Transfer All Funds Then Close Source

```text
A = 100

A -> B = 100
A = 0
```

Closing A is now valid.

---

## XOP-005 — Closed Source After Transfer Prevents Later Reversal

If the original source reaches zero and is closed after a completed transfer, a later reversal involving that Closed account is rejected.

---

## XOP-006 — Block Does Not Change Money

Blocking or unblocking an account never creates ledger entries and never modifies balance.

---

# 17. Out of Scope for the MVP

The following behaviors are intentionally not defined in the initial version:

- partial transfer reversals;
- customer withdrawals to an external bank;
- scheduled transfers;
- pending or delayed transfer settlement;
- account overdraft;
- credit limits;
- transaction fees;
- interest;
- multiple currencies;
- foreign exchange;
- chargebacks;
- fraud decisions;
- authentication and authorization;
- KYC;
- account ownership changes;
- customer account deletion;
- manual balance editing;
- ledger entry editing;
- asynchronous financial settlement;
- message broker processing.

These features may be introduced later only after new business rules and invariants are explicitly defined.
