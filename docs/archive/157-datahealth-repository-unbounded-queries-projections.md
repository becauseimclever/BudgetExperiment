# Feature 157: DataHealth Repository — Fix Unbounded Queries and Add Sub-Analysis Projections

> **Status:** Done  
> **Severity:** 🟠 High — P-004 + P-005 (combined)  
> **Audit Source:** `docs/audit/2026-04-09-performance-review.md`

---

## Overview

Two repository methods in `TransactionRepository` supply data to the `DataHealthService` and `CategorySuggestionService`, and both are unbounded:

- **P-004:** `GetUncategorizedAsync()` (line 214–222) returns ALL uncategorized transactions with no `Take()` or pagination. Used by `CategorySuggestionService` to load full entities when only distinct descriptions are needed.
- **P-005:** `GetAllForHealthAnalysisAsync()` (line 528–544) loads ALL transactions as full entity graphs with `.Include(t => t.Category)`. Used by `DataHealthService` for three distinct sub-analyses, each of which needs only a small projection of the full entity.

Both findings share the same infrastructure fix: **replace full-entity unbounded queries with targeted projections and/or limits**. They are combined in this document because the changes are in the same repository file and follow the same pattern.

On a **Raspberry Pi ARM64**, these are the two largest memory-consuming repository operations in the application. Fixing them reduces peak heap usage, lowers GC pressure, and improves latency directly.

---

## Problem Statement

### Current State — P-004

```csharp
// TransactionRepository.cs:214-222
public async Task<IReadOnlyList<Transaction>> GetUncategorizedAsync()
{
    return await _context.Transactions
        .AsNoTracking()
        .ApplyScopeFilter(_scope)
        .Where(t => t.CategoryId == null)
        .ToListAsync();  // no Take() — returns entire uncategorized set
}
```

`CategorySuggestionService` loads all uncategorized transactions to analyse descriptions and match merchant patterns. Only the `Description` field is used — the full entity payload is wasted.

### Current State — P-005

```csharp
// TransactionRepository.cs:528-544
public async Task<IReadOnlyList<Transaction>> GetAllForHealthAnalysisAsync()
{
    return await _context.Transactions
        .AsNoTracking()
        .Include(t => t.Category)
        .ApplyScopeFilter(_scope)
        .ToListAsync();  // no Take() — returns all transactions with full Category include
}
```

Three sub-analyses each need different, small subsets of this data:
- **Date gap:** Only `(AccountId, Date)`.
- **Duplicate detection:** Only `(Id, AccountId, Date, Amount, Description)`.
- **Outlier analysis:** Only `(Id, Description, Amount)`.

### Target State

| P | Method | Change |
|---|--------|--------|
| P-004 | `GetUncategorizedAsync` | Add `maxCount` parameter with default; for suggestion-service callers, add `GetUncategorizedDescriptionsAsync` projection |
| P-005 | `GetAllForHealthAnalysisAsync` | Replace with three targeted projection methods for each sub-analysis type |

---

## User Stories

### US-157-001: Bounded Uncategorized Transaction Load

**As a** user with thousands of uncategorized imports  
**I want** suggestion analysis to complete quickly without OOM risk  
**So that** I can get category recommendations even with a large backlog

**Acceptance Criteria:**
- [ ] `GetUncategorizedAsync` accepts a `maxCount` parameter (default: 500) and applies `Take(maxCount)` in the query
- [ ] `CategorySuggestionService` callers that need only descriptions use `GetUncategorizedDescriptionsAsync` (returns `IReadOnlyList<string>`)
- [ ] No caller receives an unbounded full-entity result set for uncategorized transactions

### US-157-002: Projection Queries for DataHealth Sub-Analyses

**As a** Raspberry Pi deployment  
**I want** health analysis to load only the columns each sub-analysis actually uses  
**So that** the application doesn't materialise 10,000 full entity graphs per analysis

**Acceptance Criteria:**
- [ ] `GetAllForHealthAnalysisAsync` is **replaced** by three targeted methods (or this method is made private/removed from the interface)
- [ ] `GetTransactionProjectionsForDuplicateDetectionAsync` returns only `(Id, AccountId, Date, Amount, Description)`
- [ ] `GetTransactionDatesForGapAnalysisAsync` returns only `(AccountId, Date)`
- [ ] `GetTransactionAmountsForOutlierAnalysisAsync` returns only `(Id, Description, Amount)`
- [ ] All three methods use `AsNoTracking()` and apply scope filtering
- [ ] `DataHealthService` (Feature 154) can be refactored to call these methods

---

## Technical Design

### Affected Files

