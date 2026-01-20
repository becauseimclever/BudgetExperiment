# Feature 029: Reports & Dashboard

## Overview

Implement a comprehensive reports section with interactive charts and visualizations to help users understand their spending patterns, budget performance, and financial trends. The reports section will be accessible via a collapsible navigation menu item (initially collapsed) and provide various chart types including donut charts for category breakdowns, trend lines, and comparative analysis views.

The initial implementation focuses on a **Monthly Category Spending Donut Chart** that visualizes how spending is distributed across budget categories for a selected month.

## Problem Statement

Users need visual representations of their financial data to identify patterns, track progress toward budget goals, and make informed financial decisions.

### Current State

- Transaction and budget data exists in the system but lacks visualization
- Users must mentally aggregate numbers to understand spending patterns
- No graphical representation of category distribution
- No trend analysis over time
- Difficult to quickly assess budget health at a glance

### Target State

- Dedicated Reports section with interactive charts
- Monthly category spending donut chart as primary visualization
- Clean, responsive chart components using pure Blazor (no external JS libraries)
- Collapsible Reports menu in navigation (initially collapsed)
- Future: Additional chart types (trends, comparisons, forecasts)

---

## User Stories

### Navigation & Access

#### US-029-001: Access Reports Section
**As a** user  
**I want to** access a dedicated Reports section from the navigation menu  
**So that** I can view visualizations of my financial data

**Acceptance Criteria:**
- [x] Reports menu item in navigation sidebar
- [x] Collapsible submenu containing report options
- [x] Initially collapsed state by default
- [x] Expand/collapse toggle with visual indicator (chevron)
- [x] Maintains collapse state during session
- [x] Works correctly in both expanded and collapsed sidebar states

#### US-029-002: Navigate to Specific Reports
**As a** user  
**I want to** navigate directly to specific report types  
**So that** I can quickly access the visualization I need

**Acceptance Criteria:**
- [x] Submenu items for each report type
- [ ] Active state indicator for current report
- [x] Keyboard accessible navigation
- [x] Clean URL routes (e.g., `/reports/categories`, `/reports/trends`)

### Monthly Category Donut Chart

#### US-029-003: View Monthly Category Distribution
**As a** user  
**I want to** see a donut chart showing my spending by category for a month  
**So that** I can visualize where my money goes

**Acceptance Criteria:**
- [x] Donut chart displaying expense categories
- [x] Each segment represents a category's percentage of total spending
- [x] Color-coded segments with legend
- [x] Category name and amount displayed in legend
- [x] Center of donut shows total monthly spending
- [x] Responsive sizing for different screen widths
- [x] Smooth rendering without JavaScript dependencies

#### US-029-004: Select Month for Category Report
**As a** user  
**I want to** select which month to view in the category chart  
**So that** I can analyze different time periods

**Acceptance Criteria:**
- [x] Month/year selector (dropdown or date picker)
- [x] Defaults to current month
- [x] Quick navigation (previous/next month buttons)
- [x] Chart updates when month selection changes
- [x] Shows message when no data available for selected month

#### US-029-005: Interactive Chart Elements
**As a** user  
**I want to** interact with the chart to see details  
**So that** I can explore my spending data

**Acceptance Criteria:**
- [x] Hover/focus on segment shows tooltip with:
  - Category name
  - Amount spent
  - Percentage of total
  - Number of transactions
- [x] Click on segment navigates to filtered transaction list
- [x] Legend items are clickable to highlight corresponding segment
- [x] Accessible via keyboard navigation

#### US-029-006: Filter Categories in Chart
**As a** user  
**I want to** filter which categories appear in the chart  
**So that** I can focus on specific spending areas

**Acceptance Criteria:**
- [ ] Toggle to include/exclude income categories
- [ ] Option to show only categories with spending
- [ ] Minimum threshold filter (hide categories below X% or $Y)
- [ ] "Other" grouping for small categories

### Data & Calculations

#### US-029-007: Accurate Category Totals
**As a** user  
**I want to** see accurate spending totals per category  
**So that** I can trust the visualization

