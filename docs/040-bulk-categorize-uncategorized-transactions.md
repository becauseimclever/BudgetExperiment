# Feature 040: Bulk Categorize Uncategorized Transactions
> **Status:** � In Progress

## Evaluation Summary

| Criteria | Assessment |
|----------|------------|
| **Feasibility** | ✅ High - Uses existing domain patterns; all infrastructure in place |
| **Complexity** | 🟡 Medium - New service/endpoints, paged queries, multi-select UI |
| **Effort Estimate** | 6-8 hours total (see phase estimates below) |
| **Value** | ✅ High - Major UX improvement for users with many imports |
| **Risk** | 🟢 Low - No schema changes, existing patterns apply |
| **Recommendation** | ✅ **Proceed** - Clear value, well-scoped, builds on existing code |

### Key Strengths
- Leverages existing `UpdateCategory()` method on `Transaction` entity
- Repository already has `GetUncategorizedAsync()` - just needs paging extension
- Follows established patterns from `TransfersController` for paging
- No database migrations required

### Potential Concerns
- UI complexity with multi-select + filters + pagination (largest effort)
- Need to decide: add to existing `TransactionsController` vs. new controller
- Ensure consistent UX with existing pages (filter patterns, table styling)

---

## Overview

Provide a UI and API to view all uncategorized transactions, filter them, and bulk assign categories. This helps users efficiently categorize transactions when auto-categorization fails or is incomplete.

## Problem Statement

Currently, users must categorize uncategorized transactions one at a time, which is slow and tedious when auto-categorization misses many items. There is no way to filter or bulk assign categories to speed up this process.

### Current State

- Uncategorized transactions are not easily discoverable in bulk
- The `ITransactionRepository.GetUncategorizedAsync()` method exists but returns all uncategorized transactions without filtering or paging
- No filtering or bulk assignment UI exists
- Users must edit each transaction individually via the Calendar or Accounts pages

### Target State

- Users can navigate to a dedicated "Uncategorized" page listing all transactions without a category
- Filtering by date range, amount range, description (contains), and account is supported
- Server-side paging prevents overwhelming response sizes
- Users can select multiple transactions and assign a category in bulk
- Bulk actions are validated and provide clear success/error feedback

---

## User Stories

### Bulk Categorization

#### US-040-001: View uncategorized transactions
**As a** user
**I want to** see all uncategorized transactions in one place
**So that** I can quickly identify and categorize them

**Acceptance Criteria:**
- [ ] There is a dedicated page at `/uncategorized` listing all uncategorized transactions
- [ ] The view supports server-side paging (default 50 per page, configurable)
- [ ] The view supports sorting by date (default: newest first), amount, or description
- [ ] Each row shows: date, description, amount, account name

#### US-040-002: Filter uncategorized transactions
**As a** user
**I want to** filter uncategorized transactions by date, amount, or description
**So that** I can find related transactions to categorize together

**Acceptance Criteria:**
- [ ] Filtering by date range (start date, end date) is supported
- [ ] Filtering by amount range (min, max) is supported
- [ ] Filtering by description (case-insensitive contains) is supported
- [ ] Filtering by account is supported
- [ ] Filters can be combined (AND logic)
- [ ] Filter state persists during pagination

#### US-040-003: Bulk assign categories
**As a** user
**I want to** select multiple uncategorized transactions and assign a category in one action
**So that** I can efficiently categorize many transactions at once

**Acceptance Criteria:**
- [ ] Checkbox selection allows selecting individual transactions
- [ ] "Select All (on page)" and "Deselect All" actions are available
- [ ] A dropdown or modal allows selecting the target category
- [ ] Submitting the bulk action updates all selected transactions
- [ ] Success message shows count of updated transactions
- [ ] Errors are surfaced clearly (e.g., "Failed to update 2 of 15 transactions")
- [ ] Page refreshes to show remaining uncategorized transactions after bulk action

---

## Technical Design

### Architecture Changes

