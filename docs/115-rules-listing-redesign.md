# Feature 115: Rules Listing Redesign
> **Status:** Planning

## Overview

The categorization rules page currently renders every rule as a full card in a single unbounded list. As users accumulate rules (50+, 100+, or more), the page becomes an unmanageable scroll-fest that is difficult to navigate, slow to render, and painful to maintain. This feature redesigns the rules listing for scalability — both in UI ergonomics and backend performance.

## Problem Statement

### Current State

- **All rules loaded at once** — `GetAllAsync` calls `ListAsync(0, int.MaxValue)`, pulling every rule from the database in a single query with no pagination.
- **Flat, unbounded list** — Rules render as individual `RuleCard` components in a `foreach` loop. With 100+ rules, this produces a wall of cards with no grouping, filtering, or search.
- **No client-side filtering or search** — Users must visually scan the entire list to find a specific rule.
- **No grouping** — Rules targeting the same category are scattered across the list, sorted only by priority. Understanding "what rules feed into Groceries?" requires mentally filtering the whole list.
- **Performance concerns** — Loading hundreds of rules with their category navigation properties in one query, then rendering hundreds of Blazor components, will degrade both API response time and client-side rendering.
- **Card layout is space-inefficient** — Each rule card takes significant vertical space (name, pattern, match type badges, target category, action buttons). A compact table/row layout would show 3–5× more rules per viewport.

### Target State

- **Server-side pagination** — Rules API returns paginated results (default 25 per page) with total count.
- **Filtering & search** — Users can filter by category, active/inactive status, and search by rule name or pattern text.
- **Grouping by category** — A "group by category" toggle collapses rules into expandable category sections, making it easy to see all rules targeting a given category.
- **Compact list view** — A dense table/row layout is the default, showing rule name, pattern, match type, category, priority, and status in a single row. The full card view remains available as an optional toggle.
- **Bulk operations** — Select multiple rules for bulk delete, bulk activate/deactivate, or bulk category reassignment.
- **Rule count indicators** — Header shows total rule count, active count, and filtered count.
- **Responsive performance** — Page loads quickly even with 500+ rules via pagination and virtualization.

---

## User Stories

### Efficient Rule Browsing

#### US-115-001: Paginated Rule Listing
**As a** budget user with many rules  
**I want to** see rules in paginated pages  
**So that** the page loads quickly and I can navigate through rules without endless scrolling

**Acceptance Criteria:**
- [ ] Rules are displayed in pages of 25 (configurable: 10, 25, 50, 100)
- [ ] Pagination controls show current page, total pages, and total rule count
- [ ] Page size preference persists across sessions (local storage)
- [ ] API returns paginated results with `X-Pagination-TotalCount` header

#### US-115-002: Search Rules
**As a** user looking for a specific rule  
**I want to** search rules by name or pattern text  
**So that** I can quickly find and edit a rule without scrolling

**Acceptance Criteria:**
- [ ] Search input with debounced (300ms) server-side filtering
- [ ] Searches against rule name and pattern text (case-insensitive)
- [ ] Search results show match count
- [ ] Clear search button resets to full list

#### US-115-003: Filter by Category
**As a** user managing rules for a specific category  
**I want to** filter rules to show only those targeting a specific category  
**So that** I can review and manage related rules together

**Acceptance Criteria:**
- [ ] Category dropdown filter showing all categories with rule counts
- [ ] "All Categories" option to reset filter
- [ ] Filter combines with search (AND logic)

#### US-115-004: Filter by Status
**As a** user  
**I want to** filter rules by active/inactive status  
**So that** I can focus on active rules or review deactivated ones

**Acceptance Criteria:**
- [ ] Status filter: All, Active Only, Inactive Only
- [ ] Filter combines with category filter and search

### Compact Display

#### US-115-005: Table/Row View
**As a** user with many rules  
**I want to** see rules in a compact table layout  
**So that** I can see more rules at a glance without scrolling

**Acceptance Criteria:**
- [ ] Table view with columns: Priority, Name, Pattern, Match Type, Category, Status, Actions
- [ ] Sortable columns (click header to sort by that column)
- [ ] Row actions (edit, activate/deactivate, delete) in a compact action column
- [ ] Table view is the default for lists > 10 rules
- [ ] View toggle (table/card) persists in local storage

