# LedgerGuard

LedgerGuard is a financial transaction engine focused on correctness, consistency, and reliable business-rule enforcement.

The system manages accounts, deposits, transfers, and reversals using an immutable double-entry ledger.

Its main goal is to guarantee that financial operations remain correct even when requests are retried, executed concurrently, or fail during processing.

## Core Guarantees

LedgerGuard is designed around the following guarantees:

- Money cannot be created or lost during internal transfers.
- Account balances cannot become negative.
- Every financial transaction must produce balanced ledger entries.
- Financial history is immutable.
- Retrying the same request must not duplicate financial effects.
- Concurrent operations must preserve all financial invariants.
- Failed operations must not leave partial financial state.

## Main Features

- Account management
- Deposits
- Account-to-account transfers
- Transfer reversals
- Double-entry ledger
- Idempotent financial operations
- Optimistic concurrency control
- Ledger reconciliation
- Automated testing across multiple levels

## Tech Stack

- C#
- .NET 10
- ASP.NET Core
- PostgreSQL
- Entity Framework Core
- NUnit
- Testcontainers
- Docker
- GitHub Actions

## Documentation

Detailed project documentation is available under [`/docs`](./docs).

- [Project Overview](./docs/project-overview.md)
- [Business Rules](./docs/business-rules.md)
- [Architecture](./docs/architecture.md)
- [Testing Strategy](./docs/testing-strategy.md)

## Project Structure

```text
src/
├── LedgerGuard.Api
├── LedgerGuard.Application
├── LedgerGuard.Domain
└── LedgerGuard.Infrastructure

tests/
├── LedgerGuard.UnitTests
├── LedgerGuard.IntegrationTests
└── LedgerGuard.EndToEndTests
```

## Running the Application

```bash
docker compose up -d
dotnet run --project src/LedgerGuard.Api
```

## Running Tests
Run all tests:
```bash
dotnet test
```

Run only unit tests:
```bash
dotnet test --filter TestCategory=Unit
```


Run integration tests:
```bash
dotnet test --filter TestCategory=Integration
```


Run end-to-end tests:
```bash
dotnet test --filter TestCategory=E2E
```