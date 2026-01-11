# Feature: CSS Consolidation & Design System Migration

## Overview

Systematically refactor all pages and components to eliminate inline `<style>` blocks and minimize scoped `.razor.css` files by leveraging the design system CSS classes established in Feature 011. The goal is a consistent, maintainable codebase where component styling relies on shared design tokens and utility classes.

## Prerequisites

- ✅ Feature 011 (Design System Overhaul) complete
- Design system files in place: `tokens.css`, `reset.css`, `base.css`, `layout.css`, `components/*.css`, `utilities.css`
- Theme system working (Light, Dark, VS Code Dark)

## Current State Analysis

### Files with Inline `<style>` Blocks

**Pages (6 files):**
| File | Lines of Inline CSS | Priority |
|------|---------------------|----------|
| [Calendar.razor](../src/BudgetExperiment.Client/Pages/Calendar.razor) | ~60 lines | High |
| [Accounts.razor](../src/BudgetExperiment.Client/Pages/Accounts.razor) | ~150 lines | High |
| [AccountTransactions.razor](../src/BudgetExperiment.Client/Pages/AccountTransactions.razor) | ~TBD | High |
| [Recurring.razor](../src/BudgetExperiment.Client/Pages/Recurring.razor) | ~200 lines | High |
| [RecurringTransfers.razor](../src/BudgetExperiment.Client/Pages/RecurringTransfers.razor) | ~TBD | Medium |
| [Transfers.razor](../src/BudgetExperiment.Client/Pages/Transfers.razor) | Has .razor.css | Low |

**Components with Scoped CSS (.razor.css):**
| Component | File | Priority |
|-----------|------|----------|
| CalendarDay | ~~CalendarDay.razor.css~~ | ✅ Deleted (Phase 8) |
| CalendarGrid | ~~CalendarGrid.razor.css~~ | ✅ Deleted (Phase 8) |
| DayDetail | ~~DayDetail.razor.css~~ | ✅ Deleted (Phase 8) |
| ConfirmDialog | ~~ConfirmDialog.razor.css~~ | ✅ Deleted (Phase 9) |
| ErrorAlert | ~~ErrorAlert.razor.css~~ | ✅ Deleted (Phase 9) |
| LoadingSpinner | ~~LoadingSpinner.razor.css~~ | ✅ Deleted (Phase 9) |
| Modal | ~~Modal.razor.css~~ | ✅ Deleted (Phase 9) |
| PageHeader | ~~PageHeader.razor.css~~ | ✅ Deleted (Phase 9) |
| MoneyDisplay | [MoneyDisplay.razor.css](../src/BudgetExperiment.Client/Components/Display/MoneyDisplay.razor.css) | Low |
| TransactionTable | [TransactionTable.razor.css](../src/BudgetExperiment.Client/Components/Display/TransactionTable.razor.css) | Medium |
| AccountForm | [AccountForm.razor.css](../src/BudgetExperiment.Client/Components/Forms/AccountForm.razor.css) | Low |
| EditInstanceDialog | [EditInstanceDialog.razor.css](../src/BudgetExperiment.Client/Components/Forms/EditInstanceDialog.razor.css) | Low |
| EditRecurringForm | [EditRecurringForm.razor.css](../src/BudgetExperiment.Client/Components/Forms/EditRecurringForm.razor.css) | Low |
| RecurringTransactionForm | [RecurringTransactionForm.razor.css](../src/BudgetExperiment.Client/Components/Forms/RecurringTransactionForm.razor.css) | Low |
| TransactionForm | [TransactionForm.razor.css](../src/BudgetExperiment.Client/Components/Forms/TransactionForm.razor.css) | Low |
| TransferDialog | [TransferDialog.razor.css](../src/BudgetExperiment.Client/Components/Forms/TransferDialog.razor.css) | Low |
| NavMenu | [NavMenu.razor.css](../src/BudgetExperiment.Client/Components/Navigation/NavMenu.razor.css) | Done (011) |
| MainLayout | [MainLayout.razor.css](../src/BudgetExperiment.Client/Layout/MainLayout.razor.css) | Done (011) |

---

## Goals

1. **Remove all inline `<style>` blocks** from `.razor` files
2. **Consolidate scoped CSS** into design system where patterns repeat
3. **Use semantic class names** that map to design system components
4. **Ensure theme consistency** - all colors via CSS custom properties
5. **Maintain responsive behavior** established in design system

---

## Implementation Phases

### Phase 1: Audit & Extend Design System
**Status:** ✅ Complete

Audit existing inline styles to identify missing design system classes. Add any needed utility classes or component styles before migration.

