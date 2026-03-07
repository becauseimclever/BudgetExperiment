# Feature 088: Test Coverage Gaps — Application Layer

> **Status:** Done
> **Priority:** Medium-High (§15 — services and mappers need test coverage)
> **Dependencies:** None

## Overview

A test coverage audit identified several application-layer classes without test coverage: 1 service, 9 mapper classes, 2 projection utilities, and 2 business logic handlers. Per §6 (TDD) and §19 (test critical mappings), these gaps should be addressed.

## Problem Statement

### Current State

**Untested Application Classes:**

| Category | Class | Risk |
|----------|-------|------|
| **Service** | `CustomReportLayoutService` | Medium — CRUD service for report layouts |
| **Handler** | `ReconciliationMatchActionHandler` | High — business logic for match actions |
| **Handler** | `ImportPreviewEnricher` | Medium — enriches import preview data |
| **Projector** | `RecurringInstanceProjector` | High — projects recurring transaction instances |
| **Projector** | `RecurringTransferInstanceProjector` | High — projects recurring transfer instances |
| **Mapper** | `AccountMapper` | Medium — entity-to-DTO mapping |
| **Mapper** | `BudgetMapper` | Medium — entity-to-DTO mapping |
| **Mapper** | `CategorizationMapper` | Medium — entity-to-DTO mapping |
| **Mapper** | `ChatMapper` | Medium — entity-to-DTO mapping |
| **Mapper** | `CommonMapper` | Low — shared mapping utilities |
| **Mapper** | `PaycheckMapper` | Medium — entity-to-DTO mapping |
| **Mapper** | `ReconciliationMapper` | Medium — entity-to-DTO mapping |
| **Mapper** | `RecurringMapper` | Medium — entity-to-DTO mapping |

### Target State

All application services, handlers, projectors, and mappers with non-trivial logic have test coverage.

---

## User Stories

### US-088-001: Test Application Services & Handlers
**As a** developer  
**I want** all application services and business logic handlers to have unit tests  
**So that** business workflows are verified and protected from regressions.

**Acceptance Criteria:**
- [x] `CustomReportLayoutService` has unit tests
- [x] `ReconciliationMatchActionHandler` has unit tests (pre-existing)
- [x] `ImportPreviewEnricher` has unit tests
- [x] `RecurringInstanceProjector` has unit tests
- [x] `RecurringTransferInstanceProjector` has unit tests
- [x] All tests pass

### US-088-002: Test Application Mappers
**As a** developer  
**I want** mapper classes with critical mapping logic to have unit tests  
**So that** DTO mapping correctness is verified (§19).

**Acceptance Criteria:**
- [x] Mappers with non-trivial logic (`ChatMapper`, `RecurringMapper`) have test files
- [x] Tests verify polymorphic mapping and exception resolution logic
- [x] Edge cases (null actions, skipped/modified exceptions) covered
- [x] All tests pass
- Note: Simple property-mapping mappers (AccountMapper, BudgetMapper, CategorizationMapper, CommonMapper, PaycheckMapper, ReconciliationMapper) were intentionally excluded — they contain no business logic, only 1:1 property assignments

---

## Implementation Plan

### Phase 1: High-Priority Services & Handlers

**Objective:** Add tests for business-critical untested classes.

**Tasks:**
- [x] Create `ReconciliationMatchActionHandlerTests.cs` (pre-existing)
- [x] Create `RecurringInstanceProjectorTests.cs`
- [x] Create `RecurringTransferInstanceProjectorTests.cs`
- [x] Create `CustomReportLayoutServiceTests.cs`
- [x] Create `ImportPreviewEnricherTests.cs`
- [x] All tests pass

### Phase 2: Mapper Tests

**Objective:** Add tests for all mapper classes.

**Tasks:**
- [x] Create `ChatMapperTests.cs` (polymorphic action mapping)
- [x] Create `RecurringMapperTests.cs` (exception resolution logic)
- Skipped (no business logic to test — pure property mapping):
  - `AccountMapperTests.cs`
  - `BudgetMapperTests.cs`
  - `CategorizationMapperTests.cs`
  - `CommonMapperTests.cs`
  - `PaycheckMapperTests.cs`
  - `ReconciliationMapperTests.cs`
- [x] All tests pass
