# Feature 122: Test Coverage Gaps and Vanity Test Cleanup
> **Status:** Pending

## Overview

A code quality review on `feature/code-quality-review` identified meaningful test coverage gaps across two controllers and four repositories, plus approximately 20 vanity enum tests that verify integer values and can never detect a real regression. This work adds real tests where they are missing and removes tests that provide false confidence, leaving the suite trustworthy and actionable.

## Problem Statement

### Current State

**Untested controllers:**
- `RecurringChargeSuggestionsController` — 4 endpoints with zero test coverage.
- `RecurringController` — 2 endpoints (`GetPastDueAsync`, `RealizeBatchAsync`) with zero test coverage.

**Untested repositories:**
- `AppSettingsRepository`
- `CustomReportLayoutRepository`
- `RecurringChargeSuggestionRepository`
- `UserSettingsRepository`

**Vanity tests (~20):**
- Enum tests that assert `(int)SomeEnum.Value == 42`. These pass even if the enum member is renamed, the member is deleted and a new one added with the same integer, or the enum's semantic meaning changes entirely. They protect only against deliberate integer reassignment — a scenario that never causes a real runtime regression.

### Target State

- All four repositories have integration tests exercising their primary read/write operations against a real database.
- All uncovered controller endpoints have API integration tests covering happy path, validation failure (400), and not-found (404) cases where applicable.
- Vanity enum integer-value tests are removed from the test suite. Any legitimate concern about enum serialisation is covered by a serialisation-contract test, not an integer assertion.

---

## Acceptance Criteria

### Controller Coverage

- [ ] `RecurringChargeSuggestionsController` — all 4 uncovered endpoints have tests covering: happy path (2xx), not-found (404), and invalid request (400) where applicable.
- [ ] `RecurringController.GetPastDueAsync` — tests cover: returns 200 with a list when past-due items exist; returns 200 with empty list when none exist.
- [ ] `RecurringController.RealizeBatchAsync` — tests cover: valid batch request returns 200/204; invalid/empty batch returns 400.

### Repository Coverage

- [ ] `AppSettingsRepository` — integration tests cover: get existing settings, get when not found, upsert/save.
- [ ] `CustomReportLayoutRepository` — integration tests cover: get by id, list by user/scope, create, update, delete.
- [ ] `RecurringChargeSuggestionRepository` — integration tests cover: get by id, list by account and status, create, update status.
- [ ] `UserSettingsRepository` — integration tests cover: get by user id, create, update.
- [ ] All repository tests run against the Testcontainers PostgreSQL fixture (see Feature 121), not in-memory.

### Vanity Test Removal

- [ ] All tests whose sole assertion is `Assert.Equal(<integer>, (int)SomeEnum.Member)` or equivalent are identified and removed.
- [ ] No enum test file is left containing only vanity assertions; files are deleted if empty after cleanup.
- [ ] If any enum is used in a JSON serialisation contract where the integer value genuinely matters (e.g., stored in DB as integer), a targeted serialisation-contract test replaces the vanity test.

### General

- [ ] Overall test suite remains green after all additions and removals: `dotnet test --filter "Category!=Performance"`.
- [ ] Code coverage does not regress on any already-covered area.

---

## Technical Design

### Controller Tests (API Integration)

Controller tests use `WebApplicationFactory` (already wired in `BudgetExperiment.Api.Tests`). After Feature 121 lands, the factory uses a real PostgreSQL container. Each controller test class:

1. Seeds the minimum required data in `InitializeAsync`.
2. Calls the endpoint via `HttpClient`.
3. Asserts status code and response body shape.

Relevant test classes to create:
- `RecurringChargeSuggestionsControllerTests` in `BudgetExperiment.Api.Tests`
- `RecurringControllerTests` (extend or add to existing) in `BudgetExperiment.Api.Tests`

### Repository Tests (Infrastructure Integration)

Repository tests use `PostgreSqlContainerFixture` (introduced in Feature 121). Each test:

