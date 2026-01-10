# Feature 002: Architecture Reset

## Status
**Status:** In Progress (Phase 3 Complete)  
**Created:** 2026-01-09  
**Priority:** High  
**Depends On:** [001-database-reset.md](001-database-reset.md)

## Overview

Rethink and redesign the application architecture, domain models, and project structure while retaining the core technology stack.

## Technology Stack (Retained)

- **Backend:** ASP.NET Core Web API (.NET 10)
- **API Documentation:** OpenAPI + Scalar UI
- **Frontend:** Blazor WebAssembly
- **Database:** PostgreSQL with EF Core
- **Hosting:** API hosts the Blazor client (single deployment unit)

## Goals

- [x] Implement Account aggregate with basic properties
- [x] Implement Transaction entity
- [x] Retain MoneyValue value object
- [ ] Calendar-centric API endpoints (query by date range)
- [ ] Calendar-centric Blazor UI

## UI Design

### Calendar-Centric Approach

The primary view is a **calendar** showing transactions by date:

- **Month View:** See transactions per day, daily totals
- **Day View:** Detailed list of transactions for selected date
- **Account Filter:** View calendar for specific account or all accounts

### Key UI Components

1. **Calendar Grid:** Month view with transaction indicators
2. **Transaction List:** Scrollable list for selected date/range
3. **Account Selector:** Filter by account
4. **Quick Entry:** Fast transaction input

### Navigation Flow

```
Home (Calendar Month View)
  ├── Click Date → Day Detail (transactions for that day)
  ├── Click Account Filter → Filtered calendar
  └── Add Transaction → Quick entry form (defaults to selected date)
```

## Current Project Structure

```
src/
├── BudgetExperiment.Domain        # Domain entities, value objects, interfaces
├── BudgetExperiment.Application   # Services, DTOs, use cases
├── BudgetExperiment.Infrastructure # EF Core, repositories, migrations
├── BudgetExperiment.Api           # REST API, hosts Blazor client
└── BudgetExperiment.Client        # Blazor WebAssembly UI
```

## Domain Model Design

### Core Principles

1. **Traditional Accounting Model:** Accounts contain Transactions
2. **Calendar-Centric UI:** Display data organized by date/time
3. **Simplicity First:** Minimal entities, easy to extend later

### Domain Entities

#### Account (Aggregate Root)
The primary container for financial activity.

```csharp
Account
├── Id (Guid)
├── Name (string)
├── Type (AccountType enum: Checking, Savings, CreditCard, Cash, etc.)
├── Transactions (collection)
└── CreatedAt, UpdatedAt
```

#### Transaction
Individual financial events within an account.

```csharp
Transaction
├── Id (Guid)
├── AccountId (Guid)
├── Amount (MoneyValue)
├── Date (DateOnly)
├── Description (string)
├── Category (string?) - optional, for future categorization
└── CreatedAt, UpdatedAt
```

### Value Objects

#### MoneyValue (Retained)
Immutable representation of currency amounts.

```csharp
MoneyValue
├── Amount (decimal)
└── Currency (string, default "USD")
```

### Enums

```csharp
AccountType
├── Checking
├── Savings
├── CreditCard
├── Cash
└── Other
```

### Future Expansion Points

These are **NOT** in initial scope but the design accommodates them:

- **Bulk Import (Plaid):** Import transactions from bank feeds, match against hand-entered data
- **Transaction Matching:** Reconcile imported vs manual entries (requires status/source fields)
- **Recurring Transactions:** Add `RecurrenceRule` entity linked to Transaction
- **Budgets/Categories:** Expand `Category` from string to full entity
- **Transfers:** Link two transactions across accounts
- **Multi-currency:** MoneyValue already supports currency field
- **Tags/Labels:** Add collection to Transaction

### Design Considerations for Future Import/Matching

When implementing Plaid integration, the Transaction entity may need:
- `Source` (enum: Manual, Imported)
- `ExternalId` (string? - Plaid transaction ID)
- `MatchedTransactionId` (Guid? - link to matched manual entry)
- `Status` (enum: Pending, Cleared, Reconciled)

Keep the initial design simple but be aware these fields may be added later.

## Implementation Plan

### Phase 1: Domain Design
1. Whiteboard/document core domain concepts
2. Identify aggregates and their boundaries
3. Define value objects
4. Document key use cases

### Phase 2: Clean Slate Implementation
1. Remove or archive existing domain entities
2. Implement new domain model (TDD - tests first)
3. Create new repository interfaces

### Phase 3: Infrastructure
1. New EF Core entity configurations
2. New migrations (after 001-database-reset)
3. Repository implementations

### Phase 4: Application Layer
1. Define DTOs for new domain
2. Implement services/use cases
3. Map between domain and DTOs

### Phase 5: API Layer
1. Design REST endpoints
2. Implement controllers/minimal APIs
3. Update OpenAPI documentation

### Phase 6: Client Updates
1. Update Blazor client for new API contract
2. Redesign UI components as needed

## Architectural Principles (Retained)

Per [copilot-instructions.md](../.github/copilot-instructions.md):

- **Clean/Onion Architecture:** Outer layers depend inward only
- **TDD Workflow:** Red → Green → Refactor
- **SOLID Principles:** Enforced throughout
- **Domain-Driven Design:** Rich domain model, value objects
- **No EF Core in Domain:** Infrastructure supplies implementations

## API Design

### REST Endpoints

```
Accounts
  GET    /api/v1/accounts              - List all accounts
  GET    /api/v1/accounts/{id}         - Get account details
  POST   /api/v1/accounts              - Create account
  PUT    /api/v1/accounts/{id}         - Update account
  DELETE /api/v1/accounts/{id}         - Delete account

Transactions
  GET    /api/v1/transactions?startDate=&endDate=&accountId=  - Query transactions (calendar support)
  GET    /api/v1/transactions/{id}     - Get transaction
  POST   /api/v1/transactions          - Create transaction
  PUT    /api/v1/transactions/{id}     - Update transaction
  DELETE /api/v1/transactions/{id}     - Delete transaction

Calendar (convenience endpoints)
  GET    /api/v1/calendar/summary?year=&month=&accountId=  - Daily totals for month view
```

## Acceptance Criteria

- [ ] Account entity with CRUD operations
- [ ] Transaction entity with CRUD operations
- [ ] Date-range query support for transactions
- [ ] Calendar summary endpoint for month view
- [ ] Blazor calendar UI displays transactions by date
- [ ] Full test coverage (TDD approach)
- [ ] Scalar UI shows clean API structure

## Open Questions

1. ~~Should we simplify to fewer projects initially?~~ Keep current structure
2. ~~What budgeting workflows are essential for MVP?~~ Accounts + Transactions + Calendar view
3. ~~Do we need multi-account support?~~ Yes, from the start
4. ~~How should categories/budgets work?~~ Deferred - optional category string for now

## Notes

This is a significant reset. Take time to design the domain properly before coding. Use the TDD workflow - write failing tests for domain behavior first, then implement.

## Related Features

- **Prerequisite:** [001-database-reset.md](001-database-reset.md)
- **Next:** TBD (first domain aggregate implementation)
