# Feature 137: Kaizen Dashboard Report

> **Status:** Planned

## Prerequisites

- Feature 129b (Feature Flag Implementation) must be merged before implementation begins.
- Feature 131 (KakeiboCategory on BudgetCategory) must be completed.
- Feature 136 (KaizenGoal entity and micro-goal tracking) must be completed.

---

## Feature Flag

**Flag Name:** `Features:Kaizen:Dashboard`  
**Default Value:** `false` (during development); `true` when shipped  
**When Enabled:** The Kaizen Dashboard report appears in the Reports landing page and is accessible at `/reports/kaizen-dashboard`

---

## Overview

The Kaizen Dashboard is a 12-week rolling report that visualizes spending patterns through the Kakeibo lens and overlays weekly micro-goal outcomes. This report answers the question: *"How is my spending trending across Essentials, Wants, Culture, and Unexpected — and am I achieving my weekly micro-goals?"*

The dashboard shows:
- **Four stacked area lines** — one per Kakeibo category (Essentials, Wants, Culture, Unexpected) — showing weekly spending aggregations
- **Week columns** — each column represents one week, labeled with week number or date range (e.g., "Wk 14" or "Apr 7–13")
- **Month boundaries** — visual dividers or shading to mark calendar month boundaries
- **Kaizen outcome indicators** — small ✓ or ✗ badges per week showing whether the user's micro-goal for that week was achieved

This combines the Kakeibo spending rhythm with the Kaizen philosophy of visible, incremental improvement. A user can see at a glance: "Week 14 was lighter overall, Culture spending is steady, and I hit my micro-goal."

---

## Domain Model Changes

**Entity:** `KaizenGoal`  
Assumed to exist from Feature 136. Required fields:
- `Id: Guid`
- `UserId: Guid`
- `WeekStart: DateOnly`
- `Goal: string` (e.g., "Spend $10 less on coffee this week")
- `IsAchieved: bool` (set at end of week or by user)
- `CreatedAtUtc: DateTime`

**Aggregation:** No new entity. The service layer computes `WeeklyKakeiboSummary` on the fly from existing `Transaction` and `KakeiboCategory` data.

---

## API Changes

**New Endpoint:**
```
GET /api/v1/reports/kaizen-dashboard?weeks=12
```

**Query Parameters:**
- `weeks: int` (optional, default: 12) — number of weeks to include in the rolling view

**Response DTO:**
```csharp
public class KaizenDashboardDto
{
    public List<WeeklyKakeiboSummary> Weeks { get; set; } = new();
}

public class WeeklyKakeiboSummary
{
    public DateOnly WeekStart { get; set; }
    public string WeekLabel { get; set; } // e.g., "Wk 14" or "Apr 7–13"
    public decimal Essentials { get; set; }
    public decimal Wants { get; set; }
    public decimal Culture { get; set; }
    public decimal Unexpected { get; set; }
    public string? KaizenGoalDescription { get; set; }
    public bool? KaizenGoalAchieved { get; set; }
}
```

**Implementation Notes:**
- Endpoint lives in `BudgetExperiment.Api` as a minimal endpoint or controller action
- Service layer: `IKaizenDashboardService.GetDashboardAsync(userId, weeks)` in `BudgetExperiment.Application`
- Aggregation: Group `Transaction`s by week, sum by effective `KakeiboCategory`, and join with `KaizenGoal` for the week
- Effective Kakeibo category = `Transaction.KakeiboOverride ?? BudgetCategory.KakeiboCategory`

---

## UI Changes

**New Route:** `/reports/kaizen-dashboard`

**UI Components:**
- New Blazor component: `KaizenDashboardView.razor`
  - Fetches from `GET /api/v1/reports/kaizen-dashboard?weeks=12`
  - Renders a stacked area chart with four lines (Essentials, Wants, Culture, Unexpected)
  - Each week displayed as a column on the x-axis with label
  - Month boundary markers (subtle background color or vertical divider)
  - Kaizen outcome badge (✓ green or ✗ gray) overlaid on the week column
  - Hover tooltip shows week date range and category breakdown
  - Optional: small legend identifying the four Kakeibo categories and their colors

**Reports Dashboard Tile:**
- Add a new tile on `/reports` landing page linking to this dashboard
- Title: "Kaizen Dashboard"
- Brief description: "12-week spending trend by Kakeibo category with micro-goal outcomes"

---

## Acceptance Criteria

- [ ] Feature flag `Features:Kaizen:Dashboard` is defined in `FeatureFlags` table with default `false`
- [ ] API endpoint `GET /api/v1/reports/kaizen-dashboard?weeks=12` returns correct weekly aggregations grouped by Kakeibo category
- [ ] Endpoint correctly joins `Transaction` data with `KaizenGoal` to include goal description and achieved status
- [ ] Effective Kakeibo category is resolved server-side (override checked first, then category default)
- [ ] UI renders stacked area chart with four distinct colors for each Kakeibo bucket
- [ ] Month boundaries are visually marked on the chart
- [ ] Kaizen outcome badges (✓/✗) appear on each week column
- [ ] Weeks are labeled with clear identifiers (e.g., week number or date range)
- [ ] Hover tooltips display week details (date, category breakdown)
- [ ] Report tile on `/reports` links to the dashboard
- [ ] Feature flag controls visibility — when disabled, route returns 404 or redirects
- [ ] All unit and integration tests pass; OpenAPI spec is updated

---

## Implementation Notes

- **Aggregation Performance:** Weekly grouping requires scanning all transactions in the user's date range. Cache the result for 1 hour using `IMemoryCache` keyed by `userId:weeks` to avoid repeated aggregation.
- **Timezone Handling:** `WeekStart` should use the user's local calendar week (starting Monday or Sunday depending on locale/preference). For now, assume Monday-based ISO 8601 weeks.
- **Kaizen Goal Null Handling:** If no goal exists for a week, `KaizenGoalDescription` and `KaizenGoalAchieved` are `null`. The UI displays the week without a badge.
- **Color Scheme:** Use consistent colors across all Kakeibo visualizations:
  - Essentials: blue
  - Wants: green
  - Culture: purple
  - Unexpected: orange/red
- **Chart Library:** Use the existing chart library from `BudgetExperiment.Client` (e.g., Chart.js via Blazor wrapper or similar) to maintain consistency with other reports.

