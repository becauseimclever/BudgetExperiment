# Foundation Features (001-010) - Consolidated Summary

**Consolidated:** 2026-01-12  
**Original Features:** 001 through 010  
**Status:** All Completed

---

## Overview

This document consolidates the first ten feature documents that established the foundation of the Budget Experiment application. These features cover the initial architecture, core domain models, UI structure, and essential functionality.

---

## 001: Database Reset

**Completed:** 2026-01-09

Reset the database schema to start fresh with a new architectural direction.

**Key Outcomes:**
- Dropped all existing tables and archived old migrations
- Created clean slate for new domain model design
- Established migration workflow using EF Core CLI commands

---

## 002: Architecture Reset

**Completed:** 2026-01-09

Redesigned application architecture with clean/onion hybrid pattern.

**Technology Stack:**
- ASP.NET Core Web API (.NET 10)
- Blazor WebAssembly + FluentUI-Blazor
- PostgreSQL with EF Core
- OpenAPI + Scalar UI

**Project Structure:**
```
src/
├── BudgetExperiment.Domain        # Entities, value objects, interfaces
├── BudgetExperiment.Application   # Services, DTOs, use cases
├── BudgetExperiment.Infrastructure # EF Core, repositories, migrations
├── BudgetExperiment.Api           # REST API, hosts Blazor client
└── BudgetExperiment.Client        # Blazor WebAssembly UI
```

**Core Domain Entities:**
- **Account** (Aggregate Root): Id, Name, Type, Transactions collection
- **Transaction**: Id, AccountId, Amount (MoneyValue), Date, Description, Category

---

## 003: Collapsible Navigation & Component-Based UI

**Completed:** 2026-01-09

Restructured the Blazor client with calendar-first navigation.

**Key Changes:**
- Calendar page became root (`/`) route
- Collapsible left-side navigation menu
- Account transactions page at `/accounts/{id}/transactions`
- Extracted reusable UI components (NavMenu, forms, displays)

**Layout Structure:**
```
┌─────────────────────────────────────────────────────┐
│ Header: Budget Experiment              [☰ Toggle]   │
├──────────┬──────────────────────────────────────────┤
│ NavMenu  │   Page Content (@Body)                   │
│ (Left)   │                                          │
│ 📅 Cal   │                                          │
│ 🏦 Acct  │                                          │
└──────────┴──────────────────────────────────────────┘
```

---

## 004: Recurring Transactions

**Completed:** 2026-01-10

Enabled scheduling of recurring income and expenses (payday, bills, subscriptions).

**Domain Model:**
- **RecurringTransaction**: Schedule definition with amount, pattern, start/end dates
- **RecurrencePattern** (Value Object): Frequency, interval, day of month/week
- **RecurringTransactionException**: Per-instance modifications (Modified, Skipped)

**Recurrence Frequencies:** Daily, Weekly, BiWeekly, Monthly, Quarterly, Yearly

**Key Features:**
- Create/edit/delete recurring transactions
- Skip or pause individual occurrences
- Edit single instance or "this and future"
- Calendar integration showing projected recurring items
- Unified transaction display mixing actual and projected

---

## 005: Account Transfers

**Completed:** 2026-01-10

Enabled easy money movement between accounts as linked transaction pairs.

**Approach:** Transfer as linked transactions using `TransferId` on Transaction entity.

**Domain Changes:**
- `TransferId: Guid?` on Transaction (links the pair)
- `TransferDirection` enum: Source (money leaving), Destination (money entering)

**API Endpoints:**
- `POST /api/v1/transfers` - Create transfer (creates 2 linked transactions)
- `GET/PUT/DELETE /api/v1/transfers/{transferId}` - Manage transfers

**UI Features:**
- Quick transfer dialog with account selection
- Transfer indicator on linked transactions
- Delete/edit updates both transactions atomically

---

## 006: Thin Client Refactor

**Completed:** 2026-01-10

Moved all business logic from Blazor client to API layer.

**Client Responsibilities (Display Only):**
- Render data from API
- Handle user interactions
- Manage UI state (modals, loading, selection)
- Format data for display

**API Responsibilities (All Business Logic):**
- Calculate derived data (totals, counts, projections)
- Build calendar grids and merged lists
- Enforce business rules
- Handle date/time calculations