1. Resolves the repository through a `BudgetDbContext` scoped to the test connection.
2. Seeds data directly via `DbContext` for arrange steps.
3. Calls the repository method under test.
4. Asserts the result without relying on EF Core tracking.

New test classes in `BudgetExperiment.Infrastructure.Tests`:
- `AppSettingsRepositoryTests`
- `CustomReportLayoutRepositoryTests`
- `RecurringChargeSuggestionRepositoryTests`
- `UserSettingsRepositoryTests`

### Vanity Enum Test Identification

Search pattern to locate vanity tests:

```powershell
Select-String -Path "tests\**\*Tests.cs" -Pattern "\(int\)\w+\.\w+\s*==" -Recurse
```

Review each hit. Remove the test if its only value is asserting an integer constant. If an enum drives JSON serialisation and its integer values are part of a stored contract, replace with a serialisation round-trip test:

```csharp
[Fact]
public void SomeEnum_SerializesAsExpectedJsonValues()
{
    var json = JsonSerializer.Serialize(SomeEnum.Pending);
    Assert.Equal("\"Pending\"", json); // string serialisation, not integer
}
```

---

## Implementation Plan

### Phase 1: Repository Integration Tests

**Objective:** Add integration tests for the four untested repositories.

**Tasks:**
- [ ] Confirm Feature 121 `PostgreSqlContainerFixture` is available (or add a temporary in-project copy if 121 has not landed)
- [ ] Write `AppSettingsRepositoryTests` — get, upsert
- [ ] Write `CustomReportLayoutRepositoryTests` — CRUD by user/scope
- [ ] Write `RecurringChargeSuggestionRepositoryTests` — list by account+status, status transitions
- [ ] Write `UserSettingsRepositoryTests` — get by user, upsert
- [ ] Run infrastructure tests — confirm green

**Commit:**
```bash
git commit -m "test(infra): add integration tests for four untested repositories

- AppSettingsRepository: get, upsert
- CustomReportLayoutRepository: list/create/update/delete
- RecurringChargeSuggestionRepository: filter by status, transitions
- UserSettingsRepository: get, upsert

Refs: #122"
```

---

### Phase 2: Controller API Tests

**Objective:** Add API integration tests for uncovered endpoints on `RecurringChargeSuggestionsController` and `RecurringController`.

**Tasks:**
- [ ] Write `RecurringChargeSuggestionsControllerTests` covering all 4 uncovered endpoints (happy path, 400, 404)
- [ ] Extend `RecurringControllerTests` (or create if absent) for `GetPastDueAsync` and `RealizeBatchAsync`
- [ ] Seed required fixtures in test setup
- [ ] Run API tests — confirm green

**Commit:**
```bash
git commit -m "test(api): add coverage for RecurringChargeSuggestions and Recurring endpoints

- RecurringChargeSuggestionsController: detect, list, accept, dismiss
- RecurringController: GetPastDueAsync, RealizeBatchAsync
- Happy path, 400, and 404 scenarios

Refs: #122"
```

---

### Phase 3: Remove Vanity Enum Tests

**Objective:** Identify and remove all enum integer-value tests; replace with serialisation tests where the contract genuinely matters.

**Tasks:**
- [ ] Search for `(int)` casts inside test assertion expressions
- [ ] Review each hit; remove pure vanity assertions
- [ ] For any enum with a real serialisation contract, write a targeted replacement test
- [ ] Delete now-empty test files
- [ ] Run full test suite — confirm green and coverage not regressed

**Commit:**
```bash
git commit -m "test: remove vanity enum integer-value tests

- ~20 tests asserting (int)Enum.Member == N removed
- No regression: enum integer values are not part of any stored contract
- Serialisation contract tests added where JSON shape matters

Refs: #122"
```

---

## Notes

- Feature 121 (Testcontainers migration) should land before or alongside Phase 1 of this feature so repository tests run against PostgreSQL from the start.
- The vanity enum tests give a misleadingly high line-coverage number. Removing them will lower the raw coverage percentage slightly but raise the quality of that metric.
