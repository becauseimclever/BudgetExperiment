# Feature 134: Calendar — Kakeibo Enhancements

> **Status:** Done

## Prerequisites

Feature 129b (Feature Flag Implementation) must be merged before implementation begins.

Feature 131 (Budget Categories — Kakeibo Category Routing) must be merged before implementation begins.

Feature 132 (Transaction Entry — Kakeibo Selector) should be merged or in progress before implementation begins.

Feature 135 (Monthly Reflection Panel) should be complete before the savings progress bar is implemented.

## Feature Flags

**Flag 1: `Features:Calendar:SpendingHeatmap`**
- **Default:** `true`
- **Controls:** Spending intensity tinting on day cells (green/amber/red backgrounds).
- **UX:** Toggle in calendar toolbar enables/disables heatmap overlay.

**Flag 2: `Features:Kakeibo:CalendarOverlay`**
- **Default:** `false` (during development), `true` (when shipped)
- **Controls:** Kakeibo badges and category breakdown display on calendar (day cell badges, week summary bars).
- **Rationale:** Allows gradual rollout and testing of Kakeibo UI before full visibility.

**Flag 3: `Features:Kakeibo:MonthlyReflectionPrompts`**
- **Default:** `true`
- **Controls:** Month-start intention prompt and month-end reflection accessibility from calendar header.
- **Note:** Shared flag with Feature 135 (Monthly Reflection Panel).

## Overview

This feature adds six Kakeibo-aware enhancements to the calendar, transforming it from a transaction display surface into a mindful budgeting ritual hub:

1. **Spending Heatmap Overlay** — day cells tinted by intensity (green = low, amber = moderate, red = high).
2. **Month Header Savings Progress Bar** — shows progress toward `MonthlyReflection.SavingsGoal`.
3. **Month-Start Intention Prompt** — modal asking "What do you want to save this month?"
4. **Week Summary Kakeibo Breakdown** — mini horizontal bars per Kakeibo category in week header.
5. **Day Cell Kakeibo Badge** — small icon showing predominant Kakeibo category for the day.
6. **Month Header Reflection Link** — quick access to end-of-month reflection panel (Feature 135).

## Problem Statement

### Current State

- The calendar is a capable transaction view but philosophically neutral — it displays facts without inviting reflection.
- Spending patterns are invisible at a glance; users must examine transaction lists to see whether a day was light or heavy.
- Week and month boundaries are administrative (calendar grid) but not ritual boundaries — there is no prompt to review weekly spending or set monthly intentions.
- The four Kakeibo questions are never asked within the calendar experience.

### Target State

- The calendar gains visual cues that prompt reflection:
  - Heatmap tinting immediately signals spending intensity.
  - Kakeibo badges and breakdown bars surface the *nature* of spending (Essentials vs. Wants).
  - Month header includes a savings progress bar and "set intention" prompt.
  - Week summaries show spending by Kakeibo category, inviting weekly micro-reviews.
- Users can access the monthly reflection panel from the calendar month header to journal about the month.
- Visual signals are subtle and non-alarming (no harsh reds; no gamification; no guilt language).

## Domain Model Changes

**No new domain entities or fields required.**

All enhancements compute from existing data:
- `KakeiboCategory` field on `BudgetCategory` (Feature 131).
- `KakeiboOverride` on `Transaction` (Feature 132).
- `MonthlyReflection` entity (Feature 135).
- Computed "effective Kakeibo category" per transaction.

**Service additions:**

A new `KakeiboCalendarService` (Application layer) aggregates spending by Kakeibo category for week/month views:

