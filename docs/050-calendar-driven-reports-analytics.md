# Feature 050: Calendar-Driven Reports & Analytics
> **Status:** 🔄 In Progress (Phase 2 Complete)  
> **Priority:** Medium  
> **Estimated Effort:** Large (6–9 sprints across all phases)  
> **Dependencies:** Feature 048 (Calendar Budget Editing — Complete), Reports Infrastructure (Partial — see below)

## Overview

Transform reports and analytics into a calendar-integrated experience where users can navigate and filter financial data by selecting dates, weeks, or months from the familiar calendar interface. Reports become an extension of the calendar-centric workflow, not a separate destination.

This feature bridges the gap between the calendar view (where users track daily transactions) and analytics (where users understand spending patterns), creating a seamless flow from "what happened" to "what does it mean."

## Problem Statement

Currently, the Reports section exists as a separate navigation destination disconnected from the calendar. Users must mentally translate between the calendar view (organized by date) and reports (organized by month dropdown). There's no way to click a date or week on the calendar and see focused analytics for that period.

### Current State (Verified 2026-02-07)

**What Exists:**
- **Reports Index** ([ReportsIndex.razor](../src/BudgetExperiment.Client/Pages/Reports/ReportsIndex.razor) at `/reports`): Landing page with 4 cards. Only "Category Spending" is a live link; "Monthly Trends", "Budget vs. Actual", and "Year in Review" are placeholder cards with a "Coming Soon" badge.
- **Monthly Categories Report** ([MonthlyCategoriesReport.razor](../src/BudgetExperiment.Client/Pages/Reports/MonthlyCategoriesReport.razor) at `/reports/categories`): Donut chart with interactive segments + category list + summary card (Total Spending, Total Income, Net, # Categories). Month navigation via Previous/Next buttons. Clicking a category navigates to `/accounts?categoryId=&startDate=&endDate=`. Subscribes to `ScopeService.ScopeChanged`.
- **Calendar Page** ([Calendar.razor](../src/BudgetExperiment.Client/Pages/Calendar.razor) at `/` and `/{Year}/{Month}`): 844 lines. Month/week view toggle with swipe, account filter, `PastDueAlert` + `PastDueReviewModal`, `BudgetAlert` + `CalendarBudgetPanel` (collapsible budget summary with per-category progress bars, on-track/warning/over-budget counts, copy-from-previous), `BudgetGoalModal`, `CalendarGrid`/`CalendarWeekView`, `DayDetail` panel, `TransactionForm`, `EditInstanceDialog`, `ConfirmDialog`.
- **Day Detail** ([DayDetail.razor](../src/BudgetExperiment.Client/Components/Calendar/DayDetail.razor)): Splits items into Scheduled Recurring and Actual Transactions sections. Day summary at bottom: Actual total, Projected total, Combined total, Item count. Purely transactional — no analytics or category breakdown.
- **Chart Components**: Only the DonutChart family exists (`DonutChart.razor`, `DonutChartSegment.razor`, `ChartLegend.razor`, `DonutSegmentData.cs`). No BarChart, LineChart, or SparkLine components.
- **API**: Single endpoint — `GET /api/v1/reports/categories/monthly?year={year}&month={month}` → `MonthlyCategoryReportDto`. Controller has `[Authorize]`.
- **Application Layer**: `IReportService` has a single method `GetMonthlyCategoryReportAsync`. `ReportService` fetches transactions via `GetByDateRangeAsync`, filters transfers, groups by category in-memory, hardcodes `"USD"` currency.
- **Budget Goals** (Feature 048): Fully implemented across all layers — `BudgetGoal` entity, `BudgetGoalService`, `BudgetProgressService` (with `GetMonthlySummaryAsync` → `BudgetSummaryDto`), `CalendarBudgetPanel`. `BudgetProgressDto` already contains per-category: TargetAmount, SpentAmount, RemainingAmount, PercentUsed, Status ("OnTrack"/"Warning"/"OverBudget"/"NoBudgetSet").
- **Transaction Repository**: `ITransactionRepository.GetByDateRangeAsync(DateOnly start, DateOnly end, Guid? accountId)` already supports flexible date range queries. `GetSpendingByCategoryAsync` is single-category single-month only.
- **Scope System**: `X-Budget-Scope` header (Personal/Shared/All) is set by client and parsed by `BudgetScopeMiddleware`. Scope filtering likely applied at the repository/DbContext level. Current `ReportService` does NOT explicitly reference `IUserContext`.

**Existing DTOs with Reuse Potential:**
| DTO | Location | Overlap with 050 |
|-----|----------|-------------------|
| `BudgetSummaryDto` | `Contracts/Dtos/BudgetProgressDto.cs` | Nearly identical shape to proposed `BudgetComparisonReportDto` — has per-category target/spent/remaining/status + overall summary |
| `BudgetProgressDto` | Same file | Per-category budget vs. actual with status — could be reused directly in Budget vs. Actual report |
| `DayDetailSummaryDto` | `Contracts/Dtos/DayDetailSummaryDto.cs` | Contains TotalActual, TotalProjected, CombinedTotal, ItemCount — lacks income/expense split and category breakdown |
| `MonthlyCategoryReportDto` | `Contracts/Dtos/ReportDtos.cs` | Good base for date-range variant |
| `CategorySpendingDto` | Same file | Reusable as-is in date-range category reports |

**Existing Tests:**
- 8 unit tests for `ReportService` (category aggregation, percentages, transfers, date ranges, uncategorized)
- 5 integration tests for `ReportsController` (validation, data correctness)
- 7 bUnit tests for `DonutChart`
- E2E: `ReportsPage_ShouldLoad`, `CategorySpendingReportPage_ShouldLoad`, accessibility audit

**Current Gaps:**
1. Reports use their own month navigation (separate from calendar)
2. No way to view analytics for a specific day or week
3. No way to drill down from calendar to related reports
4. Calendar and reports don't share date context
5. No quick insights visible in calendar view itself
6. No date range reports (custom start/end dates)
7. No trends or comparisons visible
8. No BarChart/LineChart components — only DonutChart exists
9. Currency hardcoded to "USD" in ReportService (not derived from transactions)
10. `BudgetProgressService.GetMonthlySummaryAsync` has N+1 query pattern (sequential per-category DB calls)

### Target State

- **Calendar ↔ Reports Integration**: Click a day, week, or month on calendar to view related analytics
- **Date Range Picker**: Reports support custom date ranges, not just full months
- **Quick Insights Panel**: Calendar page shows mini analytics for selected period
- **Unified Date Context**: Navigating calendar updates report date range and vice versa
- **New Reports**: Monthly Trends, Budget vs. Actual implemented (replacing "Coming Soon" placeholders)
- **Deep Links**: URL parameters allow bookmarking and sharing specific report views
- **Mobile-First**: Touch-friendly charts and date selection (leverages Feature 047 components)

### Reuse & Overlap Analysis

Several existing artifacts can be leveraged to reduce scope:

| Existing Asset | Reuse Strategy |
|----------------|----------------|
| `BudgetSummaryDto` + `BudgetProgressDto` | **Budget vs. Actual report can reuse these directly.** `BudgetSummaryDto` already contains per-category target/spent/remaining/percentUsed/status and overall summary. Rather than creating a new `BudgetComparisonReportDto`, expose the existing `GetMonthlySummaryAsync` through the reports controller and render it with a new chart component on the client. |
| `CategorySpendingDto` | Reuse as-is in the date-range category report; shape is identical. |
| `DayDetailSummaryDto` | Partially reusable. It has TotalActual/TotalProjected/CombinedTotal/ItemCount but **lacks** income vs. expense split and category breakdown. Extend or compose rather than replace. |
| `CalendarBudgetPanel` pattern | Follow the same collapsible panel pattern (localStorage state persistence, expand/collapse animation) for the new `CalendarInsightsPanel`. |
| `ITransactionRepository.GetByDateRangeAsync` | Already supports arbitrary date range + optional account filter — no new repository method needed for date-range category reports. |
| `ReportService.GetMonthlyCategoryReportAsync` | Refactor to accept `DateOnly start, DateOnly end` internally; the monthly variant becomes a thin wrapper. Avoids duplicating the grouping/percentage logic. |
| `DonutChart` component | Reuse for category breakdown in InsightsPanel and date-range reports. |
| `ScopeService.ScopeChanged` | New report pages must subscribe to this event and re-fetch data when scope changes (same pattern as existing `MonthlyCategoriesReport`). |

---

## User Stories

### Calendar-to-Reports Navigation

#### US-050-001: View Reports for Selected Month
**As a** user viewing the calendar  
**I want to** click a "View Reports" button for the displayed month  
**So that** I can see analytics for the same period I'm viewing

**Acceptance Criteria:**
- [ ] Calendar page has a "View Reports" action (button or icon link)
- [ ] Clicking navigates to `/reports/categories?year={year}&month={month}`
- [ ] Report loads with the same month pre-selected
- [ ] "Back to Calendar" link returns to the same month view

#### US-050-002: View Day Analytics from Calendar
**As a** user viewing a specific day in the calendar  
**I want to** see spending summary for that day  
**So that** I can quickly understand my spending without manual calculation

**Acceptance Criteria:**
- [ ] Day detail panel includes a "Daily Summary" section
- [ ] Shows: total spent, total income, net, transaction count
- [ ] Shows top 3 categories for that day (if any spending)
- [ ] Link to full report for the day's month

#### US-050-003: View Week Summary from Calendar
**As a** user  
**I want to** select a week on the calendar and see weekly analytics  
**So that** I can understand my spending patterns on a weekly basis

**Acceptance Criteria:**
- [ ] Week rows in calendar are selectable (row click or week number click)
- [ ] Selecting a week shows a week summary panel or navigates to week report
- [ ] Week summary includes: total spent, daily average, category breakdown
- [ ] Week boundaries use Sunday-Saturday or configurable start

### Date Range Filtering

#### US-050-004: Filter Reports by Custom Date Range
**As a** user viewing the Category Spending report  
**I want to** select a custom date range (not just full month)  
**So that** I can analyze specific periods like "last 2 weeks" or "paycheck to paycheck"

**Acceptance Criteria:**
- [ ] Reports page has date range picker (start date, end date)
- [ ] Quick presets: "This Month", "Last Month", "Last 7 Days", "Last 30 Days", "Custom"
- [ ] URL updates with date range: `/reports/categories?start=2026-01-15&end=2026-02-01`
- [ ] API supports date range parameter (new endpoint or updated existing)
- [ ] Chart and data reflect the selected range

#### US-050-005: Navigate Report Dates via Calendar Picker
**As a** user viewing a report  
**I want to** click a calendar icon to pick a date visually  
**So that** I can intuitively navigate to any period

**Acceptance Criteria:**
- [ ] Reports page has a calendar picker button next to date range
- [ ] Clicking opens a mini calendar overlay (month view)
- [ ] Selecting a day sets that as the start or end date
- [ ] Can select a full month by clicking month header
- [ ] Picker matches app theme and accessibility standards

### Quick Insights in Calendar

#### US-050-006: Monthly Spending Summary in Calendar
**As a** user viewing the calendar  
**I want to** see a quick monthly spending summary without navigating away  
**So that** I get insights while planning my days

**Acceptance Criteria:**
- [ ] Calendar page has a collapsible "Month Insights" panel (similar to Budget Panel pattern from Feature 048)
- [ ] Shows: Total Income, Total Spending, Net, Top 3 Categories
- [ ] Mini donut or bar chart showing category distribution
- [ ] "See Full Report" link navigates to detailed report

#### US-050-007: Spending Trend Indicator
**As a** user  
**I want to** see at-a-glance trend indicators (up/down compared to previous period)  
**So that** I can quickly spot changes in spending behavior

**Acceptance Criteria:**
- [ ] Month Insights shows "vs. Last Month" trend: +15% or -10%
- [ ] Color coding: green for reduced spending, yellow for similar, red for increased
- [ ] Trend appears next to total spending amount
- [ ] Optional: daily/weekly mini sparkline chart

### New Report Types

#### US-050-008: Monthly Trends Report
**As a** user  
**I want to** view spending trends over multiple months  
**So that** I can identify long-term patterns

**Acceptance Criteria:**
- [ ] New report at `/reports/trends`
- [ ] Bar chart showing monthly totals (spending, income, net) over 6-12 months
- [ ] Line chart option for trends
- [ ] Can filter by category to see single category over time
- [ ] Hover/tap shows detailed month breakdown

#### US-050-009: Budget vs. Actual Report
**As a** user  
**I want to** compare my budget goals against actual spending  
**So that** I can see how well I'm sticking to my budget

**Acceptance Criteria:**
- [ ] New report at `/reports/budget-comparison`
- [ ] Shows each category with: Budget, Actual, Variance, % of Target
- [ ] Visual: grouped bar chart (budget vs. actual per category)
- [ ] Overall summary: total budgeted, total spent, overall variance
- [ ] Can toggle between current month and any past month

---

## Technical Design

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Calendar Page (/)                                                          │
│  ├── CalendarGrid (existing)                                               │
│  ├── DayDetail (existing) → enhanced with DaySummary                       │
│  ├── CalendarBudgetPanel (existing - Feature 048)                          │
│  └── NEW: CalendarInsightsPanel (month quick stats + chart)                │
└─────────────────────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ /reports        │ │ /reports/       │ │ /reports/       │
│ (Index)         │ │ categories      │ │ trends          │
│                 │ │                 │ │                 │
│ - Grid of cards │ │ - Enhanced with │ │ - NEW: Multi-   │
│ - Links to all  │ │   DateRangePkr  │ │   month chart   │
│   reports       │ │ - Donut chart   │ │ - Bar/line view │
└─────────────────┘ └─────────────────┘ └─────────────────┘
                              │
                              ▼
          ┌───────────────────┴───────────────────┐
          ▼                                       ▼
┌─────────────────────────┐     ┌─────────────────────────┐
│ /reports/budget-        │     │ Components/Reports/     │
│ comparison              │     │ ├── DateRangePicker     │
│                         │     │ ├── CalendarInsights    │
│ - NEW: Budget vs actual │     │ ├── DaySummary          │
│ - Grouped bar chart     │     │ ├── TrendIndicator      │
│ - Variance table        │     │ └── WeekSummary         │
└─────────────────────────┘     └─────────────────────────┘
```

### New/Modified Files

#### Client — Components

| File | Status | Description |
|------|--------|-------------|
| `Components/Reports/DateRangePicker.razor` | New | Start/end date picker with presets |
| `Components/Reports/DateRangePicker.razor.css` | New | Date picker styles |
| `Components/Reports/CalendarInsightsPanel.razor` | New | Quick monthly stats for calendar page |
| `Components/Reports/CalendarInsightsPanel.razor.css` | New | Insights panel styles |
| `Components/Reports/DaySummary.razor` | New | Day-level category breakdown for DayDetail |
| `Components/Reports/TrendIndicator.razor` | New | Up/down trend badge component |
| `Components/Reports/WeekSummary.razor` | New (Phase 6 — optional) | Week analytics panel |
| `Components/Charts/BarChart.razor` | New | Grouped/stacked bar chart for trends and comparison |
| `Components/Charts/BarChart.razor.css` | New | Bar chart styles |
| `Components/Charts/SparkLine.razor` | New (optional) | Mini inline trend chart |

> **Note:** All new chart components should follow the same pure-SVG approach as the existing `DonutChart` — no external chart library dependencies.

#### Client — Pages

| File | Status | Description |
|------|--------|-------------|
| `Pages/Reports/MonthlyCategoriesReport.razor` | Modified | Add DateRangePicker, URL query param support |
| `Pages/Reports/MonthlyTrendsReport.razor` | New | Multi-month trend report at `/reports/trends` |
| `Pages/Reports/BudgetComparisonReport.razor` | New | Budget vs. Actual report at `/reports/budget-comparison` |
| `Pages/Reports/ReportsIndex.razor` | Modified | Enable card links, remove "Coming Soon" badges |
| `Pages/Calendar.razor` | Modified | Add CalendarInsightsPanel, "View Reports" navigation |

#### Client — Services

| File | Status | Description |
|------|--------|-------------|
| `Services/IBudgetApiService.cs` | Modified | Add: `GetCategoryReportByRangeAsync`, `GetSpendingTrendsAsync`, `GetDaySummaryAsync` |
| `Services/BudgetApiService.cs` | Modified | Implement new API calls |

> **Note:** Budget vs. Actual does NOT need a new client API method — `GetBudgetSummaryAsync(year, month)` already returns `BudgetSummaryDto` with the required data shape.

#### API — Controllers

| File | Status | Description |
|------|--------|-------------|
| `Controllers/ReportsController.cs` | Modified | Add endpoints: categories/range, trends, day-summary. Add budget-comparison endpoint that delegates to existing `IBudgetProgressService.GetMonthlySummaryAsync`. |

#### Application — Services

| File | Status | Description |
|------|--------|-------------|
| `Reports/IReportService.cs` | Modified | Add: `GetCategoryReportByRangeAsync`, `GetSpendingTrendsAsync`, `GetDaySummaryAsync` |
| `Reports/ReportService.cs` | Modified | Implement new methods. Refactor `GetMonthlyCategoryReportAsync` to delegate to a shared `BuildCategoryReport(DateOnly start, DateOnly end)` internal method. |

> **Note:** No new `GetBudgetComparisonAsync` method needed on `IReportService`. The existing `IBudgetProgressService.GetMonthlySummaryAsync` → `BudgetSummaryDto` already computes per-category target vs. spent vs. remaining with status. The reports controller simply calls the existing service.

#### Contracts — DTOs

| File | Status | Description |
|------|--------|-------------|
| `Dtos/ReportDtos.cs` | Modified | Add `DateRangeCategoryReportDto` (extends monthly shape with `StartDate`/`EndDate` instead of `Year`/`Month`). Add `SpendingTrendsReportDto` + `MonthlyTrendPointDto`. Add `DaySummaryDto` (date, income, spending, net, count, top categories). |

> **Reuse decision:** `BudgetComparisonReportDto` is **NOT created** — the existing `BudgetSummaryDto` + `BudgetProgressDto` already carry the exact data needed. If the report view needs additional fields (e.g., variance as a signed amount), extend `BudgetProgressDto` or add a small wrapper DTO.

### Cross-Cutting Requirement: Scope Filtering

All new report endpoints and client pages **must respect the budget scope** (Personal/Shared/All):

- **API:** The `X-Budget-Scope` header is already parsed by `BudgetScopeMiddleware` and set on `IUserContext.CurrentScope`. Verify that `ITransactionRepository.GetByDateRangeAsync` applies scope filtering at the DbContext level. If not, add explicit scope filtering in the new `ReportService` methods.
- **Client:** New report pages must subscribe to `ScopeService.ScopeChanged` and re-fetch data when scope changes (same pattern as `MonthlyCategoriesReport.razor`, which already does this).
- **Testing:** Include scope-filtered test cases in unit and integration tests (e.g., verify that Personal scope excludes Shared transactions from reports).

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/reports/categories/monthly` | **Existing** — Monthly category breakdown |
| GET | `/api/v1/reports/categories/range` | **New** — Category breakdown by arbitrary date range |
| GET | `/api/v1/reports/trends` | **New** — Monthly totals over N months |
| GET | `/api/v1/reports/budget-comparison` | **New** — Delegates to existing `IBudgetProgressService.GetMonthlySummaryAsync` → `BudgetSummaryDto` |
| GET | `/api/v1/reports/day-summary/{date}` | **New** — Single day analytics with category breakdown |

> **Existing endpoint reused:** `GET /api/v1/budgetgoals/summary?year={year}&month={month}` already returns `BudgetSummaryDto`. The new `/reports/budget-comparison` endpoint is a **convenience alias** that routes through the reports controller for discoverability in the reports UI. Alternatively, the client could call the existing budget goals endpoint directly — decide during implementation which approach is cleaner.

#### New Endpoint Details

**GET `/api/v1/reports/categories/range`**
```
Query params:
  - startDate: DateOnly (required)
  - endDate: DateOnly (required)
  - accountId: Guid? (optional filter)

Response: DateRangeCategoryReportDto
{
  "startDate": "2026-01-15",
  "endDate": "2026-02-15",
  "totalSpending": { "currency": "USD", "amount": 1500.00 },
  "totalIncome": { "currency": "USD", "amount": 3000.00 },
  "categories": [
    {
      "categoryId": "...",
      "categoryName": "Groceries",
      "categoryColor": "#4CAF50",
      "amount": { "currency": "USD", "amount": 450.00 },
      "percentage": 30.0,
      "transactionCount": 12
    }
  ]
}
```

**GET `/api/v1/reports/trends`**
```
Query params:
  - months: int (default 6, max 24) - number of months to include
  - endMonth: int? (optional, defaults to current)
  - endYear: int? (optional, defaults to current)
  - categoryId: Guid? (optional - filter to single category)

Response: SpendingTrendsReportDto
{
  "monthlyData": [
    {
      "year": 2026,
      "month": 1,
      "totalSpending": { "currency": "USD", "amount": 2500.00 },
      "totalIncome": { "currency": "USD", "amount": 4000.00 },
      "netAmount": { "currency": "USD", "amount": 1500.00 },
      "transactionCount": 45
    },
    // ... more months
  ],
  "averageMonthlySpending": { "currency": "USD", "amount": 2400.00 },
  "averageMonthlyIncome": { "currency": "USD", "amount": 3800.00 },
  "trendDirection": "decreasing", // "increasing" | "stable" | "decreasing"
  "trendPercentage": -5.2
}
```

**GET `/api/v1/reports/budget-comparison`**
```
Query params:
  - year: int (required)
  - month: int (required)

Response: BudgetSummaryDto (EXISTING — from BudgetProgressDto.cs)
{
  "year": 2026,
  "month": 2,
  "totalBudgeted": { "currency": "USD", "amount": 3000.00 },
  "totalSpent": { "currency": "USD", "amount": 2750.00 },
  "totalRemaining": { "currency": "USD", "amount": 250.00 },
  "overallPercentUsed": 91.7,
  "categoriesOnTrack": 5,
  "categoriesWarning": 2,
  "categoriesOverBudget": 1,
  "categoriesNoBudgetSet": 3,
  "categoryProgress": [
    {
      "categoryId": "...",
      "categoryName": "Groceries",
      "categoryIcon": "🛒",
      "categoryColor": "#4CAF50",
      "targetAmount": { "currency": "USD", "amount": 500.00 },
      "spentAmount": { "currency": "USD", "amount": 475.00 },
      "remainingAmount": { "currency": "USD", "amount": 25.00 },
      "percentUsed": 95.0,
      "status": "Warning",
      "transactionCount": 12
    }
  ]
}

NOTE: This is the same BudgetSummaryDto already returned by the budget goals
summary endpoint. The reports controller delegates to IBudgetProgressService
directly — no new service method or DTO required.
```

**GET `/api/v1/reports/day-summary/{date}`**
```
Route param:
  - date: DateOnly (format: yyyy-MM-dd)

Query params:
  - accountId: Guid? (optional filter)

Response: DaySummaryDto
{
  "date": "2026-02-02",
  "totalSpending": { "currency": "USD", "amount": 85.50 },
  "totalIncome": { "currency": "USD", "amount": 0.00 },
  "netAmount": { "currency": "USD", "amount": -85.50 },
  "transactionCount": 4,
  "topCategories": [
    { "categoryName": "Dining", "amount": { "currency": "USD", "amount": 45.00 } },
    { "categoryName": "Transportation", "amount": { "currency": "USD", "amount": 25.50 } },
    { "categoryName": "Shopping", "amount": { "currency": "USD", "amount": 15.00 } }
  ]
}
```

### Component Specifications

#### DateRangePicker.razor Parameters

```csharp
[Parameter] public DateOnly StartDate { get; set; }
[Parameter] public DateOnly EndDate { get; set; }
[Parameter] public EventCallback<(DateOnly Start, DateOnly End)> OnRangeChanged { get; set; }
[Parameter] public bool ShowPresets { get; set; } = true;
[Parameter] public string Size { get; set; } = "medium"; // small | medium | large

// Presets: "This Month", "Last Month", "Last 7 Days", "Last 30 Days", "This Year", "Custom"
```

#### CalendarInsightsPanel.razor Parameters

```csharp
[Parameter] public int Year { get; set; }
[Parameter] public int Month { get; set; }
[Parameter] public bool IsExpanded { get; set; } = false;
[Parameter] public EventCallback<bool> OnExpandedChanged { get; set; }
// Fetches its own data via ApiService
```

#### TrendIndicator.razor Parameters

```csharp
[Parameter] public decimal CurrentValue { get; set; }
[Parameter] public decimal PreviousValue { get; set; }
[Parameter] public string Label { get; set; } = "vs. last period";
[Parameter] public bool InvertColors { get; set; } = false; // true for spending (down=good)
// Calculates and displays: +15% ↑ (green) or -10% ↓ (based on InvertColors)
```

### URL Routing

Reports will support bookmarkable URLs with query parameters:

```
/reports/categories?year=2026&month=2
/reports/categories?start=2026-01-15&end=2026-02-15
/reports/trends?months=12&categoryId=abc123
/reports/budget-comparison?year=2026&month=2
```

Calendar will add optional report context:

```
/2026/2?showInsights=true
```

---

## Implementation Plan

### MVP Definition

The **minimum viable delivery** (Phases 1–3) delivers the highest-value slice: date-range flexibility on the existing report, plus the two most-requested new reports (Trends, Budget vs. Actual). This unblocks the calendar integration work in Phases 4–5.

| Phase | Scope | Effort | MVP? |
|-------|-------|--------|------|
| 1 | Date Range API + Trends + Day Summary endpoints | Medium | ✅ |
| 2 | DateRangePicker + Enhanced Category Report + Trends Page | Medium | ✅ |
| 3 | Budget vs. Actual Report Page + BarChart component | Medium | ✅ |
| 4 | Calendar Insights Panel + Trend Indicator | Medium | |
| 5 | Day Summary in DayDetail + Calendar ↔ Reports navigation | Small | |
| 6 | Week Summary + Testing & Documentation | Small–Medium (optional) | |

### Phase 1: Date Range & Trends API Endpoints (TDD) ✅
> **Commit prefix:** `feat(api): add date range, trends, and day summary report endpoints`

**Objective:** Extend the API with flexible date-range queries and new report data. Follow TDD — tests first.

**Tasks:**
- [x] **Refactor** `ReportService.GetMonthlyCategoryReportAsync` to delegate to a private `BuildCategoryReportAsync(DateOnly start, DateOnly end, Guid? accountId)` method — existing tests must continue to pass
- [x] Create `DateRangeCategoryReportDto` in `ReportDtos.cs` (adds `StartDate`, `EndDate` fields; reuses `CategorySpendingDto` for the category list)
- [x] Add `GetCategoryReportByRangeAsync(DateOnly start, DateOnly end, Guid? accountId)` to `IReportService` + implement in `ReportService`
- [x] Create `SpendingTrendsReportDto` + `MonthlyTrendPointDto` in `ReportDtos.cs`
- [x] Add `GetSpendingTrendsAsync(int months, int? endYear, int? endMonth, Guid? categoryId)` to `IReportService` + implement
- [x] Create `DaySummaryDto` in `ReportDtos.cs` (date, totalIncome, totalSpending, netAmount, transactionCount, topCategories list)
- [x] Add `GetDaySummaryAsync(DateOnly date, Guid? accountId)` to `IReportService` + implement
- [x] Add 3 new endpoints to `ReportsController`: `categories/range`, `trends`, `day-summary/{date}`
- [x] Add **convenience** `budget-comparison` endpoint that delegates to `IBudgetProgressService.GetMonthlySummaryAsync`
- [x] Write unit tests for each new service method (edge cases: no data, single category, gap months, future dates)
- [x] Write API integration tests (validation, response shapes, 400 for invalid ranges)
- [ ] **Fix:** Derive currency from transaction data instead of hardcoding `"USD"` → tracked in [Feature 064](./064-report-currency-derivation.md)
- [ ] **Verify:** Scope filtering is applied by `GetByDateRangeAsync` → tracked in [Feature 065](./065-report-scope-filtering-audit.md) (confirmed bug: filter missing)
- [x] Update OpenAPI documentation (auto-generated via `[ProducesResponseType]` attributes and XML doc comments)

**Validation:**
- All new endpoints return correct data shapes
- Existing `/categories/monthly` endpoint is unaffected (regression tests pass)
- Edge cases: empty data, invalid dates, date range spanning year boundary, month with no transactions in trends
- Scope filtering works (Personal scope excludes Shared transactions)
- Max range validation: report date range limited to 1 year

---

### Phase 2: DateRangePicker + Enhanced Category Report + Trends Page ✅
> **Commit prefix:** `feat(client): add DateRangePicker and date-range category filtering`

**Objective:** Create DateRangePicker component, update existing Category report to use it, and build the Monthly Trends report page.

**Tasks:**
- [x] Create `DateRangePicker.razor` component with presets ("This Month", "Last Month", "Last 7 Days", "Last 30 Days", "Custom")
- [x] Implement start/end date inputs using native `<input type="date">` for accessibility
- [x] Style component following existing design patterns (responsive, theme-aware)
- [x] Write bUnit tests (preset calculation, end >= start validation, event callbacks) — 13 tests in `DateRangePickerTests.cs`
- [x] Integrate `DateRangePicker` into `MonthlyCategoriesReport.razor`
- [x] Update page to read `?start=` and `?end=` query params, falling back to `?year=&month=` for backward compatibility
- [x] Add `GetCategoryReportByRangeAsync` and `GetSpendingTrendsAsync` to `IBudgetApiService` + implement
- [x] Create `BarChart.razor` component (pure SVG, grouped bars, hover tooltips, ARIA labels, responsive) — code-behind pattern (`BarChart.razor.cs`) with `MarkupString` for SVG `<text>` elements
- [x] Write bUnit tests for BarChart (empty state, single bar, grouped bars, accessibility) — 10 tests in `BarChartTests.cs`
- [x] Create `MonthlyTrendsReport.razor` page at `/reports/trends`
- [x] Add month count selector (6, 12, 24 months)
- [x] Implement bar chart showing monthly income/spending/net over selected period
- [x] Add category filter dropdown (optional single-category view)
- [x] Subscribe to `ScopeService.ScopeChanged` on both new/modified pages
- [x] Update `ReportsIndex.razor`: enable "Monthly Trends" card link, remove "Coming Soon" badge
- [x] Add "Back to Calendar" navigation link on report pages
- [ ] Test mobile responsiveness

**Validation:**
- URL bookmarking works for both month and custom range
- Switching between month preset and custom range works seamlessly
- Trends chart renders correctly for 6/12/24 months
- Category filter on trends works
- Responsive on mobile viewports

---

### Phase 3: Budget vs. Actual Report Page
> **Commit prefix:** `feat(client): add Budget vs. Actual comparison report`

**Objective:** Create the Budget vs. Actual report page, reusing existing `BudgetSummaryDto` data.

**Tasks:**
- [ ] Create `BudgetComparisonReport.razor` page at `/reports/budget-comparison`
- [ ] Call existing `GetBudgetSummaryAsync(year, month)` (no new API method needed)
- [ ] Implement grouped bar chart (budget vs. actual per category) using new `BarChart` component
- [ ] Add data table with per-category: Target, Spent, Remaining, % Used, Status
- [ ] Add overall summary section (total budgeted, total spent, total remaining, % used)
- [ ] Color-code rows/bars by status (OnTrack → green, Warning → yellow, OverBudget → red, NoBudgetSet → gray)
- [ ] Add month navigation (Previous/Next) matching category report pattern
- [ ] Handle edge: month with no budget goals → show "No budget goals set for this month" with CTA to create goals
- [ ] Subscribe to `ScopeService.ScopeChanged`
- [ ] Update `ReportsIndex.razor`: enable "Budget vs. Actual" card link, remove "Coming Soon" badge
- [ ] Write bUnit tests for the page component

**Validation:**
- Data matches CalendarBudgetPanel for the same month (cross-check)
- Categories without budgets shown as "NoBudgetSet" (not hidden)
- Variance calculations correct (positive = under budget, negative = over)
- Works for months with zero goals

---

### Phase 4: Calendar Insights Panel
> **Commit prefix:** `feat(client): add CalendarInsightsPanel for at-a-glance analytics`

**Objective:** Add quick analytics visibility to the calendar page without navigating away.

**Tasks:**
- [ ] Create `CalendarInsightsPanel.razor` component (follows `CalendarBudgetPanel` collapsible pattern)
- [ ] Fetch monthly category report data (use existing `GetMonthlyCategoryReportAsync`)
- [ ] Display: Total Income, Total Spending, Net, Top 3 Categories
- [ ] Add mini donut chart (reuse `DonutChart` with `Compact=true`)
- [ ] Create `TrendIndicator.razor` component (% change vs. previous month, color-coded)
- [ ] Add "View Full Report" link → `/reports/categories?year={year}&month={month}`
- [ ] Persist collapsed/expanded state in `localStorage`
- [ ] Integrate into `Calendar.razor` (below or beside CalendarBudgetPanel)
- [ ] Style for mobile (collapsible, touch-friendly)
- [ ] Write bUnit tests for CalendarInsightsPanel and TrendIndicator

**Validation:**
- Panel data matches the full Category Spending report for the same month
- Trend calculation is correct (handles first month with no previous data)
- Collapse state persists across page reloads
- Doesn't increase Calendar page initial load time (lazy-fetch when expanded, or load after initial render)

---

### Phase 5: Day Summary + Calendar ↔ Reports Navigation
> **Commit prefix:** `feat(client): add day summary analytics and calendar-reports navigation`

**Objective:** Enhance DayDetail with a category breakdown and add bidirectional navigation between calendar and reports.

**Tasks:**
- [ ] Add `GetDaySummaryAsync` to `IBudgetApiService` + implement
- [ ] Create `DaySummary.razor` component (income, spending, net, top 3 categories)
- [ ] Integrate into existing `DayDetail.razor` (above or below the transaction list)
- [ ] Handle days with no transactions gracefully ("No transactions on this day")
- [ ] Add "View Reports" button/icon to Calendar page header → navigates to reports with current month pre-selected
- [ ] Calendar month navigation updates URL: `/{Year}/{Month}` (already exists)
- [ ] Reports "Back to Calendar" link uses `/{Year}/{Month}` to return to the same month
- [ ] Test: navigating calendar → reports → back preserves month context

**Validation:**
- Day summary totals match the sum of visible transactions in DayDetail
- Navigation flow is seamless in both directions
- No extra API calls on day select if panel is not visible

---

### Phase 6: Week Summary + Testing & Documentation (Optional)
> **Commit prefix:** `feat(client): add week selection and WeekSummary panel`

**Objective:** Enable week-based analytics and comprehensive testing.

**Tasks:**
- [ ] Create `WeekSummary.razor` component
- [ ] Add week row click handling to `CalendarGrid.razor` (visual highlight)
- [ ] Calculate week boundaries (configurable Sun–Sat vs Mon–Sun, default Sun–Sat)
- [ ] Show week summary in side panel or modal
- [ ] Include daily breakdown and category totals (client-side calculation from existing transaction data — no new API endpoint)
- [ ] Add Playwright E2E tests for all new report pages
- [ ] Test: date range navigation and URL bookmarking
- [ ] Test: calendar → report navigation flows
- [ ] Test: mobile viewports
- [ ] Run accessibility audit (axe-core) on all new pages/components
- [ ] Update README with new report features
- [ ] Add OpenAPI examples for new endpoints
- [ ] Performance test with large transaction sets (>1000 transactions/month)

**Validation:**
- Week selection has visual feedback
- Summary accurate for week boundaries
- All E2E tests pass
- No accessibility violations
- Documentation complete

---

## Testing Strategy

### Unit Tests (Application Layer)

- [ ] `ReportService.GetCategoryReportByRangeAsync` returns correct categories for date range
- [ ] `ReportService.GetCategoryReportByRangeAsync` handles range spanning year boundary
- [ ] `ReportService.GetSpendingTrendsAsync` calculates monthly totals correctly
- [ ] `ReportService.GetSpendingTrendsAsync` handles gaps (months with no data — should return zero-amount entries)
- [ ] `ReportService.GetSpendingTrendsAsync` handles single-category filter
- [ ] `ReportService.GetDaySummaryAsync` returns correct daily totals with top 3 categories
- [ ] `ReportService.GetDaySummaryAsync` returns empty result for day with no transactions
- [ ] Trend direction calculation: increasing (>5%), decreasing (<-5%), stable (±5%)
- [ ] Refactored `GetMonthlyCategoryReportAsync` still passes all 8 existing tests
- [ ] Scope filtering is respected in new report methods (if explicit filtering needed)

> **Note:** Budget comparison does NOT need new service tests — `BudgetProgressService.GetMonthlySummaryAsync` already has tests in `BudgetGoalServiceTests.cs`.

### API Integration Tests

- [ ] `GET /api/v1/reports/categories/range` returns 200 with valid data
- [ ] `GET /api/v1/reports/categories/range` returns 400 for `endDate < startDate`
- [ ] `GET /api/v1/reports/categories/range` returns 400 for range exceeding 1 year
- [ ] `GET /api/v1/reports/trends` returns correct number of months (default 6)
- [ ] `GET /api/v1/reports/trends` caps at 24 months max
- [ ] `GET /api/v1/reports/budget-comparison` returns 200 (delegates to existing service)
- [ ] `GET /api/v1/reports/budget-comparison` returns 400 for invalid year/month
- [ ] `GET /api/v1/reports/day-summary/{date}` returns 200 for valid date
- [ ] `GET /api/v1/reports/day-summary/{date}` returns 200 with empty data for date with no transactions

### Client Component Tests (bUnit)

- [ ] `DateRangePicker` emits correct range on preset click (each preset)
- [ ] `DateRangePicker` validates end >= start (does not emit invalid ranges)
- [ ] `DateRangePicker` custom range inputs update bound values
- [ ] `CalendarInsightsPanel` renders summary data in collapsed and expanded states
- [ ] `CalendarInsightsPanel` shows "View Full Report" link with correct route
- [ ] `TrendIndicator` shows correct direction and color (increase, decrease, stable)
- [ ] `TrendIndicator` inverts colors when `InvertColors=true` (down=green for spending)
- [ ] `DaySummary` handles empty data gracefully (shows message, not errors)
- [ ] `BarChart` renders correct number of bars, handles empty data, has ARIA labels
- [ ] `BudgetComparisonReport` calls existing `GetBudgetSummaryAsync` (not a new method)

### E2E Tests (Playwright)

- [ ] Navigate calendar → reports → back to calendar (month preserved)
- [ ] Select custom date range on category report, verify chart updates
- [ ] Bookmark report URL with date range params, refresh, verify same view
- [ ] Monthly Trends page loads with 6-month default, switching to 12 months works
- [ ] Budget vs. Actual page loads, shows correct status colors
- [ ] Mobile: insights panel collapses/expands with touch
- [ ] All reports load without console errors
- [ ] Accessibility: axe-core passes on all new pages

---

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Date range vs. month-only | Support both | Flexibility for users; month is common case but range enables paycheck-to-paycheck analysis |
| Calendar popup in DateRangePicker | Defer — use native `<input type="date">` first | Keeps initial implementation simple and accessible; can add a visual calendar popup later |
| Week summary | Optional Phase 6 | Core value is in month/day; week is nice-to-have |
| Insights in calendar | Collapsible panel | Follows budget panel pattern (CalendarBudgetPanel); doesn't clutter calendar |
| Chart library | Extend existing pure-SVG approach (DonutChart pattern) | Consistency, no new dependencies, full control over accessibility and theming |
| Trend calculation | Compare to previous same-length period | Simple, intuitive for users |
| Budget vs. Actual DTO | **Reuse existing `BudgetSummaryDto`** | Already contains all needed fields (target, spent, remaining, %, status per category + overall totals). Avoids a parallel DTO with the same shape. |
| Budget comparison API | Delegate to existing `IBudgetProgressService` | No new application service method needed — the computation already exists and is tested |
| DaySummaryDto vs DayDetailSummaryDto | Create new DTO | `DayDetailSummaryDto` is for the transactional view (actual vs. projected with recurring items); `DaySummaryDto` is for analytics (income/spending/net + category breakdown). Different concerns. |
| Scope filtering | Verify repository-level filtering; add explicit if missing | Reports must respect Personal/Shared/All scope |

---

## Risk Analysis

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **BarChart SVG complexity** — Building a grouped bar chart from scratch in pure SVG is significantly more work than the DonutChart | High | Medium | Start with a simple vertical bar chart (single series) for trends; add grouped bars for budget comparison later. Consider whether a lightweight JS chart interop (e.g., Chart.js via JSInterop) is justified if SVG effort exceeds 2 sprints. |
| **N+1 queries in trends** — `GetSpendingTrendsAsync` could hit the DB 24 times (once per month) if naively implemented | Medium | High | Implement as a single `GetByDateRangeAsync` call spanning the full range, then group in-memory. Same pattern as existing `GetMonthlyCategoryReportAsync`. |
| **Calendar page bloat** — Calendar.razor is already 844 lines; adding CalendarInsightsPanel increases complexity | Medium | Medium | Keep panel as a self-contained component that fetches its own data. Calendar.razor just toggles its visibility. |
| **Currency hardcoding** — `ReportService` hardcodes "USD". New endpoints inherit this bug. | High | Low | Track as a separate bug/debt item. Fix before or during Phase 1. |
| **Scope filtering gap** — If `GetByDateRangeAsync` doesn't apply scope filters, all new reports will leak cross-scope data | Medium | High | Verify in Phase 1 before implementing any new methods. Add integration test that asserts scope isolation. |
| **Performance with large datasets** — Trends over 24 months with many transactions could be slow | Low | Medium | Add index on `TransactionDate` if not present. Consider caching completed months (data won't change for past months). |
| **URL backward compatibility** — Changing category report to support `?start=&end=` must not break existing `?year=&month=` links | Low | Medium | Support both param styles; `?year=&month=` takes precedence if both are present. |

---

## Security Considerations

- All new endpoints require authentication (existing `[Authorize]` attribute on controller)
- **Scope isolation**: Reports must only return data visible to the user's current scope (Personal/Shared/All). Verify that `ITransactionRepository.GetByDateRangeAsync` applies scope filtering at the DbContext level. If not, add explicit filtering in `ReportService`.
- Date range validation prevents excessive queries (max range: 1 year)
- No sensitive data exposure in reports (only aggregated amounts)
- Rate limiting applies to all API endpoints (existing infrastructure)
- Input validation: reject invalid dates, months outside 1–12, years outside 2000–2100 (match existing controller validation pattern)

---

## Performance Considerations

- **Caching**: Consider caching monthly trend data (rarely changes for past months). Past-month report data is immutable — cache at the application layer or use HTTP `Cache-Control` headers with ETag.
- **Lazy loading**: CalendarInsightsPanel should fetch data only when expanded (or after initial calendar render), not on page load.
- **Pagination**: Trends endpoint limits to 24 months max.
- **Indexing**: Verify `TransactionDate` column is indexed. Current `GetByDateRangeAsync` relies on date range queries — without an index, trends over 24 months could table-scan.
- **N+1 avoidance**: `GetSpendingTrendsAsync` MUST fetch all transactions in a single `GetByDateRangeAsync` call and group in-memory, NOT loop per-month. The existing N+1 pattern in `BudgetProgressService.GetMonthlySummaryAsync` should not be copied.
- **Client-side**: Charts render progressively; show loading skeletons while data loads. BarChart SVG rendering should be efficient for up to 24 bar groups.
- **Currency**: Fix the hardcoded `"USD"` in `ReportService` — derive from the scope's default currency or the transactions themselves. (Track as a prerequisite or parallel task.)

---

## Accessibility Considerations

- Date inputs use native `<input type="date">` for screen reader compatibility
- Charts include text alternative (data table below or toggle)
- Trend indicators have accessible labels ("Spending increased 15% compared to last month")
- Focus management when panels expand/collapse
- Color is not only indicator (icons + text accompany color coding)

---

## Future Enhancements

- **Year in Review Report**: Annual summary with monthly breakdown, category totals, year-over-year comparison
- **Merchant Analytics**: Top merchants, recurring merchant detection
- **Export Reports**: PDF/CSV export of any report
- **Scheduled Reports**: Email weekly/monthly spending summary
- **Comparative Analytics**: Compare spending across time periods (this month vs. same month last year)
- **Goal Tracking Over Time**: Line chart showing budget adherence over months
- **SparkLine in Calendar Days**: Tiny inline charts in day cells showing daily pattern

---

## References

- [Feature 048 - Calendar Budget Editing](./048-calendar-edit-budget-goals.md) - Panel pattern reference
- [Feature 047 - Mobile Experience](./047-mobile-experience-calendar-quick-add-ai.md) - Mobile chart considerations
- [Feature 046.1 - Calendar Audit](./046.1-calendar-centric-navigation-audit.md) - Gap analysis source
- [Component Standards](./COMPONENT-STANDARDS.md) - Component design patterns
- [Current Reports Implementation](../src/BudgetExperiment.Client/Pages/Reports/) - Existing code reference

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-26 | Initial draft | @becauseimclever |
| 2026-02-02 | Fleshed out with full technical design, user stories, and implementation phases | @copilot |
| 2026-02-07 | **Evaluation & overhaul**: Verified current state against codebase. Added reuse/overlap analysis (reuse `BudgetSummaryDto` for budget comparison, eliminating redundant DTO). Added scope filtering cross-cutting requirement. Added risk analysis section. Consolidated 9 phases → 6 with MVP definition (Phases 1–3). Fixed inaccurate file paths and assumptions. Added N+1 query warning, currency hardcoding flag, and specific test cases. | @copilot |
| 2026-02-09 | **Phase 1 complete**: Implemented all Phase 1 API endpoints (categories/range, trends, day-summary, budget-comparison). Refactored `ReportService` with shared `BuildCategoryReportAsync`. Added 5 new DTOs, 3 new `IReportService` methods, 4 controller endpoints. 19 new unit tests, 20 new integration tests — all passing. Currency hardcoding and scope verification deferred to follow-up. | @copilot |
| 2026-02-08 | **Phase 2 complete**: Created `DateRangePicker` component (13 bUnit tests), `BarChart` SVG component with code-behind (10 bUnit tests), `BarChartData` models, `MonthlyTrendsReport` page at `/reports/trends`. Enhanced `MonthlyCategoriesReport` with DateRangePicker and URL query params. Added `GetCategoryReportByRangeAsync` and `GetSpendingTrendsAsync` to client API service. Enabled Monthly Trends card on ReportsIndex. Added Back to Calendar navigation. **Doc cleanup**: removed pervasive line duplication throughout entire document. | @copilot |
