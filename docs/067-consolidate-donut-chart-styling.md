# Feature 067: Consolidate Donut Chart Styling Across Pages
> **Status:** ✅ Complete  
> **Priority:** Low  
> **Estimated Effort:** Small (< 1 sprint)  
> **Dependencies:** None

## Overview

The `DonutChart` component is used on two pages — the **Calendar Insights Panel** and the **Monthly Categories Report** — with different parameter configurations that produce visually inconsistent charts. The Calendar page uses a thinner ring (`StrokeWidth=20`, radius ≈ 38) giving a clean, proportional donut, while the Reports page uses a much thicker ring (`StrokeWidth=50`, radius ≈ 23) that results in a cramped center hole and a visually heavy appearance.

This feature consolidates the donut chart styling so that both pages use the Calendar page's proportions as the canonical look, while preserving the size and interactivity differences that are appropriate per context (compact on Calendar, full-size with legend on Reports).

## Problem Statement

### Current State

Both pages use the same `DonutChart` component but with divergent configurations:

| Parameter | Calendar Insights Panel | Reports Page |
|-----------|------------------------|--------------|
| `Size` | 120 | 320 |
| `StrokeWidth` | 20 | 50 |
| `Compact` | `true` | `false` (default) |
| `ShowLegend` | `false` | `true` |
| `CenterLabel` | `"Spent"` | `"Total"` |
| `Currency` | Explicitly set | Default (`"USD"`) |
| `OnSegmentClick` | Not set | `HandleSegmentClick` |
| `AriaLabel` | `"Top spending categories"` | Default (`"Spending by category"`) |
| Fallback color | `#6b7280` | `#999999` |
| Data filter | All categories | Only `Amount > 0` |
| Segment ordering | No explicit ordering | `OrderByDescending` by amount |

The key visual discrepancy is caused by `StrokeWidth`. The SVG viewBox is fixed at `100×100`, and the radius formula is `(100 - StrokeWidth) / 2 - 2`. This means:

- **Calendar** (`StrokeWidth=20`): radius = 38 → ring-to-hole ratio looks balanced and clean.
- **Reports** (`StrokeWidth=50`): radius = 23 → ring is extremely thick relative to the tiny center hole, making center text cramped.

Additional minor inconsistencies:
- The Reports page does not pass `Currency`, defaulting to `"USD"` instead of using the report's actual currency.
- Different fallback colors for uncategorized segments (`#6b7280` vs `#999999`).
- Calendar does not filter out zero-amount categories or sort by amount like Reports does.

### Target State

