# Feature 154: DataHealthService — Fix Triple Transaction Load and O(n²) Near-Duplicate Detection

> **Status:** Proposed  
> **Severity:** 🔴 Critical — P-001  
> **Audit Source:** `docs/audit/2026-04-09-performance-review.md`  
> **Feature Flag:** `feature-data-health-optimized-analysis`

---

## Overview

`DataHealthService.AnalyzeAsync()` currently calls three sub-methods — `FindDuplicatesAsync`, `FindOutliersAsync`, and `FindDateGapsAsync` — each of which independently calls `GetAllForHealthAnalysisAsync()`. This means every health analysis request materialises the full transaction table **three times** from PostgreSQL into managed memory. Compounding this, `FindNearDuplicateClusters` uses a nested loop comparison (O(n²)) over the loaded transaction set. On a **Raspberry Pi ARM64** with 1–4 GB RAM, a 5,000-transaction database produces 15,000+ entity objects per analysis, with GC pressure and near-quadratic CPU cost. At 10,000+ transactions this risks out-of-memory termination.

This feature reduces the memory allocation to a single load per `AnalyzeAsync` call, eliminates the O(n²) duplicate comparison through batching/pagination, and pushes date-gap and outlier detection into SQL aggregations so the application layer handles only pre-aggregated data.

---

## Problem Statement

### Current State

- `AnalyzeAsync` (lines 49–89 of `DataHealthService.cs`) calls three sub-methods sequentially.
- Each sub-method independently calls `_transactionRepository.GetAllForHealthAnalysisAsync()`.
- `GetAllForHealthAnalysisAsync` (lines 528–544 of `TransactionRepository.cs`) loads ALL transactions with `.Include(t => t.Category)` and no `.Take()`.
- `FindNearDuplicateClusters` (lines 189–228 of `DataHealthService.cs`) iterates all pairs in a nested loop — O(n²) with no early exit or batching.
- `GetUncategorizedSummaryAsync` also loads all uncategorized transactions unbounded (covered by Doc 157 / P-004).

### Target State

- `AnalyzeAsync` fetches the transaction set **once** and passes the in-memory result to each sub-method — eliminating 2 of 3 full-table loads.
- Date-gap and outlier detection pushed into targeted SQL aggregations; `AnalyzeAsync` receives pre-computed summaries rather than raw entity lists.
- Duplicate detection operates in sorted, bounded windows (pagination/batching) rather than an unbounded nested loop.
- The optimised path is gated behind `feature-data-health-optimized-analysis`; the original path remains callable for comparison during rollout.

---

## User Stories

### US-154-001: Health Analysis Without Memory Exhaustion

**As a** user on a Raspberry Pi deployment  
**I want to** run the Data Health analysis without the application slowing or crashing  
**So that** I can identify duplicate and miscategorised transactions even as my transaction history grows

**Acceptance Criteria:**
- [ ] `AnalyzeAsync` calls `GetAll*` repository methods exactly **once** per invocation when the feature flag is enabled
- [ ] A 10,000-transaction database does not cause visible GC pauses or OOM errors during health analysis on a 2 GB Pi
- [ ] All existing `DataHealthService` unit tests continue to pass
- [ ] New unit tests assert the single-fetch contract on `AnalyzeAsync`

### US-154-002: Bounded Near-Duplicate Detection

**As a** user  
**I want** near-duplicate detection to complete in a predictable, bounded time  
**So that** health analysis does not stall on large transaction histories

**Acceptance Criteria:**
- [ ] `FindNearDuplicateClusters` no longer performs a nested O(n²) loop over the full transaction set
- [ ] Duplicate detection uses a date-windowed or paginated strategy (e.g., compare only transactions within a ±N day window when sorted by date)
- [ ] Unit test asserts that a 10,000-item input does not call the comparison predicate O(n²) times (use a counter or verify strategy)

---

## Technical Design

### Affected Files

