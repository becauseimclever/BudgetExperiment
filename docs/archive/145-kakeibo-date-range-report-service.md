# Feature 145: Kakeibo Date-Range Report Service

> **Status:** Done

## Overview

This feature introduces a domain service to compute Kakeibo bucket totals (Essentials, Wants, Culture, Unexpected) for arbitrary date ranges. The feature closes **CAT-8 accuracy gap** by providing a unified, testable aggregation service that respects transaction-level `KakeiboOverride` settings and ensures every expense maps to exactly one bucket with no orphans or double-counting.

Kakeibo is a Japanese budgeting philosophy that groups expenses into four philosophical buckets. Currently, the domain correctly assigns individual transactions to buckets (INV-8), but there is no aggregation service that can answer "what did we spend in Essentials during week of March 3rd?" or "what are the four bucket totals for March?" This service fills that gap.

## Problem Statement

### Current State

- `Transaction` domain model includes `KakeiboOverride` for explicit bucket assignment and a `ResolveKakeiboCategory()` method that respects override-first logic (INV-8 domain enforcement is correct).
- `BudgetCategory` correctly enforces Kakeibo assignment only on Expense-type categories (Income and Transfer reject it).
- No aggregation service exists that groups transactions by Kakeibo bucket for a date range.
- No accuracy test proves "every expense transaction in a date range maps to exactly one bucket with no orphans."

### Target State

- New `IKakeiboReportService` interface (Application layer) that accepts a date range and produces per-bucket totals.
- New `KakeiboReportService` implementation that:
  - Queries transactions for a date range filtered by account (optional) and user scope.
  - Groups by Kakeibo bucket using transaction's `ResolveKakeiboCategory()` method (respects `KakeiboOverride`).
  - Excludes Income and Transfer category types (by domain model check).
  - Returns `KakeiboSummary` DTO with daily/weekly/monthly bucket totals.
- New API endpoint `GET /api/v1/reports/kakeibo?from={date}&to={date}` for user to retrieve bucket summaries.
- Feature flag `feature-kakeibo-date-range-reports` gates the service and endpoint.
- Accuracy test suite verifies:
  - Every expense transaction maps to exactly one bucket.
  - Weekly sub-totals sum to monthly total.
  - No transactions orphaned from aggregation.

---

## User Stories

### User Report Requests

#### US-145-001: View Monthly Kakeibo Budget Summary
**As a** budget-conscious user  
**I want to** see how much I spent in each Kakeibo bucket (Essentials, Wants, Culture, Unexpected) for a given month  
**So that** I can understand spending distribution across my four philosophical categories

**Acceptance Criteria:**
- [ ] API endpoint returns Kakeibo summary for a specified month
- [ ] Summary includes total for each of the four buckets
- [ ] Transactions with explicit `KakeiboOverride` are grouped by override, not category default
- [ ] Income and Transfer transactions are excluded from totals
- [ ] Feature flag gates the endpoint; disabled by default

#### US-145-002: View Weekly Kakeibo Breakdown
**As a** user tracking weekly spending patterns  
**I want to** see Kakeibo bucket totals broken down by week (Monday–Sunday, ISO week)  
**So that** I can spot weekly spending spikes in specific categories

**Acceptance Criteria:**
- [ ] API returns weekly sub-totals for each Kakeibo bucket within a date range
- [ ] Weeks follow ISO standard (Monday–Sunday)
- [ ] Weekly totals for a bucket sum to the month's total for that bucket
- [ ] Zero-amount buckets are still returned (no silent omission)

---

## Technical Design

### Architecture Changes

- New service interface `IKakeiboReportService` in `BudgetExperiment.Application` → Reports subfolder
- Implementation `KakeiboReportService` depends on `ITransactionRepository` (read-only) and `ICategoryRepository`
- Exposes a single async method: `GetKakeiboSummaryAsync(dateRange, accountId?, cancellationToken)`
- Returns `KakeiboSummary` DTO from `BudgetExperiment.Contracts` → Reports subfolder

