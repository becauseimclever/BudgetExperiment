# Calendar, Theming & Mobile (041-050) - Consolidated Summary

**Consolidated:** 2026-02-08  
**Original Features:** 041 through 050 (including 044.1, 046.1)  
**Status:** All Completed (041 Cancelled — superseded by Feature 062)

---

## Overview

This document consolidates features 041–050, which focused on CI/CD optimization, AI UI consolidation, a comprehensive theming system with nine themes and theme-aware icons, UI component standardization, WCAG accessibility compliance, mobile-first touch experience, and calendar-centric budget editing, recurring transaction visibility, and integrated reporting/analytics.

---

## 041: Validate Starting Balance in Calendar (E2E)

**Status:** ❌ Cancelled — Superseded by Feature 062

This narrow single-scenario E2E test was consolidated into Feature 062 (Functional E2E Test Suite), where the starting balance validation scenario became US-062-002.

---

## 042: Optimize CI/CD Pipeline Speed

**Completed:** 2026-02-01

Reduced CI/CD pipeline execution time from ~20 minutes to under 5 minutes with a restructured 3-job parallel pipeline.

**Key Outcomes:**
- 1,623 tests running in CI with TRX reporting
- Separate `build-and-test` job with NuGet caching
- `Dockerfile.prebuilt` uses pre-compiled artifacts (no in-container build)
- Native ARM64 runners (`ubuntu-24.04-arm`) — eliminated QEMU emulation
- Matrix Docker builds: amd64 and arm64 in parallel
- `docker-merge` job creates multi-arch manifest
- Tag strategy: `preview` on main pushes, `latest` only on version tags
- Auto-cancel in-progress workflows on new pushes

**Pipeline Structure:**
```
build-and-test → docker-build (amd64) ─┐
                 docker-build (arm64) ─┤→ docker-merge → ghcr.io
```

---

## 043: Consolidate AI Features in UI

**Completed:** February 2026

Grouped all AI-related features under a single expandable "AI Tools" menu section with conditional visibility based on AI availability.

**Key Outcomes:**
- "AI Tools" expandable section in NavMenu (matching Reports/Accounts pattern)
- Smart Insights and Category Suggestions grouped as sub-items
- Three visual states: Hidden (AI disabled), Warning (enabled but Ollama unavailable), Normal (fully operational)
- `IAiAvailabilityService` with centralized status checking and caching
- AI Assistant button in header hidden when AI is disabled, warning badge when Ollama unavailable
- Tooltip explains connection issues for troubleshooting

---

## 044: UI Theme Rework and Theming

**Completed:** February 2026

Polished the theme system and added four new themes, bringing the total to nine.

**Key Outcomes:**
- Extracted `ThemeOption` to its own file (one-class-per-file compliance)
- Fixed `theme.js` meta theme-color to include all themes
- Created `docs/THEMING.md` — step-by-step guide for adding new themes
- Verified dropdown visibility across all themes

**9 Available Themes:**
| Theme | Aesthetic |
|-------|-----------|
| System | Follows OS preference |
| Light | Clean, bright default |
| Dark | Dark mode |
| VS Code Dark | VS Code-inspired |
| Monopoly | Board game parchment |
| Windows 95 | Classic gray, navy blue, 3D bevels |
| macOS | Apple aesthetic, subtle gradients |
| GeoCities | 90s web — neon, Comic Sans, high contrast |
| Crayon Box | Crayola-inspired bold primary colors |

---

## 044.1: Theme-Aware Navbar Icons

**Completed:** February 2026  
**Depends on:** Feature 044

Enabled navbar icons to change based on the selected theme for a more cohesive, immersive UI.

**Key Outcomes:**
- `ThemedIconRegistry` maps themes to custom icon sets
- `ThemeService.GetThemedIcon()` resolves icon name for current theme
- `Icon` component automatically switches icons on theme change
- Fallback to default icons when theme doesn't define custom icons
- Smooth transitions with no flicker during theme switches
- Theme-specific icon styles: pixelated for Win95, hand-drawn for Crayons, animated for GeoCities

---

## 045: UI Component Refactor and Library Preparation

**Completed:** February 2026

Established consistent component patterns and consolidated the design system for potential library extraction.

**Key Outcomes:**
- **New Tier 1 components** with full test coverage (87 bUnit tests):
  - `Button.razor` (20 tests) — variants: primary, secondary, success, danger, ghost; sizes: sm, md, lg
  - `Badge.razor` (13 tests)
  - `Card.razor` (11 tests)
  - `EmptyState.razor` (11 tests)
  - `FormField.razor` (14 tests) — label, validation, help text, required indicator
- **Standardized parameter naming**: `IsVisible`, `IsDisabled`, `IsLoading`, `Size` enum, `On{Event}` callbacks
- **Component tier classification**: Tier 1 (atomic, library-ready), Tier 2 (composite), Tier 3 (domain-specific)
- All 6 existing Tier 1/2 components verified as already standards-compliant
- `docs/COMPONENT-STANDARDS.md` created with naming conventions and patterns
- Updated `Components/README.md` as component catalog

---

## 046: Accessible Theme and WCAG 2.0 UI Tests

**Completed:** February 2026  
**Dependencies:** Features 044, 045

Implemented a high-contrast accessible theme and automated WCAG compliance testing.

**Key Outcomes:**
- High-contrast `accessible` theme with WCAG AAA color ratios (7:1 for text)
- Auto-detection of `forced-colors`, `prefers-contrast: more`, and Windows High Contrast Mode
- Skip-to-main-content link implemented
- Proper ARIA landmarks, roles, labels across all components
- Focus trapping in modals/dialogs with visible focus indicators (3px+ solid outline)
- Keyboard navigation verified across all interactive elements
- axe-core integrated with Playwright E2E tests (`Deque.AxeCore.Playwright`)
- WCAG 2.0 AA compliance tests scan key pages in CI
- `docs/ACCESSIBILITY.md` created with accessibility guidelines