| File | Layer | Change |
|------|-------|--------|
| `src/BudgetExperiment.Application/DataHealth/DataHealthService.cs` | Application | Refactor `AnalyzeAsync` to single-fetch; rewrite `FindNearDuplicateClusters` |
| `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs` | Infrastructure | Add projection-based sub-analysis queries (see Doc 157 / P-005) |
| `tests/BudgetExperiment.Application.Tests/DataHealth/DataHealthServiceTests.cs` | Tests | Add single-fetch contract tests, O(n²) guard test |

> **Note:** The projection query changes to `TransactionRepository` are specified separately in Feature 157 (P-004 / P-005). This doc assumes those projections are available; if implemented in the same PR, coordinate task ordering.

### Architectural Change: Single Fetch + Targeted Projections

```csharp
// Before (3 separate loads)
public async Task<DataHealthReport> AnalyzeAsync(...)
{
    var duplicates = await FindDuplicatesAsync(...);   // loads all transactions
    var outliers   = await FindOutliersAsync(...);     // loads all transactions again
    var gaps       = await FindDateGapsAsync(...);     // loads all transactions again
    ...
}

// After (single fetch, then pass to sub-methods)
public async Task<DataHealthReport> AnalyzeAsync(...)
{
    var transactions = await _transactionRepository
        .GetTransactionProjectionsForDuplicateDetectionAsync(scope);   // one load
    
    var duplicates = FindDuplicates(transactions);              // in-memory, windowed
    var outliers   = await _transactionRepository
        .GetOutlierSummaryAsync(scope);                         // SQL aggregation
    var gaps       = await _transactionRepository
        .GetDateGapSummaryAsync(scope);                         // SQL aggregation
    ...
}
```

### Near-Duplicate Algorithm Replacement

Replace the current nested loop with a **date-sorted sliding window**:

1. Sort transactions by `(AccountId, Date)`.
2. For each transaction, compare only against transactions within a ±3 day window.
3. Two transactions are near-duplicate candidates if `|date diff| ≤ 3 days && amount == amount && LevenshteinDistance(desc1, desc2) ≤ 2`.
4. This reduces worst-case from O(n²) to O(n × w) where w is the window size (constant ~6 days × avg daily count).

### Feature Flag

```json
{
  "FeatureFlags": {
    "feature-data-health-optimized-analysis": false
  }
}
```

The flag defaults to `false` in production until verified stable. Set to `true` in development and staging. The `IFeatureFlagService` (or equivalent) gates which code path `AnalyzeAsync` follows.

---

## Implementation Plan

### Phase 1: Tests (RED)

**Objective:** Write failing tests that express the correct contracts before touching production code.

**Tasks:**
- [ ] In `DataHealthServiceTests`, add `AnalyzeAsync_WithFeatureFlagEnabled_CallsGetAllExactlyOnce` — mock repository, assert single `GetAll*` call
- [ ] Add `FindNearDuplicateClusters_WithLargeInput_DoesNotExceedLinearWindowComparisons` — verify the replacement algorithm's call count against a controlled 1,000-item list
- [ ] Add `AnalyzeAsync_WithFeatureFlagDisabled_BehavesAsOriginal` — ensure flag-off path is unchanged
- [ ] Run tests — expect **RED** (production code not yet changed)

**Commit:**
```
test(app): failing tests for DataHealthService single-fetch and windowed dedup

Refs: Feature 154, P-001
```

---

### Phase 2: Projection Queries in TransactionRepository (GREEN prerequisite)

**Objective:** Add the projection repository methods this feature relies on (coordinate with Feature 157 if separate).

**Tasks:**
- [ ] Add `GetTransactionProjectionsForDuplicateDetectionAsync(BudgetScope scope)` returning `IReadOnlyList<DuplicateDetectionProjection>` with `(Id, AccountId, Date, Amount, Description)`
- [ ] Add `GetDateGapSummaryAsync(BudgetScope scope)` returning `IReadOnlyList<DateGapEntry>` (SQL aggregation: `(AccountId, Date)` only)
- [ ] Add `GetOutlierSummaryAsync(BudgetScope scope)` returning `IReadOnlyList<OutlierEntry>` (SQL aggregation: `(Description, Amount, StdDev)`)
- [ ] Add interface methods to the relevant `ITransactionRepository` sub-interface (or `ITransactionAnalyticsRepository` per Doc 150)
- [ ] Unit-test each new repository method against a known fixture data set

