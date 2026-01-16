# UI, Recurring & Settings Features (011-020) - Consolidated Summary

**Consolidated:** 2026-01-15  
**Original Features:** 011 through 020  
**Status:** All Completed

---

## Overview

This document consolidates features 011-020 which focused on establishing a comprehensive design system, improving code architecture, and adding essential user-facing features like settings, running balances, and paycheck allocation suggestions.

---

## 011: Design System Overhaul

**Completed:** 2026-01-12

Implemented a comprehensive CSS design system with theming support.

**Key Decisions:**
- Custom CSS with CSS custom properties (no Bootstrap/Tailwind)
- Lightweight (~10KB minified)
- Full control over Fluent Design-inspired aesthetics

**CSS Architecture:**
```
wwwroot/css/
├── design-system/
│   ├── tokens.css          # CSS custom properties
│   ├── reset.css           # Modern CSS reset
│   ├── base.css            # Base element styles
│   ├── layout.css          # Layout utilities
│   ├── components/         # Button, card, form, table, modal, nav, alert styles
│   └── utilities.css       # Utility classes
├── themes/
│   ├── light.css           # Light theme
│   ├── dark.css            # Dark theme
│   └── vscode-dark.css     # VS Code Dark theme
└── app.css                 # Main entry point
```

**Theme System:**
- Light, Dark, and VS Code Dark themes
- System preference detection via `prefers-color-scheme`
- Theme persistence in localStorage
- `data-theme` attribute on document for switching

**Design Tokens:**
- Brand colors, semantic colors (success, warning, error, info)
- Neutral palette with light/dark variants
- Transaction-specific colors (income, expense, transfer, recurring)
- Typography scale, spacing scale, border radius, shadows

---

## 012: Shared Contracts Library

**Completed:** 2026-01-12

Created `BudgetExperiment.Contracts` project to eliminate DTO duplication.

**Problem Solved:**
- Duplicate models in API (DTOs) and Client (Models)
- Manual synchronization required between layers
- No compile-time safety for API contract changes

**Solution:**
```
Domain → Contracts → Application → Infrastructure → Api
                 ↘             ↗
                   Client
```

**Key DTOs Centralized:**
- Entity DTOs: `AccountDto`, `TransactionDto`, `RecurringTransactionDto`
- Value DTOs: `MoneyDto` (currency + amount)
- View DTOs: `CalendarGridDto`, `DayDetailDto`, `TransactionListItemDto`
- Request/Response DTOs: `CreateTransferRequest`, `TransferResponse`

**Naming Conventions:**
- Entity read: `{Entity}Dto`
- Entity create: `{Entity}CreateDto`
- Entity update: `{Entity}UpdateDto`
- Requests: `{Action}{Entity}Request`
- Responses: `{Entity}Response`

---

## 013: CSS Consolidation

**Completed:** 2026-01-12

Migrated all inline styles and scoped CSS to the design system.

**Migration Scope:**
- 6 pages with inline `<style>` blocks (~800+ lines removed)
- 16 components with `.razor.css` files (all deleted)
- Only `NavMenu.razor.css` and `MainLayout.razor.css` retained (established in 011)

**Phases Completed:**
1. Audit & extend design system with missing patterns
2. Calendar page migration
3. Accounts page migration
4. AccountTransactions page migration
5. Recurring page migration
6. RecurringTransfers page migration
7. Transfers page migration
8-11. Component CSS file deletions

**New Design System Files Added:**
- `badges.css` - Account, frequency, category, status badges
- `empty-state.css` - Empty state patterns
- `detail-list.css` - Detail list and balance display
- `filters.css` - Filter section components
- `recurring-card.css` - Recurring transaction/transfer cards

---

## 014: Unified Transaction Display

**Completed:** 2026-01-12

Ensured all financial activities display consistently across views.

**Data Types Unified:**
| Type | Description | Storage |
|------|-------------|---------|
| Transaction | One-time entry | `Transaction` entity |
| Recurring Transaction | Scheduled repeating | `RecurringTransaction` + exceptions |
| Transfer | Paired transactions | Two linked `Transaction` entities |
| Recurring Transfer | Scheduled transfer | `RecurringTransfer` + exceptions |

**Display Implementation:**
- Calendar grid shows totals including all types
- Day detail lists all items with type indicators
- Account transactions list shows unified view with badges
- Proper deduplication (realized items hide projections)

**Visual Indicators:**
- 🔄 Recurring items
- ↔️ Transfer items
- Both icons for recurring transfers
- "Modified" badge for changed instances
- Direction indicators (→ outgoing, ← incoming)

