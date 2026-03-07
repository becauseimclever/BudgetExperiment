# Feature 089: Test Coverage Gaps — Domain Value Objects & Enums

> **Status:** Done
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
- [x] `BillInfoValue` has unit tests (already existed)
- [x] `DailyTotalValue` — excluded: pure positional record, no custom logic
- [x] `TransactionMatchResultValue` — excluded: pure positional record, no custom logic
- [x] `RecurringInstanceInfoValue` — excluded: pure positional record, no custom logic
- [x] `RecurringTransferInstanceInfoValue` — excluded: pure positional record, no custom logic
- [x] `CustomReportLayout` has unit tests (24 tests added)
- [x] All tests pass

---

## Implementation Plan

### Phase 1: Reconciliation & Recurring Value Objects

**Objective:** Add tests for value objects used in reconciliation and recurring projections.

**Tasks:**
- [x] `BillInfoValueTests.cs` — already existed with 10 tests covering Create, validation, FromRecurringTransaction, record equality
- [x] `DailyTotalValue` — skipped: pure positional record `(DateOnly, MoneyValue, int)`, no custom logic to test
- [x] `TransactionMatchResultValue` — skipped: pure positional record, no custom logic to test
- [x] `RecurringInstanceInfoValue` — skipped: pure positional record, no custom logic to test
- [x] `RecurringTransferInstanceInfoValue` — skipped: pure positional record, no custom logic to test
- [x] All tests pass

### Phase 2: Entity & Chat Action Tests

**Objective:** Add tests for remaining untested domain types.

**Tasks:**
- [x] Create `CustomReportLayoutTests.cs` — 24 tests covering CreateShared, CreatePersonal, validation (empty name, max length, empty userId), NormalizeLayoutJson, UpdateName, UpdateLayout, trimming, unique IDs
- [x] `ChatActionTests.cs` — already covers all action subtypes (CreateTransaction, CreateTransfer, CreateRecurringTransaction, CreateRecurringTransfer, ClarificationNeeded, ClarificationOption) with Type and GetPreviewSummary tests
- [x] All tests pass
