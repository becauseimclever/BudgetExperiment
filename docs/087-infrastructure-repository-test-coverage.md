# Feature 087: Test Coverage Gaps — Infrastructure Repositories

> **Status:** Done
> **Priority:** Medium (5 untested repositories with testable logic)
> **Dependencies:** None

## Overview

A test coverage audit identified 8 Infrastructure repository implementations with no corresponding test files. After review, 3 repositories (AppSettings, UserSettings, CustomReportLayout) were excluded — they are pure EF Core pass-through with no custom logic worth testing (we test *our* logic, not the framework). The remaining 5 have testable behavior: scope filtering, pattern normalization, composite key lookups, date-range queries, and bulk delete operations.

## Problem Statement

### Current State

**Tested repositories (11/19):**
- AccountRepository, CategorizationRuleRepository, ChatMessageRepository, ChatSessionRepository, DismissedSuggestionPatternRepository, ImportBatchRepository, ImportMappingRepository, ReconciliationMatchRepository, RecurringTransferRepository, RuleSuggestionRepository, TransactionRepository

**Untested repositories with testable logic (5/19):**
1. `BudgetCategoryRepository` — scope filtering, 5 custom query methods (GetByName, GetActive, GetByType, GetAll, GetByIds)
2. `BudgetGoalRepository` — scope filtering, composite key lookup (category + year + month), navigation include
3. `CategorySuggestionRepository` — case-normalization in `ExistsPendingWithNameAsync`, fetch-then-remove in `DeletePendingByOwnerAsync`, batch `AddRangeAsync`
4. `LearnedMerchantMappingRepository` — pattern normalization (`Trim().ToUpperInvariant()`) in `GetByPatternAsync` and `ExistsAsync`
5. `RecurringTransactionRepository` — 9 custom methods, separate exceptions table with date-range queries and bulk `RemoveExceptionsFromDateAsync`, scope filtering

**Excluded (no custom logic — pure EF Core pass-through):**
- `AppSettingsRepository` — 2 methods, singleton get + no-op save
- `UserSettingsRepository` — 2 methods, user-specific get + `Update` call
- `CustomReportLayoutRepository` — only adds `GetAllAsync` to base CRUD

### Target State

All 5 repositories with testable logic have integration test coverage.

---

## User Stories

### US-087-001: Add Repository Integration Tests
**As a** developer  
**I want** repositories with custom data access logic to have integration tests  
**So that** scope filtering, normalization, composite lookups, and bulk operations are verified.

**Acceptance Criteria:**
- [x] Each of the 5 repositories has a test file
- [x] Tests focus on custom query logic (not basic CRUD already proven by the framework)
- [x] Tests verify scope filtering where applicable
- [x] Tests verify normalization behavior where applicable
- [x] Tests follow the existing SQLite in-memory pattern used by other repository tests
- [x] All tests pass

---

## Implementation Plan

### Phase 1: Budget Repositories (scope filtering + custom queries)

**Objective:** Add tests for BudgetCategory and BudgetGoal repositories — both have scope filtering and custom queries.

**Tasks:**
- [x] Create `BudgetCategoryRepositoryTests.cs` — scope filtering, GetByName, GetActive, GetByType, GetByIds (9 tests)
- [x] Create `BudgetGoalRepositoryTests.cs` — scope filtering, GetByCategoryAndMonth, GetByMonth with Include, GetByCategory (6 tests)
- [x] All tests pass

### Phase 2: Categorization Repositories (normalization + batch operations)

**Objective:** Add tests for CategorySuggestion and LearnedMerchantMapping — both have normalization logic.

**Tasks:**
- [x] Create `CategorySuggestionRepositoryTests.cs` — ExistsPendingWithName normalization, DeletePendingByOwner fetch-then-remove, AddRange (6 tests)
- [x] Create `LearnedMerchantMappingRepositoryTests.cs` — GetByPattern/Exists normalization, GetByOwner ordering (5 tests)
- [x] All tests pass

### Phase 3: Recurring Transaction Repository (exceptions table + bulk delete)

**Objective:** Add tests for RecurringTransaction repository — mirrors RecurringTransfer (already tested).

**Tasks:**
- [x] Create `RecurringTransactionRepositoryTests.cs` — exception CRUD, date-range queries, RemoveExceptionsFromDate, scope filtering, GetActive (10 tests)
- [x] All tests pass

---

## Test Pattern Reference

Follow the existing pattern established in `AccountRepositoryTests.cs`:
- Use SQLite in-memory database via `InMemoryDbFixture`
- Use `FakeUserContext` for scope filtering tests
- Verify persistence with `CreateSharedContext`
- Focus tests on *our* logic: scope filtering, normalization, ordering, composite lookups, date-range queries, bulk operations
