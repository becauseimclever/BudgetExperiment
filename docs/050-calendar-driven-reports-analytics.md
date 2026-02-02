# Feature 050: Calendar-Driven Reports & Analytics
> **Status:** 🗒️ Planning  
> **Priority:** Medium  
> **Dependencies:** Feature 048 (Calendar Budget Editing - Complete), Reports Infrastructure (Complete)

## Overview

Transform reports and analytics into a calendar-integrated experience where users can navigate and filter financial data by selecting dates, weeks, or months from the familiar calendar interface. Reports become an extension of the calendar-centric workflow, not a separate destination.

This feature bridges the gap between the calendar view (where users track daily transactions) and analytics (where users understand spending patterns), creating a seamless flow from "what happened" to "what does it mean."

## Problem Statement

Currently, the Reports section exists as a separate navigation destination disconnected from the calendar. Users must mentally translate between the calendar view (organized by date) and reports (organized by month dropdown). There's no way to click a date or week on the calendar and see focused analytics for that period.

### Current State

**What Exists:**
- **Reports Index** (`/reports`): Landing page with cards for different report types
- **Monthly Categories Report** (`/reports/categories`): Donut chart showing category spending breakdown with month selector (Previous/Next buttons)
- **"Coming Soon" Reports**: Monthly Trends, Budget vs. Actual, Year in Review (placeholders)
- **Calendar Page** (`/`): Month grid with transaction totals per day, day detail panel
- **API**: `GET /api/v1/reports/categories/monthly?year={year}&month={month}` returns `MonthlyCategoryReportDto`

**Current Gaps:**
1. Reports use their own month navigation (separate from calendar)
2. No way to view analytics for a specific day or week
3. No way to drill down from calendar to related reports
4. Calendar and reports don't share date context
5. No quick insights visible in calendar view itself
6. No date range reports (custom start/end dates)
7. No trends or comparisons visible

### Target State

- **Calendar ↔ Reports Integration**: Click a day, week, or month on calendar to view related analytics
- **Date Range Picker**: Reports support custom date ranges, not just full months
- **Quick Insights Panel**: Calendar page shows mini analytics for selected period
- **Unified Date Context**: Navigating calendar updates report date range and vice versa
- **New Reports**: Monthly Trends, Budget vs. Actual implemented
- **Deep Links**: URL parameters allow bookmarking and sharing specific report views
- **Mobile-First**: Touch-friendly charts and date selection (leverages Feature 047 components)

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

#### Client - Components

| File | Status | Description |
|------|--------|-------------|
| `Components/Reports/DateRangePicker.razor` | New | Start/end date picker with presets |
| `Components/Reports/DateRangePicker.razor.css` | New | Date picker styles |
| `Components/Reports/CalendarInsightsPanel.razor` | New | Quick monthly stats for calendar page |
| `Components/Reports/CalendarInsightsPanel.razor.css` | New | Insights panel styles |
| `Components/Reports/DaySummary.razor` | New | Day analytics for DayDetail |
| `Components/Reports/TrendIndicator.razor` | New | Up/down trend badge component |
| `Components/Reports/WeekSummary.razor` | New | Week analytics panel |
| `Components/Charts/BarChart.razor` | New | Bar chart for trends (extend existing charts) |
| `Components/Charts/SparkLine.razor` | New | Mini inline trend chart |

#### Client - Pages

| File | Status | Description |
|------|--------|-------------|
| `Pages/Reports/MonthlyCategoriesReport.razor` | Modified | Add DateRangePicker, URL params |
| `Pages/Reports/MonthlyTrendsReport.razor` | New | Multi-month trend report |
| `Pages/Reports/BudgetComparisonReport.razor` | New | Budget vs. Actual report |
| `Pages/Reports/ReportsIndex.razor` | Modified | Update card links, remove "Coming Soon" |
| `Pages/Calendar.razor` | Modified | Add CalendarInsightsPanel, navigation links |

#### Client - Services

