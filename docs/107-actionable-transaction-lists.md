# Feature 107: Unified Transaction List

> **Status:** Done
> **Priority:** High
> **Dependencies:** None (builds on existing transaction infrastructure)

## Overview

Replace the separate Uncategorized Transactions and Account Transactions pages with a single, unified **Transactions** page at `/transactions`. The page provides a comprehensive, filterable, sortable, paginated view of all transactions with inline actions — categorize, edit, delete, create rules — regardless of which account they belong to or whether they have a category. The current "Uncategorized" view becomes simply a pre-applied filter (`category = none`), and the "Account Transactions" view becomes a pre-applied filter (`account = X`).

## Problem Statement

### Current State

Transaction data is scattered across **multiple pages** with different capabilities, different table implementations, and different action sets:

| Page | Route | Filters | Sorting | Pagination | Actions |
|------|-------|---------|---------|-----------|---------|
| **Uncategorized** | `/uncategorized` | Account, date, description, amount | Server-side (3 fields) | Server-side (25/50/100) | Bulk categorize only |
| **Account Transactions** | `/accounts/{id}/transactions` | Date range only | None | None (all loaded) | Edit, delete, recurring mgmt |
| **Calendar Day Detail** | `/` (click day) | Single day, account | None | None | Recurring mgmt only |

**Problems:**

- **Fragmented navigation** — Users must visit different pages depending on what they want to do. To categorize transactions they go to `/uncategorized`. To edit or delete they go to `/accounts/{id}/transactions`. There's no single place to manage all transactions.
- **Inconsistent capabilities** — Uncategorized has robust server-side filtering/sorting/pagination but no edit/delete. Account Transactions has edit/delete but no sorting, no pagination, and only a date filter. Neither has inline categorization.
- **Duplicated UI code** — Two separate table implementations (custom HTML table in Uncategorized, `TransactionTable` component in Account Transactions) rendering the same underlying data differently.
- **No cross-account view** — There is no way to see all transactions across all accounts in one list. Users must click into each account individually.
- **Context switching** — When viewing uncategorized transactions, a user who wants to edit a transaction's amount must first identify its account, navigate to that account's page, find the transaction again, and then edit it.

### Target State

- **Single `/transactions` page** with a powerful filter bar covering: account(s), category (including "Uncategorized"), date range, description search, amount range
- **All actions available everywhere** — Every row supports: inline category assignment, edit, delete, create rule from description
- **Deep-linkable filters** — `/transactions?category=uncategorized` replaces the old Uncategorized page. `/transactions?account={id}` replaces Account Transactions. Existing nav links and in-app navigation update to use the new routes.
- **Consistent server-side sorting and pagination** across all views
- **Single shared table component** for rendering transaction rows
- **Running balance** shown when filtered to a single account (same as current Account Transactions behavior)
- **Bulk actions** (categorize, delete) available via multi-select regardless of filter state
- **AI-suggested categories** shown inline for uncategorized transactions

---

## User Stories

### Core Unified List

#### US-107-001: Unified Transaction Page
**As a** user  
**I want to** see all my transactions in one filterable list  
**So that** I don't have to navigate between separate pages to find, categorize, or edit transactions.

**Acceptance Criteria:**
- [x] A new page at `/transactions` displays all transactions across all accounts
- [x] The page supports server-side pagination (default 50, options: 25/50/100)
- [x] The page supports server-side sorting by date, description, amount, category, and account
- [x] Default sort is date descending
- [x] Nav menu replaces "Uncategorized" link with "Transactions" link
- [x] Account Transactions page redirects or links to `/transactions?account={id}`
- [ ] *(Deferred)* The page loads performantly even with thousands of transactions — requires dedicated load test suite

#### US-107-002: Filter Bar
**As a** user  
**I want to** filter the transaction list by multiple criteria  
**So that** I can focus on exactly the transactions I need to work with.

**Acceptance Criteria:**
- [x] Filter by account (dropdown, multi-select or single-select)
- [x] Filter by category (dropdown with "Uncategorized" as a specific option)
- [x] Filter by date range (from/to date pickers)
- [x] Filter by description (text search, contains)
- [x] Filter by amount range (min/max)
- [x] Filters are reflected in the URL query string for deep-linking and bookmarking
- [x] A "Clear Filters" button resets all filters
- [x] Active filter count is shown as a badge
- [x] Filters persist across pagination (server-side filtering)

