# Feature 155: BudgetProgressService — Fix N+1 Category Spending Queries

> **Status:** Done  
> **Severity:** 🟠 High — P-002  
> **Audit Source:** `docs/audit/2026-04-09-performance-review.md`

---

## Overview

`BudgetProgressService.GetMonthlySummaryAsync()` iterates over all active expense categories and calls `GetSpendingByCategoryAsync(category.Id, year, month)` once per category. With a typical budget of 20 expense categories, this is **20 sequential database round-trips** for a single page load. On a Raspberry Pi with SD card I/O and a single-core-dominated PostgreSQL process, each query adds 50–200 ms — totalling 1–4 seconds for a page the user visits on every navigation.

The correct pattern — a single grouped SQL query — is already demonstrated by `GetDailyTotalsAsync` in the same repository. This feature ports that pattern to monthly budget summaries.

---

## Problem Statement

### Current State

```csharp
// BudgetProgressService.cs:88-120 — simplified
foreach (var category in allExpenseCategories.Where(c => c.IsActive))
{
    var spent = await _transactionRepository
        .GetSpendingByCategoryAsync(category.Id, year, month);  // 1 query per category
    ...
}
```

With 20 active expense categories this produces 20 sequential `SELECT SUM(Amount) … WHERE CategoryId = @id AND …` queries.

### Target State

```csharp
// After: single grouped query
var spendingByCategory = await _transactionRepository
    .GetSpendingByCategoriesAsync(year, month, scope);  // 1 query, returns Dictionary<Guid, decimal>

foreach (var category in allExpenseCategories.Where(c => c.IsActive))
{
    var spent = spendingByCategory.GetValueOrDefault(category.Id, 0m);
    ...
}
```

Result: exactly **1 database query** regardless of category count.

---

## User Stories

### US-155-001: Fast Budget Page Load

**As a** user who opens the Budget page frequently  
**I want** the monthly summary to load in under 500 ms  
**So that** I can review my spending position without waiting

**Acceptance Criteria:**
- [ ] `GetMonthlySummaryAsync` issues **exactly 1** database query for spending totals regardless of category count
- [ ] Monthly spending totals returned are identical to the previous implementation for identical input data
- [ ] No feature flag required — behaviour-identical refactor
- [ ] Existing `BudgetProgressService` tests all pass without modification (or are updated to reflect the new query structure)
- [ ] New unit test asserts the single-query contract

---

## Technical Design

### Affected Files

| File | Layer | Change |
|------|-------|--------|
| `src/BudgetExperiment.Application/Budgeting/BudgetProgressService.cs` | Application | Replace per-category loop calls with single dictionary lookup |
| `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs` | Infrastructure | Add `GetSpendingByCategoriesAsync` |
| Interface(s) for `ITransactionRepository` (or `ITransactionAnalyticsRepository` per Doc 150) | Domain/Application | Add method signature |
| `tests/BudgetExperiment.Application.Tests/Budgeting/BudgetProgressServiceTests.cs` | Tests | Add single-query contract test |

### New Repository Method

```csharp
/// <summary>
/// Returns spending totals grouped by category for the given year and month.
/// </summary>
Task<Dictionary<Guid, decimal>> GetSpendingByCategoriesAsync(
    int year, int month, BudgetScope scope, CancellationToken cancellationToken = default);
```

**EF Core implementation** (mirrors `GetDailyTotalsAsync` pattern):

```csharp
return await _context.Transactions
    .AsNoTracking()
    .ApplyScopeFilter(scope)
    .Where(t => t.Date.Year == year && t.Date.Month == month
                && !t.IsTransfer && t.CategoryId != null)
    .GroupBy(t => t.CategoryId!.Value)
    .Select(g => new { CategoryId = g.Key, Total = g.Sum(t => t.Amount) })
    .ToDictionaryAsync(x => x.CategoryId, x => x.Total, cancellationToken);
```

### No API or Domain Changes

This change is entirely within the Application and Infrastructure layers. No DTO, endpoint, or migration changes.