- Both usages share a consistent ring proportion (Calendar page's `StrokeWidth=20` style).
- The Reports page chart is larger (as appropriate for a full-page report) but visually matches the Calendar chart's proportions.
- Common defaults are standardized (fallback color, currency handling, segment ordering/filtering).
- Both build segment lists with the same filtering (exclude zero-amount) and ordering (descending by amount).

---

## User Stories

### Visual Consistency

#### US-067-001: Consistent donut ring proportions
**As a** user  
**I want to** see visually consistent donut charts across all pages  
**So that** the application feels polished and unified

**Acceptance Criteria:**
- [ ] Reports page donut chart uses `StrokeWidth="20"` (matching Calendar)
- [ ] The ring-to-center-hole ratio is identical between Calendar and Reports charts
- [ ] Center text ("Total" / "Spent") remains legible on both sizes

#### US-067-002: Consistent segment data preparation
**As a** user  
**I want to** see the same categories represented consistently on both charts  
**So that** data presentation is predictable across pages

**Acceptance Criteria:**
- [ ] Both usages filter out zero-amount categories
- [ ] Both usages sort segments by amount descending
- [ ] Both usages use the same fallback color for uncategorized segments (`#6b7280`)

#### US-067-003: Correct currency on Reports page chart
**As a** user  
**I want to** see my actual currency in the Reports chart tooltip  
**So that** monetary values are displayed accurately

**Acceptance Criteria:**
- [ ] Reports page passes the report's currency to `DonutChart` instead of relying on the `"USD"` default

---

## Technical Design

### Architecture Changes

No architectural changes. This is a parameter and data-preparation alignment within the existing `DonutChart` component and its two consumers.

### UI Components

#### Affected Files

| File | Change |
|------|--------|
| `Pages/Reports/MonthlyCategoriesReport.razor` | Update `StrokeWidth` from `50` to `20`; pass `Currency` parameter; use consistent fallback color `#6b7280` |
| `Components/Reports/CalendarInsightsPanel.razor` | Filter out zero-amount categories; sort segments by amount descending |

#### DonutChart Parameter Alignment (After)

| Parameter | Calendar Insights Panel | Reports Page |
|-----------|------------------------|--------------|
| `Size` | 120 (compact context) | 320 (full page context) |
| `StrokeWidth` | **20** | **20** |
| `Compact` | `true` | `false` |
| `ShowLegend` | `false` | `true` |
| `CenterLabel` | `"Spent"` | `"Total"` |
| `Currency` | From report | **From report** |
| `OnSegmentClick` | Not set | `HandleSegmentClick` |
| Fallback color | **`#6b7280`** | **`#6b7280`** |
| Data filter | **`Amount > 0`** | `Amount > 0` |
| Segment ordering | **`OrderByDescending`** | `OrderByDescending` |

---

## Implementation Plan

### Phase 1: Align Reports Page DonutChart Parameters ✅

**Objective:** Update the Reports page to use Calendar-style ring proportions and pass correct currency.

**Tasks:**
- [x] Change `StrokeWidth` from `50` to `20` in `MonthlyCategoriesReport.razor`
- [x] Add `Currency` parameter binding to `MonthlyCategoriesReport.razor` DonutChart usage
- [x] Update fallback color in `BuildChartSegments()` from `#999999` to `#6b7280`
- [ ] Visual verification: confirm chart proportions match Calendar page

**Commit:**
```bash
git add .
git commit -m "fix(client): align reports donut chart proportions with calendar

- Change StrokeWidth from 50 to 20 for consistent ring proportions
- Pass Currency parameter instead of defaulting to USD
- Standardize fallback color to #6b7280

Refs: #067"
```

---

### Phase 2: Align Calendar Insights Segment Data Preparation ✅

**Objective:** Ensure Calendar insights panel uses the same data filtering and ordering as Reports.

**Tasks:**
- [x] Filter out zero-amount categories in `CalendarInsightsPanel.GetChartSegments()`
- [x] Sort segments by amount descending in `CalendarInsightsPanel.GetChartSegments()`
- [ ] Verify Calendar insights panel renders correctly with updated data

**Commit:**
```bash
git add .
git commit -m "fix(client): standardize calendar insights chart segment preparation

- Filter out zero-amount categories
- Sort segments by amount descending
- Consistent with Reports page data preparation

Refs: #067"
```

---

### Phase 3: Cleanup & Documentation ✅

**Objective:** Final polish and verify consistency.

**Tasks:**
- [x] Side-by-side visual comparison of both charts
- [x] Verify tooltip currency formatting on Reports page
- [x] Verify accessible labels are correct on both pages
- [x] Update any relevant component documentation

**Commit:**
```bash
git add .
git commit -m "docs(client): document donut chart consolidation

- Verify visual consistency across calendar and reports
- Confirm accessibility labels

Refs: #067"
```

---

## Testing Strategy

### Unit Tests

- [x] `CalendarInsightsPanel` `GetChartSegments()` excludes zero-amount categories
- [x] `CalendarInsightsPanel` `GetChartSegments()` returns segments sorted by amount descending
- [x] `MonthlyCategoriesReport` `BuildChartSegments()` uses `#6b7280` as fallback color

### Manual Testing Checklist

- [ ] Navigate to Calendar page → confirm donut chart appearance unchanged
- [ ] Navigate to Reports → Monthly Categories → confirm donut ring is thinner and center text is legible
- [ ] Hover over segments on Reports page → confirm tooltip shows correct currency
- [ ] Compare both charts side-by-side → proportions match
- [ ] Test with zero-amount categories in data → confirm they are excluded on both pages
- [ ] Test with single category → chart renders correctly on both pages
- [ ] Test with no data → empty state renders correctly on both pages

---

## Security Considerations

None — this is a purely cosmetic/UI-consistency change.

---

## Performance Considerations

None — no new data fetching, computation, or rendering overhead.

---

## Future Enhancements

- Consider extracting chart proportion constants (e.g., default `StrokeWidth`, fallback color) into a shared chart configuration class to prevent future drift.
- Evaluate whether `Size` parameter should drive CSS directly or remain unused (currently the SVG viewBox is fixed at `100×100` and sizing is CSS-driven).

---

## References

- [DonutChart component](../src/BudgetExperiment.Client/Components/Charts/DonutChart.razor)
- [CalendarInsightsPanel](../src/BudgetExperiment.Client/Components/Reports/CalendarInsightsPanel.razor)
- [MonthlyCategoriesReport](../src/BudgetExperiment.Client/Pages/Reports/MonthlyCategoriesReport.razor)
- [Feature 045: UI Component Refactor](./045-ui-component-refactor-and-library.md)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-08 | Initial draft | @copilot |
| 2026-02-08 | Phase 1: Aligned Reports DonutChart (StrokeWidth, Currency, fallback color) | @copilot |
| 2026-02-08 | Phase 2: Standardized Calendar segment filtering and sorting | @copilot |
| 2026-02-08 | Phase 3: Added unit tests, marked complete | @copilot |
