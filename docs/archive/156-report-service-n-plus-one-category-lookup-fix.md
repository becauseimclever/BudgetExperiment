# Feature 156: ReportService — Fix N+1 Category Name Lookups

> **Status:** Done  
> **Severity:** 🟠 High — P-003  
> **Audit Source:** `docs/audit/2026-04-09-performance-review.md`

---

## Overview

`ReportService.BuildCategorySpendingListAsync` and `BuildTopCategoriesAsync` each call a per-category repository lookup (`GetByIdAsync`) inside a loop to resolve category names and colours. However, `GetByDateRangeAsync` — which loads transactions for reports — already applies `.Include(t => t.Category)`. The category data is therefore **already present** on every loaded transaction, making each `GetByIdAsync` call a redundant, wasted database round-trip.

With 15 categories in a month's report, this produces 15 individual `SELECT … WHERE Id = @id` queries after the data is already in memory. On a Raspberry Pi, this adds 50–200 ms of latency per query, totalling 750 ms–3 s of unnecessary I/O on every report generation.

This is a pure refactor — no domain logic changes, no new APIs.

---

## Problem Statement

### Current State

```csharp
// BuildCategorySpendingListAsync (lines 267-289)
foreach (var group in categoryGroups)
{
    var category = await ResolveCategoryDetailsAsync(group.Key);  // hits DB: GetByIdAsync
    ...
}

// BuildTopCategoriesAsync (lines 200-224)
foreach (var group in topGroups)
{
    var name = await ResolveCategoryNameAsync(group.Key);  // hits DB: GetByIdAsync
    ...
}
```

`GetByDateRangeAsync` already `.Include(t => t.Category)`, so every transaction object in `categoryGroups` carries a populated `.Category` navigation property.

### Target State

```csharp
// Pre-build lookup dictionary once, before any loop
var categoryLookup = transactions
    .Where(t => t.Category != null)
    .Select(t => t.Category!)
    .DistinctBy(c => c.Id)
    .ToDictionary(c => c.Id);

// Use dictionary in loops — zero additional DB calls
foreach (var group in categoryGroups)
{
    if (!categoryLookup.TryGetValue(group.Key, out var category)) continue;
    ...
}
```

Result: **0 additional category DB queries** after the initial transaction load.

---

## User Stories

### US-156-001: Fast Report Generation

**As a** user generating a monthly or date-range spending report  
**I want** the report to render without a per-category database round-trip  
**So that** report pages load in well under a second on my Raspberry Pi

**Acceptance Criteria:**
- [x] `BuildCategorySpendingListAsync` issues **0** calls to `GetByIdAsync` when building its category list
- [x] `BuildTopCategoriesAsync` issues **0** calls to `GetByIdAsync` when building its top category list
- [x] Report output (names, colours, amounts) is identical to the previous implementation for the same input data
- [x] No feature flag required — pure refactor
- [x] Existing `ReportService` tests continue to pass
- [x] New tests assert that `ICategoryRepository.GetByIdAsync` is never called during report construction

---

## Technical Design

### Affected Files

| File | Layer | Change |
|------|-------|--------|
| `src/BudgetExperiment.Application/Reports/ReportService.cs` | Application | Pre-build category dictionary; remove `ResolveCategoryDetailsAsync` / `ResolveCategoryNameAsync` DB calls |
| `tests/BudgetExperiment.Application.Tests/Reports/ReportServiceTests.cs` | Tests | Assert zero `GetByIdAsync` calls; parity tests |

### Change Pattern

1. After loading transactions via `GetByDateRangeAsync`, extract the navigation property to a dictionary:

   ```csharp
   var categoryLookup = transactions
       .Where(t => t.Category is not null)
       .Select(t => t.Category!)
       .DistinctBy(c => c.Id)
       .ToDictionary(c => c.Id);
   ```

2. Replace every `await ResolveCategoryDetailsAsync(id)` and `await ResolveCategoryNameAsync(id)` call with a `categoryLookup.TryGetValue(id, out var cat)` lookup.

3. If a category is not found in the lookup (edge case: transaction with a deleted category), fall back to a placeholder name (e.g., `"Unknown"`) rather than issuing a DB query. This maintains report completeness without introducing network I/O.