**Acceptance Criteria:**
- [ ] Aggregates all transactions for selected month
- [ ] Groups by assigned category
- [ ] Handles uncategorized transactions (shown as "Uncategorized")
- [ ] Excludes transfers (or shows separately)
- [ ] Respects current budget scope (personal/household)
- [ ] Matches totals shown elsewhere in the app

---

## Technical Design

### Architecture Changes

The Reports feature introduces a new UI section with chart components. Charts will be implemented using **pure SVG rendered by Blazor** to avoid JavaScript dependencies and maintain the project's plain Blazor approach.

```
src/BudgetExperiment.Client/
├── Pages/
│   └── Reports/
│       ├── ReportsIndex.razor          # Reports landing page
│       └── MonthlyCategoriesReport.razor # Category donut chart page
├── Components/
│   ├── Charts/
│   │   ├── DonutChart.razor            # Reusable donut chart component
│   │   ├── DonutChartSegment.razor     # Individual segment component
│   │   └── ChartLegend.razor           # Reusable legend component
│   └── Navigation/
│       └── NavMenu.razor               # Updated with collapsible Reports section
└── Services/
    └── ReportDataService.cs            # Client-side report calculations (optional)
```

### Domain Model

No new domain entities required. Reports aggregate existing transaction data.

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/reports/categories/monthly?year={year}&month={month}` | Get category spending summary for a month |

**Response DTO:**

```csharp
public record MonthlyCategoryReportDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public decimal TotalSpending { get; init; }
    public decimal TotalIncome { get; init; }
    public IReadOnlyList<CategorySpendingDto> Categories { get; init; } = [];
}

public record CategorySpendingDto
{
    public Guid? CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public string? CategoryColor { get; init; }
    public decimal Amount { get; init; }
    public decimal Percentage { get; init; }
    public int TransactionCount { get; init; }
}
```

### Database Changes

No schema changes required. Report data is aggregated from existing `Transactions` and `BudgetCategories` tables.

### UI Components

#### DonutChart Component

Pure SVG-based donut chart rendered by Blazor:

```csharp
// DonutChart.razor parameters
[Parameter] public IReadOnlyList<DonutSegment> Segments { get; set; } = [];
[Parameter] public decimal CenterValue { get; set; }
[Parameter] public string CenterLabel { get; set; } = string.Empty;
[Parameter] public int Size { get; set; } = 300;
[Parameter] public int StrokeWidth { get; set; } = 40;
[Parameter] public EventCallback<DonutSegment> OnSegmentClick { get; set; }
[Parameter] public EventCallback<DonutSegment> OnSegmentHover { get; set; }

public record DonutSegment(
    string Label,
    decimal Value,
    decimal Percentage,
    string Color,
    object? Data = null);
```

#### SVG Rendering Approach

```razor
@* DonutChart.razor - simplified structure *@
<div class="donut-chart-container">
    <svg viewBox="0 0 @ViewBoxSize @ViewBoxSize" class="donut-chart">
        @foreach (var segment in CalculatedSegments)
        {
            <DonutChartSegment 
                StartAngle="@segment.StartAngle"
                EndAngle="@segment.EndAngle"
                Color="@segment.Color"
                Radius="@Radius"
                StrokeWidth="@StrokeWidth"
                OnClick="@(() => OnSegmentClick.InvokeAsync(segment.Data))"
                OnHover="@(() => OnSegmentHover.InvokeAsync(segment.Data))" />
        }
        <text class="center-value">@CenterValue.ToString("C")</text>
        <text class="center-label">@CenterLabel</text>
    </svg>
    <ChartLegend Items="@LegendItems" OnItemClick="@HandleLegendClick" />