#### US-115-006: Group by Category
**As a** user  
**I want to** group rules by their target category  
**So that** I can see all rules feeding into each category in one place

**Acceptance Criteria:**
- [ ] "Group by Category" toggle in the toolbar
- [ ] Each category is a collapsible section header showing category name and rule count
- [ ] Rules within each group are sorted by priority
- [ ] Collapsed state persists during the session

### Bulk Operations

#### US-115-007: Bulk Selection and Actions
**As a** user managing many rules  
**I want to** select multiple rules and perform actions on them  
**So that** I can efficiently manage rules in bulk instead of one at a time

**Acceptance Criteria:**
- [ ] Checkbox selection on each row/card
- [ ] "Select All" on current page
- [ ] Bulk action toolbar appears when items are selected: Delete, Activate, Deactivate
- [ ] Confirmation dialog for bulk delete showing count
- [ ] Success toast showing number of affected rules

---

## Technical Design

### Architecture Changes

No new projects. Changes span API, Application, Infrastructure, Client, and Contracts layers.

### API Endpoint Changes

| Method | Endpoint | Change |
|--------|----------|--------|
| GET | `/api/v1/categorizationrules` | Add query params: `page`, `pageSize`, `search`, `categoryId`, `status`, `sortBy`, `sortDirection`. Return paginated response with `X-Pagination-TotalCount` header. |
| DELETE | `/api/v1/categorizationrules/bulk` | **New.** Accept `{ ids: Guid[] }` for bulk delete. |
| POST | `/api/v1/categorizationrules/bulk/activate` | **New.** Accept `{ ids: Guid[] }` for bulk activation. |
| POST | `/api/v1/categorizationrules/bulk/deactivate` | **New.** Accept `{ ids: Guid[] }` for bulk deactivation. |

### Contracts (New/Modified DTOs)

```csharp
// New: Paginated request for rules listing
public record CategorizationRuleListRequest(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    Guid? CategoryId = null,
    string? Status = null,       // "active", "inactive", or null for all
    string? SortBy = null,       // "priority", "name", "category", "createdAt"
    string? SortDirection = null  // "asc" or "desc"
);

// New: Paginated response
public record CategorizationRulePageResponse(
    IReadOnlyList<CategorizationRuleDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

// New: Bulk action request
public record BulkRuleActionRequest(IReadOnlyList<Guid> Ids);
```

### Repository Changes

```csharp
// New method on ICategorizationRuleRepository
Task<(IReadOnlyList<CategorizationRule> Items, int TotalCount)> ListPagedAsync(
    int page,
    int pageSize,
    string? search,
    Guid? categoryId,
    bool? isActive,
    string? sortBy,
    string? sortDirection,
    CancellationToken cancellationToken = default);
```

### Service Changes

- `GetAllAsync` is preserved for backward compatibility (used by Apply Rules and AI features) but a new `ListPagedAsync` method is added for the listing UI.
- New `BulkDeleteAsync`, `BulkActivateAsync`, `BulkDeactivateAsync` methods.

### UI Components

- **RulesToolbar** — New component: search input, category filter dropdown, status filter, view toggle (table/card), group-by toggle.
- **RulesTable** — New component: compact table view of rules with sortable headers and inline actions.
- **RulesPagination** — New component: page navigation, page size selector, total count display.
- **BulkActionBar** — New component: floating/sticky bar that appears when items are selected, showing selected count and bulk action buttons.
- **Rules.razor** — Refactored to compose the new components, defaulting to table view.

### Performance Considerations

#### Listing Performance
- Server-side pagination prevents loading all rules at once.
- Server-side search and filtering push work to the database (indexed queries) instead of client-side scanning.
- EF Core query uses `Skip`/`Take` for efficient pagination.
- Consider adding a database index on `CategorizationRule.Name` for search performance.
- Bulk operations use a single round-trip per action instead of N individual calls.

#### Rule Application Performance (ApplyRules / Categorization Engine)

The current `CategorizationEngine.ApplyRulesAsync` has an **O(N × M)** evaluation pattern: every transaction (N) is tested against every active rule (M) until a match is found. This has several scaling concerns:

