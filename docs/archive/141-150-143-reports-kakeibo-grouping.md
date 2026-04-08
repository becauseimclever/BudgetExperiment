# Feature 143: Reports — Kakeibo Grouping

> **Status:** Done

## Prerequisites

- Feature 129b (Feature Flag Implementation) must be merged before implementation begins.
- Feature 131 (KakeiboCategory on BudgetCategory) must be completed.

---

## Feature Flag

**Flag Name:** None  
**Note:** Existing report feature flags already control report visibility (e.g., `Features:Reports:CategoryReport`). This feature adds optional query parameters to those reports; no new flag is needed. Toggles are UI controls provided on the report page itself.

---

## Overview

Existing reports gain optional **Kakeibo grouping** — the ability to view spending aggregated by Kakeibo bucket instead of individual categories. This transforms the reporting lens from "What did I spend on Groceries vs. Dining?" to "How did I balance Essentials vs. Wants?"

Affected reports:
1. **Monthly Categories Report** — Add a toggle: "Group by Kakeibo Bucket"
   - When enabled, replace category-level grouping with four segments (Essentials/Wants/Culture/Unexpected)
   - When disabled, show the existing per-category breakdown

2. **Budget Comparison Report** — Optional: Show Kakeibo category variance
   - When enabled, display trend comparison by Kakeibo bucket (e.g., "Essentials is stable, Wants is trending up")
   - Useful for identifying which Kakeibo bucket is driving spending variance

3. **Monthly Trends Report** — Optional: Show Kakeibo trend lines
   - When enabled, display four trend lines (one per Kakeibo bucket) showing month-over-month spending
   - User can toggle between category-level trends and Kakeibo trends

---

## Domain Model Changes

**None.** Aggregation and grouping are service-layer concerns. No new domain entities or fields needed.

---

## API Changes

**Modified Existing Endpoints:**

1. **Monthly Categories Report:**
   ```
   GET /api/v1/reports/monthly-categories?groupByKakeibo=true
   ```
   Query parameter: `groupByKakeibo: bool?` (optional, default: false)

2. **Budget Comparison Report:**
   ```
   GET /api/v1/reports/budget-comparison?groupByKakeibo=true
   ```
   Query parameter: `groupByKakeibo: bool?` (optional, default: false)

3. **Monthly Trends Report:**
   ```
   GET /api/v1/reports/monthly-trends?groupByKakeibo=true
   ```
   Query parameter: `groupByKakeibo: bool?` (optional, default: false)

**Response DTOs — Modified:**

When `groupByKakeibo=true`, responses are restructured to group by Kakeibo bucket instead of category:

```csharp
// Monthly Categories Report
public class MonthlyCategoriesReportDto
{
    public List<CategoryBreakdownItem> Categories { get; set; } = new();
}

public class CategoryBreakdownItem
{
    public string Name { get; set; } // Category name, or "Essentials" / "Wants" / "Culture" / "Unexpected"
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
    public string? KakeiboCategory { get; set; } // Only populated if grouping by Kakeibo
}
```

When `groupByKakeibo=true`, the `Name` field contains the Kakeibo bucket ("Essentials", "Wants", etc.) and the `Amount` is the sum of all transactions in that bucket. When `groupByKakeibo=false`, `Name` is the category name and `KakeiboCategory` is informational.

**Budget Comparison Report:**
```csharp
public class BudgetComparisonReportDto
{
    public List<BudgetItemVariance> Items { get; set; } = new();
}

public class BudgetItemVariance
{
    public string Name { get; set; } // Category or Kakeibo bucket name
    public decimal Budgeted { get; set; }
    public decimal Actual { get; set; }
    public decimal Variance { get; set; }
    public string? KakeiboCategory { get; set; } // Only if groupByKakeibo
}
```

**Monthly Trends Report:**
```csharp
public class MonthlyTrendsReportDto
{
    public List<TrendDataPoint> DataPoints { get; set; } = new();
}

public class TrendDataPoint
{
    public string Month { get; set; }
    public List<CategorySpending> CategorySpending { get; set; } = new();
}

public class CategorySpending
{
    public string Name { get; set; } // Category or Kakeibo bucket
    public decimal Amount { get; set; }
    public string? KakeiboCategory { get; set; } // Only if groupByKakeibo
}
```

