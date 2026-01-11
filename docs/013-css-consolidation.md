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
| MoneyDisplay | ~~MoneyDisplay.razor.css~~ | ✅ Deleted (Phase 10) |
| TransactionTable | ~~TransactionTable.razor.css~~ | ✅ Deleted (Phase 10) |
| AccountForm | ~~AccountForm.razor.css~~ | ✅ Deleted (Phase 11) |
| EditInstanceDialog | ~~EditInstanceDialog.razor.css~~ | ✅ Deleted (Phase 11) |
| EditRecurringForm | ~~EditRecurringForm.razor.css~~ | ✅ Deleted (Phase 11) |
| RecurringTransactionForm | ~~RecurringTransactionForm.razor.css~~ | ✅ Deleted (Phase 11) |
| TransactionForm | ~~TransactionForm.razor.css~~ | ✅ Deleted (Phase 11) |
| TransferDialog | ~~TransferDialog.razor.css~~ | ✅ Deleted (Phase 11) |
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
**Status:** ✅ Complete

Migrated Display component scoped CSS files to design system.

**Tasks:**
- [x] Audit scoped CSS (MoneyDisplay: 13 lines, TransactionTable: 123 lines)
- [x] Add money display utilities to `common.css` (`.money-display-*`)
- [x] Extend `tables.css` with transaction-specific row styles
- [x] Update TransactionTable.razor to use design system classes
- [x] Update MoneyDisplay.razor to use design system classes
- [x] Delete both scoped CSS files
- [x] Verify themes (light, dark)

**Changes Made:**
- Extended `common.css` with money display utilities:
  - `.money-display`, `.money-display-positive`, `.money-display-negative`
  - `.money-display-inline`, `.money-display-lg`, `.money-display-sm`
- Extended `tables.css` with transaction table styles:
  - `.row-recurring` - Dashed purple border for scheduled recurring rows
  - `.row-transfer` - Dashed border for transfer rows
  - `.row-indicator-recurring`, `.row-indicator-transfer` - Left border indicators
  - `.badge-modified` - Orange badge for modified amounts
- Updated TransactionTable.razor:
  - Uses `.table .table-hover`, `.btn btn-sm btn-*`
  - Uses `.row-recurring`, `.row-transfer`
  - Uses `.money-display-*` for amounts
- Updated MoneyDisplay.razor:
  - Uses `.money-display .money-display-positive/negative`
- Deleted `MoneyDisplay.razor.css` (13 lines)
- Deleted `TransactionTable.razor.css` (123 lines)
- **Total: 136 lines of scoped CSS removed**

**Acceptance Criteria:**
- ✅ Money display uses design system utilities
- ✅ Table uses design system table styles
- ✅ Scoped CSS eliminated (both files deleted)
- ✅ Visual appearance unchanged
- ✅ Theme support verified (light, dark)

---

### Phase 11: Form Components Migration
**Status:** ✅ Complete

Migrated Form component scoped CSS files to design system.

**Tasks:**
- [x] Audit each form's scoped CSS (AccountForm: 117, EditInstanceDialog: 133, EditRecurringForm: 102, RecurringTransactionForm: 96, TransactionForm: 107, TransferDialog: 120 = 675 total lines)
- [x] Extend `forms.css` with fieldset, info box, error box styles
- [x] Update AccountForm.razor to use `.form-label`, `.form-control`, `.input-group`, `.form-fieldset`, `.form-actions-right`
- [x] Update TransactionForm.razor to use `.form-label`, `.form-control`, `.form-text`, `.form-error-box`
- [x] Update RecurringTransactionForm.razor to use design system classes
- [x] Update EditRecurringForm.razor to use design system classes
- [x] Update EditInstanceDialog.razor to use `.form-info-box-*`, design system classes
- [x] Update TransferDialog.razor to use `.form-info-box`, design system classes
- [x] Delete all 6 scoped CSS files
- [x] Verify forms visually

**Changes Made:**
- Extended `forms.css` with:
  - `.form-fieldset` - Styled fieldset with background
  - `.form-info-box`, `.form-info-box-label`, `.form-info-box-title`, `.form-info-box-subtitle` - Info boxes for forms
  - `.form-error-box`, `.form-error-box-icon`, `.form-error-box-text` - Error message boxes
  - `.form-group-sm`, `.form-group-md`, `.form-group-lg` - Width variants
- Updated all 6 Form components:
  - Replaced inline labels with `.form-label`
  - Replaced inputs/selects with `.form-control`
  - Replaced `.help-text`/`.form-hint` with `.form-text`
  - Replaced `.form-error` with `.form-error-box`
  - Replaced `.btn-primary`/`.btn-secondary` with `.btn btn-primary`/`.btn btn-secondary`
  - Replaced `.form-group-small` with `.form-group-sm`
  - Added `.form-actions-right` for button alignment
- Deleted `AccountForm.razor.css` (117 lines)
- Deleted `EditInstanceDialog.razor.css` (133 lines)
- Deleted `EditRecurringForm.razor.css` (102 lines)
- Deleted `RecurringTransactionForm.razor.css` (96 lines)
- Deleted `TransactionForm.razor.css` (107 lines)
- Deleted `TransferDialog.razor.css` (120 lines)
- **Total: 675 lines of scoped CSS removed**

**Acceptance Criteria:**
- ✅ Form patterns consolidated in design system
- ✅ Scoped CSS eliminated (all 6 files deleted)
- ✅ Visual appearance unchanged
- ✅ All forms working correctly

---

### Phase 12: Final Cleanup & Documentation
**Status:** ✅ Complete

Final review, cleanup, and documentation.

**Tasks:**
- [x] Remove any remaining inline styles discovered
- [x] Delete empty or unnecessary .razor.css files
- [x] Verify which scoped CSS files remain and why
- [x] Run full build and visual verification
- [x] Update documentation

**Remaining Scoped CSS Files (2):**
1. `MainLayout.razor.css` - Required for core app layout shell
2. `NavMenu.razor.css` - Required for navigation menu component

These files contain essential layout-specific styles that cannot be generalized to the design system.

**Total CSS Removed:**
| Phase | Component Type | Files | Lines Removed |
|-------|---------------|-------|---------------|
| Phase 7 | Pages (Transfers) | 1 | 192 |
| Phase 8 | Calendar Components | 3 | 380 |
| Phase 9 | Common Components | 5 | 417 |
| Phase 10 | Display Components | 2 | 136 |
| Phase 11 | Form Components | 6 | 675 |
| **Total** | | **17** | **~1,800** |

**Design System Files Created/Extended:**
- `calendar.css` (~330 lines) - Calendar-specific styles
- `common.css` (~155 lines) - Loading, page header, money display
- Extended `alerts.css` (~100 lines) - Error alert styles
- Extended `modals.css` - Animation and centering
- Extended `tables.css` (~80 lines) - Transaction row styles
- Extended `forms.css` (~100 lines) - Fieldset, info box, error box

**Acceptance Criteria:**
- ✅ No inline `<style>` blocks in any .razor file
- ✅ Scoped CSS files only contain truly component-specific styles (2 files remain)
- ✅ Documentation complete
- ✅ All themes render correctly on all pages

---

## Success Metrics

| Metric | Before | Target | Final |
|--------|--------|--------|-------|
| Files with inline `<style>` | 6+ pages | 0 | ✅ 0 |
| Scoped .razor.css files | 18 | ≤5 | ✅ 2 |
| CSS custom properties usage | Partial | 100% for colors | ✅ 100% |
| Theme consistency | Partial | All pages support all themes | ✅ All themes work |

**Migration Complete!** 🎉

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
