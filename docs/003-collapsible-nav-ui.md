# Feature: Collapsible Navigation & Component-Based UI

**Created**: 2026-01-09  
**Status**: ✅ COMPLETED

## Overview
Restructure the Blazor WebAssembly client to:
1. Make the calendar the default landing page (first thing users see)
2. Add a collapsible side navigation menu for navigating between pages
3. Enable transaction management by account
4. Refactor UI into reusable components to promote code reuse

## Current State Analysis

### Existing Pages
1. **Index.razor** (`/`) - Landing page with action cards
2. **Calendar.razor** (`/calendar`, `/calendar/{Year}/{Month}`) - Month view calendar with daily totals
3. **Accounts.razor** (`/accounts`) - Account management page

### Existing Layout
- **MainLayout.razor** - Simple layout with top navbar and links
- **CalendarLayout.razor** - Full-screen calendar layout (unused currently)

### Issues with Current Design
- Landing page is redundant (just links to calendar/accounts)
- No collapsible navigation - uses top nav bar
- No dedicated transactions page per account
- UI components are embedded in pages, not reusable
- Duplicate code patterns (modals, forms, tables, etc.)

## Goals
- [x] Calendar page becomes the root (`/`) route - first thing users see
- [x] Collapsible left-side navigation menu
- [x] Navigation items: Calendar, Accounts, and dynamic per-account transaction links
- [x] Transactions management page filtered by account
- [x] Extract reusable UI components

## Target Architecture

### Pages
| Route | Page | Description |
|-------|------|-------------|
| `/` | Calendar.razor | Calendar view (landing page) |
| `/accounts` | Accounts.razor | Account list and management |
| `/accounts/{id}/transactions` | AccountTransactions.razor | Transactions for a specific account |

### Components (Reusable)
```
Components/
├── Navigation/
│   ├── NavMenu.razor              # Collapsible navigation menu
│   └── NavMenuItem.razor          # Individual nav item
├── Common/
│   ├── Modal.razor                # Generic modal wrapper
│   ├── ConfirmDialog.razor        # Confirmation dialog
│   ├── LoadingSpinner.razor       # Loading indicator
│   └── PageHeader.razor           # Consistent page header with title/actions
├── Forms/
│   ├── AccountForm.razor          # Account create/edit form
│   └── TransactionForm.razor      # Transaction create/edit form
├── Display/
│   ├── MoneyDisplay.razor         # Formatted currency display
│   ├── AccountCard.razor          # Account summary card
│   └── TransactionTable.razor     # Transaction list table
└── Calendar/
    ├── CalendarGrid.razor         # Month grid display
    ├── CalendarDay.razor          # Individual day cell
    └── DayTransactionList.razor   # Transaction list for selected day
```

### Layout Structure
```
┌─────────────────────────────────────────────────────┐
│ Header: Budget Experiment              [☰ Toggle]   │
├──────────┬──────────────────────────────────────────┤
│          │                                          │
│  NavMenu │   Page Content (@Body)                   │
│  (Left)  │                                          │
│          │                                          │
│  📅 Cal  │                                          │
│  🏦 Acct │                                          │
│  └─Acct1 │                                          │
│  └─Acct2 │                                          │
│          │                                          │
└──────────┴──────────────────────────────────────────┘
```

- NavMenu collapses to icons only (or fully hidden on mobile)
- Accounts in nav expand to show sub-items for each account's transactions

## Implementation Plan

### Phase 1: Refactor Layout with Collapsible Navigation ✅ COMPLETED
1. ✅ Create `Components/Navigation/NavMenu.razor` - collapsible side menu with account sub-items
2. ✅ Update `MainLayout.razor` with sidebar layout structure
3. ✅ Add collapse/expand state management with hamburger menu toggle
4. ✅ Move calendar to `/` route (deleted Index.razor)
5. ✅ Create `Pages/AccountTransactions.razor` at `/accounts/{id}/transactions`
6. ✅ Update Accounts.razor to navigate to new transactions page

**Commit**: Phase 1 complete - 2026-01-09

### Phase 2: Extract Reusable Components ✅ COMPLETED
1. ✅ Create `Components/Common/Modal.razor` - generic modal with size options
2. ✅ Create `Components/Common/PageHeader.razor` - consistent header with title, subtitle, back button, and actions
3. ✅ Create `Components/Common/LoadingSpinner.razor` - loading indicator with size/message options
4. ✅ Create `Components/Display/MoneyDisplay.razor` - formatted currency with positive/negative coloring
5. ✅ Create `Components/Common/ComponentEnums.cs` - shared enums (ModalSize, SpinnerSize)
6. ✅ Refactor `Accounts.razor` to use Modal, LoadingSpinner, PageHeader
7. ✅ Refactor `Calendar.razor` to use Modal, LoadingSpinner, MoneyDisplay
8. ✅ Refactor `AccountTransactions.razor` to use Modal, LoadingSpinner, PageHeader, MoneyDisplay

**Commit**: Phase 2 complete - 2026-01-09

### Phase 3: Extract Form Components ✅ COMPLETED
1. ✅ Create `Components/Forms/AccountForm.razor` - account create/edit with validation
2. ✅ Create `Components/Forms/TransactionForm.razor` - transaction create/edit with account selector
3. ✅ Refactor Accounts.razor to use AccountForm component
4. ✅ Refactor Calendar.razor to use TransactionForm component
5. ✅ Refactor AccountTransactions.razor to use TransactionForm component
6. ✅ Clean up unused form styles from all pages

**Commit**: Phase 3 complete - 2026-01-09

