# Feature 065: Report Scope Filtering Audit & Fix
> **Status:** 🗒️ Planning  
> **Priority:** High  
> **Estimated Effort:** Small (1 sprint)  
> **Dependencies:** Feature 050 Phase 1 (Complete)

## Overview

Audit and fix scope filtering in `TransactionRepository.GetByDateRangeAsync` to ensure all report endpoints respect the budget scope (Personal/Shared/All). Currently, `GetByDateRangeAsync` does **not** call `ApplyScopeFilter`, meaning report data leaks across scopes.

## Problem Statement

### Current State — Confirmed Bug

The `TransactionRepository` has a private `ApplyScopeFilter` method that filters transactions by `BudgetScope` (Personal/Shared/All) using `IUserContext.CurrentScope`. However, **not all query methods call it consistently**:

| Method | Calls `ApplyScopeFilter`? | Impact |
|--------|--------------------------|--------|
| `GetByDateRangeAsync` | ❌ **NO** | Reports, day summaries, trends — all leak cross-scope data |
| `GetDailyTotalsAsync` | ✅ Yes | Calendar daily totals are correctly scoped |
| `GetByMonthAsync` | Needs audit | Calendar month view |
| `SearchAsync` | Needs audit | Transaction search |
| Other query methods | Needs audit | Various features |

This means:
- **All report endpoints** (categories/monthly, categories/range, trends, day-summary) return data from all scopes regardless of the user's selected scope
- A user in "Personal" scope sees Shared transactions in their reports
- A user in "Shared" scope sees Personal transactions from all users in reports
- This is a **data isolation bug**, not just a UI issue

### Target State

- Every `TransactionRepository` query method applies `ApplyScopeFilter` consistently
- Report data respects Personal/Shared/All scope selection
- Integration tests verify scope isolation for report queries
- Audit all repositories for the same pattern

---

## User Stories

#### US-065-001: Reports respect budget scope
**As a** user switching between Personal and Shared scopes  
**I want to** see report data filtered to my selected scope  
**So that** my personal spending reports don't include shared household expenses

**Acceptance Criteria:**
- [ ] `GET /api/v1/reports/categories/monthly` returns only transactions matching current scope
- [ ] `GET /api/v1/reports/categories/range` returns only transactions matching current scope
- [ ] `GET /api/v1/reports/trends` returns only transactions matching current scope
- [ ] `GET /api/v1/reports/day-summary/{date}` returns only transactions matching current scope

#### US-065-002: Consistent scope filtering across all repository methods
**As a** developer  
**I want to** know that all repository query methods apply scope filtering  
**So that** new features don't accidentally introduce scope leaks

**Acceptance Criteria:**
- [ ] All public query methods in `TransactionRepository` call `ApplyScopeFilter`
- [ ] Audit covers `RecurringTransactionRepository`, `RecurringTransferRepository`, `BudgetGoalRepository`
- [ ] Each repository has at least one integration test verifying scope isolation

---

## Technical Design

### Root Cause

In `TransactionRepository.cs`, `GetByDateRangeAsync` (line 56) queries `_context.Transactions` directly without calling the existing `ApplyScopeFilter` helper:

```csharp
// CURRENT — missing scope filter
public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(...)
{
    var query = this._context.Transactions
        .Include(t => t.Category)
        .Where(t => t.Date >= startDate && t.Date <= endDate);
    // ...
}

// FIXED — apply scope filter
public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(...)
{
    var query = this.ApplyScopeFilter(this._context.Transactions)
        .Include(t => t.Category)
        .Where(t => t.Date >= startDate && t.Date <= endDate);
    // ...
}
```

### Full Repository Audit Needed

Each repository that has `ApplyScopeFilter` should be audited to ensure **every public query method** calls it:

| Repository | Has `ApplyScopeFilter`? | Methods to Audit |
|-----------|------------------------|------------------|
| `TransactionRepository` | ✅ | `GetByDateRangeAsync`, `GetByMonthAsync`, `SearchAsync`, `GetByIdAsync`, others |
| `RecurringTransactionRepository` | ✅ | All `Get*` methods |
| `RecurringTransferRepository` | ✅ | All `Get*` methods |
| `BudgetGoalRepository` | ✅ | All `Get*` methods |
| `ReconciliationMatchRepository` | ✅ | All `Get*` methods |
| `AccountRepository` | Needs check | May need scope filtering for multi-user scenarios |
| `BudgetCategoryRepository` | Needs check | Categories may be shared across scopes |