#### US-107-003: Deep-Linkable Filter Presets
**As a** user navigating from other parts of the app  
**I want to** land on the transactions page with relevant filters pre-applied  
**So that** the transition from account pages or dashboard feels seamless.

**Acceptance Criteria:**
- [x] `/transactions?category=uncategorized` shows only uncategorized transactions (replaces old `/uncategorized` route)
- [x] `/transactions?account={id}` shows transactions for a specific account (replaces old `/accounts/{id}/transactions` route)
- [x] `/transactions?account={id}&category=uncategorized` combines filters (uncategorized for one account)
- [x] Old routes (`/uncategorized`, `/accounts/{id}/transactions`) redirect to the new URL with appropriate query params
- [x] Links from the Accounts page, Dashboard, and Nav Menu all point to the new routes

### Inline Actions

#### US-107-004: Inline Category Assignment
**As a** user viewing any transaction  
**I want to** assign or change its category directly from the table row  
**So that** categorization is fast and doesn't require opening a modal.

**Acceptance Criteria:**
- [x] Each row displays the current category (or "Uncategorized" placeholder) as a clickable element
- [x] Clicking it opens an inline dropdown/combobox with category search
- [x] Selecting a category saves immediately and updates the row in place
- [x] A toast confirms the change
- [x] Works for both uncategorized transactions (initial assignment) and categorized transactions (re-categorization)

#### US-107-005: Row Actions (Edit, Delete, Create Rule)
**As a** user viewing a transaction  
**I want to** edit, delete, or create a categorization rule from any transaction row  
**So that** I can manage transactions without navigating away.

**Acceptance Criteria:**
- [x] Each row has an actions column with: Edit (pencil icon), Delete (trash icon), Create Rule (wand/magic icon)
- [x] Edit opens the existing `TransactionForm` modal pre-filled with the transaction's data
- [x] Delete opens a confirmation dialog, then removes the transaction
- [x] Create Rule opens a modal pre-filled with the transaction's description as the pattern and a category picker
- [x] After creating a rule, offer to apply it to matching uncategorized transactions
- [x] Actions are consistent regardless of which filters are active

#### US-107-006: Bulk Actions
**As a** user viewing multiple transactions  
**I want to** select several and perform batch operations  
**So that** I can categorize or manage groups of transactions efficiently.

**Acceptance Criteria:**
- [x] Checkbox on each row for multi-select
- [x] "Select All" checkbox in the header (selects current page)
- [x] Floating action bar appears when items are selected, showing count and actions
- [x] Bulk categorize: pick a category and apply to all selected
- [x] Bulk delete: confirm and delete all selected
- [x] Selection persists across sort changes but clears on filter/page changes

### Account-Specific Features

#### US-107-007: Running Balance for Single-Account View
**As a** user filtering to a single account  
**I want to** see a running balance column  
**So that** I can track the account balance over time, just like the current Account Transactions page.

**Acceptance Criteria:**
- [x] When exactly one account is selected in the filter, a "Balance" column appears
- [x] Running balance is computed server-side based on the account's initial balance and sorted transactions
- [x] The balance column is hidden when multiple accounts or "All Accounts" is selected (balance across accounts is meaningless)
- [x] Starting balance and current balance are shown in a summary bar above the table

### AI Integration

#### US-107-008: AI-Suggested Categories
**As a** user viewing uncategorized transactions  
**I want to** see the system's best guess for each transaction's category  
**So that** I can quickly accept accurate suggestions.

**Acceptance Criteria:**
- [x] When the "Uncategorized" category filter is active, the system fetches suggested categories for visible transactions
- [x] Suggestions are shown as a subtle chip/badge next to the category column
- [x] Clicking the suggestion accepts it (same as inline category assignment)
- [x] Suggestions use the existing `FindMatchingCategoryAsync` categorization engine
- [x] A batch API endpoint avoids N+1 calls (one request for all visible transaction descriptions)
- [x] No suggestions shown for already-categorized transactions

---

## Technical Design

### API Changes

