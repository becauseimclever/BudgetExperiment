# Feature: Collapsible Navigation & Component-Based UI

**Created**: 2026-01-09  
**Status**: 🔄 IN PROGRESS

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
- [ ] Calendar page becomes the root (`/`) route - first thing users see
- [ ] Collapsible left-side navigation menu
- [ ] Navigation items: Calendar, Accounts, and dynamic per-account transaction links
- [ ] Transactions management page filtered by account
- [ ] Extract reusable UI components

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

### Phase 1: Refactor Layout with Collapsible Navigation
1. Create `Components/Navigation/NavMenu.razor` - collapsible side menu
2. Update `MainLayout.razor` with sidebar layout structure
3. Add collapse/expand state management
4. Move calendar to `/` route (remove Index.razor or redirect)

### Phase 2: Extract Reusable Components
1. Create `Components/Common/Modal.razor` - extract modal pattern from existing pages
2. Create `Components/Common/PageHeader.razor` - consistent header with title + action buttons
3. Create `Components/Common/LoadingSpinner.razor` - loading state indicator
4. Create `Components/Display/MoneyDisplay.razor` - formatted money with color coding

### Phase 3: Extract Form Components
1. Create `Components/Forms/AccountForm.razor` - account create/edit
2. Create `Components/Forms/TransactionForm.razor` - transaction create/edit
3. Refactor Accounts.razor to use AccountForm component
4. Refactor Calendar.razor to use TransactionForm component

### Phase 4: Account Transactions Page
1. Create `Pages/AccountTransactions.razor` at `/accounts/{id}/transactions`
2. Create `Components/Display/TransactionTable.razor` - reusable transaction table
3. Add edit/delete functionality for transactions
4. Update navigation to include per-account transaction links

### Phase 5: Calendar Component Extraction
1. Create `Components/Calendar/CalendarGrid.razor` - month grid
2. Create `Components/Calendar/CalendarDay.razor` - day cell with totals
3. Create `Components/Calendar/DayTransactionList.razor` - selected day details
4. Refactor Calendar.razor to compose these components

### Phase 6: Polish & Testing
1. Add responsive design for mobile (collapse nav fully)
2. Add keyboard navigation support
3. Component tests with bUnit (if needed)
4. Manual testing of all workflows

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