---

## Implementation Plan

### Phase 1: Tests (RED)

**Objective:** Write a failing test asserting the single-query contract.

**Tasks:**
- [ ] In `BudgetProgressServiceTests`, add `GetMonthlySummaryAsync_IssuesExactlyOneSpendingQuery` — mock repository, provide 20 categories, assert `GetSpendingByCategoriesAsync` called once and `GetSpendingByCategoryAsync` never called
- [ ] Run tests — expect **RED**

**Commit:**
```
test(app): failing test for BudgetProgress single spending query

Refs: Feature 155, P-002
```

---

### Phase 2: Repository Method (GREEN prerequisite)

**Objective:** Implement `GetSpendingByCategoriesAsync` in the repository and its interface.

**Tasks:**
- [ ] Add `GetSpendingByCategoriesAsync(int year, int month, BudgetScope scope, CancellationToken)` to the appropriate repository interface
- [ ] Implement using `GroupBy`/`Sum`/`ToDictionaryAsync` as shown above
- [ ] Add integration test: `GetSpendingByCategoriesAsync_WithKnownData_ReturnsSameTotalsAsPerCategoryQueries`

**Commit:**
```
feat(infra): add GetSpendingByCategoriesAsync to TransactionRepository

Single GROUP BY query replaces per-category SUM queries.

Refs: Feature 155, P-002
```

---

### Phase 3: Refactor BudgetProgressService (GREEN)

**Objective:** Replace the N+1 loop with a dictionary lookup.

**Tasks:**
- [ ] Replace the `foreach` loop body that calls `GetSpendingByCategoryAsync` with a single pre-call to `GetSpendingByCategoriesAsync` followed by `GetValueOrDefault` lookups
- [ ] Verify parity: the refactored method returns identical `BudgetProgressSummary` for the same inputs
- [ ] Run tests — expect **GREEN**

**Commit:**
```
perf(app): BudgetProgressService replaces N+1 queries with single GROUP BY

20 sequential DB round-trips → 1 query for monthly spending summary.

Fixes: P-002 (2026-04-09 performance audit)
Refs: Feature 155
```

---

### Phase 4: Verification

**Tasks:**
- [ ] Run full test suite: `dotnet test --filter "Category!=Performance"` — all green
- [ ] Run `dotnet format` — no style violations
- [ ] Review generated SQL in development logs; confirm single `GROUP BY` query emitted

---

## Testing Strategy

### Unit Tests (Application.Tests)

- `GetMonthlySummaryAsync_IssuesExactlyOneSpendingQuery`
- `GetMonthlySummaryAsync_WithCategoryNotInSpendingResult_ReturnsZeroSpend`
- `GetMonthlySummaryAsync_WithNoCategories_ReturnsEmptySummary`

### Integration Tests (Infrastructure.Tests)

- `GetSpendingByCategoriesAsync_WithKnownData_ReturnsSameTotalsAsPerCategoryQueries`
- `GetSpendingByCategoriesAsync_WithNoTransactionsForMonth_ReturnsEmptyDictionary`

---

## Security Considerations

None — scope filtering is already applied at the repository level.

---

## Performance Considerations

- **Hardware target:** Raspberry Pi ARM64. Sequential DB round-trips are especially costly on SD card I/O.
- **Expected improvement:** Budget page load time reduced from ~1–4 s (20 queries) to ~50–100 ms (1 query).
- No memory footprint increase — the dictionary payload is comparable to the individual scalar results.

---

## References

- [2026-04-09 Performance Audit — P-002](../docs/audit/2026-04-09-performance-review.md#p-002-high--budgetprogressservice-n1-query-pattern-in-getmonthlysummaryasync)
- `src/BudgetExperiment.Application/Budgeting/BudgetProgressService.cs:88-120`
- `GetDailyTotalsAsync` — existing GROUP BY pattern in `TransactionRepository` (reference implementation)
- Engineering Guide §11 (Data & Persistence — push aggregation to SQL)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit P-002 | Alfred (Lead) |
