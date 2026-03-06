# Feature 089: Test Coverage Gaps — Domain Value Objects & Enums

> **Status:** Planning
> **Priority:** Medium (§6 TDD — domain logic must be tested)
> **Dependencies:** None

## Overview

A test coverage audit identified domain value objects and entity classes without corresponding unit tests. Per §6, domain logic requires unit tests (pure, fast). While some of these are simple data carriers, they may contain validation or equality logic worth testing.

## Problem Statement

### Current State

**Untested Domain Value Objects:**
1. `BillInfoValue` (Recurring/) — bill info for recurring transactions
2. `DailyTotalValue` (Reconciliation/) — daily transaction totals
3. `TransactionMatchResultValue` (Reconciliation/) — match result data
4. `RecurringInstanceInfoValue` (Recurring/) — projected instance info
5. `RecurringTransferInstanceInfoValue` (Recurring/) — projected transfer instance info

**Untested Domain Entities:**
6. `CustomReportLayout` (Reports/) — custom report layout entity

**Partially Tested:**
7. `BudgetProgress` — record type, may need edge case tests
8. `ClarificationOption` — record type, simple data carrier

**Untested Domain Records (ChatAction subtypes):**
9. `ClarificationNeededAction` — tested indirectly via `ChatActionTests` but no dedicated tests
10. `CreateRecurringTransactionAction` — no dedicated tests
11. `CreateRecurringTransferAction` — no dedicated tests

### Target State

All domain value objects with validation or construction logic have unit tests. Simple data-carrier records (no logic) may be excluded with justification.

---

## User Stories

### US-089-001: Test Domain Value Objects
**As a** developer  
**I want** all domain value objects to have unit tests  
**So that** construction, validation, and equality semantics are verified.

**Acceptance Criteria:**
- [ ] `BillInfoValue` has unit tests
- [ ] `DailyTotalValue` has unit tests
- [ ] `TransactionMatchResultValue` has unit tests
- [ ] `RecurringInstanceInfoValue` has unit tests
- [ ] `RecurringTransferInstanceInfoValue` has unit tests
- [ ] `CustomReportLayout` has unit tests
- [ ] All tests pass

---

## Implementation Plan

### Phase 1: Reconciliation & Recurring Value Objects

**Objective:** Add tests for value objects used in reconciliation and recurring projections.

**Tasks:**
- [ ] Create `BillInfoValueTests.cs`
- [ ] Create `DailyTotalValueTests.cs`
- [ ] Create `TransactionMatchResultValueTests.cs`
- [ ] Create `RecurringInstanceInfoValueTests.cs`
- [ ] Create `RecurringTransferInstanceInfoValueTests.cs`
- [ ] All tests pass

### Phase 2: Entity & Chat Action Tests

**Objective:** Add tests for remaining untested domain types.

**Tasks:**
- [ ] Create `CustomReportLayoutTests.cs`
- [ ] Expand `ChatActionTests.cs` to cover all action subtypes
- [ ] All tests pass
