# Feature 147: Recurring Projection / Realization Accuracy

> **Status:** Done

## Overview

This feature hardens the accuracy of recurring transaction projection and realization. Currently, `RecurringInstanceProjector` does not filter already-realized transactions—that responsibility lives in `AutoRealizeService`. This means the full pipeline is never tested as a unified behavior. The projector is missing an optional parameter to exclude already-realized dates, which would keep the domain pure and testable.

This feature adds an integration test suite with real PostgreSQL to prove **INV-7 (Recurring Projection No-Double-Count)** holds end-to-end: the sum of projected instances plus realized transactions equals the expected occurrence count, with no double-counting.

## Problem Statement

### Current State

- `RecurringInstanceProjector.GetInstancesByDateRangeAsync()` accepts recurring transactions and a date range, returning projected instances for future occurrences.
- `AutoRealizeService` separately queries for already-realized transactions (`GetByRecurringInstanceAsync`) and skips them during projection.
- The projector itself does not know about realized transactions—the filtering happens at the service layer.
- No unified accuracy test verifies `projected_count + realized_count = expected_occurrences` across a date range.
- No integration test with real PostgreSQL proves the pipeline doesn't double-count when auto-realization converts a projected instance to a realized one.

### Target State

- Enhance `RecurringInstanceProjector` to accept an optional set of already-realized dates (or a collection of realized transaction IDs).
- **Approach decision: Pass realized dates as optional parameter `excludeDates: ISet<DateOnly>?`**
  - Keeps the projector pure: same input always yields same output.
  - Testable in isolation: unit test can verify exclusion logic.
  - Service layer (e.g., `RecurringQueryService`) calls projector with excluded dates from repository query.
- New integration test: `RecurringAccuracy_ProjectedPlusRealized_EqualsExpectedOccurrences`
  - Set up a recurring transaction with 12 occurrences
  - Realize 5 of them manually
  - Query projector for full date range (excluding realized)
  - Assert: `7 projected + 5 realized = 12 expected`
- Feature flag `feature-recurring-projection-accuracy` gates the new `excludeDates` parameter and integration test infrastructure.

---

## User Stories

### Recurring Accuracy & Pipeline

#### US-147-001: Ensure No Double-Count of Recurring Instances
**As a** system maintainer  
**I want to** verify that a recurring transaction's occurrences are counted exactly once (either projected OR realized, not both)  
**So that** users see accurate forecasts and no duplicates in reports

**Acceptance Criteria:**
- [ ] Projector can exclude already-realized dates to prevent duplicates
- [ ] Integration test proves `projected + realized = expected` for a date range
- [ ] Auto-realization converts a projected instance to realized without creating a duplicate
- [ ] Test covers multiple realizations within a single range

#### US-147-002: Pure & Testable Projection Domain Logic
**As a** developer  
**I want to** unit test `RecurringInstanceProjector` without mocking the entire repository  
**So that** projection logic is decoupled from realization state

**Acceptance Criteria:**
- [ ] Projector accepts an optional `excludeDates` parameter
- [ ] Excludes parameter is applied before returning instances (no side effects)
- [ ] Unit test can verify exclusion in isolation with mocked data
- [ ] Projector's core logic (frequency-based occurrence calculation) is tested separately from realization

---

## Technical Design

### Architecture Changes

- Modify `RecurringInstanceProjector.GetInstancesByDateRangeAsync()` signature to include optional `ISet<DateOnly>? excludeDates` parameter.
- Create (or extend) a query service in Application layer to fetch realized dates and call projector with exclusion set.
- Update `IRecurringInstanceProjector` interface accordingly.

### Domain Model

No domain model changes. Uses existing:
- `RecurringTransaction` domain entity
- `RecurringInstanceInfoValue` value object (returned by projector)
- `ExceptionType` enum (Skipped, Modified, etc.)

```csharp
// Signature change to projector interface
public interface IRecurringInstanceProjector
{
    Task<Dictionary<DateOnly, List<RecurringInstanceInfoValue>>> GetInstancesByDateRangeAsync(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        DateOnly fromDate,
        DateOnly toDate,
        ISet<DateOnly>? excludeDates = null,  // NEW: optional excluded dates
        CancellationToken cancellationToken = default);

    Task<List<RecurringInstanceInfoValue>> GetInstancesForDateAsync(
        IReadOnlyList<RecurringTransaction> recurringTransactions,
        DateOnly date,
        CancellationToken cancellationToken = default);
}
```

### API Endpoints

No new endpoints. This is a correctness hardening, not a user-facing feature. Existing endpoints using the projector benefit automatically.

### Database Changes

