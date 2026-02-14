# Feature 068: Playwright Local Critical User Flows
> **Status:** 🗒️ Planning  
> **Priority:** High  
> **Estimated Effort:** Small (< 1 sprint)  
> **Dependencies:** Feature 062 (Functional E2E Test Suite)

## Overview

Define and implement a focused Playwright E2E coverage baseline for the most critical day-to-day user journeys in the local development environment. This feature intentionally limits scope to three flows: adding transactions, adding accounts, and adding categories.

Tests are designed to run against the local API-hosted client during development and debugging, while remaining excluded from GitHub Actions until the suite is stabilized.

## Problem Statement

### Current State

- E2E direction exists, but core create flows are not guaranteed by a single minimal, high-signal local suite.
- Developers can introduce regressions in critical create workflows without immediate local Playwright feedback.
- CI execution scope for E2E is intentionally conservative and should not be expanded yet.

### Target State

- A small Playwright suite validates the three core creation workflows end-to-end.
- Tests are easy to run locally against the standard development host.
- GitHub Actions does not execute this new local-critical suite yet.

---

## User Stories

### Core Local E2E Coverage

#### US-068-001: Add Transaction Flow
**As a** developer  
**I want to** validate the transaction creation flow end-to-end with Playwright locally  
**So that** transaction input regressions are caught early

**Acceptance Criteria:**
- [ ] Playwright test creates a transaction through the UI on local environment
- [ ] Saved transaction appears in the expected list/calendar context
- [ ] Test asserts success feedback and persisted visible result

#### US-068-002: Add Account Flow
**As a** developer  
**I want to** validate account creation end-to-end with Playwright locally  
**So that** account setup regressions are caught before merge

**Acceptance Criteria:**
- [ ] Playwright test creates an account through the UI on local environment
- [ ] New account is selectable/visible in account UI
- [ ] Test asserts resulting state reflects the created account

#### US-068-003: Add Category Flow
**As a** developer  
**I want to** validate category creation end-to-end with Playwright locally  
**So that** category-management regressions are detected quickly

**Acceptance Criteria:**
- [ ] Playwright test creates a category through the UI on local environment
- [ ] New category appears in category management UI and is usable in relevant forms
- [ ] Test asserts created category can be selected where applicable

#### US-068-004: Local-Only Execution Scope
**As a** maintainer  
**I want to** keep this suite local-only for now  
**So that** CI runtime and flakiness risk stay controlled while scenarios mature

**Acceptance Criteria:**
- [ ] Local command/documentation exists for running the critical Playwright suite
- [ ] GitHub Actions workflows do not run this new suite
- [ ] CI exclusion approach is explicit and documented (e.g., path/suite filter or separate non-invoked workflow)

---

## Technical Design

### Test Architecture

Target structure in `tests/BudgetExperiment.E2E.Tests`:

- `Tests/FunctionalTests/TransactionCreateTests.cs`
- `Tests/FunctionalTests/AccountCreateTests.cs`
- `Tests/FunctionalTests/CategoryCreateTests.cs`

Common conventions:
- Use resilient selectors (role/label/test-id based where available)
- Use unique test data suffixes to avoid collisions
- Keep each test independent and cleanup-safe when practical

### Local Environment Execution

Per repository workflow, run only the API project locally (the API hosts the Blazor client):

```powershell
dotnet run --project c:\ws\BudgetExperiment\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj
```

Then execute the focused Playwright E2E tests for these critical flows.

### CI Exclusion Strategy (Initial)

For this feature phase, keep execution local-only by one of the following:
- Ensure existing GitHub Action test filters do not include these specific tests, or
- Place them under a naming/category convention that current CI selection excludes.

No change is required to enable CI execution in this feature. A future feature can promote these tests into CI once stability is proven.

---

## Implementation Plan

### Phase 1: Add Focused Local Critical Flow Tests

**Objective:** Introduce the three high-value create-flow Playwright tests.

**Tasks:**
- [ ] Add transaction create E2E test
- [ ] Add account create E2E test
- [ ] Add category create E2E test
- [ ] Verify all three pass locally against API-hosted client

**Commit:**
- `test(e2e): add local critical create-flow playwright tests`

### Phase 2: Ensure CI Exclusion and Document Local Run Path

**Objective:** Keep suite local-only and make intent explicit.

**Tasks:**
- [ ] Verify GitHub Actions does not execute this critical local suite
- [ ] Document local run commands and prerequisites
- [ ] Document CI exclusion rationale in test docs/workflow notes

**Commit:**
- `docs(ci): document local-only execution for critical playwright flows`

---

## Testing Strategy

### E2E Scenarios (Playwright)

- [ ] Create transaction and verify visibility in primary transaction/calendar UI
- [ ] Create account and verify it appears/selects in account controls
- [ ] Create category and verify it appears in category UI and transaction category picker

### Manual Verification Checklist

- [ ] Start API locally and confirm client is served
- [ ] Run focused Playwright critical-flow tests locally
- [ ] Confirm tests are not included in current GitHub Actions execution

---

## Risks and Mitigations

- **Risk:** UI selector instability causes flaky tests.  
  **Mitigation:** Prefer semantic selectors and add stable test IDs only where needed.
- **Risk:** Local data collisions across repeated runs.  
  **Mitigation:** Use unique test data identifiers and isolate scenarios.
- **Risk:** Future drift between local and CI expectations.  
  **Mitigation:** Keep CI exclusion explicitly documented and revisit in a follow-up feature.

---

## Related Features

- Feature 062: Functional E2E Test Suite
- Feature 059: Performance E2E tests

---

## Changelog

| Date | Author | Description |
|------|--------|-------------|
| 2026-02-14 | AI | Created feature doc for local-only Playwright critical create-flow coverage |
