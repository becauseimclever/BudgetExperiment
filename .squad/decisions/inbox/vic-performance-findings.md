# Vic — Performance Audit Findings (2026-04-09)

**Report:** `docs/audit/2026-04-09-performance-review.md`
**Priority:** Team should review before next sprint planning.

## Critical

- **P-001:** `DataHealthService.AnalyzeAsync()` loads ALL transactions into memory 3 separate times via `GetAllForHealthAnalysisAsync()`. On Pi with 5K+ transactions, this risks OOM. Also contains O(n²) near-duplicate loop.

## High

- **P-002:** `BudgetProgressService.GetMonthlySummaryAsync()` issues N+1 queries — one `GetSpendingByCategoryAsync` per expense category in a `foreach` loop. 20 categories = 20 sequential DB round-trips.
- **P-003:** `ReportService.BuildCategorySpendingListAsync()` and `BuildTopCategoriesAsync()` issue N+1 queries to resolve category names via `GetByIdAsync` per category — despite categories already being loaded via `.Include()`.
- **P-004:** `GetUncategorizedAsync()` returns ALL uncategorized transactions with no limit. Called by `CategorySuggestionService`.
- **P-005:** `GetAllForHealthAnalysisAsync()` loads full entity graphs for all transactions with no projection.
- **P-006:** `GetAllDescriptionsAsync()` returns all distinct descriptions unbounded.
- **P-007:** `GET /api/v1/transactions` (by date range) has no pagination parameters.

## Medium

- **P-008 through P-014:** Various double iterations, unbounded results, correlated subqueries, missing `<Virtualize>` and `@key` in Blazor client.

## Decision Needed

Should the team prioritize P-001 and P-002 as immediate fixes, or batch all High findings into a performance sprint?