| File | Layer | Change |
|------|-------|--------|
| `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs` | Infrastructure | Modify `GetUncategorizedAsync`; add `GetUncategorizedDescriptionsAsync`; add three projection methods; deprecate/remove `GetAllForHealthAnalysisAsync` |
| Repository interface(s) — `ITransactionRepository` or sub-interfaces (per Doc 150) | Domain/Application | Add method signatures for all new methods |
| `src/BudgetExperiment.Application/Categorization/CategorySuggestionService.cs` | Application | Update callers to use `GetUncategorizedDescriptionsAsync` |
| `src/BudgetExperiment.Application/DataHealth/DataHealthService.cs` | Application | Updated as part of Feature 154 |
| `tests/BudgetExperiment.Infrastructure.Tests/Repositories/TransactionRepositoryTests.cs` | Tests | Integration tests for all new projection methods |

### New Projection Record Types

Defined in the Application layer (or Contracts layer if shared across boundaries):

```csharp
public sealed record DuplicateDetectionProjection(
    Guid Id, Guid AccountId, DateOnly Date, decimal Amount, string Description);

public sealed record DateGapProjection(Guid AccountId, DateOnly Date);

public sealed record OutlierProjection(Guid Id, string Description, decimal Amount);
```

### New / Modified Repository Methods

```csharp
// P-004 fix: add maxCount parameter
Task<IReadOnlyList<Transaction>> GetUncategorizedAsync(
    int maxCount = 500, CancellationToken cancellationToken = default);

// P-004 fix: description-only projection for suggestion service
Task<IReadOnlyList<string>> GetUncategorizedDescriptionsAsync(
    int maxCount = 500, CancellationToken cancellationToken = default);

// P-005 fix: three targeted projections for DataHealthService
Task<IReadOnlyList<DuplicateDetectionProjection>>
    GetTransactionProjectionsForDuplicateDetectionAsync(
        CancellationToken cancellationToken = default);

Task<IReadOnlyList<DateGapProjection>>
    GetTransactionDatesForGapAnalysisAsync(
        CancellationToken cancellationToken = default);

Task<IReadOnlyList<OutlierProjection>>
    GetTransactionAmountsForOutlierAnalysisAsync(
        CancellationToken cancellationToken = default);
```

### EF Core Projection Pattern

```csharp
// Example: DuplicateDetectionProjection
return await _context.Transactions
    .AsNoTracking()
    .ApplyScopeFilter(_scope)
    .Select(t => new DuplicateDetectionProjection(
        t.Id, t.AccountId, t.Date, t.Amount, t.Description))
    .ToListAsync(cancellationToken);

// Example: DateGapProjection
return await _context.Transactions
    .AsNoTracking()
    .ApplyScopeFilter(_scope)
    .Select(t => new DateGapProjection(t.AccountId, t.Date))
    .Distinct()
    .OrderBy(p => p.AccountId).ThenBy(p => p.Date)
    .ToListAsync(cancellationToken);
```

### Deprecation of GetAllForHealthAnalysisAsync

Once Feature 154 migrates `DataHealthService` to use the three targeted projection methods, `GetAllForHealthAnalysisAsync` can be:
1. Removed from the repository interface (breaking change — coordinate with Feature 154).
2. Made `private` or `internal` in the concrete repository as a transitional step.

Prefer removal; document as a breaking infrastructure change in the PR description.

---

## Implementation Plan

### Phase 1: Tests (RED)

**Tasks:**
- [ ] Add integration test `GetUncategorizedAsync_WithMaxCount_ReturnsAtMostMaxCountItems`
- [ ] Add integration test `GetUncategorizedDescriptionsAsync_ReturnsOnlyDescriptionStrings`
- [ ] Add integration test `GetTransactionProjectionsForDuplicateDetectionAsync_ReturnsExpectedShape`
- [ ] Add integration test `GetTransactionDatesForGapAnalysisAsync_ReturnsDistinctAccountDates`
- [ ] Add integration test `GetTransactionAmountsForOutlierAnalysisAsync_ReturnsIdDescriptionAmount`
- [ ] Run tests — expect **RED** (methods do not exist yet)

**Commit:**
```
test(infra): failing integration tests for projection repository methods

Refs: Feature 157, P-004, P-005
```

---

### Phase 2: Implement Projection Methods (GREEN)