**Summary Calculations:**
- `ActualTotal` = realized transactions only
- `ProjectedTotal` = unrealized recurring instances
- Proper income/expense categorization including transfers

---

## 015: Realize Recurring Items

**Completed:** 2026-01-13

Enabled users to confirm recurring items into actual transactions.

**Realization Process:**
1. Create transaction(s) linked via `RecurringTransactionId`/`RecurringTransferId`
2. Set `RecurringInstanceDate` to scheduled date
3. Deduplication logic hides the projection

**API Endpoints:**
- `POST /api/v1/recurring-transactions/{id}/realize`
- `POST /api/v1/recurring-transfers/{id}/realize`
- `GET /api/v1/recurring/past-due`
- `POST /api/v1/recurring/realize-batch`

**UI Features:**
- Past-due alert banner on Calendar and Account Transactions
- "Confirm" button on recurring items in day detail
- Past-due review modal for bulk actions
- Support for realizing with modifications (different amount/date/description)

**Visual States:**
| State | Icon | Color |
|-------|------|-------|
| Future | 🔄 | Blue |
| Today | 🔄 | Green |
| Past-Due | 🔄⚠️ | Orange/Red |

---

## 016: User Settings

**Completed:** 2026-01-13

Added Settings page for user runtime configuration.

**Architecture:**
- Server-side storage (settings impact domain logic)
- Single-tenant model (no auth yet)
- `AppSettings` entity with singleton pattern

**Database Schema:**
```sql
CREATE TABLE "AppSettings" (
    "Id" uuid PRIMARY KEY,
    "AutoRealizePastDueItems" boolean NOT NULL DEFAULT false,
    "PastDueLookbackDays" integer NOT NULL DEFAULT 30,
    "CreatedAtUtc" timestamp NOT NULL,
    "UpdatedAtUtc" timestamp NOT NULL
);
```

**Settings Implemented:**
- `AutoRealizePastDueItems` - Auto-confirm past-due recurring items
- `PastDueLookbackDays` - How far back to look (1-365 days)

**UI Design:**
- Settings link at bottom of navigation (separated from main items)
- Toggle switches for boolean settings
- Section grouping (Recurring Items, Display, Data)

**API:**
- `GET /api/v1/settings` - Retrieve current settings
- `PUT /api/v1/settings` - Update settings (partial updates supported)

---

## 017: Running Balance Display

**Completed:** 2026-01-13

Display running account balances throughout the application.

**Balance Calculation:**
```
Day N Ending Balance = Day N-1 Ending Balance + Day N Transactions + Day N Projected Recurring
```

**Balance Types:**
| Type | Description |
|------|-------------|
| Actual Balance | Realized transactions only |
| Projected Balance | Includes unrealized recurring |
| Combined Balance | Actual + Projected (calendar shows this) |

**Calendar Enhancement:**
- Running balance at bottom of each day cell
- Red/warning color for negative projected balance
- Starting balance for the grid included in response

**Account Transactions Enhancement:**
- Day headers with start/end balance
- Running balance column per transaction
- Daily balance summaries

**DTOs Enhanced:**
- `CalendarDaySummaryDto`: Added `EndOfDayBalance`, `IsBalanceNegative`
- `CalendarGridDto`: Added `StartingBalance`
- `TransactionListDto`: Added `DailyBalances` collection
- New `DailyBalanceSummaryDto` for daily balance tracking

---

## 018: Paycheck Allocation Suggestions

**Completed:** 2026-01-14

Provide suggestions for how much to set aside per paycheck for bills.

**Core Formula:**
```
AnnualBillAmount = BillAmount × AnnualMultiplier(BillFrequency)
AllocationPerPaycheck = AnnualBillAmount ÷ PeriodsPerYear(PaycheckFrequency)
```

**Annual Multipliers:**
| Frequency | Multiplier |
|-----------|------------|
| Daily | 365 |
| Weekly | 52 |
| BiWeekly | 26 |
| Monthly | 12 |
| Quarterly | 4 |
| Yearly | 1 |

**Domain Value Objects:**
- `BillInfo` - Bill description, amount, frequency
- `PaycheckAllocation` - Per-bill allocation result
- `PaycheckAllocationSummary` - Complete allocation summary
- `PaycheckAllocationWarning` - Warning types (insufficient income, cannot reconcile)

**Warning Types:**
- `InsufficientIncome` - Bills require more than paycheck amount
- `CannotReconcile` - Annual bills exceed annual income
- `NoBillsConfigured` - No recurring expenses to allocate
- `NoIncomeConfigured` - No paycheck information entered

**Features:**
- View per-paycheck allocation for each bill
- Total allocation summary across all bills
- Remaining after bills calculation
- Filter by account
- Create recurring transfer for allocation amount

