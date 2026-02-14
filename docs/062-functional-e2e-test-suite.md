# Feature 062: Functional E2E Test Suite
> **Status:** ✅ Implemented
> **Priority:** Medium

## Overview

Establish a comprehensive functional E2E test suite using Playwright to validate critical user flows and prevent regressions. This consolidates narrow E2E scenarios (like Feature 041) into a maintainable, extensible test suite.

## Problem Statement

### Current State

- E2E tests exist for demo/smoke scenarios but lack comprehensive functional coverage
- Individual feature docs were created for single E2E test scenarios (e.g., 041), leading to fragmentation
- No systematic approach to functional E2E test coverage

### Target State

- Consolidated E2E test suite covering critical user journeys
- Tests run against demo environment in CI
- Clear test organization by feature area
- Easy to add new functional scenarios

---

## User Stories

### US-062-001: Calendar Navigation and Display
**As a** user  
**I want to** navigate the calendar and see correct transaction data  
**So that** I can trust the calendar view accuracy

**Acceptance Criteria:**
- [ ] Calendar loads with current month displayed
- [ ] Month navigation (prev/next) works correctly
- [ ] Transactions appear on correct dates
- [ ] Daily totals are calculated correctly

### US-062-002: Starting Balance Validation (from Feature 041)
**As a** user  
**I want to** see the correct running balance on the calendar, starting from my account's starting balance  
**So that** I can trust the accuracy of my financial overview

**Acceptance Criteria:**
- [ ] The calendar running balance starts with the account's starting balance
- [ ] All transactions are applied in order to the running balance
- [ ] E2E tests confirm this for demo accounts
- [ ] Running balance updates when switching accounts

### US-062-003: Transaction CRUD Operations
**As a** user  
**I want to** create, read, update, and delete transactions  
**So that** I can manage my financial records

**Acceptance Criteria:**
- [ ] Can create a new transaction via calendar
- [ ] Can view transaction details
- [ ] Can edit an existing transaction
- [ ] Can delete a transaction
- [ ] Changes reflect immediately in the calendar

### US-062-004: Account Management
**As a** user  
**I want to** manage my accounts  
**So that** I can organize my finances

**Acceptance Criteria:**
- [ ] Can create a new account with starting balance
- [ ] Can switch between accounts
- [ ] Account-specific transactions filter correctly
- [ ] Starting balance is correctly applied

### US-062-005: CSV Import Flow
**As a** user  
**I want to** import transactions from CSV files  
**So that** I can quickly add bulk transaction data

**Acceptance Criteria:**
- [ ] Can upload a CSV file
- [ ] Field mapping works correctly
- [ ] Preview shows expected transactions
- [ ] Import completes and transactions appear

---

## Technical Design

### Test Architecture

```
tests/BudgetExperiment.E2E.Tests/
├── Tests/
│   ├── FunctionalTests/
│   │   ├── CalendarTests.cs         # US-062-001, US-062-002
│   │   ├── TransactionTests.cs      # US-062-003
│   │   ├── AccountTests.cs          # US-062-004
│   │   └── CsvImportTests.cs        # US-062-005
│   ├── PerformanceTests/            # (Feature 059)
│   └── SmokeTests/                  # Existing demo tests
├── Helpers/
│   └── TestDataHelper.cs            # Demo data utilities
└── Fixtures/
    └── PlaywrightFixture.cs         # Shared browser context
```

### Test Data Strategy

- Tests run against demo environment with seeded data
- Tests should be idempotent (clean up after themselves)
- Use unique identifiers for test-created data

---

## Implementation Plan

### Phase 1: Calendar Tests

**Objective:** Validate calendar display and navigation

**Tasks:**
- [x] Create `CalendarTests.cs`
- [x] Test calendar loads and displays current month
- [x] Test month navigation
- [x] Test daily transaction display

**Commit:**
- test(e2e): add calendar navigation tests

### Phase 2: Starting Balance Tests (from Feature 041)

**Objective:** Validate starting balance in running balance calculation

**Tasks:**
- [x] Add starting balance validation tests to `CalendarTests.cs`
- [x] Verify running balance starts with account starting balance
- [x] Verify running balance accumulates correctly with transactions
- [x] Test account switching updates running balance

**Commit:**
- test(e2e): validate starting balance in calendar

### Phase 3: Transaction CRUD Tests

**Objective:** Validate transaction management

**Tasks:**
- [x] Create `TransactionTests.cs`
- [x] Test create transaction flow
- [x] Test edit transaction flow
- [x] Test delete transaction flow
- [x] Verify calendar updates after changes

**Commit:**
- test(e2e): add transaction CRUD tests

### Phase 4: Account and Import Tests

**Objective:** Validate account management and CSV import

**Tasks:**
- [x] Create `AccountTests.cs`
- [x] Create `CsvImportTests.cs`
- [x] Test account creation and switching
- [x] Test CSV import end-to-end

**Commit:**
- test(e2e): add account and import tests

---

## Dependencies

- E2E test infrastructure (Playwright setup)
- Demo environment with seeded data
- Feature 059 (Performance E2E) for shared fixtures

## Local Execution

- Run in VS Code task runner:
    - `E2E: Functional 062 (Local)`
    - `E2E: Functional 062 (DemoSafe)`
- Task file: `.vscode/tasks.json`
- Local task sets:
    - `RUN_E2E_TESTS=true`
    - `BUDGET_APP_URL=http://localhost:5099`
- Local task command:
    - `dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests\BudgetExperiment.E2E.Tests.csproj --filter "Category=Functional"`
- DemoSafe task sets:
    - `RUN_E2E_TESTS=true`
    - `BUDGET_APP_URL=https://budgetdemo.becauseimclever.com`
- DemoSafe task command:
    - `dotnet test c:\ws\BudgetExperiment\tests\BudgetExperiment.E2E.Tests\BudgetExperiment.E2E.Tests.csproj --filter "Category=Functional&Category=DemoSafe"`

---

## Related Features

- Feature 041 (Cancelled) - Starting balance validation consolidated here
- Feature 059 - Performance E2E tests (complementary)

---

## Changelog

| Date | Author | Description |
|------|--------|-------------|
| 2026-02-01 | AI | Created feature doc, consolidated Feature 041 scenarios |
| 2026-02-14 | AI | Implemented functional Playwright suite: Calendar, Transaction, Account, CSV Import tests + local-run guidance |
| 2026-02-14 | AI | Added VS Code local task for one-click Feature 062 functional suite execution |
| 2026-02-14 | AI | Added VS Code DemoSafe task for shared-environment functional validation |