**Tasks:**
- [x] Audit all inline styles and catalog unique patterns
- [x] Identify gaps in design system (missing utilities, component variants)
- [x] Add missing styles to appropriate design system files
- [x] Document new classes in component README

**New Design System Files Added:**
- `badges.css` - Badge components for account, frequency, category, status
- `empty-state.css` - Empty state patterns for pages with no data
- `detail-list.css` - Detail list and balance display components
- `filters.css` - Filter section and date filter components
- `recurring-card.css` - Recurring transaction/transfer card component
- Extended `layout.css` with `.page-container`, `.page-container-wide`, `.page-container-narrow`

**Acceptance Criteria:**
- Design system has all necessary classes to replace inline styles
- No new patterns need to be invented during migration

---

### Phase 2: Calendar Page Migration
**Status:** ✅ Complete

Migrate [Calendar.razor](../src/BudgetExperiment.Client/Pages/Calendar.razor) inline styles to design system classes.

**Tasks:**
- [x] Replace `.calendar-container` with `.page-container`
- [x] Replace `.calendar-header` with `.split` utility
- [x] Replace `.nav-button` with `btn btn-primary`
- [x] Replace `.account-filter` with form utilities
- [x] Remove `<style>` block entirely

**Acceptance Criteria:**
- ✅ Zero inline styles in Calendar.razor
- Visual appearance unchanged
- All three themes work correctly

---

### Phase 3: Accounts Page Migration
**Status:** ✅ Complete

Migrate [Accounts.razor](../src/BudgetExperiment.Client/Pages/Accounts.razor) inline styles to design system classes.

**Tasks:**
- [x] Replace `.accounts-container` with `.page-container`
- [x] Use `.card`, `.card-body`, `.card-footer` from design system
- [x] Use `btn btn-success`, `btn btn-primary`, `btn btn-danger` variants
- [x] Use `.empty-message` from design system
- [x] Use `.alert alert-warning` for edit warning
- [x] Remove `<style>` block entirely (~150 lines removed)

**Acceptance Criteria:**
- ✅ Zero inline styles in Accounts.razor
- Visual appearance unchanged
- All three themes work correctly

---

### Phase 4: AccountTransactions Page Migration
**Status:** ✅ Complete

Migrate [AccountTransactions.razor](../src/BudgetExperiment.Client/Pages/AccountTransactions.razor) inline styles.

**Tasks:**
- [x] Replace `.transactions-container` with `.page-container-wide`
- [x] Use `.filter-section`, `.date-filter`, `.summary-stats` from design system
- [x] Use `.balance-banner`, `.balance-display` from design system
- [x] Remove `<style>` block entirely

**Acceptance Criteria:**
- ✅ Zero inline styles
- Visual appearance unchanged

---

### Phase 5: Recurring Page Migration
**Status:** ✅ Complete

Migrate [Recurring.razor](../src/BudgetExperiment.Client/Pages/Recurring.razor) inline styles (~200 lines).

**Tasks:**
- [x] Replace `.recurring-container` with `.page-container`
- [x] Use `.empty-state`, `.empty-state-icon`, `.empty-state-title`, `.empty-state-description`
- [x] Use `.recurring-list`, `.recurring-card`, `.recurring-card-header`, `.recurring-card-info`
- [x] Use `.badge-group`, `.badge badge-account`, `.badge badge-frequency`, `.badge badge-category`
- [x] Use `.detail-list`, `.detail-item`, `.detail-label`, `.detail-value`
- [x] Use `.badge-status`, `.badge-status-active`, `.badge-status-paused`
- [x] Use `.recurring-card-actions`, `.btn-action`, `.btn-action-resume`, `.btn-action-delete`
- [x] Remove `<style>` block entirely (~200 lines removed)

**Acceptance Criteria:**
- ✅ Zero inline styles in Recurring.razor
- Badge and status components reusable
- Visual appearance unchanged

---

### Phase 6: RecurringTransfers Page Migration
**Status:** ✅ Complete

Migrate [RecurringTransfers.razor](../src/BudgetExperiment.Client/Pages/RecurringTransfers.razor) inline styles.

**Tasks:**
- [x] Replace with same design system classes as Recurring page
- [x] Use `.badge-transfer-flow`, `.badge-transfer-from`, `.badge-transfer-to` for transfer flow
- [x] Remove `<style>` block entirely (~200 lines removed)

**Acceptance Criteria:**
- ✅ Zero inline styles
- Visual appearance unchanged

---

### Phase 7: Transfers Page Review
**Status:** ✅ Complete

Review [Transfers.razor](../src/BudgetExperiment.Client/Pages/Transfers.razor) - scoped CSS file removed.

