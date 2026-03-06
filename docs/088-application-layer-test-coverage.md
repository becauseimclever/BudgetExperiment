# Feature 088: Test Coverage Gaps — Application Layer

> **Status:** Planning
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
- [ ] `CustomReportLayoutService` has unit tests
- [ ] `ReconciliationMatchActionHandler` has unit tests
- [ ] `ImportPreviewEnricher` has unit tests
- [ ] `RecurringInstanceProjector` has unit tests
- [ ] `RecurringTransferInstanceProjector` has unit tests
- [ ] All tests pass

### US-088-002: Test Application Mappers
**As a** developer  
**I want** mapper classes with critical mapping logic to have unit tests  
**So that** DTO mapping correctness is verified (§19).

**Acceptance Criteria:**
- [ ] Each mapper class has a corresponding test file
- [ ] Tests verify all public mapping methods
- [ ] Edge cases (null fields, empty collections) covered
- [ ] All tests pass

---

## Implementation Plan

### Phase 1: High-Priority Services & Handlers

**Objective:** Add tests for business-critical untested classes.

**Tasks:**
- [ ] Create `ReconciliationMatchActionHandlerTests.cs`
- [ ] Create `RecurringInstanceProjectorTests.cs`
- [ ] Create `RecurringTransferInstanceProjectorTests.cs`
- [ ] Create `CustomReportLayoutServiceTests.cs`
- [ ] Create `ImportPreviewEnricherTests.cs`
- [ ] All tests pass

### Phase 2: Mapper Tests

**Objective:** Add tests for all mapper classes.

**Tasks:**
- [ ] Create `AccountMapperTests.cs`
- [ ] Create `BudgetMapperTests.cs`
- [ ] Create `CategorizationMapperTests.cs`
- [ ] Create `ChatMapperTests.cs`
- [ ] Create `CommonMapperTests.cs`
- [ ] Create `PaycheckMapperTests.cs`
- [ ] Create `ReconciliationMapperTests.cs`
- [ ] Create `RecurringMapperTests.cs`
- [ ] All tests pass