### Affected Files

| File | Changes |
|------|---------|
| `Infrastructure/Persistence/Repositories/TransactionRepository.cs` | Add `ApplyScopeFilter` to `GetByDateRangeAsync` and any other missing methods |
| Other repositories (TBD after audit) | Same pattern |

---

## Implementation Plan

### Phase 1: Fix TransactionRepository.GetByDateRangeAsync
> **Commit prefix:** `fix(infra): apply scope filter to GetByDateRangeAsync`

**Tasks:**
- [ ] Add `ApplyScopeFilter` call to `GetByDateRangeAsync` in `TransactionRepository`
- [ ] Write integration test: Personal scope excludes Shared transactions from `GetByDateRangeAsync`
- [ ] Write integration test: Shared scope excludes Personal transactions
- [ ] Write integration test: All scope returns both Personal and Shared
- [ ] Verify all existing report tests still pass (reports use `GetByDateRangeAsync`)
- [ ] Verify calendar tests still pass

### Phase 2: Full repository audit
> **Commit prefix:** `fix(infra): audit and fix scope filtering across all repositories`

**Tasks:**
- [ ] Audit every public method in `TransactionRepository` — list which call `ApplyScopeFilter` and which don't
- [ ] Fix any missing `ApplyScopeFilter` calls
- [ ] Audit `RecurringTransactionRepository` — same pattern
- [ ] Audit `RecurringTransferRepository` — same pattern
- [ ] Audit `BudgetGoalRepository` — same pattern
- [ ] Audit `ReconciliationMatchRepository` — same pattern
- [ ] Determine whether `AccountRepository` and `BudgetCategoryRepository` need scope filtering
- [ ] Add integration tests for any newly fixed methods

### Phase 3: Prevention
> **Commit prefix:** `test(infra): add scope filtering regression tests`

**Tasks:**
- [ ] Consider adding a code review checklist item for scope filtering on new repository methods
- [ ] Add a comment/documentation near `ApplyScopeFilter` in each repository reminding developers to use it
- [ ] Optionally: refactor repositories to apply scope filtering by default (e.g., a `ScopedQuery` property that always applies the filter)

---

## Testing Strategy

### Integration Tests (Testcontainers / InMemory)

- [ ] Create transactions with different `Scope` values (Personal, Shared) and different `OwnerUserId`
- [ ] Set `IUserContext.CurrentScope` to Personal → verify `GetByDateRangeAsync` excludes Shared
- [ ] Set `IUserContext.CurrentScope` to Shared → verify `GetByDateRangeAsync` excludes Personal
- [ ] Set `IUserContext.CurrentScope` to All → verify `GetByDateRangeAsync` returns both
- [ ] Repeat for each fixed repository method

### API Integration Tests

- [ ] Report endpoints return different data when `X-Budget-Scope` header changes
- [ ] Trends endpoint with Personal scope shows only personal transaction trends

---

## Security Considerations

- **This is a data isolation bug** — fixing it is a security improvement
- Personal transactions should never be visible in Shared scope (and vice versa) unless the user selects "All"
- After fix, verify that the `OwnerUserId` check in Personal scope correctly identifies the authenticated user

---

## Performance Considerations

- Adding `ApplyScopeFilter` adds a `WHERE` clause to every query — this is the correct behavior and should have negligible performance impact
- Ensure `Scope` and `OwnerUserId` columns have appropriate indexes (likely already indexed)

---

## Risk Analysis

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Fixing scope filter changes report data for existing users | Medium | Medium | This is the correct behavior — users were seeing incorrect data before. No migration needed. |
| Breaking integration tests that don't set scope | Medium | Low | Existing `CustomWebApplicationFactory` may need to set a default scope in `IUserContext` for tests |
| Other repository methods also missing scope filter | High | High | Phase 2 full audit addresses this systematically |

---

## References

- [Feature 050: Calendar-Driven Reports](./050-calendar-driven-reports-analytics.md) — Phase 1 deferred task ("Verify: Scope filtering")
- `TransactionRepository.cs` line 56 — `GetByDateRangeAsync` (missing filter)
- `TransactionRepository.cs` line 329 — `ApplyScopeFilter` method (exists but not universally applied)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-07 | Initial draft — spun off from Feature 050 Phase 1 deferred task. Confirmed `GetByDateRangeAsync` does NOT apply scope filter. | @copilot |