#### New Endpoint: Unified Transaction List

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/transactions` | Paginated, filtered, sorted transaction list (replaces both uncategorized and account-specific list endpoints) |

**Query Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `accountId` | `Guid?` | Filter to a specific account |
| `categoryId` | `Guid?` | Filter to a specific category |
| `uncategorized` | `bool?` | If `true`, show only transactions without a category |
| `startDate` | `DateOnly?` | Start of date range |
| `endDate` | `DateOnly?` | End of date range |
| `description` | `string?` | Description search (contains) |
| `minAmount` | `decimal?` | Minimum amount |
| `maxAmount` | `decimal?` | Maximum amount |
| `sortBy` | `string?` | Sort field: `date`, `description`, `amount`, `category`, `account` |
| `sortDescending` | `bool?` | Sort direction (default: `true`) |
| `page` | `int?` | Page number (default: 1) |
| `pageSize` | `int?` | Items per page (default: 50, max: 100) |

**Response:** `UnifiedTransactionPageDto`

```csharp
public record UnifiedTransactionPageDto(
    IReadOnlyList<UnifiedTransactionItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize,
    TransactionSummaryDto? Summary,
    AccountBalanceInfoDto? BalanceInfo); // Only populated when single account filtered

public record UnifiedTransactionItemDto(
    Guid Id,
    DateOnly Date,
    string Description,
    MoneyDto Amount,
    Guid AccountId,
    string AccountName,
    Guid? CategoryId,
    string? CategoryName,
    bool IsRecurring,
    bool IsTransfer,
    int Version); // For optimistic concurrency on inline edits

public record TransactionSummaryDto(
    int TotalCount,
    MoneyDto TotalAmount,
    MoneyDto IncomeTotal,
    MoneyDto ExpenseTotal,
    int UncategorizedCount);

public record AccountBalanceInfoDto(
    MoneyDto InitialBalance,
    DateOnly InitialBalanceDate,
    MoneyDto CurrentBalance);
```

#### New Endpoint: Batch Category Suggestions

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/v1/transactions/suggest-categories` | Returns suggested categories for a list of transaction IDs |

#### Modified Endpoint: Single Transaction Category Update

| Method | Endpoint | Description |
|--------|----------|-------------|
| PATCH | `/api/v1/transactions/{id}/category` | Quick category assignment (lighter than full PUT) |

### Client Architecture

#### New Components

| Component | Purpose |
|-----------|---------|
| `TransactionsPage.razor` | Main unified page at `/transactions` with filter bar, table, and bulk actions |
| `TransactionFilterBar.razor` | Reusable filter bar component with all filter controls |
| `InlineCategoryPicker.razor` | Inline category dropdown for table rows |
| `CreateRuleFromTransactionModal.razor` | Pre-filled rule creation from transaction context |

#### ViewModel

A `TransactionsViewModel` encapsulates all page state and operations:

```csharp
public class TransactionsViewModel
{
    // Filter state (bound to URL query params)
    public Guid? AccountId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool? ShowUncategorizedOnly { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? DescriptionSearch { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }

    // Sort & pagination
    public string SortBy { get; set; } = "date";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    // Data
    public UnifiedTransactionPageDto? Results { get; }
    public Dictionary<Guid, SuggestedCategoryDto> Suggestions { get; }

    // Actions
    Task LoadAsync();
    Task ChangeCategoryAsync(Guid transactionId, Guid categoryId, int version);
    Task DeleteTransactionAsync(Guid transactionId);
    Task BulkCategorizeAsync(IEnumerable<Guid> transactionIds, Guid categoryId);
    Task BulkDeleteAsync(IEnumerable<Guid> transactionIds);
    Task LoadSuggestionsAsync();
}
```

#### Navigation Changes

| Current | New | Redirect |
|---------|-----|----------|
| `/uncategorized` | `/transactions?uncategorized=true` | 302 redirect or `@page` alias |
| `/accounts/{id}/transactions` | `/transactions?account={id}` | 302 redirect or `@page` alias |
| Nav menu "Uncategorized" badge | Nav menu "Transactions" with uncategorized count badge | Update `NavMenu.razor` |

### Database Changes

None. The unified query is built from the existing `Transactions` table with joins to `Accounts` and `Categories`.

---

## Implementation Plan

### Phase 1: Unified API Endpoint

**Objective:** Create the `GET /api/v1/transactions` endpoint with full filtering, sorting, and pagination.

**Tasks:**
- [x] Create `UnifiedTransactionPageDto`, `UnifiedTransactionItemDto`, `TransactionSummaryDto`, `AccountBalanceInfoDto` contracts
- [x] Create `UnifiedTransactionFilterDto` with all filter/sort/page parameters
- [x] Create `IUnifiedTransactionService` interface and `UnifiedTransactionService` implementation
- [x] Add repository method for filtered/sorted/paged transaction query with account and category joins
- [x] Add balance computation when filtered to single account
- [x] Add endpoint to `TransactionsController`
- [x] Write unit tests for service (filter combinations, sorting, pagination, summary, balance)
- [x] Write API integration tests

