# Feature 071: Transaction List — Running Balance Bug, Sorting & Pagination
> **Status:** In Progress  
> **Priority:** High (bug), Medium (enhancements)  
> **Estimated Effort:** Medium (2–3 days)  
> **Dependencies:** None

## Overview

Fix a bug where the account's initial balance is excluded from the running balance calculation in the transaction list, and enhance the `TransactionTable` component with column sorting and client-side pagination.

## Problem Statement

### Bug: Initial Balance Missing from Running Balance

When viewing an account's transaction list where the date range starts on or before the account's `InitialBalanceDate`, the running balance column ignores the initial balance entirely.

**Root cause:** `BalanceCalculationService.GetBalanceBeforeDateAsync` uses a strict less-than comparison (`InitialBalanceDate < date`). When `startDate == InitialBalanceDate`, the initial balance is not included in the starting balance seed, and no other code path adds it to the running total.

```csharp
// BalanceCalculationService.cs — current (buggy) logic
var initialBalanceSum = accounts
    .Where(a => a.InitialBalanceDate < date)   // ← excludes same-day
    .Sum(a => a.InitialBalance.Amount);
```

**Example:** Account created with $1 000 initial balance on 2026-01-01. Viewing transactions from 2026-01-01 onward shows running balances that are $1 000 too low because the initial balance is never factored in.

### Enhancement: Sortable Columns

The transaction table (`TransactionTable.razor`) currently has a fixed sort order (date descending, then `CreatedAt` descending). Users cannot click a column header to change the sort column or direction. This is inconvenient for finding the largest transactions or locating entries by description.

### Enhancement: Client-Side Pagination

All transactions in the date range are rendered in a single unbroken list. For accounts with hundreds of transactions per month, this causes long scroll distances and slower rendering. Adding pagination (with configurable page size) will improve usability and performance.

### Current State

- Running balance seed does not include the initial balance when the view starts on the account's initial-balance date.
- `TransactionTable.razor` renders all items with a hard-coded descending-date sort; no column headers are interactive.
- No pagination control; every item in the date range is shown at once.

### Target State

- Running balance correctly incorporates the account's initial balance regardless of the selected date range.
- Each sortable column header (Date, Description, Amount, Balance) is clickable, toggling ascending/descending order. A visual indicator (arrow) shows the active sort column and direction.
- A pagination bar appears below the table (e.g., « 1 2 3 … » with a page-size selector defaulting to 50). Navigation updates the visible rows without a server round-trip.

---

## User Stories

### Running Balance Fix

#### US-071-001: Correct Running Balance with Initial Balance
**As a** budget user  
**I want** the running balance column to include my account's initial balance  
**So that** the balances shown match my real account balance at each point in time.

**Acceptance Criteria:**
- [ ] When the transaction list date range starts on the account's `InitialBalanceDate`, the first transaction's running balance equals `InitialBalance.Amount + transaction.Amount`.
- [ ] When the date range starts after `InitialBalanceDate`, the running balance already includes the initial balance (existing behaviour, verified by test).
- [ ] The `CurrentBalance` summary value remains consistent with the last running balance in the list.
- [ ] Existing unit tests for `BalanceCalculationService` and `TransactionListService` are updated / extended to cover this case.

### Sortable Columns

#### US-071-002: Sort Transaction Table by Column
**As a** budget user  
**I want to** click a column header to sort the table by that column  
**So that** I can quickly find specific transactions (e.g., largest amount, earliest date).

**Acceptance Criteria:**
- [ ] Clickable headers for: Date, Description, Amount, Balance.
- [ ] First click sorts ascending; second click toggles to descending (or vice-versa for Date which defaults descending).
- [ ] An arrow icon (▲ / ▼) appears next to the active sort column.
- [ ] Sorting preserves the correct running balance values (running balance is always computed in chronological order; only the display order changes).
- [ ] Category column sorting is optional stretch goal.

### Pagination

#### US-071-003: Paginate Transaction Table
**As a** budget user  
**I want** the transaction list to be paginated  
**So that** large lists are easier to browse and the page remains responsive.

**Acceptance Criteria:**
- [ ] A pagination bar appears below the table when items exceed the page size.
- [ ] Default page size is 50; selectable options: 25, 50, 100.
- [ ] Page number and page size survive a sort change (reset to page 1 on sort change).
- [ ] Total item count is displayed (e.g., "Showing 1–50 of 237").
- [ ] Pagination is client-side only (all items are already fetched for running balance computation).

---

## Technical Design

### Architecture Changes

No new projects or layers. Changes are localised to:

| Layer | File(s) | Change |
|-------|---------|--------|
| Application | `BalanceCalculationService.cs` | Fix `<` to `<=` boundary for `GetBalanceBeforeDateAsync` when called with the account's own initial-balance date. |
| Application | `TransactionListService.cs` | Ensure running balance seed includes initial balance when `startDate <= InitialBalanceDate`. |
| Client | `TransactionTable.razor` | Add sort state, clickable headers, sort logic, pagination state, and pagination UI. |
| Client | `TransactionTable.razor.css` (scoped) | Styles for sortable headers and pagination bar. |
| Tests | `BalanceCalculationServiceTests.cs` | New test: starting balance when `startDate == InitialBalanceDate`. |
| Tests | `TransactionListServiceTests.cs` | New test: running balance includes initial balance on boundary date. |
| Tests | `TransactionTable.bunit` (if applicable) | Sort and pagination behaviour tests. |

### Bug Fix Detail

`BalanceCalculationService.GetBalanceBeforeDateAsync` must include the initial balance when computing the seed for a date range that starts on the account's `InitialBalanceDate`. The cleanest fix is:

**Option A (recommended):** In `TransactionListService.GetAccountTransactionListAsync`, after obtaining `startingBalance`, check whether `startDate <= account.InitialBalanceDate`. If so, add `account.InitialBalance.Amount` to the running balance seed (since `GetBalanceBeforeDateAsync` intentionally excludes it for "before" semantics). This keeps `GetBalanceBeforeDateAsync` semantically correct ("balance strictly before date") and only adjusts the transaction list's seed.

**Option B:** Change `GetBalanceBeforeDateAsync` to use `<=`. This would alter the semantics for all callers, including the calendar grid's opening-balance computation, and risks double-counting in other flows.

### Sorting Design

Sorting is purely client-side in `TransactionTable.razor`:

```csharp
private string _sortColumn = "Date";
private bool _sortDescending = true;

private IEnumerable<TransactionListItem> SortedItems => /* apply sort */;
```

Running balance values are pre-computed server-side in chronological order and attached to each item. Changing the display sort does **not** recalculate running balances — it only reorders the visible rows. This is correct because each row's running balance represents its chronological position regardless of display order.

### Pagination Design

Client-side pagination slices `SortedItems`:

```csharp
private int _currentPage = 1;
private int _pageSize = 50;

private IEnumerable<TransactionListItem> PagedItems =>
    SortedItems.Skip((_currentPage - 1) * _pageSize).Take(_pageSize);
```

A `Pagination` component (or inline markup) renders page buttons and a page-size selector.

### UI Components

**Sortable header cell:**
```html
<th class="sortable @(IsActive("Date") ? "sorted" : "")" @onclick="() => ToggleSort("Date")">
    Date <span class="sort-arrow">@(GetSortArrow("Date"))</span>
</th>
```

**Pagination bar:**
```html
<div class="pagination-bar">
    <span>Showing @start–@end of @total</span>
    <button disabled="@(currentPage == 1)" @onclick="PrevPage">«</button>
    @foreach (var p in pageNumbers) { <button class="@(p == currentPage ? "active" : "")" @onclick="() => GoToPage(p)">@p</button> }
    <button disabled="@(currentPage == totalPages)" @onclick="NextPage">»</button>
    <select @bind="PageSize">
        <option value="25">25</option>
        <option value="50">50</option>
        <option value="100">100</option>
    </select>
</div>
```

---

## Implementation Plan

### Phase 1: Fix Running Balance Bug (TDD)

**Objective:** Ensure the initial balance is always included in the running balance seed.

**Tasks:**
- [x] Write failing unit tests in `TransactionListServiceTests`: running balance when `startDate == InitialBalanceDate`, when `startDate < InitialBalanceDate`, daily balances on boundary, and zero initial balance.
- [x] Fix `TransactionListService.GetAccountTransactionListAsync` to add `account.InitialBalance.Amount` to the running balance seed when `startDate <= account.InitialBalanceDate`.
- [x] Ensure `StartingBalance` DTO uses the corrected seed.
- [x] Verify `CurrentBalance` summary is consistent (already correct — uses `account.InitialBalance.Amount + totalAmount` directly).
- [x] Run all existing tests — 1,407 pass, 0 regressions.

**Commit:**
```bash
git add .
git commit -m "fix(transaction): include initial balance in running balance calculation

- Add initial balance to running balance seed when startDate <= InitialBalanceDate
- Add unit tests for boundary condition
- Fixes running balance being off by initial balance amount

Refs: #071"
```

---

### Phase 2: Sortable Column Headers

**Objective:** Make Date, Description, Amount, and Balance columns sortable with visual indicators.

**Tasks:**
- [ ] Add sort state (`_sortColumn`, `_sortDescending`) to `TransactionTable.razor` `@code` block.
- [ ] Replace static `<th>` elements with clickable sort headers.
- [ ] Implement `ToggleSort(column)` method and `SortedItems` computed property.
- [ ] Add CSS for `.sortable`, `.sorted`, `.sort-arrow` in scoped stylesheet.
- [ ] Verify running balance values remain unchanged regardless of display sort order.
- [ ] Add bUnit tests for sort toggle behaviour (if bUnit is in use).