**Tasks:**
- [x] Audit scoped CSS file (~192 lines)
- [x] Migrate styles to design system classes
- [x] Delete `Transfers.razor.css` file entirely
- [x] Verify themes

**Changes Made:**
- Replaced `.transfers-container` with `.page-container-wide`
- Replaced `.filters-row` with `.filter-section` and `.filter-group`
- Replaced `.filter-group` inputs with `.date-filter` and `.form-control`
- Replaced `.btn-clear` with `.btn btn-secondary`
- Replaced `.add-button` with `.btn btn-success`
- Replaced `.transfers-table` with `.card` wrapper and `.table table-hover`
- Replaced `.amount-col` with `.text-right`
- Replaced `.actions-col` with `.text-center`
- Replaced `.btn-edit`, `.btn-delete` with `.btn btn-sm btn-primary`, `.btn btn-sm btn-danger`
- Replaced `.empty-message` with `.empty-state empty-state-inline`
- Deleted `Transfers.razor.css` (192 lines removed)

**Acceptance Criteria:**
- ✅ Scoped CSS eliminated (file deleted)
- ✅ Visual appearance unchanged

---

### Phase 8: Calendar Components Migration
**Status:** ✅ Complete

Migrated Calendar component scoped CSS files to design system.

**Tasks:**
- [x] Audit each component's scoped CSS (CalendarDay: 124 lines, CalendarGrid: 35 lines, DayDetail: 221 lines)
- [x] Create `calendar.css` design system component (330 lines)
- [x] Move all calendar styles to design system
- [x] Delete all scoped CSS files
- [x] Update DayDetail.razor to use `.btn btn-success` for add button
- [x] Verify themes

**Changes Made:**
- Created `design-system/components/calendar.css` with all calendar-specific styles
- Added import to `app.css`
- Consolidated `.calendar-grid`, `.calendar-day-header` styles
- Consolidated `.calendar-day` variants (today, selected, other-month, has-recurring)
- Consolidated `.day-detail`, `.recurring-section`, `.transaction-section` styles
- Consolidated `.day-summary`, `.summary-row` styles
- All colors now use CSS variables for theme support
- Deleted `CalendarDay.razor.css` (124 lines)
- Deleted `CalendarGrid.razor.css` (35 lines)
- Deleted `DayDetail.razor.css` (221 lines)
- **Total: 380 lines of scoped CSS removed**

**Acceptance Criteria:**
- ✅ Scoped CSS eliminated (all 3 files deleted)
- ✅ Reusable patterns in design system (`calendar.css`)
- ✅ Visual appearance unchanged

---

### Phase 9: Common Components Migration
**Status:** ✅ Complete

Migrated Common component scoped CSS files to design system.

**Tasks:**
- [x] Audit each component's scoped CSS (ConfirmDialog: 57 lines, ErrorAlert: 109 lines, LoadingSpinner: 55 lines, Modal: 139 lines, PageHeader: 57 lines)
- [x] Create `common.css` design system component with loading spinner and page header styles
- [x] Extend `alerts.css` with error alert styles (error-alert-*, retry button, dismiss button)
- [x] Update `modals.css` for modal backdrop and dialog with animations
- [x] Update Modal.razor to use `.modal-backdrop`, `.modal-dialog` design system classes
- [x] Update ConfirmDialog.razor to use `.btn btn-danger`, `.btn btn-secondary`
- [x] Update ErrorAlert.razor to use `.error-alert-*` design system classes
- [x] Update LoadingSpinner.razor to use `.loading-container-fullpage`
- [x] Update PageHeader.razor to use `.page-header-*` design system classes
- [x] Delete all 5 scoped CSS files
- [x] Verify themes (light, dark, VS Code dark)

**Changes Made:**
- Created `design-system/components/common.css` (~120 lines):
  - Loading spinner (`.loading-container`, `.spinner`, size variants, animation)
  - Page header (`.page-header`, `.page-header-content`, `.page-header-title`, etc.)
  - Confirm dialog content (`.confirm-content`, `.confirm-message`, `.confirm-actions`)
- Extended `alerts.css` with error alert styles:
  - `.error-alert`, `.error-alert-content`, `.error-alert-icon`, `.error-alert-text`
  - `.error-alert-message`, `.error-alert-details`, `.error-alert-actions`
  - `.error-alert-retry`, `.error-alert-dismiss` buttons
  - Responsive styles
- Updated `modals.css`:
  - `.modal-backdrop` now includes flex centering and fade-in animation
  - `.modal-dialog` now includes slide-in animation