**Commit:**
```
feat(infra): add projection queries for DataHealth sub-analyses

Adds GetTransactionProjectionsForDuplicateDetectionAsync,
GetDateGapSummaryAsync, and GetOutlierSummaryAsync.

Refs: Feature 154, P-001, P-005
```

---

### Phase 3: Refactor DataHealthService (GREEN)

**Objective:** Implement the single-fetch refactor and windowed duplicate detection, making the Phase 1 tests pass.

**Tasks:**
- [ ] Introduce `DuplicateDetectionProjection`, `DateGapEntry`, `OutlierEntry` record types in Application layer
- [ ] Refactor `AnalyzeAsync` to call `GetTransactionProjectionsForDuplicateDetectionAsync` once, then pass result to `FindDuplicates`; call SQL-side aggregations for date-gap and outlier sub-analyses
- [ ] Replace `FindNearDuplicateClusters` nested loop with the date-sorted sliding window algorithm
- [ ] Gate the new path with `feature-data-health-optimized-analysis` flag check
- [ ] Run tests — expect **GREEN**

**Commit:**
```
perf(app): DataHealthService single-fetch + windowed near-duplicate detection

Reduces 3× full-table load to 1×. Replaces O(n²) nested loop with
O(n × window) sliding date comparison.

Gated by: feature-data-health-optimized-analysis
Fixes: P-001 (2026-04-09 performance audit)
Refs: Feature 154
```

---

### Phase 4: Verification and Cleanup

**Tasks:**
- [ ] Run full test suite: `dotnet test --filter "Category!=Performance"` — all green
- [ ] Run `dotnet format` — no style violations
- [ ] Enable flag in `appsettings.Development.json`
- [ ] Manually run health analysis against a development database; confirm report output is identical to original path
- [ ] Remove any TODO comments added during implementation

---

## Testing Strategy

### Unit Tests (Application.Tests)

- `AnalyzeAsync_WithFeatureFlagEnabled_CallsGetAllExactlyOnce`
- `AnalyzeAsync_WithFeatureFlagDisabled_CallsGetAllThreeTimes` (regression guard)
- `FindNearDuplicateClusters_WithWindowedInput_ReturnsSameResultsAsNestedLoop` (parity test with small input)
- `FindNearDuplicateClusters_WithLargeInput_DoesNotExceedLinearWindowComparisons`
- `AnalyzeAsync_WithNoTransactions_ReturnsEmptyReport`

### Integration Tests (Infrastructure.Tests)

- `GetTransactionProjectionsForDuplicateDetectionAsync_ReturnsExpectedProjectionShape`
- `GetDateGapSummaryAsync_WithGapInData_ReturnsCorrectGapEntries`
- `GetOutlierSummaryAsync_WithOutliers_ReturnsOutlierAboveThreshold`

---

## Security Considerations

No authentication or authorization surface changes. Data remains scope-filtered by existing repository predicates.

---

## Performance Considerations

- **Hardware target:** Raspberry Pi ARM64, 1–4 GB RAM, SD card I/O. Every unnecessary entity materialisation matters.
- **Expected improvement:** Memory allocation per `AnalyzeAsync` call reduced by ~66% (1 load instead of 3). Duplicate detection CPU reduced from O(n²) to O(n × w) where w ≈ 6–12.
- The feature flag allows safe A/B comparison before committing to the new path.

---

## References

- [2026-04-09 Performance Audit — P-001](../docs/audit/2026-04-09-performance-review.md#p-001-critical--datahealth-service-loads-all-transactions-into-memory-multiple-times)
- `src/BudgetExperiment.Application/DataHealth/DataHealthService.cs:49-89`, `:189-228`
- `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs:528-544`
- Feature 157 (P-004 / P-005 — projection queries)
- Engineering Guide §7 (SOLID), §8 (Clean Code — short methods), §11 (Data & Persistence)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit P-001 | Alfred (Lead) |