| Layer | Changes |
|-------|---------|
| Domain | Add `GetUncategorizedPagedAsync()` to `ITransactionRepository` with filter/page params |
| Application | Add `IUncategorizedTransactionService` with list and bulk categorize methods |
| Contracts | Add `UncategorizedTransactionFilterDto`, `BulkCategorizeRequest`, `BulkCategorizeResponse` DTOs |
| API | Add `GET /api/v1/transactions/uncategorized` and `POST /api/v1/transactions/bulk-categorize` endpoints |
| Client | Add `IUncategorizedApiService`, update navigation, create `Uncategorized.razor` page |

### Domain Model

No new entities required. Leverages existing:
- `Transaction` entity (already has `CategoryId` nullable property)
- `BudgetCategory` entity for category selection
- `Account` entity for account filter dropdown

### New Repository Method

```csharp
// ITransactionRepository.cs - add new method
Task<(IReadOnlyList<Transaction> Items, int TotalCount)> GetUncategorizedPagedAsync(
    DateOnly? startDate = null,
    DateOnly? endDate = null,
    decimal? minAmount = null,
    decimal? maxAmount = null,
    string? descriptionContains = null,
    Guid? accountId = null,
    string sortBy = "Date",
    bool sortDescending = true,
    int skip = 0,
    int take = 50,
    CancellationToken cancellationToken = default);
```

### New DTOs (Contracts)

```csharp
// UncategorizedTransactionDtos.cs

/// <summary>
/// Filter parameters for querying uncategorized transactions.
/// </summary>
public sealed class UncategorizedTransactionFilterDto
{
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? DescriptionContains { get; set; }
    public Guid? AccountId { get; set; }
    public string SortBy { get; set; } = "Date"; // Date, Amount, Description
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paged response for uncategorized transactions.
/// </summary>
public sealed class UncategorizedTransactionPageDto
{
    public IReadOnlyList<TransactionDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Request to bulk categorize transactions.
/// </summary>
public sealed class BulkCategorizeRequest
{
    /// <summary>
    /// Gets or sets the transaction IDs to categorize.
    /// </summary>
    public IReadOnlyList<Guid> TransactionIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the target category ID.
    /// </summary>
    public Guid CategoryId { get; set; }
}

/// <summary>
/// Response from bulk categorize operation.
/// </summary>
public sealed class BulkCategorizeResponse
{
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];
}
```

### API Endpoints

| Method | Endpoint | Query/Body | Response | Description |
|--------|----------|------------|----------|-------------|
| GET | `/api/v1/transactions/uncategorized` | Query: filter params | `UncategorizedTransactionPageDto` | List uncategorized with filters and paging |
| POST | `/api/v1/transactions/bulk-categorize` | Body: `BulkCategorizeRequest` | `BulkCategorizeResponse` | Assign category to multiple transactions |

**GET Response Headers:**
- `X-Pagination-TotalCount`: Total number of matching uncategorized transactions

### Database Considerations

The existing index on `category_id` in the `transactions` table should support efficient queries for `WHERE category_id IS NULL`. Verify with query plan:

```sql
-- Verify index usage
EXPLAIN ANALYZE
SELECT * FROM transactions
WHERE category_id IS NULL
ORDER BY date DESC
LIMIT 50 OFFSET 0;
```

If performance is poor, consider a partial index:
```sql
CREATE INDEX idx_transactions_uncategorized ON transactions (date DESC)
WHERE category_id IS NULL;
```

### UI Components

**New Page: `Uncategorized.razor`**
- Route: `/uncategorized`
- Filter bar with date range picker, amount inputs, description search, account dropdown
- Table with checkbox selection, sortable columns
- Pagination controls
- Bulk action bar (visible when items selected): category dropdown + "Categorize Selected" button
- Navigation link in sidebar between "Auto-Categorize" and "Budget" (near related categorization features)