---

## 019: UI Design, Themes & Bug Fixes

**Completed:** 2026-01-14

Audit and fix UI inconsistencies, theme issues, and bugs.

**Bugs Fixed:**
| ID | Component | Issue | Status |
|----|-----------|-------|--------|
| BUG-001 | AccountTransactions | Column header misalignment | ✅ Fixed |
| BUG-002 | Theme Dropdown | Transparent/unreadable dropdown | ✅ Fixed |
| BUG-003 | Calendar Page | White background in dark mode | ✅ Fixed |

**Areas Reviewed:**
- Color palette consistency
- Theme switching functionality
- Component design system compliance
- Responsive design (mobile, tablet, desktop)
- Interactive states (hover, focus, active)

**Accessibility Improvements:**
- Color contrast verification (WCAG AA)
- Focus indicators visibility
- Keyboard navigation
- Touch target sizing (44x44px minimum)

---

## 020: SOLID Principles Refactoring

**Completed:** 2026-01-14

Decomposed large classes to follow Single Responsibility Principle.

**CalendarGridService Refactoring (801 → 185 lines):**

| New Service | Lines | Responsibility |
|-------------|-------|----------------|
| `IRecurringInstanceProjector` | ~200 | Project recurring transaction instances |
| `IRecurringTransferInstanceProjector` | ~200 | Project recurring transfer instances |
| `IAutoRealizeService` | ~150 | Auto-realize past-due items |
| `IDayDetailService` | 179 | Build day detail views |
| `ITransactionListService` | 251 | Build account transaction lists |

**RecurringTransferService Refactoring (605 → 304 lines):**

| New Service | Lines | Responsibility |
|-------------|-------|----------------|
| `RecurringTransferInstanceService` | 164 | Instance projection and modification |
| `RecurringTransferRealizationService` | 112 | Realize instances into transfers |

**RecurringTransactionService Refactoring (518 → 266 lines):**

| New Service | Lines | Responsibility |
|-------------|-------|----------------|
| `RecurringTransactionInstanceService` | 140 | Instance projection and modification |
| `RecurringTransactionRealizationService` | 70 | Realize instances into transactions |

**BudgetApiService Evaluation (499 lines):**
- Evaluated but NOT split
- Reasons: Thin HTTP façade, single dependency, acceptable size, Blazor DI simplicity
- Trigger: Revisit if caching/retry logic needed or lines exceed 600

**SOLID Principles Applied:**
- **SRP**: Each service has ONE primary responsibility
- **OCP**: New projector implementations can be added without modification
- **LSP**: All implementations fulfill contracts completely
- **ISP**: Focused interfaces created (e.g., `IRecurringInstanceProjector`)
- **DIP**: Application depends on Domain interfaces, Infrastructure provides implementations

---

## Files Created/Modified Summary

### Domain Layer
- `AppSettings.cs` - Application settings entity
- `IAppSettingsRepository.cs` - Settings repository interface
- `IRecurringInstanceProjector.cs` - Recurring projection interface
- `IRecurringTransferInstanceProjector.cs` - Transfer projection interface
- `IAutoRealizeService.cs` - Auto-realize interface
- `BillInfo.cs`, `PaycheckAllocation.cs` - Allocation value objects
- `AllocationWarningType.cs` - Warning enum

### Contracts Layer
- `AppSettingsDto.cs`, `AppSettingsUpdateDto.cs`
- `DailyBalanceSummaryDto.cs`
- `PaycheckAllocationDto.cs`, `PaycheckAllocationSummaryDto.cs`

### Application Layer
- `RecurringInstanceProjector.cs`
- `RecurringTransferInstanceProjector.cs`
- `AutoRealizeService.cs`
- `DayDetailService.cs`
- `TransactionListService.cs`
- `RecurringTransactionInstanceService.cs`
- `RecurringTransactionRealizationService.cs`
- `RecurringTransferInstanceService.cs`
- `RecurringTransferRealizationService.cs`
- `PaycheckAllocationService.cs`

### Infrastructure Layer
- `AppSettingsConfiguration.cs` - EF Core configuration
- `AppSettingsRepository.cs` - Repository implementation
- Migration for AppSettings table

### Client Layer
- Complete design system CSS files
- Settings page
- Past-due alert components
- Enhanced calendar and transaction displays
- All inline styles removed from components

---

## Related Documents

- [001-010-foundation-features.md](001-010-foundation-features.md) - Previous feature batch
- [copilot-instructions.md](../../.github/copilot-instructions.md) - Engineering guidelines
- [021-budget-categories-goals.md](../021-budget-categories-goals.md) - Next feature