**Tasks:**
- [ ] Add projection record types (`DuplicateDetectionProjection`, `DateGapProjection`, `OutlierProjection`) in Application layer
- [ ] Add method signatures to the relevant repository interface
- [ ] Implement `GetUncategorizedAsync(int maxCount)` with `Take(maxCount)` in TransactionRepository
- [ ] Implement `GetUncategorizedDescriptionsAsync(int maxCount)` with `Select(t => t.Description).Distinct().Take(maxCount)`
- [ ] Implement `GetTransactionProjectionsForDuplicateDetectionAsync`
- [ ] Implement `GetTransactionDatesForGapAnalysisAsync`
- [ ] Implement `GetTransactionAmountsForOutlierAnalysisAsync`
- [ ] Run tests — expect **GREEN**

**Commit:**
```
feat(infra): add bounded and projection queries for DataHealth + SuggestionService

Adds GetUncategorizedDescriptionsAsync (P-004),
GetTransactionProjectionsForDuplicateDetectionAsync,
GetTransactionDatesForGapAnalysisAsync,
GetTransactionAmountsForOutlierAnalysisAsync (P-005).

Applies Take() limit to GetUncategorizedAsync.

Fixes: P-004, P-005 (2026-04-09 performance audit)
Refs: Feature 157
```

---

### Phase 3: Update CategorySuggestionService Callers

**Tasks:**
- [ ] Identify all call sites of `GetUncategorizedAsync` in `CategorySuggestionService`
- [ ] Replace calls where only descriptions are needed with `GetUncategorizedDescriptionsAsync`
- [ ] Update unit tests to mock `GetUncategorizedDescriptionsAsync` instead of `GetUncategorizedAsync`

**Commit:**
```
refactor(app): CategorySuggestionService uses description projection query

Refs: Feature 157, P-004
```

---

### Phase 4: Remove GetAllForHealthAnalysisAsync (after Feature 154 merges)

**Tasks:**
- [ ] Confirm Feature 154 `DataHealthService` refactor does not call `GetAllForHealthAnalysisAsync`
- [ ] Remove `GetAllForHealthAnalysisAsync` from repository interface and implementation
- [ ] Confirm all tests green after removal

**Commit:**
```
refactor(infra): remove GetAllForHealthAnalysisAsync (replaced by projections)

Refs: Feature 157, Feature 154, P-005
```

---

### Phase 5: Verification

**Tasks:**
- [ ] Run full test suite: `dotnet test --filter "Category!=Performance"` — all green
- [ ] Run `dotnet format` — no style violations

---

## Testing Strategy

### Integration Tests (Infrastructure.Tests)

- `GetUncategorizedAsync_WithMaxCount500_ReturnsAtMostMaxCountItems`
- `GetUncategorizedAsync_DefaultMaxCount_DoesNotExceedDefault`
- `GetUncategorizedDescriptionsAsync_ReturnsOnlyDescriptionStrings`
- `GetTransactionProjectionsForDuplicateDetectionAsync_ReturnsExpectedShape`
- `GetTransactionProjectionsForDuplicateDetectionAsync_DoesNotIncludeCategoryData`
- `GetTransactionDatesForGapAnalysisAsync_ReturnsDistinctAccountDates`
- `GetTransactionAmountsForOutlierAnalysisAsync_ReturnsIdDescriptionAmount`

### Unit Tests (Application.Tests)

- `CategorySuggestionService_AnalyzeTransactionsAsync_UsesDescriptionProjection` (not full entity)

---

## Security Considerations

None — all new methods apply the existing scope filter (`ApplyScopeFilter`). No data leaks across scope boundaries.

---

## Performance Considerations

- **Hardware target:** Raspberry Pi ARM64, 1–4 GB RAM.
- **P-004 expected improvement:** `CategorySuggestionService` memory reduced from N full entities → N string allocations for description projection path.
- **P-005 expected improvement:** `DataHealthService` (with Feature 154) loads `(Id, AccountId, Date, Amount, Description)` per row instead of full entity graphs with Category navigation — ~70–80% reduction in allocated bytes per analysis call.
- Projection queries avoid EF Core identity resolution overhead even with `AsNoTrackingWithIdentityResolution()`.

---

## References

- [2026-04-09 Performance Audit — P-004](../docs/audit/2026-04-09-performance-review.md#p-004-high--getuncategorizedasync-returns-unbounded-result-set)
- [2026-04-09 Performance Audit — P-005](../docs/audit/2026-04-09-performance-review.md#p-005-high--getallforhealthanalysisasync-returns-unbounded-full-entities)
- `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs:214-222` (P-004)
- `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs:528-544` (P-005)
- Feature 154 (DataHealthService refactor — consumes the projection methods defined here)
- Feature 150 (ITransactionRepository ISP split — interface placement context)
- Engineering Guide §11 (Data & Persistence), §7 (ISP)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit P-004 + P-005 (combined) | Alfred (Lead) |