**New API Endpoints:**
- `GET /api/v1/calendar/grid?year={year}&month={month}` - Pre-computed calendar grid
- `GET /api/v1/calendar/day/{date}` - Day detail with merged transactions
- Enhanced account transaction list with pre-merged data

---

## 007: Graceful Error Handling

**Completed:** 2026-01-10

Added user-friendly error handling with retry capability across all UI pages.

**ErrorAlert Component:**
- Displays clear error messages
- Retry button for failed operations
- Dismiss option to continue using app
- Loading state during retry

**Pattern Applied to All Pages:**
```csharp
// State
private string? errorMessage;
private bool isRetrying;

// Try-catch in load methods
// RetryLoad() method with isRetrying state
// DismissError() to clear message
```

**Pages Updated:** Calendar, Accounts, Account Transactions, Recurring, Transfers

---

## 008: Recurring Transfers

**Completed:** 2026-01-10

Combined recurring transactions and transfers for scheduled money movement.

**Domain Model:**
- **RecurringTransfer**: Source/destination accounts, amount, recurrence pattern
- **RecurringTransferException**: Per-instance modifications
- Extended Transaction with `RecurringTransferId` link

**API Endpoints:**
- Full CRUD for recurring transfers
- Skip/pause/resume operations
- Instance-level modifications
- Projected transfers for date range

**Validation Rules:**
- Source ≠ destination account
- Amount must be positive
- Valid recurrence pattern

---

## 009: Account Initial Balance & Edit

**Completed:** 2026-01-11

Enabled setting initial account balance and editing account details.

**Domain Changes:**
- `InitialBalance: MoneyValue` on Account
- `InitialBalanceDate: DateOnly` - when balance was recorded

**Balance Calculation:**
```
Running Balance = InitialBalance + Sum(transactions where date >= InitialBalanceDate)
```

**API Changes:**
- POST/GET accounts include `initialBalance` and `initialBalanceDate`
- New `PUT /api/v1/accounts/{id}` for updates

**UI Features:**
- Initial balance and date fields in account creation
- Edit account dialog for name, type, initial balance

---

## 010: Automatic Database Migrations

**Completed:** 2026-01-11

Automatic EF Core migrations at application startup.

**Behavior:**
| Environment | Migrations | Seed Data |
|-------------|------------|-----------|
| Development | ✅ Auto-apply | ✅ Run |
| Staging/Production | ✅ Auto-apply | ❌ Skip |

**Configuration:**
```json
{
  "Database": {
    "AutoMigrate": true,
    "MigrationTimeoutSeconds": 300
  }
}
```

**Health Endpoint:** `GET /health`
- Database connectivity check
- Migration status check (Healthy if none pending, Degraded if pending)

**Key Benefits:**
- No manual migration commands during deployment
- Fail-fast if migrations fail
- Clear logging of pending/applied migrations

---

## Files Created/Modified Summary

### Domain Layer
- `Account.cs` - Core aggregate with InitialBalance
- `Transaction.cs` - Extended with TransferId, RecurringTransactionId links
- `RecurringTransaction.cs`, `RecurringTransfer.cs` - Recurring entities
- `RecurrencePattern.cs`, `RecurrenceFrequency.cs` - Recurrence value objects
- `MoneyValue.cs` - Currency value object
- Repository interfaces for all aggregates

### Infrastructure Layer
- EF Core configurations for all entities
- Repository implementations
- Migrations for schema evolution

### Application Layer
- DTOs for all entities
- Services: AccountService, TransactionService, RecurringTransactionService, TransferService
- Mapping extensions

### API Layer
- Controllers: Accounts, Transactions, RecurringTransactions, Transfers, Calendar
- Health checks for database and migrations
- OpenAPI/Scalar configuration

### Client Layer
- Pages: Calendar, Accounts, AccountTransactions, Recurring, Transfers
- Components: NavMenu, ErrorAlert, TransactionTable, CalendarGrid, various forms
- Services: BudgetApiService with full API client implementation

---

## Related Documents

- [copilot-instructions.md](../.github/copilot-instructions.md) - Engineering guidelines
- [011-design-system.md](011-design-system.md) - Next feature (design system)