### Domain Model

No domain model changes. Uses existing:
- `Transaction.ResolveKakeiboCategory()` method (respects `KakeiboOverride` then falls back to category's `KakeiboCategory`)
- `BudgetCategory.Type` (filter for Expense only)
- `KakeiboCategory` enum (Essentials=1, Wants=2, Culture=3, Unexpected=4)

```csharp
// Example domain method already exists
public KakeiboCategory ResolveKakeiboCategory()
    => KakeiboOverride ?? Category?.KakeiboCategory ?? KakeiboCategory.Wants;
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/reports/kakeibo?from={date}&to={date}` | Get Kakeibo bucket totals for date range (ISO 8601 format, e.g., `2026-03-01`) |

**Query Parameters:**
- `from` (required): Start date (inclusive), `DateOnly` ISO format
- `to` (required): End date (inclusive), `DateOnly` ISO format
- `accountId` (optional): Filter to specific account; defaults to all user's accounts

**Response (KakeiboSummary DTO):**
```json
{
  "dateRange": {
    "from": "2026-03-01",
    "to": "2026-03-31"
  },
  "dailyTotals": [
    {
      "date": "2026-03-01",
      "bucketTotals": {
        "Essentials": "150.50",
        "Wants": "25.00",
        "Culture": "0.00",
        "Unexpected": "0.00"
      }
    }
  ],
  "weeklyTotals": [
    {
      "weekStartDate": "2026-03-02",
      "weekNumber": 10,
      "bucketTotals": {
        "Essentials": "1050.75",
        "Wants": "180.25",
        "Culture": "45.00",
        "Unexpected": "0.00"
      }
    }
  ],
  "monthlyTotals": {
    "Essentials": "4200.00",
    "Wants": "750.50",
    "Culture": "150.00",
    "Unexpected": "85.75"
  }
}
```

**Status Codes:**
- `200 OK`: Summary returned successfully
- `400 Bad Request`: Invalid date range (e.g., `from > to`) or malformed `DateOnly`
- `404 Not Found`: Account ID not found (if specified)

### Database Changes

No new tables or columns. Query existing `Transactions`, `BudgetCategories` tables via EF Core.

### UI Components

*Out of scope for this feature.* A future dashboard component will consume the endpoint to display charts/tables. For now, API-only.

---

## Implementation Plan

### Phase 1: Domain & Application Service Setup

**Objective:** Create the service interface, implementation, and DTOs.

**Tasks:**
- [ ] Create `KakeiboSummary` DTO in `BudgetExperiment.Contracts/Reports/`
  - Nested DTOs for daily/weekly/monthly totals
  - Use `MoneyValue` for bucket amounts
- [ ] Create `IKakeiboReportService` interface in `BudgetExperiment.Application/Reports/`
- [ ] Implement `KakeiboReportService` class:
  - Constructor accepts `ITransactionRepository`, `ICategoryRepository`
  - `GetKakeiboSummaryAsync(DateOnly from, DateOnly to, Guid? accountId, CancellationToken)` method
  - Filter transactions: date range, expense categories only, user's scope
  - Group by `transaction.ResolveKakeiboCategory()`
  - Compute daily, weekly (ISO), monthly aggregates
  - Handle zero-amount buckets (include, don't omit)
- [ ] Unit tests (mocked repositories):
  - `KakeiboReportService_SingleExpense_MapsToCorrectBucket`
  - `KakeiboReportService_OverrideTakesPrecedence_OverCategoryDefault`
  - `KakeiboReportService_IncomeAndTransfer_AreExcluded`
  - `KakeiboReportService_WeeklyTotals_SumToMonthly`
  - `KakeiboReportService_EmptyRange_ReturnsZeros`
  - `KakeiboReportService_DateBoundaries_Inclusive`
- [ ] Register service in `BudgetExperiment.Application/DependencyInjection.cs`

**Commit:**
```bash
git add .
git commit -m "feat(app): add KakeiboReportService for date-range bucket aggregation

- Add IKakeiboReportService interface and implementation
- Create KakeiboSummary DTO with daily/weekly/monthly breakdowns
- Respect transaction KakeiboOverride and category defaults
- Exclude Income and Transfer transactions
- Unit tests for bucket mapping and aggregation accuracy

Refs: CAT-8"
```

---

### Phase 2: API Endpoint & Feature Flag

**Objective:** Expose the service via REST and gate with feature flag.

**Tasks:**
- [ ] Create `ReportsController` (or extend existing) with `GetKakeiboReportAsync` endpoint
- [ ] Add endpoint to `/api/v1/reports/kakeibo`
- [ ] Validate date range (from ≤ to, dates are `DateOnly`)
- [ ] Map response to OpenAPI schema
- [ ] Add `[FeatureGate("feature-kakeibo-date-range-reports")]` attribute
- [ ] Write API tests (WebApplicationFactory):
  - `ReportsController_GetKakeibo_FeatureFlagDisabled_Returns403`
  - `ReportsController_GetKakeibo_ValidRange_Returns200WithSummary`
  - `ReportsController_GetKakeibo_InvalidDateRange_Returns400`
  - `ReportsController_GetKakeibo_WithAccountFilter_ReturnsAccountTransactionsOnly`
- [ ] Update OpenAPI/Scalar documentation

**Commit:**
```bash
git add .
git commit -m "feat(api): expose /api/v1/reports/kakeibo endpoint

- Add ReportsController.GetKakeiboReportAsync
- Feature flag: feature-kakeibo-date-range-reports
- Validate date ranges, map to KakeiboSummary DTO
- OpenAPI documentation

Refs: CAT-8"
```

---

### Phase 3: Integration & Accuracy Tests

**Objective:** Prove INV-8 holds end-to-end with PostgreSQL.

**Tasks:**
- [ ] Create `KakeiboReportServiceAccuracyTests.cs` in `BudgetExperiment.Infrastructure.Tests/Accuracy/`
- [ ] Integration test with Testcontainers (PostgreSQL):
  - Set up account, create 10–20 expense transactions with mixed categories/overrides
  - Include some Income and Transfer transactions (should be excluded)
  - Query service for date range spanning all transactions
  - Assert:
    - Sum of daily buckets = monthly bucket total
    - Sum of all buckets = sum of all expense transactions (no orphans)
    - Transactions with `KakeiboOverride` are in correct bucket (not default)
  - Test accuracy case: `AccuracyTest_EveryExpenseTracksToOneBucket_NoneOrphaned`
- [ ] Performance sanity check (date range with 1000+ transactions should complete < 500ms)
- [ ] Verify zero-amount buckets are returned (not omitted)

**Commit:**
```bash
git add .
git commit -m "test(infra): add KakeiboReportService integration and accuracy tests

- Integration test with Testcontainers PostgreSQL
- Prove every expense maps to exactly one bucket (INV-8)
- Verify weekly sub-totals sum to monthly (no drift)
- Test override precedence over category default
- Exclude Income and Transfer transactions

Refs: CAT-8"
```

---

### Phase 4: Documentation & Cleanup

**Objective:** Final documentation and polish.

**Tasks:**
- [ ] Update `docs/ACCURACY-FRAMEWORK.md` section 6 to mark CAT-8 as covered
- [ ] Add XML comments to public API surface (interface + controller)
- [ ] Add feature flag documentation to `docs/feature-flags.md` (if exists) or create entry
- [ ] Verify all tests pass: `dotnet test --filter "Category!=Performance"`
- [ ] Review for code style: `dotnet format`

**Commit:**
```bash
git add .
git commit -m "docs(reports): complete Kakeibo report service documentation

- XML comments for public APIs
- Update accuracy framework (CAT-8 now covered)
- Document feature flag and endpoint behavior

Refs: CAT-8"
```

---

## Testing Strategy

### Unit Tests

- **KakeiboReportService initialization & validation:**
  - `KakeiboReportService_NullRepository_ThrowsArgumentNull`
  - `KakeiboReportService_FromGreaterThanTo_Throws`
  
- **Bucket mapping:**
  - `KakeiboReportService_SingleExpense_MapsToCorrectBucket` — each of four buckets
  - `KakeiboReportService_KakeiboOverride_TakesPrecedence` — override ≠ category default
  - `KakeiboReportService_IncomeCategory_Excluded`
  - `KakeiboReportService_TransferCategory_Excluded`
  
- **Aggregation:**
  - `KakeiboReportService_DailyTotals_SumCorrectly`
  - `KakeiboReportService_WeeklyTotals_SumToMonthly`
  - `KakeiboReportService_EmptyRange_AllBucketsZero`
  - `KakeiboReportService_ZeroBuckets_StillReturned`

### Integration Tests

- **End-to-end with PostgreSQL (Testcontainers):**
  - `KakeiboReportServiceAccuracy_EveryExpenseTracksToOneBucket_NoneOrphaned`
  - `KakeiboReportServiceAccuracy_ManyTransactions_AggregatesExactly`
  - `KakeiboReportServiceAccuracy_OverridePrecedence_AppliesAcrossRange`

### Manual Testing Checklist

- [ ] Feature flag disabled: endpoint returns 403 Forbidden
- [ ] Feature flag enabled: endpoint returns 200 with data
- [ ] Date range boundaries: transactions on `from` and `to` dates included
- [ ] Account filter: results limited to selected account
- [ ] Zero-amount buckets still returned (no silent omission)
- [ ] Weekly totals sum to monthly in UI (if dashboard added later)

---

## Migration Notes

No database migration required. This feature uses existing tables and columns.

---

## Security Considerations

- **Authorization:** Only return Kakeibo reports for transactions the user owns (scope check: Shared categories visible to all, Personal categories scoped to user).
- **Date range validation:** Reject unreasonably large ranges (e.g., > 10 years) to prevent DoS scanning.
- **No sensitive data leakage:** KakeiboSummary DTO contains only aggregated amounts; no transaction details.

---

## Performance Considerations

- **Query optimization:** Single query to fetch transactions + categories for date range (use EF Core `AsNoTracking()` for read-only).
- **Aggregation in memory:** After fetch, group by Kakeibo bucket in C#. For typical ranges (1 month = ~100–500 transactions), this is negligible.
- **Caching:** Not required for initial release; date ranges are user-specific and rarely repeated.
- **Acceptable latency:** Target < 500ms for 1000-transaction range on standard PostgreSQL.

---

## Future Enhancements

- **Dashboard UI:** Charts/tables displaying Kakeibo bucket breakdown by month or week.
- **Comparisons:** Month-over-month or year-over-year Kakeibo spending trends.
- **Alerts:** Notify user if spending in a bucket exceeds threshold.
- **Budget targets:** Allow users to set budget goals per Kakeibo bucket (requires BudgetGoal changes).
- **Export:** CSV/PDF report of Kakeibo summaries.

---

## References

- [INV-8: Kakeibo Category Assignment Completeness](./ACCURACY-FRAMEWORK.md#inv-8-kakeibo-category-assignment-completeness)
- [CAT-8: Kakeibo Allocation Accuracy Test](./ACCURACY-FRAMEWORK.md#cat-8-kakeibo-allocation)
- [KakeiboCategory enum](../src/BudgetExperiment.Shared/Budgeting/KakeiboCategory.cs)
- [Transaction domain model](../src/BudgetExperiment.Domain/Accounts/Transaction.cs)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-09 | Initial planning draft | Alfred (Lead) |