---

## 046.1: Calendar-Centric Navigation and Feature Audit

**Completed:** February 2026 (Audit)

Audited all Budget Experiment features for calendar-centric consistency and identified gaps.

**Key Outcomes:**
- Confirmed calendar is primary navigation entry point at `/`
- Quick Add and transaction entry are date-aware
- Recurring transactions visible in calendar with confirm/edit/skip actions
- Month navigation is seamless with URL routing (`/{Year}/{Month}`)

**Gaps Identified → New Features Created:**
| Gap | Feature |
|-----|---------|
| Budget goals not editable from calendar | Feature 048 |
| Recurring items need more visual distinction | Feature 049 |
| Reports not calendar-filtered | Feature 050 |
| AI suggestions lack calendar context | Feature 051 |

---

## 047: Mobile Experience — Calendar, Quick Add, AI Assistant

**Completed:** February 2026 (v3.13.0)

Delivered a first-class mobile experience with touch navigation, floating action button, and bottom-sheet modals.

**Key Outcomes:**
- **Swipe navigation** for calendar month navigation (50px threshold, passive listeners)
- **Touch-optimized calendar** with 48x48px minimum touch targets (WCAG 2.5.5)
- **Week view toggle** for mobile only (< 768px) with larger cells
- **Speed dial FAB** (bottom-right, 56px) with Quick Add (+) and AI Assistant options
- **Bottom-sheet modals** for transaction entry and AI chat (slide-up, 60-80% viewport, drag handle, swipe-to-dismiss)
- **Optimized form inputs** with 48px minimum height per field
- **Safe area support** for notched devices (`env(safe-area-inset-*)`)
- **AI-assisted Quick Add**: natural language parsing ("Coffee at Starbucks $5" → filled fields)
- All mobile UI meets WCAG AA accessibility standards

---

## 048: Calendar-First Budget Editing

**Completed:** February 2026

Enabled viewing and editing monthly budget goals directly from the calendar view.

**Key Outcomes:**
- **CalendarBudgetPanel** — collapsible budget summary panel on calendar page:
  - Overall monthly progress (total budgeted, spent, remaining, % used)
  - Status counts: on track, warning, over budget
  - Per-category progress bars sorted by status
- **Inline budget editing** via `BudgetGoalModal` triggered from panel
- **Create/edit/delete** budget goals without leaving calendar
- **Copy from previous month** — one-click budget setup for new months
- Mobile-friendly: collapsed by default on mobile, expanded on desktop
- Panel state persists via localStorage

---

## 049: Calendar Recurring Transactions Visibility

**Completed:** 2026-02-01

Enhanced recurring transaction visibility in the calendar with visual distinction and interactive actions.

**Key Outcomes:**
- `CalendarDay.razor` displays recurring indicator with refresh icon and projected total
- `DayDetail.razor` shows dedicated "Scheduled Recurring" section
- Confirm, edit, and skip actions for each recurring item
- CSS class `has-recurring` applied to calendar days with recurring items
- Icons and color-coded amounts distinguish recurring from actual transactions

---

## 050: Calendar-Driven Reports & Analytics

**Completed:** February 2026 (All 6 Phases)

Integrated reports and analytics into the calendar experience with date-range filtering and new report types.

**Key Outcomes:**
- **Calendar ↔ Reports integration**: "View Reports" action on calendar navigates to matching month
- **Day analytics**: daily summary section in DayDetail (spent, income, net, top categories)
- **Week summary**: selectable week rows with total, daily average, category breakdown
- **Date range filtering**: custom start/end dates, quick presets (This Month, Last 7 Days, etc.)
- **Calendar picker**: mini calendar overlay for visual date selection on reports
- **Month Insights panel**: collapsible on calendar page with Total Income/Spending/Net, mini donut chart, trend indicator vs. previous month
- **Monthly Trends report** at `/reports/trends` — bar/line charts over 6-12 months with category filter
- **Budget vs. Actual report** at `/reports/budget-comparison` — reuses existing `BudgetSummaryDto`/`BudgetProgressDto` (no new DTOs needed)
- Existing `ReportService` refactored to accept arbitrary date ranges
- `DonutChart` reused in insights panel; new chart components for trends
- All report pages subscribe to `ScopeService.ScopeChanged` for scope filtering

**Reused Existing Assets:**
- `BudgetSummaryDto` + `BudgetProgressDto` → Budget vs. Actual report (no new DTOs)
- `CategorySpendingDto` → date-range category reports
- `ITransactionRepository.GetByDateRangeAsync` → no new repository methods needed
- `CalendarBudgetPanel` pattern → `CalendarInsightsPanel` follows same collapsible pattern

---

## Cross-Cutting Themes

### Testing
- 1,623 tests running in CI (Feature 042)
- 87 new bUnit component tests (Feature 045)
- axe-core WCAG compliance tests in CI (Feature 046)
- 8 ReportService unit tests + 5 integration tests + 7 DonutChart tests (Feature 050)

### Architecture Patterns Established
- CSS variable-based theming with modular theme files
- Component tier system (Tier 1/2/3) with standardized parameters
- Centralized service pattern (`IAiAvailabilityService`, `ThemeService`)
- Collapsible panel pattern with localStorage persistence
- Bottom-sheet modal for mobile interactions

---

*Consolidated from original feature documents 041–050 on 2026-02-08.*
