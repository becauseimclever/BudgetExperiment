# Perf Batch 156/159 Decisions

## Feature 156 — ReportService N+1 Fix
- Use Transaction.Category navigation properties to build a category lookup dictionary once per report.
- Fallback to "Unknown" when a category ID is missing from the lookup (no repository calls in loops).

## Feature 159 — Date-Range Endpoint Deprecation + v2 Pagination
- Deprecate v1 GET /api/v1/transactions with Deprecation/Sunset/Link headers and [Obsolete] metadata.
- Add GET /api/v2/transactions/by-date-range with page/pageSize validation and X-Pagination-TotalCount header.
- Reuse IUnifiedTransactionService for pagination to avoid adding new service methods.
