# Feature 085: DateTime Naming Consistency

> **Status:** Done
> **Priority:** Medium (§30 compliance)
> **Dependencies:** None
> **Completed:** 2026-03-06

## Overview

A codebase audit found that `Account` and `Transaction` entities used `CreatedAt`/`UpdatedAt` instead of the `CreatedAtUtc`/`UpdatedAtUtc` naming convention required by §30. All other entities already followed the correct pattern. This was a cross-cutting rename affecting Domain, Infrastructure (EF configurations + migrations), Contracts (DTOs), Application (mappers), Client (display references), and all associated tests.

## Problem Statement

### Previous State

- `Account.cs` properties: `CreatedAt`, `UpdatedAt`
- `Transaction.cs` properties: `CreatedAt`, `UpdatedAt`
- All other entities (BudgetGoal, BudgetCategory, ChatSession, ChatMessage, AppSettings, UserSettings, ImportMapping, ReconciliationMatch, etc.) already used `CreatedAtUtc`/`UpdatedAtUtc`.

### Resolved State

- All entities consistently use `CreatedAtUtc` / `UpdatedAtUtc` naming per §30.
- Database columns renamed via EF migration (`Feature085_RenameCreatedAtUpdatedAtToUtcSuffix`).
- DTOs, mappers, client code, and tests updated accordingly.

---

## User Stories

### US-085-001: Rename Account DateTime Properties
**As a** developer  
**I want** Account entity DateTime properties to follow the `Utc` suffix convention  
**So that** the codebase is consistent and clearly communicates that all DateTimes are UTC.

**Acceptance Criteria:**
- [x] `Account.CreatedAt` → `Account.CreatedAtUtc`
- [x] `Account.UpdatedAt` → `Account.UpdatedAtUtc`
- [x] EF configuration updated
- [x] Migration generated
- [x] All references updated (DTOs, mappers, controllers, client, tests)
- [x] All tests pass

### US-085-002: Rename Transaction DateTime Properties
**As a** developer  
**I want** Transaction entity DateTime properties to follow the `Utc` suffix convention  
**So that** the codebase is consistent and clearly communicates that all DateTimes are UTC.

**Acceptance Criteria:**
- [x] `Transaction.CreatedAt` → `Transaction.CreatedAtUtc`
- [x] `Transaction.UpdatedAt` → `Transaction.UpdatedAtUtc`
- [x] EF configuration updated
- [x] Migration generated
- [x] All references updated (DTOs, mappers, controllers, client, tests)
- [x] All tests pass

---

## Technical Design

### Architecture Changes

No architectural changes. This was a pure rename refactoring across layers.

### Domain Model

```csharp
// Before
public DateTime CreatedAt { get; private set; }
public DateTime UpdatedAt { get; private set; }

// After
public DateTime CreatedAtUtc { get; private set; }
public DateTime UpdatedAtUtc { get; private set; }
```

### Database Changes

Single EF Core migration (`Feature085_RenameCreatedAtUpdatedAtToUtcSuffix`) to rename columns:
- `Accounts.CreatedAt` → `Accounts.CreatedAtUtc`
- `Accounts.UpdatedAt` → `Accounts.UpdatedAtUtc`
- `Transactions.CreatedAt` → `Transactions.CreatedAtUtc`
- `Transactions.UpdatedAt` → `Transactions.UpdatedAtUtc`

---

## Implementation Summary

### Phase 1: Rename Account Properties

- [x] Update `Account.cs` properties and factory methods (`Create`, `CreateShared`, `CreatePersonal`)
- [x] Update EF configuration (`AccountConfiguration.cs`)
- [x] Update `AccountDto` in Contracts
- [x] Update `AccountMapper` in Application
- [x] Update Account-related tests (`AccountTests.cs`)

### Phase 2: Rename Transaction Properties

- [x] Update `Transaction.cs` properties and factory method (`Create`)
- [x] Update EF configuration (`TransactionConfiguration.cs`)
- [x] Update `TransactionDto`, `TransactionListItemDto`, `DayDetailItemDto` in Contracts
- [x] Update `AccountMapper`, `TransferService`, `TransactionListService`, `DayDetailService` in Application
- [x] Update `TransactionListItem` model and `AccountTransactions.razor` in Client
- [x] Update Transaction-related tests (`TransactionTests.cs`, `TransactionTableSortTests.cs`, `TransactionTablePaginationTests.cs`, `TransactionListServiceTests.cs`)

### Phase 3: Verification

- [x] Full test suite: 2,917 tests pass (831 Domain, 757 Application, 645 Client, 541 API, 143 Infrastructure)
- [x] No remaining `CreatedAt` / `UpdatedAt` references on Account/Transaction (outside historical migrations)
- [x] Migration generated via `dotnet ef migrations add`

---

## Affected Files

| Layer | Files |
|-------|-------|
| Domain | `Account.cs`, `Transaction.cs` |
| Infrastructure | `AccountConfiguration.cs`, `TransactionConfiguration.cs`, new migration |
| Contracts | `AccountDto.cs`, `TransactionDto.cs`, `TransactionListItemDto.cs`, `DayDetailItemDto.cs` |
| Application | `AccountMapper.cs`, `TransferService.cs`, `TransactionListService.cs`, `DayDetailService.cs` |
| Client | `TransactionListItem.cs`, `AccountTransactions.razor` |
| Tests | `AccountTests.cs`, `TransactionTests.cs`, `TransactionTableSortTests.cs`, `TransactionTablePaginationTests.cs`, `TransactionListServiceTests.cs` |