**Component Structure:**
```
Uncategorized.razor
├── Filter bar (inline filters)
├── Selection summary ("5 selected")
├── Bulk action bar (category dropdown + button)
├── Data table
│   ├── Checkbox column
│   ├── Date column (sortable)
│   ├── Description column (sortable)
│   ├── Amount column (sortable)
│   └── Account column
└── Pagination controls
```

---

## Implementation Plan

### Phase 1: Domain and Repository

**Objective:** Add filtered/paged uncategorized query to repository

**Effort:** ~1 hour

**Tasks:**
- [x] Add `GetUncategorizedPagedAsync()` signature to `ITransactionRepository`
- [x] Implement in `TransactionRepository` with EF Core query
- [x] Write integration tests for filtering, sorting, and paging
- [ ] Verify query performance with EXPLAIN ANALYZE (deferred to production testing)

**Commit:**
```
feat(domain): add paged uncategorized transaction query to repository
```

---

### Phase 2: Application Service

**Objective:** Add service layer for uncategorized transactions

**Effort:** ~1.5 hours

**Tasks:**
- [x] Create `IUncategorizedTransactionService` interface
- [x] Create `UncategorizedTransactionService` implementation
- [x] Add `GetPagedAsync()` method that maps to DTOs
- [x] Add `BulkCategorizeAsync()` method with transaction and validation
- [x] Write unit tests with mocked repository (8 tests)
- [x] Add DTOs to `BudgetExperiment.Contracts`

**Commit:**
```
feat(application): add uncategorized transaction service with bulk categorize
```

---

### Phase 3: API Endpoints

**Objective:** Expose uncategorized transaction operations via REST API

**Effort:** ~1 hour

**Tasks:**
- [x] Add DTOs to `BudgetExperiment.Contracts` (completed in Phase 2)
- [ ] Add endpoints to `TransactionsController` or create dedicated controller
- [ ] Add input validation (page bounds, valid category ID)
- [ ] Return pagination header (`X-Pagination-TotalCount`)
- [ ] Write API integration tests

**Commit:**
```
feat(api): add uncategorized transactions list and bulk categorize endpoints
```

---

### Phase 4: Client Service

**Objective:** Add client-side API service for uncategorized transactions

**Effort:** ~0.5 hours

**Tasks:**
- [ ] Add `IUncategorizedApiService` interface
- [ ] Implement `UncategorizedApiService` with HTTP calls
- [ ] Register in DI
- [ ] Write service unit tests

**Commit:**
```
feat(client): add uncategorized transactions API service
```

---

### Phase 5: UI Implementation

**Objective:** Create the Uncategorized transactions page

**Effort:** ~2-3 hours (largest phase due to multi-select UI complexity)

**Tasks:**
- [ ] Create `Uncategorized.razor` page at route `/uncategorized`
- [ ] Implement filter bar with all filter controls
- [ ] Implement data table with checkbox selection
- [ ] Implement pagination controls
- [ ] Implement bulk categorize action bar
- [ ] Add navigation link in `NavMenu.razor`
- [ ] Style consistently with existing pages
- [ ] Test responsiveness on mobile

**Commit:**
```
feat(client): add uncategorized transactions page with bulk categorize
```

---

### Phase 6: Polish and Documentation

**Objective:** Finalize feature and update docs

**Effort:** ~0.5 hours

**Tasks:**
- [ ] Add OpenAPI examples and descriptions
- [ ] Test full end-to-end flow manually
- [ ] Update feature doc status to Complete
- [ ] Add to changelog/release notes if applicable

**Commit:**
```
docs: complete feature 040 bulk categorize uncategorized transactions
```

---

## Testing Strategy

### Unit Tests

- [ ] `TransactionRepository.GetUncategorizedPagedAsync()` - filtering logic for each parameter
- [ ] `TransactionRepository.GetUncategorizedPagedAsync()` - sorting by each column
- [ ] `TransactionRepository.GetUncategorizedPagedAsync()` - paging (skip/take, total count)
- [ ] `UncategorizedTransactionService.BulkCategorizeAsync()` - success case
- [ ] `UncategorizedTransactionService.BulkCategorizeAsync()` - partial failure handling
- [ ] `UncategorizedTransactionService.BulkCategorizeAsync()` - invalid category ID