- Added import for `common.css` to `app.css`
- Deleted `ConfirmDialog.razor.css` (57 lines)
- Deleted `ErrorAlert.razor.css` (109 lines)
- Deleted `LoadingSpinner.razor.css` (55 lines)
- Deleted `Modal.razor.css` (139 lines)
- Deleted `PageHeader.razor.css` (57 lines)
- **Total: 417 lines of scoped CSS removed**

**Acceptance Criteria:**
- ✅ Scoped CSS eliminated (all 5 files deleted)
- ✅ Common patterns consolidated in design system
- ✅ Components use design system classes
- ✅ Theme support verified (light, dark, VS Code dark)
- ✅ Visual appearance unchanged

---

### Phase 10: Display Components Migration
**Status:** ⬜ Not Started

Migrate Display component scoped CSS files:
- [MoneyDisplay.razor.css](../src/BudgetExperiment.Client/Components/Display/MoneyDisplay.razor.css)
- [TransactionTable.razor.css](../src/BudgetExperiment.Client/Components/Display/TransactionTable.razor.css)

**Tasks:**
- [ ] Audit scoped CSS
- [ ] Add money display utilities to design system
- [ ] Ensure table styles are in `tables.css`
- [ ] Verify themes

**Acceptance Criteria:**
- Money display uses design system utilities
- Table uses design system table styles
- Visual appearance unchanged

---

### Phase 11: Form Components Migration
**Status:** ⬜ Not Started

Migrate Form component scoped CSS files:
- [AccountForm.razor.css](../src/BudgetExperiment.Client/Components/Forms/AccountForm.razor.css)
- [EditInstanceDialog.razor.css](../src/BudgetExperiment.Client/Components/Forms/EditInstanceDialog.razor.css)
- [EditRecurringForm.razor.css](../src/BudgetExperiment.Client/Components/Forms/EditRecurringForm.razor.css)
- [RecurringTransactionForm.razor.css](../src/BudgetExperiment.Client/Components/Forms/RecurringTransactionForm.razor.css)
- [TransactionForm.razor.css](../src/BudgetExperiment.Client/Components/Forms/TransactionForm.razor.css)
- [TransferDialog.razor.css](../src/BudgetExperiment.Client/Components/Forms/TransferDialog.razor.css)

**Tasks:**
- [ ] Audit each form's scoped CSS
- [ ] Ensure form styles consolidated in `forms.css`
- [ ] Keep only truly unique component styles
- [ ] Verify themes

**Acceptance Criteria:**
- Form patterns consolidated in design system
- Minimal scoped CSS remaining
- Visual appearance unchanged

---

### Phase 12: Final Cleanup & Documentation
**Status:** ⬜ Not Started

Final review, cleanup, and documentation.

**Tasks:**
- [ ] Remove any remaining inline styles discovered
- [ ] Delete empty or unnecessary .razor.css files
- [ ] Update [Components README](../src/BudgetExperiment.Client/Components/README.md) with CSS guidelines
- [ ] Document which scoped CSS files remain and why
- [ ] Run full visual regression test across all themes
- [ ] Update design system documentation

**Acceptance Criteria:**
- No inline `<style>` blocks in any .razor file
- Scoped CSS files only contain truly component-specific styles
- Documentation complete
- All themes render correctly on all pages

---

## Success Metrics

| Metric | Before | Target |
|--------|--------|--------|
| Files with inline `<style>` | 6+ pages | 0 |
| Scoped .razor.css files | 18 | ≤5 (component-specific only) |
| CSS custom properties usage | Partial | 100% for colors |
| Theme consistency | Partial | All pages support all themes |

---

## Technical Guidelines

### When to Use Design System Classes
- Page containers, spacing, margins
- Buttons, links, form controls
- Cards, panels, alerts
- Tables, lists
- Typography (headings, text sizes)
- Colors (always via CSS custom properties)

### When Scoped CSS is Acceptable
- Truly unique component layout not reusable elsewhere
- Complex animations specific to one component
- Third-party component overrides

### Migration Pattern

```razor
@* BEFORE: Inline style block *@
<div class="my-container">
    <button class="my-button">Click</button>
</div>

<style>
    .my-container { max-width: 900px; margin: 0 auto; padding: 20px; }
    .my-button { background: #28a745; color: white; }
</style>

@* AFTER: Design system classes *@
<div class="page-container">
    <button class="btn btn-success">Click</button>
</div>
```

---

## Related Documents

- [011-design-system.md](011-design-system.md) - Design System Overhaul (prerequisite)
- [Components README](../src/BudgetExperiment.Client/Components/README.md) - Component documentation