No new tables or columns. Uses existing recurring transaction and realized transaction data.

### UI Components

No UI changes. This is infrastructure/accuracy hardening.

---

## Implementation Plan

### Phase 1: Projector Signature & Implementation

**Objective:** Add `excludeDates` parameter to projector and implement filtering.

**Tasks:**
- [ ] Update `IRecurringInstanceProjector` interface:
  - Add `ISet<DateOnly>? excludeDates = null` parameter to `GetInstancesByDateRangeAsync`
  - Add nullable parameter to `GetInstancesForDateAsync` for consistency
- [ ] Modify `RecurringInstanceProjector` implementation:
  ```csharp
  public async Task<Dictionary<DateOnly, List<RecurringInstanceInfoValue>>> GetInstancesByDateRangeAsync(
      IReadOnlyList<RecurringTransaction> recurringTransactions,
      DateOnly fromDate,
      DateOnly toDate,
      ISet<DateOnly>? excludeDates = null,
      CancellationToken cancellationToken = default)
  {
      var result = new Dictionary<DateOnly, List<RecurringInstanceInfoValue>>();
      // ... existing code ...
      
      foreach (var date in occurrences)
      {
          // NEW: Skip if in exclude set
          if (excludeDates?.Contains(date) == true)
          {
              continue;
          }
          
          // ... rest of existing logic ...
      }
  }
  ```
- [ ] Unit tests:
  - `RecurringInstanceProjector_ExcludeDates_SkipsExcludedOccurrences`
  - `RecurringInstanceProjector_ExcludeEmpty_ReturnsAll`
  - `RecurringInstanceProjector_ExcludeAll_ReturnsEmpty`
- [ ] Update all call sites of `GetInstancesByDateRangeAsync` to pass `null` for `excludeDates` (backward compat)

**Commit:**
```bash
git add .
git commit -m "feat(app): add excludeDates parameter to RecurringInstanceProjector

- Add optional excludeDates: ISet<DateOnly>? parameter
- Filter occurrences before returning instances
- Enables pure, testable projection logic
- Unit tests for exclusion behavior

Refs: INV-7"
```

---

### Phase 2: Query Service & Realization Integration

**Objective:** Create a service layer that queries realized dates and calls projector with exclusion.

**Tasks:**
- [ ] Create `IRecurringQueryService` in Application layer (or extend existing):
  ```csharp
  public interface IRecurringQueryService
  {
      Task<Dictionary<DateOnly, List<RecurringInstanceInfoValue>>> GetProjectedInstancesAsync(
          IReadOnlyList<RecurringTransaction> recurringTransactions,
          DateOnly fromDate,
          DateOnly toDate,
          Guid? accountId = null,
          CancellationToken cancellationToken = default);
  }
  ```
- [ ] Implement `RecurringQueryService`:
  - Fetch all realized transactions (by account, if specified) for the date range
  - Collect their occurrence dates into a set
  - Call `_projector.GetInstancesByDateRangeAsync(..., excludeDates: realizedSet, ...)`
  - Return the result
- [ ] Unit tests (mocked repositories):
  - `RecurringQueryService_WithRealizations_ExcludesThemFromProjection`
  - `RecurringQueryService_NoRealizations_ReturnsAllProjections`
- [ ] Register in DependencyInjection

**Commit:**
```bash
git add .
git commit -m "feat(app): add RecurringQueryService for projection + realization integration

- Fetch realized dates from repository
- Call projector with excludeDates parameter
- Ensures no double-count at service layer
- Unit tests for integration

Refs: INV-7"
```

---

### Phase 3: Integration & Accuracy Tests

**Objective:** Prove INV-7 end-to-end with PostgreSQL.

**Tasks:**
- [ ] Create `RecurringProjectionAccuracyTests.cs` in `BudgetExperiment.Infrastructure.Tests/Accuracy/`
- [ ] Integration test with Testcontainers (PostgreSQL):
  - Set up account and recurring transaction (e.g., every Friday for 3 months = 12 occurrences)
  - Realize 5 of them manually (create actual `Transaction` with `RecurringTransactionId` set)
  - Call `RecurringQueryService.GetProjectedInstancesAsync` for full 3-month range
  - Assert:
    - Projected instances: 7 (12 - 5 realized)
    - Query repository for realized: 5 transactions with matching `RecurringTransactionId`
    - Sum: 7 + 5 = 12 ✓
  - Test case: `RecurringProjectionAccuracy_ProjectedPlusRealized_EqualsExpectedOccurrences`
- [ ] Variant tests:
  - `RecurringProjectionAccuracy_NoRealizations_ProjectsAll`
  - `RecurringProjectionAccuracy_AllRealized_ProjectsNone`
  - `RecurringProjectionAccuracy_PartialMonth_CountsCorrectly`