</div>
```

#### Navigation Menu Update

Add collapsible Reports section to NavMenu:

```razor
@* In NavMenu.razor - Reports section *@
<div class="nav-section nav-collapsible @(reportsExpanded ? "expanded" : "collapsed")">
    <button class="nav-item nav-section-header" @onclick="ToggleReports" title="Reports">
        <span class="nav-icon"><Icon Name="bar-chart-2" Size="20" /></span>
        @if (!IsCollapsed)
        {
            <span class="nav-text">Reports</span>
            <span class="nav-chevron">
                <Icon Name="@(reportsExpanded ? "chevron-down" : "chevron-right")" Size="16" />
            </span>
        }
    </button>
    
    @if (!IsCollapsed && reportsExpanded)
    {
        <div class="nav-subitems">
            <NavLink class="nav-item nav-subitem" href="reports/categories" title="Category Spending">
                <span class="nav-icon"><Icon Name="pie-chart" Size="16" /></span>
                <span class="nav-text">Categories</span>
            </NavLink>
            @* Future report links *@
        </div>
    }
</div>

@code {
    private bool reportsExpanded = false; // Initially collapsed
    
    private void ToggleReports()
    {
        reportsExpanded = !reportsExpanded;
    }
}
```

---

## Implementation Plan

### Phase 1: API Endpoint for Category Report ✅

**Objective:** Create the backend endpoint that aggregates transaction data by category for a given month.

**Tasks:**
- [x] Create `MonthlyCategoryReportDto` and `CategorySpendingDto` in Contracts
- [x] Add `IReportService` interface in Application layer
- [x] Implement `ReportService.GetMonthlyCategoryReportAsync()` 
- [x] Create `ReportsController` with GET endpoint
- [x] Write unit tests for aggregation logic
- [x] Write integration tests for endpoint

**Commit:**
```bash
git add .
git commit -m "feat(api): add monthly category report endpoint

- Add MonthlyCategoryReportDto and CategorySpendingDto
- Implement ReportService with category aggregation
- Create ReportsController with GET /api/v1/reports/categories/monthly
- Include unit and integration tests

Refs: #029"
```

---

### Phase 2: Donut Chart Component ✅

**Objective:** Build a reusable, pure-Blazor SVG donut chart component.

**Tasks:**
- [x] Create `DonutChart.razor` component with SVG rendering
- [x] Create `DonutChartSegment.razor` for individual segments
- [x] Implement SVG arc path calculations (stroke-dasharray approach)
- [x] Add hover state handling (CSS + Blazor events)
- [x] Create `ChartLegend.razor` component
- [x] Add responsive sizing
- [x] Write component unit tests (bUnit)
- [x] Add CSS styling in scoped stylesheet

**Commit:**
```bash
git add .
git commit -m "feat(client): add DonutChart component

- Pure SVG donut chart rendered by Blazor
- Segment hover and click interactions
- Reusable legend component
- Responsive sizing with CSS

Refs: #029"
```

---

### Phase 3: Navigation Menu Update ✅

**Objective:** Add collapsible Reports section to navigation.

**Tasks:**
- [x] Add Reports section to NavMenu.razor
- [x] Implement expand/collapse toggle
- [x] Set initially collapsed state
- [x] Add chevron icon indicator
- [x] Style collapsible section
- [x] Test in both sidebar states (expanded/collapsed)
- [x] Ensure keyboard accessibility (focus-visible outline)

**Commit:**
```bash
git add .
git commit -m "feat(client): add collapsible Reports menu section

- Add Reports section to navigation menu
- Initially collapsed by default
- Chevron indicator for expand/collapse state
- Works in both sidebar states

Refs: #029"
```

---

### Phase 4: Monthly Categories Report Page ✅

**Objective:** Create the report page that displays the donut chart with category spending.

**Tasks:**
- [x] Create `MonthlyCategoriesReport.razor` page
- [x] Add month/year selector
- [x] Integrate with API service (GetMonthlyCategoryReportAsync)
- [x] Connect DonutChart component with data
- [x] Add loading and empty states
- [x] Implement segment click navigation to transactions
- [x] Add responsive layout (grid with responsive breakpoints)
- [x] Add categories list with color indicators

**Commit:**
```bash
git add .
git commit -m "feat(client): add monthly categories report page