| File | Status | Description |
|------|--------|-------------|
| `Services/IBudgetApiService.cs` | Modified | Add new report methods |
| `Services/BudgetApiService.cs` | Modified | Implement new API calls |

#### API - Controllers

| File | Status | Description |
|------|--------|-------------|
| `Controllers/ReportsController.cs` | Modified | Add new endpoints for date range, trends, comparison |

#### Application - Services

| File | Status | Description |
|------|--------|-------------|
| `Application/Reports/IReportService.cs` | Modified | Add new report methods |
| `Application/Reports/ReportService.cs` | Modified | Implement new reports |

#### Contracts - DTOs

| File | Status | Description |
|------|--------|-------------|
| `Contracts/Dtos/DateRangeCategoryReportDto.cs` | New | Report for custom date range |
| `Contracts/Dtos/MonthlyTrendDto.cs` | New | Single month in trend series |
| `Contracts/Dtos/SpendingTrendsReportDto.cs` | New | Multi-month trend data |
| `Contracts/Dtos/BudgetComparisonReportDto.cs` | New | Budget vs. actual data |
| `Contracts/Dtos/DaySummaryDto.cs` | New | Single day spending summary |

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/reports/categories/monthly` | **Existing** - Monthly category breakdown |
| GET | `/api/v1/reports/categories/range` | **New** - Category breakdown by date range |
| GET | `/api/v1/reports/trends` | **New** - Monthly totals over N months |
| GET | `/api/v1/reports/budget-comparison` | **New** - Budget vs. actual for a month |
| GET | `/api/v1/reports/day-summary/{date}` | **New** - Single day summary |

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

Response: BudgetComparisonReportDto
{
  "year": 2026,
  "month": 2,
  "totalBudgeted": { "currency": "USD", "amount": 3000.00 },
  "totalSpent": { "currency": "USD", "amount": 2750.00 },
  "totalVariance": { "currency": "USD", "amount": 250.00 },
  "overallPercentage": 91.7,
  "categories": [
    {
      "categoryId": "...",
      "categoryName": "Groceries",
      "categoryColor": "#4CAF50",
      "budgetedAmount": { "currency": "USD", "amount": 500.00 },
      "actualAmount": { "currency": "USD", "amount": 475.00 },
      "variance": { "currency": "USD", "amount": 25.00 },
      "percentageUsed": 95.0,
      "status": "warning" // "on-track" | "warning" | "over-budget"
    }
  ]
}
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

### Phase 1: Date Range API Endpoints
> **Commit:** `feat(api): add date range and trends report endpoints`

**Objective:** Extend the API to support flexible date ranges and new report types.

**Tasks:**
- [ ] Create `DateRangeCategoryReportDto`, `SpendingTrendsReportDto`, `BudgetComparisonReportDto`, `DaySummaryDto` in Contracts
- [ ] Add `GetCategoryReportByRangeAsync` to `IReportService`
- [ ] Add `GetSpendingTrendsAsync` to `IReportService`
- [ ] Add `GetBudgetComparisonAsync` to `IReportService`
- [ ] Add `GetDaySummaryAsync` to `IReportService`
- [ ] Implement methods in `ReportService`
- [ ] Write unit tests for each new service method
- [ ] Add endpoints to `ReportsController`
- [ ] Write API integration tests
- [ ] Update OpenAPI documentation

**Validation:**
- All new endpoints return correct data shapes
- Edge cases handled: empty data, invalid dates, missing budgets
- Existing monthly endpoint unaffected

---

### Phase 2: DateRangePicker Component
> **Commit:** `feat(client): add DateRangePicker component for reports`

**Objective:** Create a reusable date range selection component.

**Tasks:**
- [ ] Create `DateRangePicker.razor` component
- [ ] Implement preset buttons (This Month, Last Month, etc.)
- [ ] Implement start/end date inputs
- [ ] Add calendar popup for visual date selection (optional enhancement)
- [ ] Style component with responsive design
- [ ] Add ARIA labels for accessibility
- [ ] Write bUnit tests for component behavior
- [ ] Create documentation in COMPONENT-STANDARDS.md

**Validation:**
- Presets correctly calculate date ranges
- Custom range validation (end >= start)
- Keyboard accessible

---

### Phase 3: Enhanced Category Report Page
> **Commit:** `feat(client): add date range filtering to category spending report`

**Objective:** Update the existing Category Spending report to use DateRangePicker.

**Tasks:**
- [ ] Integrate `DateRangePicker` into `MonthlyCategoriesReport.razor`
- [ ] Update page to read date range from URL query params
- [ ] Call new date range API endpoint when custom range selected
- [ ] Update URL when range changes (for bookmarking)
- [ ] Add "Back to Calendar" navigation link
- [ ] Test mobile responsiveness
- [ ] Update navigation from calendar to include month params

**Validation:**
- URL bookmarking works
- Switching between month and custom range works
- Chart updates correctly

---

### Phase 4: Calendar Insights Panel
> **Commit:** `feat(client): add CalendarInsightsPanel for month-at-a-glance analytics`

**Objective:** Add quick analytics visibility to the calendar page.

**Tasks:**
- [ ] Create `CalendarInsightsPanel.razor` component
- [ ] Add to `Calendar.razor` page (below or beside budget panel)
- [ ] Fetch monthly summary data (use existing endpoint)
- [ ] Display: Total Income, Spending, Net, Top Categories
- [ ] Add mini donut chart (reuse `DonutChart` component)
- [ ] Create `TrendIndicator.razor` for vs. previous month
- [ ] Add "View Full Report" link
- [ ] Persist collapsed/expanded state in localStorage
- [ ] Style for mobile (collapsible)

**Validation:**
- Panel shows accurate data matching report page
- Trend calculation is correct
- Collapse state persists

---

### Phase 5: Day Summary in Day Detail
> **Commit:** `feat(client): add DaySummary to calendar day detail panel`

**Objective:** Show spending summary when a day is selected on the calendar.

**Tasks:**
- [ ] Create `DaySummary.razor` component
- [ ] Add `GetDaySummaryAsync` to `BudgetApiService`
- [ ] Integrate into `DayDetail.razor`
- [ ] Show: total spent, income, net, top 3 categories
- [ ] Handle days with no transactions gracefully
- [ ] Style to match existing day detail design

**Validation:**
- Day summary matches transaction totals
- Empty days show appropriate message
- Doesn't add significant load time

---

### Phase 6: Monthly Trends Report
> **Commit:** `feat(client): add Monthly Trends report with multi-month charts`

**Objective:** Create the new Monthly Trends report page.

**Tasks:**
- [ ] Create `BarChart.razor` component (or extend existing chart)
- [ ] Create `MonthlyTrendsReport.razor` page at `/reports/trends`
- [ ] Add month count selector (6, 12, 24 months)
- [ ] Implement bar chart showing monthly income/spending
- [ ] Add optional line chart view toggle
- [ ] Add category filter dropdown
- [ ] Update `ReportsIndex.razor` to enable the card link

**Validation:**
- Chart displays correctly for 6/12/24 months
- Category filtering works
- Responsive on mobile

---

### Phase 7: Budget Comparison Report
> **Commit:** `feat(client): add Budget vs. Actual comparison report`

**Objective:** Create the Budget vs. Actual report page.

**Tasks:**
- [ ] Create `BudgetComparisonReport.razor` page at `/reports/budget-comparison`
- [ ] Implement grouped bar chart (budget vs. actual per category)
- [ ] Add data table with variance details
- [ ] Add overall summary section
- [ ] Color-code by status (on-track, warning, over-budget)
- [ ] Add month navigation (Previous/Next)
- [ ] Update `ReportsIndex.razor` to enable the card link

**Validation:**
- Data matches budget goals and actual spending
- Categories without budgets handled gracefully
- Variance calculations correct

---

### Phase 8: Week Summary (Optional Enhancement)
> **Commit:** `feat(client): add week selection and WeekSummary panel`

**Objective:** Enable week-based analytics from the calendar.

**Tasks:**
- [ ] Create `WeekSummary.razor` component
- [ ] Add week row click handling to `CalendarGrid.razor`
- [ ] Calculate week boundaries (configurable Sun-Sat vs Mon-Sun)
- [ ] Show week summary in side panel or modal
- [ ] Include daily breakdown and category totals
- [ ] Add API endpoint if needed (or calculate client-side)

**Validation:**
- Week selection visual feedback
- Summary accurate for week boundaries
- Works on mobile

---

### Phase 9: Testing & Documentation
> **Commit:** `test(reports): add E2E tests for calendar-driven reports`

**Objective:** Comprehensive testing and documentation.

**Tasks:**
- [ ] Add Playwright E2E tests for all new report pages
- [ ] Test date range navigation and URL bookmarking
- [ ] Test calendar → report navigation flows
- [ ] Test mobile viewports
- [ ] Run accessibility audit (axe-core)
- [ ] Update README with new report features
- [ ] Add OpenAPI examples for new endpoints
- [ ] Performance test with large transaction sets

**Validation:**
- All E2E tests pass
- No accessibility violations
- Documentation complete

---

## Testing Strategy

### Unit Tests (Application Layer)

- [ ] `ReportService.GetCategoryReportByRangeAsync` returns correct categories for date range
- [ ] `ReportService.GetSpendingTrendsAsync` calculates monthly totals correctly
- [ ] `ReportService.GetSpendingTrendsAsync` handles gaps (months with no data)
- [ ] `ReportService.GetBudgetComparisonAsync` calculates variance correctly
- [ ] `ReportService.GetBudgetComparisonAsync` handles categories without budgets
- [ ] `ReportService.GetDaySummaryAsync` returns correct daily totals
- [ ] Trend direction calculation (increasing/decreasing/stable)

### API Integration Tests

- [ ] `GET /api/v1/reports/categories/range` returns 200 with valid data
- [ ] `GET /api/v1/reports/categories/range` returns 400 for invalid date range
- [ ] `GET /api/v1/reports/trends` returns correct number of months
- [ ] `GET /api/v1/reports/budget-comparison` handles month with no budgets
- [ ] `GET /api/v1/reports/day-summary/{date}` returns 200 for valid date

### Client Component Tests (bUnit)

- [ ] `DateRangePicker` emits correct range on preset click
- [ ] `DateRangePicker` validates end >= start
- [ ] `CalendarInsightsPanel` renders summary data
- [ ] `TrendIndicator` shows correct direction and color
- [ ] `DaySummary` handles empty data gracefully

### E2E Tests (Playwright)

- [ ] Navigate calendar → reports → back to calendar (month preserved)
- [ ] Select custom date range, verify chart updates
- [ ] Bookmark report URL, refresh, verify same view
- [ ] Mobile: insights panel collapses/expands
- [ ] All reports load without console errors

---

## Decisions Made

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Date range vs. month-only | Support both | Flexibility for users; month is common case but range enables paycheck-to-paycheck analysis |
| Calendar popup in DateRangePicker | Phase 2 (basic inputs first) | Keep initial implementation simple; can enhance later |
| Week summary | Optional Phase 8 | Core value is in month/day; week is nice-to-have |
| Insights in calendar | Collapsible panel | Follows budget panel pattern; doesn't clutter calendar |
| Chart library | Extend existing DonutChart approach | Consistency, avoid new dependencies |
| Trend calculation | Compare to previous same-length period | Simple, intuitive for users |

---

## Security Considerations

- All new endpoints require authentication (existing `[Authorize]` attribute on controller)
- Date range validation prevents excessive queries (max range: 1 year)
- No sensitive data exposure in reports (only aggregated amounts)
- Rate limiting applies to all API endpoints (existing infrastructure)

---

## Performance Considerations

- **Caching**: Consider caching monthly trend data (rarely changes for past months)
- **Lazy loading**: Insights panel fetches data only when visible
- **Pagination**: Trends endpoint limits to 24 months max
- **Indexing**: Ensure `TransactionDate` column is indexed for date range queries
- **Client-side**: Charts render progressively; loading states for large datasets

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