| Aspect | Current Behavior | Risk at Scale |
|--------|-----------------|---------------|
| **Rule evaluation** | N×M: each transaction tested against all rules until first match | 10K transactions × 100 rules = up to 1M evaluations |
| **Regex compilation** | Pre-compiled per entity instance via `_compiledRegex` field | **Lost on every request** — rules are re-loaded from DB each time, so compiled regex is rebuilt per request |
| **Regex timeout** | 1-second timeout per match (`RegexMatchTimeoutException` → returns `false`) | A single pathological regex can block for 1 second × N transactions |
| **Rule loading** | Fresh `GetActiveByPriorityAsync()` query per apply request, no cache | Moderate — OK for infrequent apply, wasteful if called repeatedly |
| **Transaction fetch (by IDs)** | Individual `GetByIdAsync` in a loop — N+1 problem | **Critical**: 1000 IDs = 1000 DB round-trips |
| **String methods** | `Contains`, `StartsWith`, `EndsWith` use native `StringComparison` | Fast — O(description length), negligible overhead |

**Mitigation strategies (in priority order):**

1. **Batch transaction loading** — Replace the per-ID loop in `ApplyRulesAsync` with a single `GetByIdsAsync(IReadOnlyList<Guid>)` batch query. Eliminates N+1 problem.

2. **In-memory rule cache** — Cache active rules (with compiled regexes) using `IMemoryCache` with short TTL (e.g., 30 seconds) or invalidation on rule CRUD. Avoids rebuilding compiled regexes per request and eliminates redundant DB queries.

3. **Separate non-regex from regex rules** — Partition active rules into two groups during evaluation:
   - **String rules** (Contains/StartsWith/EndsWith/Exact): Very fast, evaluate first.
   - **Regex rules**: More expensive, evaluate only if no string rule matched. Most categorization patterns are simple `Contains` matches ("WALMART" → Groceries). Only evaluate regex rules for transactions that didn't match any string rule.

4. **Compiled regex with `RegexOptions.Compiled`** — The current `BuildRegex` does NOT use `RegexOptions.Compiled` (which JIT-compiles to IL). Adding this flag trades startup time for significantly faster matching on repeated evaluations. Ideal when rules are cached.

5. **Regex complexity validation** — At rule creation time, validate that regex patterns are not pathologically complex (e.g., reject nested quantifiers like `(a+)+`). The 1-second timeout is a safety net, not a solution.

6. **Database-side matching for simple patterns** — For `Contains`/`StartsWith`/`EndsWith` rules, consider pushing matching to SQL (`LIKE`, `ILIKE` on PostgreSQL) to avoid loading transactions into memory. This is a bigger architectural change but would be the ultimate performance win for non-regex rules.

**Recommendation:** Items 1–3 are low-risk, high-impact improvements that should be included in this feature. Items 4–6 are optimizations to consider if profiling shows bottlenecks at higher scale.

---

## Implementation Plan

Each slice is a vertical cut delivering testable, deployable value from API through client. Every slice includes its own tests and can be merged independently.

### Slice 1: Paginated Rules Listing (End-to-End)

**Objective:** Replace the unbounded rule list with a paginated table view — API through UI in one slice.

**Tasks:**
- [ ] Add `CategorizationRuleListRequest` and `CategorizationRulePageResponse` to Contracts
- [ ] Add `ListPagedAsync` to `ICategorizationRuleRepository` interface
- [ ] Implement `ListPagedAsync` in `CategorizationRuleRepository` (EF Core `Skip`/`Take`, ordered by priority)
- [ ] Add `ListPagedAsync` to `ICategorizationRuleService` and implement in `CategorizationRuleService`
- [ ] Update `CategorizationRulesController` GET endpoint to accept `page` and `pageSize` params, return `X-Pagination-TotalCount` header
- [ ] Update `IBudgetApiService` client to call paginated endpoint
- [ ] Create `RulesPagination` component (page nav, page size selector, total count)
- [ ] Create `RulesTable` component (compact table: Priority, Name, Pattern, Match Type, Category, Status, Actions)
- [ ] Update `RulesViewModel` with pagination state (`CurrentPage`, `PageSize`, `TotalCount`)
- [ ] Refactor `Rules.razor` to use `RulesTable` + `RulesPagination` as default view
- [ ] Write unit tests: service pagination logic, ViewModel pagination state
- [ ] Write bUnit tests: `RulesTable` renders columns, `RulesPagination` emits page changes
- [ ] Write API integration test: paginated GET returns correct page/count

