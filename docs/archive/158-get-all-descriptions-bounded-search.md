# Feature 158: GetAllDescriptionsAsync — Add Bounded Prefix Search

> **Status:** Done  
> **Severity:** 🟠 High — P-006  
> **Audit Source:** `docs/audit/2026-04-09-performance-review.md`

---

## Overview

`TransactionRepository.GetAllDescriptionsAsync()` (lines 314–322) returns all distinct transaction descriptions across the user's entire transaction history with no limit. While it projects to strings rather than full entities, the result set grows linearly with history: a user with three years of transactions could have 5,000+ distinct description strings loaded on every call. The method is used for autocomplete and rule-matching scenarios where loading the complete description corpus is never necessary — a search prefix is always known at the call site.

This feature adds a `searchPrefix` parameter and a `Take(100)` cap to the query, making the method safe to call as transaction history grows, and updates all Application-layer callers to pass context.

---

## Problem Statement

### Current State

```csharp
// TransactionRepository.cs:314-322
public async Task<IReadOnlyList<string>> GetAllDescriptionsAsync()
{
    return await _context.Transactions
        .AsNoTracking()
        .ApplyScopeFilter(_scope)
        .Select(t => t.Description)
        .Distinct()
        .ToListAsync();  // no Where, no Take — returns entire distinct description set
}
```

With years of transaction history, this returns thousands of strings. All callers in the Application layer receive them all, then filter in-memory.

### Target State

```csharp
// After
public async Task<IReadOnlyList<string>> GetAllDescriptionsAsync(
    string searchPrefix = "", int maxResults = 100, CancellationToken cancellationToken = default)
{
    var query = _context.Transactions
        .AsNoTracking()
        .ApplyScopeFilter(_scope)
        .Select(t => t.Description)
        .Distinct();

    if (!string.IsNullOrWhiteSpace(searchPrefix))
    {
        query = query.Where(d => d.StartsWith(searchPrefix));
    }

    return await query
        .OrderBy(d => d)
        .Take(maxResults)
        .ToListAsync(cancellationToken);
}
```

Result: the database filters and limits the result set before sending data over the wire — always bounded at 100 results.

---

## User Stories

### US-158-001: Scalable Description Autocomplete

**As a** user with years of transaction history  
**I want** description autocomplete and suggestion matching to remain fast and memory-efficient  
**So that** the application performs well on my Raspberry Pi as my transaction history grows

**Acceptance Criteria:**
- [ ] `GetAllDescriptionsAsync` accepts a `searchPrefix` parameter (default: empty string — returns up to `maxResults` across all descriptions)
- [ ] `GetAllDescriptionsAsync` accepts a `maxResults` parameter (default: 100, enforced with `Take()`)
- [ ] When `searchPrefix` is non-empty, the query uses `StartsWith` (translated to `LIKE 'prefix%'` in PostgreSQL)
- [ ] The method never returns more than `maxResults` items regardless of how many distinct descriptions exist
- [ ] All Application-layer callers pass a meaningful search context instead of receiving the full set
- [ ] No feature flag required — refactor with API contract change at service layer only (no public HTTP API change)

---

## Technical Design

### Affected Files

| File | Layer | Change |
|------|-------|--------|
| `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs` | Infrastructure | Modify `GetAllDescriptionsAsync` with `searchPrefix` + `maxResults` |
| Repository interface(s) — `ITransactionRepository` or `ITransactionQueryRepository` (Doc 150) | Domain/Application | Update method signature |
| Application-layer callers of `GetAllDescriptionsAsync` | Application | Pass `searchPrefix` from calling context |
| `tests/BudgetExperiment.Infrastructure.Tests/Repositories/TransactionRepositoryTests.cs` | Tests | Integration tests for prefix filtering and Take cap |
| `tests/BudgetExperiment.Application.Tests/` (callers) | Tests | Update mocks to new signature |

### Caller Survey

Before implementing, identify all callers:

```
grep -rn "GetAllDescriptionsAsync" src/BudgetExperiment.Application/
```

Expected callers include:
- `CategorySuggestionService` — description matching; pass the description fragment being analysed
- `TransactionImportService` or similar — rule-matching; pass the import row description prefix
- Any autocomplete service — pass the user-typed prefix from the search input

Each caller should pass the narrowest prefix available at the call site.

### Backward Compatibility

The `searchPrefix` parameter defaults to `""` (empty), which matches all descriptions, and `maxResults` defaults to `100`. Callers that are not yet updated to pass a prefix continue to receive up to 100 descriptions — which is a breaking but intentional limit. There is no scenario where a caller legitimately needs all distinct descriptions unbounded.