4. Delete (or make private/internal) the `ResolveCategoryDetailsAsync` and `ResolveCategoryNameAsync` helper methods if they are no longer called elsewhere; if they are called from other methods, leave them but do not call them from the loop.

### No API or Domain Changes

This change is entirely within the Application layer. No DTO changes, no endpoint changes, no migrations.

---

## Implementation Plan

### Phase 1: Tests (RED)

**Objective:** Write failing tests asserting zero `GetByIdAsync` calls during report construction.

**Tasks:**
- [ ] In `ReportServiceTests`, add `BuildCategorySpendingListAsync_NeverCallsGetByIdAsync` — mock `ICategoryRepository`, assert `GetByIdAsync` not called after report generation
- [ ] Add `BuildTopCategoriesAsync_NeverCallsGetByIdAsync` — same assertion pattern
- [ ] Add `BuildCategorySpendingListAsync_WithCategoryOnNavProperty_ReturnsSameNamesAsOriginalPath` — parity test
- [ ] Run tests — expect **RED**

**Commit:**
```
test(app): failing tests for ReportService zero GetByIdAsync calls

Refs: Feature 156, P-003
```

---

### Phase 2: Refactor ReportService (GREEN)

**Objective:** Replace per-category DB lookups with navigation property dictionary.

**Tasks:**
- [ ] Identify all call sites of `ResolveCategoryDetailsAsync` and `ResolveCategoryNameAsync` within `BuildCategorySpendingListAsync`, `BuildTopCategoriesAsync`, and any other methods in `ReportService`
- [ ] Add `categoryLookup` dictionary construction immediately after `GetByDateRangeAsync` call
- [ ] Replace all resolver calls in the identified loops with `categoryLookup.TryGetValue` + fallback
- [ ] Add `"Unknown"` fallback string constant (or existing project constant if one exists) for missing categories
- [ ] Run tests — expect **GREEN**

**Commit:**
```
perf(app): ReportService uses navigation property for category names

Eliminates per-category GetByIdAsync calls in BuildCategorySpendingListAsync
and BuildTopCategoriesAsync. Pre-builds dictionary from already-loaded
transaction Category navigation properties.

Fixes: P-003 (2026-04-09 performance audit)
Refs: Feature 156
```

---

### Phase 3: Verification

**Tasks:**
- [ ] Run full test suite: `dotnet test --filter "Category!=Performance"` — all green
- [ ] Run `dotnet format` — no style violations
- [ ] Enable EF Core query logging in development; verify no extra category SELECT queries on report endpoints

---

## Testing Strategy

### Unit Tests (Application.Tests)

- `BuildCategorySpendingListAsync_NeverCallsGetByIdAsync`
- `BuildTopCategoriesAsync_NeverCallsGetByIdAsync`
- `BuildCategorySpendingListAsync_WithCategoryOnNavProperty_ReturnsSameNamesAsOriginalPath`
- `BuildCategorySpendingListAsync_WithMissingCategory_UsesUnknownFallback`
- `BuildTopCategoriesAsync_WithMissingCategory_UsesUnknownFallback`

### No New Integration Tests Required

This is a pure in-memory change to how already-loaded data is accessed. The existing report integration tests cover end-to-end correctness.

---

## Security Considerations

None — this change is query-elimination only. All data access continues to go through the existing repository scope filters.

---

## Performance Considerations

- **Hardware target:** Raspberry Pi ARM64. Per-query latency is high on SD card I/O.
- **Expected improvement:** Report generation reduced from N+15 queries (transactions + one per category) to N+0 (transactions only). Saving 750 ms–3 s per report on Pi hardware.
- Dictionary construction is O(n) over the already-loaded transaction list — negligible.

---

## References

- [2026-04-09 Performance Audit — P-003](../docs/audit/2026-04-09-performance-review.md#p-003-high--reportservice-n1-query-for-category-names-in-buildcategoryspendinglistasync)
- `src/BudgetExperiment.Application/Reports/ReportService.cs:267-289` (`BuildCategorySpendingListAsync`)
- `src/BudgetExperiment.Application/Reports/ReportService.cs:200-224` (`BuildTopCategoriesAsync`)
- Engineering Guide §8 (Clean Code), §11 (no lazy loading — use explicit `.Include()` and navigate from it)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit P-003 | Alfred (Lead) |