- [ ] Verify exceptions are honored (skip dates excluded from count)

**Commit:**
```bash
git add .
git commit -m "test(infra): add recurring projection + realization integration accuracy tests

- Integration test with Testcontainers PostgreSQL
- Prove projected + realized = expected (INV-7)
- Test partial realization across date range
- Verify skip exceptions honored in count

Refs: INV-7"
```

---

### Phase 4: Feature Flag & Documentation

**Objective:** Gate the new parameter and document changes.

**Tasks:**
- [ ] Add feature flag `feature-recurring-projection-accuracy` (gates use of `excludeDates` parameter and integration test infrastructure)
  - Optional: controller/service can check flag before using parameter
  - Primary purpose: allows rollout of integration test infrastructure independently
- [ ] Update `docs/ACCURACY-FRAMEWORK.md` section 6 to mark INV-7 as covered end-to-end
- [ ] Add XML comments to public APIs
- [ ] Update any service-level documentation (if exists)
- [ ] Verify all tests pass: `dotnet test --filter "Category!=Performance"`

**Commit:**
```bash
git add .
git commit -m "docs(recurring): complete recurring projection accuracy documentation

- Feature flag: feature-recurring-projection-accuracy
- Update accuracy framework (INV-7 now covered end-to-end)
- XML comments for public APIs
- Integration test suite ready for use

Refs: INV-7"
```

---

## Testing Strategy

### Unit Tests

- **Projector exclusion logic (new):**
  - `RecurringInstanceProjector_ExcludeDates_SkipsExcludedOccurrences`
  - `RecurringInstanceProjector_ExcludeEmpty_ReturnsAll`
  - `RecurringInstanceProjector_ExcludeAll_ReturnsEmpty`
  - `RecurringInstanceProjector_ExcludeUnrelatedDates_HasNoEffect`

- **Query service integration:**
  - `RecurringQueryService_WithRealizations_ExcludesThemFromProjection`
  - `RecurringQueryService_NoRealizations_ReturnsAllProjections`
  - `RecurringQueryService_NullAccountId_FetchesAllAccounts`

### Integration Tests

- **End-to-end with PostgreSQL (Testcontainers):**
  - `RecurringProjectionAccuracy_ProjectedPlusRealized_EqualsExpectedOccurrences`
  - `RecurringProjectionAccuracy_NoRealizations_ProjectsAll` (12 projected, 0 realized, 12 expected)
  - `RecurringProjectionAccuracy_AllRealized_ProjectsNone` (0 projected, 12 realized, 12 expected)
  - `RecurringProjectionAccuracy_PartialMonth_CountsCorrectly`
  - `RecurringProjectionAccuracy_SkippedExceptions_NotCountedAsBothProjectedAndRealized`

### Manual Testing Checklist

- [ ] Dashboard/forecast view shows no duplicate charges when recurring items are auto-realized
- [ ] Monthly forecast total = sum of projected occurrences + sum of auto-realized occurrences (no double-count)
- [ ] Reports reflect accurate counts for recurring transactions

---

## Migration Notes

No database migration required. This feature uses existing `RecurringTransactionId` and realization queries.

---

## Security Considerations

- **No new security concerns:** This is a correctness feature affecting internal projection logic.
- **Authorization:** Existing repository queries already enforce user scope; no changes needed.

---

## Performance Considerations

- **Excluded set lookup:** `excludeDates.Contains(date)` is O(1) hash lookup; minimal overhead.
- **Query optimization:** Fetching realized dates once per service call (not per occurrence) avoids N+1 queries.
- **Acceptable latency:** < 500ms for 3-month range with 100+ occurrences.

---

## Future Enhancements

- **Realized transaction filtering options:** Allow filtering by account, category, or status in the query service.
- **Caching realized dates:** For frequently queried ranges, cache the realized date set to improve performance.
- **Analytics:** Track how many recurring instances are realized early, late, or on-time (requires additional timestamps).

---

## References

- [INV-7: Recurring Projection No-Double-Count](./ACCURACY-FRAMEWORK.md#inv-7-recurring-projection-no-double-count)
- [RecurringInstanceProjector](../src/BudgetExperiment.Application/Recurring/RecurringInstanceProjector.cs)
- [AutoRealizeService](../src/BudgetExperiment.Application/Recurring/AutoRealizeService.cs)
- [CAT-7: Recurring Projection Accuracy](./ACCURACY-FRAMEWORK.md#cat-7-recurring-projection-accuracy-application-tests)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-09 | Initial planning draft | Alfred (Lead) |