**Commit:**
```bash
git commit -m "feat(rules): paginated table view for rules listing

- Server-side pagination with page/pageSize params
- Compact table layout replaces card list as default
- RulesPagination component with page size selector
- X-Pagination-TotalCount response header
- Unit, bUnit, and integration tests

Refs: #115"
```

---

### Slice 2: Search & Filter

**Objective:** Add search by name/pattern and filter by category/status — full vertical slice from DB query to UI toolbar.

**Tasks:**
- [ ] Extend `ListPagedAsync` repository to support `search`, `categoryId`, `isActive` filter params
- [ ] Add database index on `Name` column for search performance
- [ ] Extend `ListPagedAsync` service to pass through filter params
- [ ] Update controller GET endpoint to accept `search`, `categoryId`, `status` query params
- [ ] Update `IBudgetApiService` client to pass filter params
- [ ] Create `RulesToolbar` component (search input with debounce, category dropdown, status filter)
- [ ] Update `RulesViewModel` with filter state (`SearchText`, `FilterCategoryId`, `FilterStatus`)
- [ ] Wire toolbar changes to reload paginated data (reset to page 1 on filter change)
- [ ] Write unit tests: repository filtering queries, service filter pass-through
- [ ] Write bUnit tests: toolbar emits filter events, debounced search
- [ ] Write ViewModel tests: filter changes reset page, trigger reload

**Commit:**
```bash
git commit -m "feat(rules): search and filter for rules listing

- Search by rule name or pattern text (debounced, server-side)
- Filter by category and active/inactive status
- RulesToolbar component with search, category dropdown, status filter
- Database index on Name for search performance

Refs: #115"
```

---

### Slice 3: Sortable Columns

**Objective:** Add server-side sorting by clicking table column headers.

**Tasks:**
- [ ] Extend `ListPagedAsync` repository to support `sortBy` and `sortDirection` params
- [ ] Extend service and controller to pass through sort params
- [ ] Update `RulesTable` headers to be clickable with sort direction indicators
- [ ] Update `RulesViewModel` with sort state (`SortBy`, `SortDirection`)
- [ ] Write unit tests: repository sort queries, ViewModel sort state toggle
- [ ] Write bUnit test: clicking header emits sort event

**Commit:**
```bash
git commit -m "feat(rules): sortable columns in rules table

- Click column headers to sort by priority, name, category, createdAt
- Toggle ascending/descending with sort direction indicators
- Server-side sorting via repository query

Refs: #115"
```

---

### Slice 4: Group by Category

**Objective:** Add a toggle to view rules grouped under collapsible category headers.

**Tasks:**
- [ ] Add group-by-category toggle to `RulesToolbar`
- [ ] Create grouped view rendering in `Rules.razor` (category section headers with rule count, collapsible)
- [ ] Update `RulesViewModel` with `IsGroupedByCategory` state and grouping logic
- [ ] When grouped, sort within each category group by priority
- [ ] Write ViewModel tests: grouping logic, collapse state
- [ ] Write bUnit test: grouped sections render with correct counts

**Commit:**
```bash
git commit -m "feat(rules): group by category view

- Toggle to group rules under collapsible category headers
- Each group shows category name and rule count
- Rules within groups sorted by priority

Refs: #115"
```

---

### Slice 5: Bulk Operations

**Objective:** Add multi-select and bulk delete/activate/deactivate — full vertical slice from API endpoints to selection UI.

**Tasks:**
- [ ] Add `BulkRuleActionRequest` to Contracts
- [ ] Add `BulkDeleteAsync`, `BulkActivateAsync`, `BulkDeactivateAsync` to repository interface and implementation
- [ ] Add bulk methods to service interface and implementation
- [ ] Add bulk endpoints to controller (`DELETE bulk`, `POST bulk/activate`, `POST bulk/deactivate`)
- [ ] Add checkbox column to `RulesTable` with "Select All" on current page
- [ ] Create `BulkActionBar` component (sticky bar with selected count and action buttons)
- [ ] Update `RulesViewModel` with selection state and bulk action methods
- [ ] Add confirmation dialog for bulk delete
- [ ] Update `IBudgetApiService` with bulk API calls
- [ ] Write unit tests: service bulk operations, ViewModel selection logic
- [ ] Write bUnit tests: checkbox selection, bulk action bar visibility
- [ ] Write API integration tests: bulk endpoints

