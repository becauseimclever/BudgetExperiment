# Feature 087: Test Coverage Gaps — Infrastructure Repositories

> **Status:** Planning
> **Priority:** High (8 untested repositories — §15 integration tests)
> **Dependencies:** None

## Overview

A test coverage audit identified 8 Infrastructure repository implementations with no corresponding test files. Per §15, integration tests for repository/data access boundaries are required when behavior is stable. All 8 repositories are mature and in active use.

## Problem Statement

### Current State

**Tested repositories (11/19):**
- AccountRepository, CategorizationRuleRepository, ChatMessageRepository, ChatSessionRepository, DismissedSuggestionPatternRepository, ImportBatchRepository, ImportMappingRepository, ReconciliationMatchRepository, RecurringTransferRepository, RuleSuggestionRepository, TransactionRepository

**Untested repositories (8/19):**
1. `AppSettingsRepository`
2. `BudgetCategoryRepository`
3. `BudgetGoalRepository`
4. `CategorySuggestionRepository`
5. `CustomReportLayoutRepository`
6. `LearnedMerchantMappingRepository`
7. `RecurringTransactionRepository`
8. `UserSettingsRepository`

### Target State

All 19 repository implementations have integration test coverage using SQLite in-memory or Testcontainers (matching existing test patterns).

---

## User Stories

### US-087-001: Add Repository Integration Tests
**As a** developer  
**I want** all repository implementations to have integration tests  
**So that** data access logic is verified and regressions are caught.

**Acceptance Criteria:**
- [ ] Each of the 8 untested repositories has a test file
- [ ] Tests cover CRUD operations (Add, GetById, List, Remove)
- [ ] Tests cover any custom query methods
- [ ] Tests follow the existing SQLite in-memory pattern used by other repository tests
- [ ] All tests pass

---

## Implementation Plan

### Phase 1: Settings & Budget Repositories

**Objective:** Add tests for AppSettings, UserSettings, BudgetCategory, BudgetGoal repositories.

**Tasks:**
- [ ] Create `AppSettingsRepositoryTests.cs`
- [ ] Create `UserSettingsRepositoryTests.cs`
- [ ] Create `BudgetCategoryRepositoryTests.cs`
- [ ] Create `BudgetGoalRepositoryTests.cs`
- [ ] All tests pass

### Phase 2: Categorization & Report Repositories

**Objective:** Add tests for CategorySuggestion, LearnedMerchantMapping, CustomReportLayout repositories.

**Tasks:**
- [ ] Create `CategorySuggestionRepositoryTests.cs`
- [ ] Create `LearnedMerchantMappingRepositoryTests.cs`
- [ ] Create `CustomReportLayoutRepositoryTests.cs`
- [ ] All tests pass

### Phase 3: Recurring Transaction Repository

**Objective:** Add tests for RecurringTransaction repository.

**Tasks:**
- [ ] Create `RecurringTransactionRepositoryTests.cs`
- [ ] Cover all custom query methods
- [ ] All tests pass

---

## Test Pattern Reference

Follow the existing pattern established in `AccountRepositoryTests.cs`:
- Use SQLite in-memory database
- Create `BudgetDbContext` with test connection
- Test CRUD operations
- Test custom query methods with specific data setups