### Integration Tests

- [ ] GET `/api/v1/transactions/uncategorized` returns only uncategorized transactions
- [ ] GET `/api/v1/transactions/uncategorized` with filters returns correct subset
- [ ] GET `/api/v1/transactions/uncategorized` pagination works correctly
- [ ] POST `/api/v1/transactions/bulk-categorize` updates transactions
- [ ] POST `/api/v1/transactions/bulk-categorize` validates category exists
- [ ] POST `/api/v1/transactions/bulk-categorize` handles non-existent transaction IDs gracefully

### Manual Testing Checklist

- [ ] Navigate to `/uncategorized` page
- [ ] Verify uncategorized transactions display correctly
- [ ] Apply date filter and verify results
- [ ] Apply amount filter and verify results
- [ ] Apply description filter and verify results
- [ ] Apply account filter and verify results
- [ ] Combine multiple filters and verify results
- [ ] Change page size and verify paging
- [ ] Navigate between pages
- [ ] Sort by each column
- [ ] Select individual transactions
- [ ] Use "Select All" button
- [ ] Bulk categorize selected transactions
- [ ] Verify success message and updated count
- [ ] Verify categorized transactions no longer appear
- [ ] Test with zero uncategorized transactions (empty state)

---

## Migration Notes

- No database migrations required (uses existing schema)
- Optionally add partial index if performance testing shows need:
  ```sql
  CREATE INDEX idx_transactions_uncategorized ON transactions (date DESC)
  WHERE category_id IS NULL;
  ```

---

## Security Considerations

- All endpoints require authentication (`[Authorize]` attribute)
- Bulk categorize validates that category ID exists and is valid
- Transaction IDs are validated to exist before update
- No cross-user data access (future: add user-scoping if multi-tenant)

---

## Performance Considerations

- Server-side paging limits response size (max 100 per page)
- Filtering happens at database level, not in-memory
- Count query uses same filters for accurate pagination
- Consider caching category list for dropdown (already loaded elsewhere)
- Bulk categorize uses single transaction for atomicity

---

## Future Enhancements

- "Select All Matching" across all pages (not just current page)
- AI-suggested categories for bulk selection
- Undo/rollback for bulk actions (soft delete or audit log)
- Keyboard shortcuts for power users
- "Create Rule" option after bulk categorize (pattern from descriptions)
- Export uncategorized transactions to CSV

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Performance with large datasets | Low | Medium | Server-side paging, partial index if needed |
| UI complexity delays | Medium | Low | Start with MVP (basic table), iterate on UX |
| Scope creep ("select all matching") | Medium | Medium | Defer to Future Enhancements |
| Category dropdown performance | Low | Low | Categories already cached/loaded elsewhere |

---

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-01 | Add endpoints to existing `TransactionsController` | Follows REST resource pattern; avoids controller proliferation |
| 2026-02-01 | Use POST for bulk-categorize (not PATCH) | Operation is not idempotent (could be called with different categories); body semantics clearer |
| 2026-02-01 | Default page size 50, max 100 | Balance between UX and response size |

---

## References

- Existing: `ITransactionRepository.GetUncategorizedAsync()` (returns all, no filtering)
- Related: `ApplyRulesRequest` / `ApplyRulesResponse` pattern for bulk operations
- Related: `TestPatternRequest` / `TestPatternResponse` for filtering patterns
- Feature 028: Recurring Transaction Reconciliation (similar filter patterns)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @github-copilot |
| 2026-01-30 | Fleshed out technical design, DTOs, implementation plan | @copilot |
| 2026-02-01 | Added evaluation summary, effort estimates, risk assessment, decision log | @copilot |
| 2026-02-01 | Started Phase 1: Domain and Repository | @copilot |