The interface signature change is **only in the Application/Infrastructure boundary** — no public HTTP API is changed.

### SQL Translation

EF Core translates `d.StartsWith(searchPrefix)` to `d LIKE 'prefix%'` in PostgreSQL, which is index-eligible on a B-tree index on the `Description` column (if one exists). If the `Description` column is not indexed, a functional index or `pg_trgm` GIN index can be considered in a follow-up.

---

## Implementation Plan

### Phase 1: Tests (RED)

**Tasks:**
- [ ] Add integration test `GetAllDescriptionsAsync_WithPrefix_ReturnsOnlyMatchingDescriptions`
- [ ] Add integration test `GetAllDescriptionsAsync_WithLargeDataSet_ReturnsAtMostMaxResults`
- [ ] Add integration test `GetAllDescriptionsAsync_WithNoPrefix_ReturnsAtMostDefaultMaxResults`
- [ ] Update caller unit tests to use new signature
- [ ] Run tests — expect **RED**

**Commit:**
```
test(infra): failing tests for bounded GetAllDescriptionsAsync

Refs: Feature 158, P-006
```

---

### Phase 2: Implement Repository Change (GREEN)

**Tasks:**
- [ ] Update `GetAllDescriptionsAsync` signature with `searchPrefix` and `maxResults` parameters
- [ ] Apply `Where(d => d.StartsWith(searchPrefix))` when prefix is non-empty
- [ ] Apply `Take(maxResults)`
- [ ] Update repository interface
- [ ] Run integration tests — expect **GREEN**

**Commit:**
```
perf(infra): GetAllDescriptionsAsync adds prefix filter and Take cap

Adds searchPrefix parameter (EF translates to LIKE 'prefix%') and
maxResults cap (default 100) to prevent unbounded string load.

Fixes: P-006 (2026-04-09 performance audit)
Refs: Feature 158
```

---

### Phase 3: Update Application-Layer Callers

**Tasks:**
- [ ] Grep Application layer for all `GetAllDescriptionsAsync` call sites
- [ ] Update each caller to pass the narrowest available `searchPrefix`
- [ ] Run all Application unit tests — expect **GREEN**

**Commit:**
```
refactor(app): callers pass searchPrefix to GetAllDescriptionsAsync

Refs: Feature 158, P-006
```

---

### Phase 4: Verification

**Tasks:**
- [ ] Run full test suite: `dotnet test --filter "Category!=Performance"` — all green
- [ ] Run `dotnet format` — no style violations
- [ ] Review SQL logs to confirm `LIKE 'prefix%'` is emitted when prefix is non-empty

---

## Testing Strategy

### Integration Tests (Infrastructure.Tests)

- `GetAllDescriptionsAsync_WithPrefix_ReturnsOnlyMatchingDescriptions`
- `GetAllDescriptionsAsync_WithPrefix_ResultCountBoundedByMaxResults`
- `GetAllDescriptionsAsync_WithLargeDataSet_ReturnsAtMostDefaultMaxResults`
- `GetAllDescriptionsAsync_WithEmptyPrefix_ReturnsAtMostMaxResults`
- `GetAllDescriptionsAsync_WithEmptyDataSet_ReturnsEmptyList`

### Unit Tests (Application.Tests)

- Updated mocks in each caller test to supply `searchPrefix` argument
- `CategorySuggestionService_GetSuggestionsAsync_PassesDescriptionPrefixToRepository`

---

## Security Considerations

`searchPrefix` is user-supplied. EF Core parameterises `StartsWith` as a SQL parameter, not string concatenation, so there is no SQL injection risk. Input does not need additional sanitisation beyond what EF provides.

---

## Performance Considerations

- **Hardware target:** Raspberry Pi ARM64. String allocation is cheaper than entity materialisation, but thousands of string objects still contribute to GC pressure.
- **Expected improvement:** Result set capped at 100 strings regardless of transaction history size. For callers that pass a meaningful prefix, the result set is typically <10 items.
- **Index opportunity:** A B-tree index on `Transaction.Description` enables the `LIKE 'prefix%'` pattern to use index scan rather than full table scan. Consider adding if query explain plans show sequential scans.

---

## References

- [2026-04-09 Performance Audit — P-006](../docs/audit/2026-04-09-performance-review.md#p-006-high--getalldescriptionsasync-returns-unbounded-distinct-descriptions)
- `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs:314-322`
- Engineering Guide §9 (REST API — `pageSize` caps), §11 (Data & Persistence — bounded queries)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit P-006 | Alfred (Lead) |