**Commit:**
```bash
git commit -m "feat(rules): bulk rule operations with multi-select

- Checkbox selection with Select All on current page
- Bulk delete, activate, deactivate endpoints
- BulkActionBar appears when items selected
- Confirmation dialog for destructive bulk actions

Refs: #115"
```

---

### Slice 6: Rule Application Performance

**Objective:** Fix the critical performance issues in the categorization engine for applying rules at scale.

**Tasks:**
- [ ] Add `GetByIdsAsync(IReadOnlyList<Guid>)` to `ITransactionRepository` and implement — replace N+1 loop in `ApplyRulesAsync`
- [ ] Add `IMemoryCache`-based caching for active rules in `CategorizationEngine` (short TTL, invalidated on rule CRUD)
- [ ] Add `RegexOptions.Compiled` to `BuildRegex` for cached rule instances
- [ ] Partition rule evaluation: evaluate string rules first, then regex rules only for unmatched transactions
- [ ] Add regex complexity validation at rule creation (reject nested quantifiers)
- [ ] Write unit tests: batch transaction fetch, cache hit/miss, evaluation ordering
- [ ] Write performance regression test: apply 100 rules against 1000 transactions within threshold

**Commit:**
```bash
git commit -m "perf(rules): optimize rule application for large datasets

- Batch transaction loading (eliminates N+1 queries)
- In-memory cache for compiled rules
- RegexOptions.Compiled for cached regex instances
- String rules evaluated before regex rules
- Regex complexity validation at creation time

Refs: #115"
```

---

### Slice 7: View Toggle & Preference Persistence

**Objective:** Allow switching between table and card views, persist user preferences.

**Tasks:**
- [ ] Add view toggle (table/card) to `RulesToolbar`
- [ ] Preserve existing `RuleCard` component for card view mode
- [ ] Persist view preference and page size in local storage via JS interop
- [ ] Add rule count indicators to page header (total, active, filtered)
- [ ] Write ViewModel test: view toggle state
- [ ] Write bUnit test: correct view renders based on toggle

**Commit:**
```bash
git commit -m "feat(rules): view toggle and preference persistence

- Table/card view toggle with local storage persistence
- Page size preference persists across sessions
- Rule count indicators in page header

Refs: #115"
```

---

### Slice 8: Polish & Accessibility

**Objective:** Final polish, accessibility audit, and documentation.

**Tasks:**
- [ ] Verify keyboard navigation in table (tab through rows, Enter to edit)
- [ ] Add ARIA labels to table, sort indicators, pagination controls
- [ ] Update OpenAPI spec documentation for new/modified endpoints
- [ ] Add XML comments for new public APIs
- [ ] Manual testing with 200+ rules dataset
- [ ] Review and clean up any TODO comments

**Commit:**
```bash
git commit -m "docs(rules): polish, accessibility, and documentation

- Keyboard navigation and ARIA labels
- OpenAPI spec updates for pagination and bulk endpoints
- XML comments for public API surface

Refs: #115"
```

---

## Open Questions

1. **Virtual scrolling vs. pagination?** — Pagination is simpler and works better with server-side filtering. Virtual scrolling could be considered later if users prefer a continuous scroll experience.
2. **Should we consolidate duplicate/overlapping rules?** — A future feature could detect and suggest merging rules with identical patterns or categories. Out of scope for this feature but worth noting.
3. **Rule priority reflow** — When filtering by category, should priority numbers be the global priority or category-local? Recommend keeping global priorities visible but sorting within group by priority.
4. **Cache invalidation strategy** — For the rule cache (Slice 6), should we use TTL-based expiry, event-based invalidation on CRUD, or both? TTL is simpler; event-based is more precise. Recommend TTL with manual invalidation on write operations.
5. **DB-side pattern matching** — Pushing `Contains`/`StartsWith`/`EndsWith` matching to PostgreSQL `ILIKE` queries would eliminate the need to load transactions into memory for simple rules. This is a significant architectural change — worth a separate feature if profiling warrants it.
