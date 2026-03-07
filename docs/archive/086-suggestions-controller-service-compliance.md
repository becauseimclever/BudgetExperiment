# Feature 086: SuggestionsController Service Layer Compliance

> **Status:** Done
> **Priority:** Medium (§13 compliance — application service mediation)
> **Dependencies:** None

## Overview

A codebase audit found that `SuggestionsController` directly injects and calls domain repository interfaces (`IBudgetCategoryRepository`, `ICategorizationRuleRepository`) instead of routing through an application service. Per §13, controllers should not directly access repositories without application service mediation.

All other controllers (25/26) correctly follow this pattern.

## Problem Statement

### Current State

`SuggestionsController` injects:
- `IBudgetCategoryRepository` — used to list/get categories for suggestion context
- `ICategorizationRuleRepository` — used to list/get rules for suggestion context

These are called directly in controller actions (e.g., `ListAsync()`, `GetByIdAsync()`) to enrich suggestion responses with category/rule metadata.

### Target State

- `SuggestionsController` only injects application service interfaces.
- Repository access is encapsulated within the application service layer.
- All 26 controllers follow the consistent mediation pattern.

---

## User Stories

### US-086-001: Extract Repository Calls to Application Service
**As a** developer  
**I want** SuggestionsController to access categories and rules through application services  
**So that** the controller follows the same mediation pattern as all other controllers.

**Acceptance Criteria:**
- [x] `SuggestionsController` no longer injects `IBudgetCategoryRepository` or `ICategorizationRuleRepository`
- [x] Category/rule lookup logic moved to `RuleSuggestionService` (or a new service if scope warrants)
- [x] Existing behavior unchanged (same API responses)
- [x] Controller tests updated
- [x] Application service tests cover the moved logic
- [x] All tests pass

---

## Technical Design

### Architecture Changes

Move repository access from `SuggestionsController` into the existing `RuleSuggestionService` (preferred) or a new `SuggestionContextService`.

### Current Flow (Non-Compliant)
```
SuggestionsController → IBudgetCategoryRepository (direct)
SuggestionsController → ICategorizationRuleRepository (direct)
```

### Target Flow (Compliant)
```
SuggestionsController → IRuleSuggestionService → IBudgetCategoryRepository
                                                → ICategorizationRuleRepository
```

### API Endpoints

No changes to API endpoints or response shapes. This is an internal refactoring.

---

## Implementation Plan

### Phase 1: Extend Application Service

**Objective:** Add category/rule lookup methods to `IRuleSuggestionService` / `RuleSuggestionService`.

**Tasks:**
- [x] Write unit tests for the new service methods
- [x] Add methods to `IRuleSuggestionService` interface
- [x] Implement in `RuleSuggestionService`
- [x] Verify tests pass

### Phase 2: Refactor Controller

**Objective:** Remove direct repository access from `SuggestionsController`.

**Tasks:**
- [x] Remove `IBudgetCategoryRepository` and `ICategorizationRuleRepository` from constructor
- [x] Replace direct calls with application service calls
- [x] Update controller tests
- [x] Verify all tests pass

### Phase 3: Verification

**Objective:** Confirm no controller directly injects repository interfaces.

**Tasks:**
- [x] Grep codebase: no `IRepository` injections in Controllers/
- [x] Full test suite passes