**Commit:**
```bash
git add .
git commit -m "feat(client): add sortable columns to transaction table

- Clickable column headers for Date, Description, Amount, Balance
- Toggle ascending/descending with arrow indicator
- Running balance values unaffected by display sort

Refs: #071"
```

---

### Phase 3: Client-Side Pagination

**Objective:** Add pagination to the transaction table with configurable page size.

**Tasks:**
- [ ] Add pagination state (`_currentPage`, `_pageSize`) to `TransactionTable.razor`.
- [ ] Compute `PagedItems` from `SortedItems` using `Skip` / `Take`.
- [ ] Render pagination bar below the table with page buttons and page-size selector.
- [ ] Reset to page 1 on sort change or when `Items` parameter changes.
- [ ] Show "Showing X–Y of Z" label.
- [ ] Add CSS for `.pagination-bar` in scoped stylesheet.
- [ ] Add bUnit tests for pagination (page navigation, page-size change).

**Commit:**
```bash
git add .
git commit -m "feat(client): add client-side pagination to transaction table

- Default page size 50, selectable 25/50/100
- Page navigation bar with item count
- Resets to page 1 on sort or data change

Refs: #071"
```

---

### Phase 4: Polish & Documentation

**Objective:** Final cleanup, accessibility, and documentation.

**Tasks:**
- [ ] Add `aria-sort` attributes to sortable headers for accessibility.
- [ ] Ensure keyboard navigation works for sort headers and pagination buttons.
- [ ] Update XML comments on modified public APIs.
- [ ] Remove any TODO comments.
- [ ] Manual testing with accounts that have zero, small, and large initial balances.
- [ ] Manual testing with 200+ transactions to verify pagination UX.

**Commit:**
```bash
git add .
git commit -m "docs(client): polish transaction table sorting and pagination

- Accessibility attributes on sort headers
- Keyboard navigation support
- XML comment updates

Refs: #071"
```

---

## Testing Strategy

### Unit Tests

- [x] `TransactionListService`: running balance correct when `startDate == InitialBalanceDate` ($1000 initial, -$50 txn → running balance $950).
- [x] `TransactionListService`: running balance correct when `startDate < InitialBalanceDate` ($500 initial on Jan 15, view from Jan 1, -$100 txn → running balance $400).
- [x] `TransactionListService`: daily balances include initial balance on boundary date.
- [x] `TransactionListService`: running balance correct when `InitialBalance` is zero.
- [ ] `TransactionListService`: running balance correct when `startDate > InitialBalanceDate` (covered by existing tests).
- [ ] `BalanceCalculationService`: `GetBalanceBeforeDateAsync` returns 0 when date == `InitialBalanceDate` (verifies "strictly before" semantics preserved).

### Integration Tests

- [ ] API endpoint returns correct running balances for an account with initial balance viewed from its start date.

### Manual Testing Checklist

- [ ] View account transactions from the account's initial-balance date — running balance starts at `InitialBalance + first txn`.
- [ ] Sort by Amount ascending — largest expense at bottom, running balance values unchanged.
- [ ] Sort by Description — alphabetical order, running balance values unchanged.
- [ ] Page through 100+ transactions — page indicator updates, table shows correct slice.
- [ ] Change page size from 50 to 25 — table re-paginates, resets to page 1.

---

## Security Considerations

No new security surface. Sorting and pagination are client-side only; no new API endpoints or query parameters.

---

## Performance Considerations

- Pagination reduces DOM node count from N to page-size, improving render performance for large lists.
- Sorting is O(n log n) on the client for the full item set; acceptable for typical date-range sizes (< 1 000 items).
- No additional API calls introduced.

---

## Future Enhancements

- Server-side pagination if transaction volumes grow beyond practical client-side limits.
- Sticky column headers for long pages.
- Column visibility toggle (show/hide columns).
- Export sorted/filtered view to CSV.

---

## References

- [TransactionListService.cs](../src/BudgetExperiment.Application/Accounts/TransactionListService.cs) — running balance calculation
- [BalanceCalculationService.cs](../src/BudgetExperiment.Application/Accounts/BalanceCalculationService.cs) — balance seed computation
- [TransactionTable.razor](../src/BudgetExperiment.Client/Components/Display/TransactionTable.razor) — table component
- [AccountTransactions.razor](../src/BudgetExperiment.Client/Pages/AccountTransactions.razor) — account transactions page
- [Bug Fix 057: Calendar Initial Balance Bug](./057-calendar-initial-balance-bug.md) — related initial-balance fix in calendar

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-16 | Initial draft | @copilot |
| 2026-02-16 | Phase 1 complete — running balance bug fixed with 4 new tests | @copilot |