```csharp
public interface IKakeiboCalendarService
{
    Task<KakeiboBreakdown> GetMonthBreakdownAsync(int year, int month, Guid userId);
    Task<KakeiboBreakdown> GetWeekBreakdownAsync(DateOnly weekStart, Guid userId);
    Task<KakeiboCategory> GetDominantCategoryAsync(DateOnly date, Guid userId);
}

public class KakeiboBreakdown
{
    public decimal EssentialsAmount { get; set; }
    public decimal WantsAmount { get; set; }
    public decimal CultureAmount { get; set; }
    public decimal UnexpectedAmount { get; set; }
    
    public decimal TotalSpend => EssentialsAmount + WantsAmount + CultureAmount + UnexpectedAmount;
}
```

## API Changes

### Calendar Grid Endpoint Enhancement

**Existing endpoint:** `GET /api/v1/calendar/month/{year}/{month}`

**Response enhancement:**

Add `KakeiboBreakdown` and savings progress to month response:

```csharp
public class MonthCalendarResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<WeekResponse> Weeks { get; set; }
    
    // NEW fields:
    public KakeiboBreakdownResponse Breakdown { get; set; }
    public SavingsProgressResponse SavingsProgress { get; set; }
}

public class KakeiboBreakdownResponse
{
    public decimal Essentials { get; set; }
    public decimal Wants { get; set; }
    public decimal Culture { get; set; }
    public decimal Unexpected { get; set; }
    public decimal Total { get; set; }
}

public class SavingsProgressResponse
{
    public decimal SavingsGoal { get; set; }
    public decimal ActualSavings { get; set; }
    public decimal Remaining { get; set; }
    public int ProgressPercentage { get; set; }  // 0-100
}
```

### Week Summary Endpoint Enhancement

**Existing endpoint (likely):** `GET /api/v1/calendar/week/{weekStart}`

**Response enhancement:**

```csharp
public class WeekResponse
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public List<DayResponse> Days { get; set; }
    
    // NEW fields:
    public KakeiboBreakdownResponse Breakdown { get; set; }
    public Guid? KaizenGoalId { get; set; }  // Link to Feature 136 (optional, for reference)
}
```

### Day Detail Endpoint Enhancement

**Existing endpoint (likely):** `GET /api/v1/calendar/day/{date}`

**Response enhancement:**

```csharp
public class DayResponse
{
    public DateOnly Date { get; set; }
    public decimal TotalSpend { get; set; }
    public List<TransactionResponse> Transactions { get; set; }
    
    // NEW fields:
    public decimal? DailyAverage { get; set; }  // User's average daily spend (for heatmap threshold)
    public KakeiboCategory DominantCategory { get; set; }  // Predominant Kakeibo category
    public KakeiboCategory? DominantCategoryForBadge { get; set; }  // Null if no transactions
}
```

### New Endpoint: Spending Heatmap Data

**Optional, if heatmap calculation is expensive:**

`GET /api/v1/calendar/heatmap/{year}/{month}`

Returns:

```csharp
public class HeatmapDataResponse
{
    public decimal DailyAverageSpend { get; set; }
    public Dictionary<int, HeatmapDay> Days { get; set; }  // day -> intensity
}

public class HeatmapDay
{
    public decimal Spend { get; set; }
    public HeatmapIntensity Intensity { get; set; }  // Low, Moderate, High
}

public enum HeatmapIntensity
{
    None = 0,
    Low = 1,
    Moderate = 2,
    High = 3
}
```

## UI Changes

### 1. Spending Heatmap Overlay

**Component affected:** `CalendarGrid.razor` (day cell rendering)

**Changes:**

- Compute daily average spend from transactions (can be memoized).
- For each day, calculate spend intensity:
  - **None:** $0
  - **Low:** 0 to 50% of daily average
  - **Moderate:** 50% to 100% of daily average
  - **High:** >100% of daily average