### Phase 4: Polish & Optimization ✅ COMPLETED
1. ✅ Create `Components/Display/TransactionTable.razor` - reusable transaction table with optional date/actions columns
2. ✅ Create `Components/Common/ConfirmDialog.razor` - confirmation dialog for destructive actions
3. ✅ Refactor Calendar.razor to use TransactionTable component
4. ✅ Refactor AccountTransactions.razor to use TransactionTable with edit/delete actions
5. ✅ Add delete confirmation dialogs to Accounts.razor and AccountTransactions.razor
6. ✅ Clean up unused table styles from pages

**Commit**: Phase 4 complete - 2026-01-09

### Phase 5: Calendar Component Extraction ✅ COMPLETED
1. ✅ Create `Models/CalendarDayModel.cs` - shared model for calendar day data
2. ✅ Create `Components/Calendar/CalendarDay.razor` - individual day cell with totals and selection
3. ✅ Create `Components/Calendar/CalendarGrid.razor` - month grid with day headers
4. ✅ Create `Components/Calendar/DayDetail.razor` - selected day transaction panel with add button
5. ✅ Update `_Imports.razor` with Calendar namespace
6. ✅ Refactor `Calendar.razor` to use CalendarGrid, CalendarDay, and DayDetail components
7. ✅ Remove inline CalendarDay class in favor of CalendarDayModel

**Commit**: Phase 5 complete - 2026-01-09

### Phase 6: Polish & Testing ✅ COMPLETED
1. ✅ Add responsive CSS for mobile (collapse nav fully on small screens)
2. ✅ Add responsive styles for CalendarGrid, CalendarDay, Modal
3. ✅ Fix TransactionForm InitialDate parameter (not applying selected date)
4. ✅ Chrome browser testing:
   - ✅ Calendar landing page with daily totals
   - ✅ Day selection and detail panel
   - ✅ Add Transaction modal with correct date prefilled
   - ✅ Navigation collapse/expand (icons only when collapsed)
   - ✅ Accounts page with account cards
   - ✅ Delete confirmation dialog
   - ✅ Account Transactions page with date filters
5. ✅ All non-Docker tests passing (73 tests)

**Commit**: Phase 6 complete - 2026-01-10

## Component Specifications

### NavMenu.razor
```razor
@* Collapsible navigation menu *@
<nav class="nav-menu @(IsCollapsed ? "collapsed" : "expanded")">
    <NavMenuItem Icon="📅" Text="Calendar" Href="/" IsCollapsed="@IsCollapsed" />
    <NavMenuItem Icon="🏦" Text="Accounts" Href="/accounts" IsCollapsed="@IsCollapsed">
        @* Dynamic account sub-items *@
        @foreach (var account in Accounts)
        {
            <NavMenuItem Text="@account.Name" 
                        Href="@($"/accounts/{account.Id}/transactions")" 
                        IsSubItem="true" />
        }
    </NavMenuItem>
</nav>

@code {
    [Parameter] public bool IsCollapsed { get; set; }
    [Parameter] public IReadOnlyList<AccountDto> Accounts { get; set; }
}
```

### Modal.razor
```razor
@* Reusable modal wrapper *@
@if (IsVisible)
{
    <div class="modal-overlay" @onclick="OnOverlayClick">
        <div class="modal-content" @onclick:stopPropagation="true">
            <header class="modal-header">
                <h3>@Title</h3>
                <button class="modal-close" @onclick="Close">×</button>
            </header>
            <div class="modal-body">
                @ChildContent
            </div>
            @if (FooterContent != null)
            {
                <footer class="modal-footer">
                    @FooterContent
                </footer>
            }
        </div>
    </div>
}

@code {
    [Parameter] public bool IsVisible { get; set; }
    [Parameter] public string Title { get; set; }
    [Parameter] public RenderFragment ChildContent { get; set; }
    [Parameter] public RenderFragment FooterContent { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
}
```

### MoneyDisplay.razor
```razor
@* Formatted currency display with positive/negative coloring *@
<span class="money-display @GetColorClass()">
    @FormatAmount()
</span>

@code {
    [Parameter] public decimal Amount { get; set; }
    [Parameter] public string CurrencyCode { get; set; } = "USD";
    
    private string GetColorClass() => Amount >= 0 ? "positive" : "negative";
    private string FormatAmount() => Amount.ToString("C");
}
```

## Migration Notes

### Route Changes
| Old Route | New Route | Action |
|-----------|-----------|--------|
| `/` | `/` | Calendar (was Index) |
| `/calendar` | `/` | Redirect or remove |
| `/calendar/{Year}/{Month}` | `/{Year}/{Month}` | Update route |
| `/accounts` | `/accounts` | No change |
| (new) | `/accounts/{id}/transactions` | New page |

### Breaking Changes
- Home page no longer shows action cards (directly shows calendar)
- Calendar route simplified to root

## Testing Strategy

### Unit Tests
- Component isolation tests for form validation
- Money formatting tests
- Navigation state management tests

### Integration Tests
- Navigation flow between pages
- Account → Transactions navigation
- Calendar day selection → transaction list

### Manual Testing Checklist
- [ ] Calendar loads as home page
- [ ] Nav menu expands/collapses correctly
- [ ] Account sub-navigation shows all accounts
- [ ] Transaction management works per account
- [ ] Responsive design on mobile widths
- [ ] All existing functionality preserved

## Success Criteria
1. Calendar is the first thing users see at `/`
2. Collapsible left navigation with accounts hierarchy
3. Users can manage transactions per account
4. Minimum 50% reduction in duplicated UI code via components
5. All existing tests pass
6. No regression in functionality