### Phase 2: Unified Transactions Page (Basic)

**Objective:** Create the `/transactions` page with filter bar, sortable/paginated table, and basic row display.

**Tasks:**
- [x] Create `TransactionsViewModel` with filter state, pagination, sorting, and data loading
- [x] Create `TransactionFilterBar.razor` component (account dropdown, category dropdown with "Uncategorized" option, date range, description search, amount range, clear button)
- [x] Create `TransactionsPage.razor` at `/transactions` with filter bar and table
- [x] Wire URL query string binding (filters in URL for deep-linking)
- [x] Show columns: date, description, account, category, amount
- [x] Show running balance column when single account is filtered
- [x] Write ViewModel unit tests
- [x] Write component tests for filter bar

### Phase 3: Inline Actions & Row Operations

**Objective:** Add per-row actions: inline category assignment, edit, delete, and create rule.

**Tasks:**
- [x] Create `PATCH /api/v1/transactions/{id}/category` endpoint for quick category update
- [x] Create `InlineCategoryPicker.razor` component (clickable category cell → inline dropdown with search)
- [x] Add Edit action (opens existing `TransactionForm` modal)
- [x] Add Delete action (confirmation dialog)
- [x] Add "Create Rule" action (opens modal pre-filled with description, wires conflict check and pattern test)
- [x] Create rule modal (uses existing `RuleForm` component)
- [x] Add toast notifications for all actions
- [x] Write tests for inline category update, rule creation flow

### Phase 4: Bulk Actions & Multi-Select

**Objective:** Add checkbox multi-select with bulk categorize and bulk delete.

**Tasks:**
- [x] Add checkbox column with select-all header
- [x] Create floating action bar (appears when items selected, shows count)
- [x] Wire bulk categorize (category dropdown + apply)
- [x] Wire bulk delete (confirmation + execute)
- [x] Manage selection state (clear on filter/page change, persist on sort change)
- [x] Write tests for bulk operations

### Phase 5: AI Category Suggestions

**Objective:** Show suggested categories inline for uncategorized transactions.

**Tasks:**
- [x] Add `POST /api/v1/transactions/suggest-categories` batch endpoint
- [x] Add client service method and ViewModel integration
- [x] Show suggestion chip next to category column for uncategorized rows
- [x] One-click accept on suggestion chip (same as inline category assignment)
- [x] Load suggestions automatically when uncategorized filter is active
- [x] Write tests for suggestion loading and acceptance

### Phase 6: Navigation & Migration

**Objective:** Update navigation, add redirects from old routes, and retire old pages.

**Tasks:**
- [x] Update `NavMenu.razor`: replace "Uncategorized" with "Transactions", keep uncategorized count badge
- [x] Add redirect from `/uncategorized` → `/transactions?uncategorized=true`
- [x] Add redirect from `/accounts/{id}/transactions` → `/transactions?account={id}`
- [x] Update all in-app links (Accounts page "Transactions" button, Dashboard links, etc.)
- [x] Deprecate old `Uncategorized.razor` and `AccountTransactions.razor` pages (converted to redirect stubs)
- [x] Write redirect tests for old routes

---

## Out of Scope

- **Transfers list unification** — Transfers have fundamentally different data (from/to accounts, no category). Keep as a separate page.
- **Recurring definitions** — Recurring transaction/transfer management (create, pause, resume) stays on dedicated pages. Only recurring *instances* appear in the unified transaction list.
- **Calendar page changes** — The calendar day-detail view is a distinct UX paradigm (calendar → click day). It may link to `/transactions?date=X` but keeps its own layout.
- **Drag-and-drop, keyboard shortcuts, or context menus** — Future features.
- **Description pattern grouping** — Valuable but adds significant complexity. Better as a follow-up feature once the unified list is stable.

## Dependencies & Risks

| Risk | Mitigation |
|------|-----------|
| Performance of cross-account queries with many transactions | Server-side pagination + indexed queries; never load all transactions at once |
| Loss of Account Transactions' running balance feature | Detect single-account filter and include balance data in API response |
| Breaking existing bookmarks/links to old routes | Redirect old routes to new URL with mapped query params |
| N+1 API calls for per-row AI suggestions | Batch suggestion endpoint returns all suggestions in one call |
| Complex URL query string management on the client | Use a dedicated `QueryStringHelper` or `NavigationManager` extension for two-way binding |
| Category dropdown performance with many categories | Searchable combobox; load categories once and cache client-side |