**Implementation Notes:**
- When `groupByKakeibo=true`, aggregate transactions by their effective `KakeiboCategory` (override checked first, then category default)
- All other report logic (date ranges, filtering, formatting) remains unchanged
- The parameter is optional and backward-compatible — existing clients without the param get category-level grouping (default behavior)

---

## UI Changes

**Modified Components:**
1. `MonthlyCategoriesReportView.razor`
2. `BudgetComparisonReportView.razor`
3. `MonthlyTrendsReportView.razor`

**New UI Elements:**

1. **Monthly Categories Report — Kakeibo Toggle**
   - Add a toggle or radio buttons above the chart: "Group by: [Categories] [Kakeibo Buckets]"
   - When user selects "Kakeibo Buckets", fetch the report with `groupByKakeibo=true`
   - Redraw the chart/table with Kakeibo grouping
   - Persist the user's selection to `localStorage` so it's remembered on revisit

2. **Budget Comparison Report — Kakeibo Variance Option**
   - Add a checkbox or dropdown: "Show variance by: [Category] [Kakeibo Bucket]"
   - Fetches with `groupByKakeibo=true` when Kakeibo is selected
   - Redraw the variance chart showing Essentials/Wants/Culture/Unexpected trends

3. **Monthly Trends Report — Trend Type Toggle**
   - Add a toggle: "Trend by: [Categories] [Kakeibo Buckets]"
   - When toggled to Kakeibo, fetches with `groupByKakeibo=true` and displays four lines (Essentials/Wants/Culture/Unexpected)
   - When toggled back to Categories, displays the original per-category trends

**Chart Updates:**
- All charts using Kakeibo grouping should use consistent Kakeibo colors:
  - Essentials: blue
  - Wants: green
  - Culture: purple
  - Unexpected: orange/red

---

## Acceptance Criteria

- [x] Query parameter `groupByKakeibo: bool?` is added to existing report endpoints (monthly-categories, budget-comparison, monthly-trends)
- [x] When `groupByKakeibo=true`, transactions are aggregated by effective Kakeibo category (override checked first)
- [x] Response DTOs correctly group data by Kakeibo bucket when the parameter is true
- [x] Response DTOs remain backward-compatible when the parameter is false or omitted (default: category-level grouping)
- [x] Monthly Categories Report displays toggle: "Group by: [Categories] [Kakeibo Buckets]"
- [x] Budget Comparison Report displays dropdown/checkbox for variance grouping
- [x] Monthly Trends Report displays toggle for trend type
- [x] Charts redraw correctly when toggle is changed
- [x] Kakeibo grouping uses consistent colors (blue/green/purple/orange)
- [x] User selections are persisted to `localStorage` and remembered on page reload
- [x] All existing report functionality remains unchanged when toggle is not used
- [x] All unit and integration tests pass; OpenAPI spec is updated

---

## Implementation Notes

- **Aggregation Strategy:** When `groupByKakeibo=true`, the service layer sums transactions grouped by `KakeiboCategory` (resolved as override OR category default). Use a single LINQ query to avoid N+1 issues.
- **Null Handling:** Transactions with `null` effective `KakeiboCategory` (Income/Transfer) are excluded from Kakeibo aggregations. They do not appear in the grouped response.
- **Performance:** Kakeibo aggregations are computationally equivalent to category aggregations (same grouping overhead). No special caching needed beyond existing report caching.
- **Color Scheme:** Use consistent Kakeibo colors across all reports and visualizations for visual coherence:
  - Essentials: blue (#3b82f6 or similar)
  - Wants: green (#10b981 or similar)
  - Culture: purple (#a855f7 or similar)
  - Unexpected: orange/red (#f97316 or similar)
- **UI State:** Persist grouping preferences to `localStorage` keyed by report name (e.g., `report:monthly-categories:groupBy=Kakeibo`) so users don't have to re-toggle on each visit.
- **Backward Compatibility:** The parameter is optional. Omitting it defaults to category-level grouping (existing behavior). This ensures old clients and bookmarks continue to work.

