# Feature 064: Report Currency Derivation
> **Status:** 🗒️ Planning  
> **Priority:** Low  
> **Estimated Effort:** Small (1 sprint)  
> **Dependencies:** Feature 050 Phase 1 (Complete)

## Overview

Replace all hardcoded `"USD"` currency strings in report and service DTOs with currency values derived from the actual transaction data. Currently, every `MoneyDto` in `ReportService`, `ChatService`, `PastDueService`, and `DayDetailService` is constructed with `Currency = "USD"` regardless of the transactions' actual currency.

## Problem Statement

### Current State

The `MoneyDto` type has a `Currency` property, but all application services hardcode it to `"USD"`:

- **`ReportService`** — 14 occurrences across `BuildCategoryReportAsync`, `GetMonthlyCategoryReportAsync`, `GetCategoryReportByRangeAsync`, `GetSpendingTrendsAsync`, `GetDaySummaryAsync`
- **`ChatService`** — 4 occurrences in transaction/transfer creation helpers
- **`PastDueService`** — 1 occurrence in past-due summary
- **`DayDetailService`** — 2 occurrences in day detail summary

The domain model already stores currency per transaction (`Transaction.Amount` is a `Money` value object with `Currency` property), so the data exists — it's simply not used when constructing DTOs.

### Target State

- Report DTOs reflect the actual currency from the underlying transactions
- Multi-currency scenarios are handled gracefully (e.g., group by currency or report the dominant currency with a note)
- No hardcoded `"USD"` strings remain in application services

---

## User Stories

#### US-064-001: Report currency matches transaction data
**As a** user with non-USD accounts  
**I want to** see reports display the correct currency code  
**So that** the amounts shown are accurately labeled

**Acceptance Criteria:**
- [ ] `MonthlyCategoryReportDto.TotalSpending.Currency` reflects the transaction currency
- [ ] `SpendingTrendsReportDto` monthly data points reflect the correct currency
- [ ] `DaySummaryDto` reflects the correct currency
- [ ] All existing tests updated to verify currency propagation

#### US-064-002: Graceful handling of mixed currencies
**As a** user with accounts in multiple currencies  
**I want to** see reports handle mixed currencies without data corruption  
**So that** I'm not confused by aggregated amounts across different currencies

**Acceptance Criteria:**
- [ ] When all transactions share a single currency, use that currency
- [ ] When transactions span multiple currencies, use a defined strategy (e.g., majority currency or separate groups)
- [ ] Edge case: empty report defaults to `"USD"` or a configurable default

---

## Technical Design

### Approach Options

| Option | Description | Complexity | Recommendation |
|--------|-------------|-----------|----------------|
| **A: Derive from transactions** | Inspect `Transaction.Amount.Currency` for each report query; use the most common currency | Low | ✅ Recommended for Phase 1 |
| **B: User/scope default** | Store a default currency on the user profile or scope settings | Medium | Future enhancement |
| **C: Full multi-currency** | Group and report per-currency, with optional conversion | High | Out of scope |

### Implementation — Option A

1. In `BuildCategoryReportAsync`, determine the currency from the first non-transfer transaction (or default to `"USD"` if none)
2. Pass the resolved currency through to all `MoneyDto` constructors in that method
3. Apply the same pattern to `GetSpendingTrendsAsync` and `GetDaySummaryAsync`
4. Repeat for `ChatService`, `PastDueService`, `DayDetailService`

```csharp
// Example helper method
private static string ResolveCurrency(IReadOnlyList<Transaction> transactions, string fallback = "USD")
{
    return transactions
        .Where(t => !t.IsTransfer)
        .Select(t => t.Amount.Currency)
        .GroupBy(c => c)
        .OrderByDescending(g => g.Count())
        .Select(g => g.Key)
        .FirstOrDefault() ?? fallback;
}
```

### Affected Files

| File | Changes |
|------|---------|
| `Application/Reports/ReportService.cs` | Add `ResolveCurrency` helper; replace 14 hardcoded `"USD"` |
| `Application/Chat/ChatService.cs` | Replace 4 hardcoded `"USD"` |
| `Application/Calendar/PastDueService.cs` | Replace 1 hardcoded `"USD"` |
| `Application/Calendar/DayDetailService.cs` | Replace 2 hardcoded `"USD"` |

---

## Implementation Plan

### Phase 1: ReportService currency derivation
> **Commit prefix:** `fix(app): derive currency from transaction data in reports`

**Tasks:**
- [ ] Add `ResolveCurrency` private static helper to `ReportService`
- [ ] Update `BuildCategoryReportAsync` to resolve and pass currency
- [ ] Update `GetSpendingTrendsAsync` to resolve currency
- [ ] Update `GetDaySummaryAsync` to resolve currency
- [ ] Update existing unit tests to verify currency is derived, not hardcoded
- [ ] Add unit test: mixed-currency transactions use majority currency
- [ ] Add unit test: empty transactions default to `"USD"`

### Phase 2: Other services
> **Commit prefix:** `fix(app): derive currency from transaction data in remaining services`

**Tasks:**
- [ ] Update `ChatService` — 4 occurrences
- [ ] Update `PastDueService` — 1 occurrence
- [ ] Update `DayDetailService` — 2 occurrences
- [ ] Update/add tests for each service

---

## Testing Strategy

### Unit Tests

- [ ] `ResolveCurrency` returns correct currency from single-currency transactions
- [ ] `ResolveCurrency` returns majority currency from mixed-currency transactions
- [ ] `ResolveCurrency` returns `"USD"` fallback for empty list
- [ ] Report methods propagate resolved currency to all `MoneyDto` instances
- [ ] Existing report tests updated to assert non-hardcoded currency

---

## Security Considerations

None — currency is already stored in transaction data; this change only surfaces it correctly.

---

## Performance Considerations

Negligible — `ResolveCurrency` operates on the already-fetched transaction list in memory.

---

## References

- [Feature 050: Calendar-Driven Reports](./050-calendar-driven-reports-analytics.md) — Phase 1 deferred task
- Current gap identified in Feature 050 current state analysis (gap #9)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-07 | Initial draft — spun off from Feature 050 Phase 1 deferred task | @copilot |