- Apply subtle CSS class to day cell: `heatmap-none`, `heatmap-low`, `heatmap-moderate`, `heatmap-high`.
- Heatmap CSS (use existing design system colours, no new styles):
  - None: transparent or very pale grey
  - Low: pale green (#e6f4e6 or similar from tokens)
  - Moderate: soft amber (#fef3c7 or similar)
  - High: soft red (#fee2e2 or similar)
- **Toggle control:** Add a checkbox or icon button to calendar toolbar: "Spending Heatmap" toggle.
- **Persistence:** Save toggle state to `UserSettings.ShowSpendingHeatmap`.
- **Feature flag gating:** Only show toggle if `Features:Calendar:SpendingHeatmap` is enabled.

**Styling note:** Tinting should be subtle (low opacity overlay, no border changes). Days with transactions should still show the transaction indicator/color. Heatmap is a background tint, not a replacement.

### 2. Month Header Savings Progress Bar

**Component affected:** Month header area (above calendar grid)

**Changes:**

- Query `MonthlyReflection` for the current month.
- If exists and has `SavingsGoal`, display a horizontal progress bar:
  - Label: "Savings Goal"
  - Bar: visual representation of `ActualSavings / SavingsGoal` (e.g., 0–100%).
  - Underneath: "Saved $X of $Y goal" text.
  - Colour: green if on track, amber if slightly behind, soft red if significantly behind.
- If no goal is set, show a CTA: "Set a savings goal for this month" (navigates to month intention prompt).
- **Feature flag gating:** Only show if `Features:Kakeibo:MonthlyReflectionPrompts` is enabled.

### 3. Month-Start Intention Prompt

**Component affected:** Calendar view (month header or as non-blocking modal)

**Trigger:**

- User navigates to a month that has no `MonthlyReflection` record yet (typically the current month on first visit).
- Only show if `Features:Kakeibo:MonthlyReflectionPrompts` is enabled.

**UI:**

- Non-blocking modal or inline prompt (not full-screen dialog):
  - Title: "What do you want to save this month?"
  - Input field: savings goal amount (with currency symbol, default to previous month's goal if available).
  - Optional text field: "Any intention for this month?" (free text, 280 chars max, placeholder: "e.g., save for a vacation, reduce dining").
  - Buttons: "Set Goal" (save and dismiss), "Maybe Later" (dismiss without saving).
- Dismissal is permissive — users are not forced to set a goal.
- Prompt can be re-accessed via "Edit Goal" link in month header if dismissed.

**Backend:** POST to `/api/v1/reflections/month/{year}/{month}` with savings goal and intention text; creates or updates `MonthlyReflection`.

### 4. Week Summary Kakeibo Breakdown

**Component affected:** Week summary row (existing `WeekSummary.razor`)

**Changes:**

- Below the week date range (e.g., "Mon 2026-04-13 — Sun 2026-04-19"), add a horizontal **Kakeibo breakdown bar**:
  - Four coloured segments, one per Kakeibo category (Essentials, Wants, Culture, Unexpected).
  - Segment width proportional to spending in that category (as % of total week spend).
  - Segment colours: consistent with design system tokens (e.g., Essentials = green, Wants = amber, Culture = blue, Unexpected = red).
  - Segment labels: show amount (e.g., "Essentials: $150").
  - **Interaction:** Clicking a segment filters the week's day cells to highlight only that category's transactions (existing filtering capability, reused).
  - **Feature flag gating:** Only show if `Features:Kakeibo:CalendarOverlay` is enabled.

**Implementation:**

- Fetch week breakdown via `GET /api/v1/calendar/week/{startDate}` (response includes `KakeiboBreakdownResponse`).
- Render as a stacked bar chart (can reuse existing chart components or a simple div-based bar).

### 5. Day Cell Kakeibo Badge

**Component affected:** Day cell rendering in calendar grid

**Changes:**

- If a day has transactions, compute the **predominant Kakeibo category** (category with highest total spend for that day).
- Display a small icon or initials badge in the day cell (top-right or bottom-right corner):
  - Icon representing the category (e.g., grocery bag = Essentials, gift = Wants, book = Culture, lightning = Unexpected).
  - Colour matching the category (same tokens as breakdown bar).
  - Tooltip on hover: "Mostly [Category Name] today: $X"
- If multiple categories are tied or no transactions exist, hide the badge.
- **Feature flag gating:** Only show if `Features:Kakeibo:CalendarOverlay` is enabled.

**Implementation:**

- Add `DominantCategoryForBadge` to day response (API enhancement above).
- Render badge in day cell template conditionally.

### 6. Month Header Reflection Link

**Component affected:** Month header area

**Changes:**

- For past months (month < current month), add a "Reflection" link or button to the month header.
- Clicking navigates to the **Monthly Reflection Panel** (Feature 135) for that month.
- For current month, link is available but disabled if month is not yet complete (or shows "Start Reflection" to encourage early journaling).
- **Feature flag gating:** Only show if `Features:Kakeibo:MonthlyReflectionPrompts` is enabled.

**Styling:** Subtle link (not prominent button), so it doesn't dominate the header.

## Feature Flags

**Three flags, as noted above.**

All are checked at render time (Blazor) and in API responses (for consistency).

## Acceptance Criteria

- [ ] `KakeiboCalendarService` implements aggregation methods for month and week breakdowns.
- [ ] Calendar month response includes `KakeiboBreakdownResponse` and `SavingsProgressResponse`.
- [ ] Calendar week response includes `KakeiboBreakdownResponse`.
- [ ] Day response includes `DominantCategory` and `DailyAverageSpend`.
- [ ] Spending heatmap is computed and applied to day cells as CSS classes (`heatmap-low/moderate/high`).
- [ ] Heatmap toggle is present in calendar toolbar and state is persisted to `UserSettings`.
- [ ] Heatmap toggle is only visible if feature flag `Features:Calendar:SpendingHeatmap` is enabled.
- [ ] Month-start intention prompt appears for months with no `MonthlyReflection`.
- [ ] Prompt allows user to set savings goal and optional intention text.
- [ ] Prompt is non-blocking and dismissible.
- [ ] Month header displays savings progress bar if goal exists.
- [ ] Progress bar shows "Saved $X of $Y goal" text and appropriate colour.
- [ ] Kakeibo breakdown bar renders in week summary with four coloured segments.
- [ ] Segment widths reflect spending proportions.
- [ ] Clicking a breakdown segment filters day cells to that category (existing filtering reused).
- [ ] Day cells show Kakeibo badge for predominant category (small icon in corner).
- [ ] Badge tooltip shows category name and amount.
- [ ] Month header shows "Reflection" link for past months (navigates to Feature 135 panel).
- [ ] All Kakeibo elements are gated by appropriate feature flags and hidden when disabled.
- [ ] All existing calendar tests pass; new tests for aggregation service, API responses, and UI rendering.
- [ ] Accessibility: ARIA labels on breakdown bars, badges, links; keyboard navigation for toggle and links.

## Implementation Order

1. **Create `KakeiboCalendarService`** (Application layer) with aggregation methods.
2. **Enhance calendar API endpoints** to return Kakeibo breakdowns and savings progress.
3. **Implement heatmap calculation and CSS styling.**
4. **Add heatmap toggle to calendar toolbar** (Blazor UI).
5. **Create month-start intention prompt component** and integrate into month header.
6. **Implement week summary Kakeibo breakdown bar** component.
7. **Add day cell Kakeibo badge** to day cell rendering.
8. **Add month header reflection link** (disabled until Feature 135 is available).
9. **Implement all feature flag checks** in components and API.
10. **Add tests** for aggregation service, API responses, component rendering, feature flag gating, and end-to-end calendar flows.

**Dependencies:** 
- Feature 131 (KakeiboCategory on categories).
- Feature 132 (KakeiboOverride on transactions, effective category computation).
- Feature 135 (MonthlyReflection entity and reflection panel component).
- Feature 129b (feature flag infrastructure).

**Related:** Feature 136 (Kaizen Micro-Goals) will integrate week summary with goal progress; coordinate data fetching to avoid redundant API calls.