- MonthlyCategoriesReport page with donut chart
- Month/year selector with navigation
- Loading and empty states
- Segment click navigates to filtered transactions

Refs: #029"
```

---

### Phase 5: Reports Index & Polish

**Objective:** Create reports landing page and final polish.

**Tasks:**
- [ ] Create `ReportsIndex.razor` landing page
- [ ] Add report cards/tiles for available reports
- [ ] Add tooltips with detailed information
- [ ] Accessibility review (ARIA labels, keyboard nav)
- [ ] Cross-browser testing
- [ ] Mobile responsiveness verification

**Commit:**
```bash
git add .
git commit -m "feat(client): add reports index page and polish

- Reports landing page with report cards
- Enhanced tooltips and accessibility
- Mobile responsive layout
- Cross-browser compatibility

Refs: #029"
```

---

### Phase 6: Documentation & Cleanup

**Objective:** Final polish, documentation updates, and cleanup.

**Tasks:**
- [ ] Update API documentation / OpenAPI specs
- [ ] Add XML comments for public APIs
- [ ] Update README if needed
- [ ] Remove any TODO comments
- [ ] Final code review

**Commit:**
```bash
git add .
git commit -m "docs(reports): add documentation for feature 029

- XML comments for public API
- OpenAPI spec updates for report endpoints
- Update feature documentation

Refs: #029"
```

---

## Testing Strategy

### Unit Tests

- [ ] ReportService.GetMonthlyCategoryReportAsync() aggregates correctly
- [ ] Empty transaction set returns empty categories
- [ ] Uncategorized transactions grouped properly
- [ ] Percentage calculations are accurate (sum to 100%)
- [ ] Budget scope filtering works correctly
- [ ] Transfers excluded from spending totals

### Integration Tests

- [ ] GET /api/v1/reports/categories/monthly returns correct structure
- [ ] Query parameters (year, month) filter correctly
- [ ] Unauthorized request returns 401
- [ ] Invalid month/year returns 400

### Component Tests (bUnit)

- [ ] DonutChart renders correct number of segments
- [ ] Segment colors match input
- [ ] Legend displays all categories
- [ ] Click events fire correctly
- [ ] Empty data shows appropriate message

### Manual Testing Checklist

- [ ] Navigate to Reports via menu
- [ ] Verify menu starts collapsed
- [ ] Expand Reports submenu
- [ ] Navigate to Categories report
- [ ] Verify donut chart displays correctly
- [ ] Change month and verify data updates
- [ ] Hover over segments and verify tooltips
- [ ] Click segment and verify navigation
- [ ] Test on mobile viewport
- [ ] Test with keyboard only

---

## Security Considerations

- Reports respect user authentication (401 for anonymous)
- Reports filtered by current budget scope (user's personal/household data only)
- No cross-user data leakage in aggregations
- Rate limiting on report endpoints (prevent abuse)

---

## Performance Considerations

- Cache report data on client for current session
- Consider server-side caching for expensive aggregations
- Lazy load chart components
- SVG rendering is lightweight (no large JS libraries)
- Paginate legend if too many categories
- Consider background calculation for complex reports

---

## Future Enhancements

- [ ] Spending trends line chart (month-over-month)
- [ ] Budget vs. actual comparison chart
- [ ] Income vs. expenses bar chart
- [ ] Category spending over time (stacked area chart)
- [ ] Export reports as PDF/image
- [ ] Custom date range selection
- [ ] Drill-down from chart to transaction details
- [ ] Comparative reports (this month vs. last month)
- [ ] Forecast/projection charts
- [ ] Dashboard with multiple charts

---

## References

- [Feature 021: Budget Categories & Goals](./021-budget-categories-goals.md)
- [Feature 024: Auto-Categorization Rules](./024-auto-categorization-rules-engine.md)
- [SVG Arc Path Reference](https://developer.mozilla.org/en-US/docs/Web/SVG/Tutorial/Paths)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-19 | Initial draft | @copilot |
