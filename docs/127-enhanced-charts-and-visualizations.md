# Feature 127: Enhanced Charts & Visualizations
> **Status:** Planning  
> **Priority:** Medium  
> **Effort:** Large (new chart types, potential library migration, theming overhaul, data service layer)

## Overview

Upgrade the charting system from hand-rolled SVG components to a professional-quality chart library, add new chart types that deliver actionable budgeting insights, and unify the visual language with the existing multi-theme design system.

## Problem Statement

### Current State

The application renders charts using **custom, hand-rolled SVG components** written entirely in Blazor (`Components/Charts/`). There is **no external charting library** — every bar, line, donut, and gauge is computed and drawn via inline SVG with C# code-behind logic.

**Strengths of the current approach:**
- Zero external dependencies — no JS interop, no npm packages, no CDN calls
- Full control over rendering; all charts are pure Blazor components
- CSS custom properties integrated with the 9-theme design system (light, dark, accessible, crayons, geocities, macOS, monopoly, vscode-dark, win95)
- Strong accessibility: ARIA labels, keyboard navigation, `role="img"`, `tabindex` on interactive elements
- 100% bUnit test coverage for all 11 chart components + 3 shared infrastructure components
- Small bundle size — ideal for Raspberry Pi deployment

**Weaknesses (why this feature is needed):**
- **Limited chart types.** Only basic types exist: bar, donut, line, area, stacked bar, grouped bar, sparkline, radial gauge, progress bar. No treemap, waterfall, heatmap, scatter, sunburst, or sankey — all of which provide budgeting insights the current types cannot.
- **Basic interactivity.** Tooltips are simple div overlays positioned statically. No zoom, pan, drill-down, crosshair, or data-point selection.
- **No animations.** Charts render instantly with no transitions. Loading, updating, and hover states lack visual polish.
- **Manual SVG math.** Each chart reimplements coordinate mapping, tick calculation, axis rendering, and tooltip logic. Adding a new chart type requires 200–400 lines of bespoke SVG geometry code plus a corresponding CSS file.
- **Limited data transformation.** Data is transformed inline in report pages before being passed to chart components. No shared data service layer for aggregation, moving averages, or cross-chart filtering.
- **Visual quality gap.** Compared to modern chart libraries (ApexCharts, Chart.js), the hand-drawn SVGs lack polish: no rounded line joins, no smooth curve interpolation (beyond basic), no gradient fills on all chart types, no shadow/glow effects.

### Target State

- A curated set of **15+ chart types** covering all common budgeting visualizations
- **Professional visual quality** — smooth animations, responsive sizing, rich tooltips with data context
- **Unified theming** — charts automatically adapt to all 9 existing themes via CSS custom properties
- **Testable data layer** — chart data preparation isolated into service classes with full unit test coverage
- **Minimal bundle impact** — the chart library must not add more than ~200 KB gzipped to the WASM payload
- **No regression** — all existing chart usages (reports, calendar insights, showcase page) continue to function

---

## Current Chart Inventory

### Chart Components (`src/BudgetExperiment.Client/Components/Charts/`)

| Component | Type | In Use | Where Used | Tests |
|-----------|------|--------|------------|-------|
| `BarChart` | Vertical bar | ✓ Active | MonthlyTrendsReport, BudgetComparisonReport | ✓ |
| `DonutChart` | Donut/pie | ✓ Active | MonthlyCategoriesReport, CalendarInsightsPanel, ComponentShowcase | ✓ |
| `LineChart` | Multi-series line | ○ Showcase only | ComponentShowcase | ✓ |
| `AreaChart` | Filled line (wraps LineChart) | ○ Not used | — | ✓ |
| `GroupedBarChart` | Side-by-side bars | ○ Not used | — | ✓ |
| `StackedBarChart` | Stacked vertical bars | ○ Not used | — | ✓ |
| `SparkLine` | Inline mini trend | ○ Showcase only | ComponentShowcase | ✓ |
| `RadialGauge` | Circular progress | ○ Showcase only | ComponentShowcase | ✓ |
| `ProgressBar` | Horizontal bar | ✓ Active | Various display components | ✓ |
| `ChartLegend` | Reusable legend | ✓ Internal | Used by DonutChart | ✓ |

### Shared Infrastructure (`Components/Charts/Shared/`)

| Component | Purpose | Tests |
|-----------|---------|-------|
| `ChartAxis` | SVG axis tick/label renderer (X and Y) | ✓ |
| `ChartGrid` | SVG horizontal/vertical grid lines | ✓ |
| `ChartTooltip` | Reusable tooltip overlay | ✓ |
| `ChartTick` | Tick data model (Position + Label) | ✓ |

### Non-Chart Visualization

| Component | Location | Notes |
|-----------|----------|-------|
| `ChoroplethMap` | `Components/Reports/` | SVG US state map — geographic visualization, not a chart. Separate concern. |

### Supporting Types

`BarChartClickInfo`, `BarChartGroup`, `BarChartSeries`, `BarChartValue`, `BarSeriesDefinition`, `DonutSegmentData`, `GroupedBarData`, `LineData`, `LineSeriesDefinition`, `ReferenceLine`, `ThresholdColor`

### Rendering Approach

All charts use **pure inline SVG** with:
- `viewBox`-based responsive sizing (`preserveAspectRatio="xMidYMid meet"`)
- C# code-behind for geometry calculation (coordinate mapping, tick computation, path generation)
- CSS custom properties for colors (e.g., `var(--chart-grid-color)`, `var(--color-income)`, `var(--color-expense)`)
- Hover/focus event handlers for tooltips (Blazor `@onmouseenter`/`@onfocusin`)
- `CultureInfo.InvariantCulture` for SVG numeric formatting; `CultureInfo.CurrentCulture` for display values

---

## Library Recommendation

### Decision: **Blazor-ApexCharts** (via `Blazor-ApexCharts` NuGet package)

#### Evaluated Options

| Library | Pros | Cons | Verdict |
|---------|------|------|---------|
| **Blazor-ApexCharts** (thirstyape) | Beautiful defaults, 20+ chart types (treemap, heatmap, radar, candlestick, radialBar, etc.), actively maintained, strong Blazor WASM support, ~80 KB gzipped JS, built-in responsiveness, animation, zoom/pan, dark mode, MIT license | Depends on ApexCharts.js via JS interop; NuGet package wraps JS library | ✅ **Recommended** |
| **Chart.js via ChartJs.Blazor** | Most popular JS charting library, huge ecosystem | Blazor wrapper (`ChartJs.Blazor`) is stale (last update 2022), not actively maintained for .NET 8+. `PSC.Blazor.Components.ChartJs` is more active but less mature. Chart.js v4 bundles are larger. | ❌ Wrapper maintenance risk |
| **Plotly.NET** | Scientific charts, extremely powerful | Heavyweight (~3 MB), designed for data science, overkill for a budgeting app, poor Blazor WASM integration | ❌ Too heavy |
| **D3.js via JS interop** | Ultimate flexibility, any chart type possible | Requires significant custom JS, effectively building a chart library from scratch, huge maintenance burden | ❌ Too much custom JS |
| **Radzen Blazor Charts** | Native Blazor, no JS interop | Tied to Radzen component library, limited chart types, styling conflicts with custom design system | ❌ Component library dependency |
| **Keep hand-rolled SVG** | Zero dependencies, full control, existing test suite | Every new chart type is 200–400 lines of geometry code; interactivity is limited; no animations; visual quality gap grows with each new type | ❌ Does not scale |

#### Rationale for Blazor-ApexCharts

1. **Chart variety.** ApexCharts supports all chart types needed for budgeting: bar, line, area, donut/pie, treemap, heatmap, radar, radialBar, scatter, candlestick, boxPlot, and range bar. This covers every new type proposed in this feature.
2. **Visual quality.** ApexCharts ships with smooth animations, gradient fills, responsive legends, rich tooltips with formatted data, and zoom/pan — all out of the box.
3. **Blazor-native API.** The `Blazor-ApexCharts` wrapper provides `<ApexChart>` and `<ApexPointSeries>` Razor components with strongly-typed C# configuration. No manual JS interop needed.
4. **Theming.** ApexCharts supports programmatic theming. Colors can be bound to CSS custom properties, enabling integration with the existing 9-theme design system.
5. **Bundle size.** ApexCharts.js is ~80 KB gzipped — well within the 200 KB budget for a Raspberry Pi deployment.
6. **Active maintenance.** The `Blazor-ApexCharts` NuGet package (by joamamrgn/thirstyape) is actively maintained with regular releases for latest .NET versions.
7. **MIT license.** Compatible with the project's license.

#### Risk Mitigation

- **JS interop latency:** ApexCharts uses JS interop under the hood. For Blazor WASM (in-browser), this is a local call — not cross-process. Latency is negligible.
- **.NET 10 compatibility:** Verify package compatibility early in Slice 1. If the NuGet package hasn't released a .NET 10 build, the `net9.0`/`net8.0` package may still work (Blazor WASM targets are forward-compatible).
- **Bundle size validation:** Measure actual gzipped payload after integration; fail-fast in Slice 1 if >200 KB.

---

## Migration Strategy

### Approach: Parallel Introduction → Gradual Replacement

The migration does NOT require a big-bang replacement. Existing hand-rolled SVG charts and ApexCharts can coexist because they're independent Blazor components.

**Phase 1: Foundation (Slices 1–2)**
- Add `Blazor-ApexCharts` NuGet package to `BudgetExperiment.Client`
- Create a `ChartThemeService` that bridges CSS custom properties → ApexCharts theme configuration
- Build first new chart type (treemap) with ApexCharts to validate integration
- Existing SVG charts remain untouched — zero regression risk

**Phase 2: New Chart Types (Slices 3–6)**
- Implement all new chart types (heatmap, waterfall, scatter, sunburst/treemap drill-down, etc.) using ApexCharts
- New chart types live in `Components/Charts/` alongside existing SVG components

**Phase 3: Replace Existing Charts (Slices 7–8)**
- Migrate `BarChart`, `DonutChart`, `LineChart`, etc. to ApexCharts equivalents one at a time
- Each migration is a single PR: swap the component reference in the parent page, update parameters, verify visually, update bUnit tests
- **Old SVG components are NOT deleted** until all consumers are migrated and tests pass
- The `ComponentShowcase` page should show both old and new during migration for visual comparison

**Phase 4: Cleanup (Slice 9)**
- Remove legacy SVG chart components after all consumers migrated
- Remove supporting types (`BarChartGroup`, `BarChartSeries`, etc.) that are no longer referenced
- Update `ComponentShowcase` to show only ApexCharts versions

### Migration Checklist Per Chart

For each existing chart replaced:
- [ ] Create ApexCharts equivalent component with same parameter interface (or adapter)
- [ ] Update parent page/component to use new component
- [ ] Verify visual parity (manual + screenshot comparison)
- [ ] Update or replace bUnit tests
- [ ] Verify accessibility (ARIA labels, keyboard navigation)
- [ ] Verify all 9 themes render correctly
- [ ] Remove old component (only after all consumers migrated)

---

## New Chart Types

### 1. Treemap — Hierarchical Spending Breakdown

**Budgeting question:** "Where does my money go? Which categories and subcategories consume the most?"

**Description:** Nested rectangles sized proportionally to spending amounts. Top-level rectangles represent categories; inner rectangles represent subcategories or merchants. Color intensity encodes spending relative to budget.

**Data requirements:**
- Category hierarchy (parent category → subcategories)
- Spending amount per category/subcategory for selected period
- Optional: budget amount per category for color coding (over/under budget)

**ApexCharts type:** `treemap`

---

### 2. Heatmap — Spending Patterns by Day/Time

**Budgeting question:** "When do I spend the most? Are there day-of-week or time-of-month patterns?"

**Description:** Grid of colored cells where rows = days of week (Mon–Sun), columns = weeks or hours. Cell color intensity represents total spending. Reveals habitual spending patterns (e.g., "I overspend on weekends" or "big expenses cluster around the 1st and 15th").

**Data requirements:**
- Transaction date and amount
- Aggregation: sum of spending per day-of-week × week-of-month (or hour-of-day)

**ApexCharts type:** `heatmap`

---

### 3. Waterfall Chart — Budget vs. Actual Monthly Flow

**Budgeting question:** "Starting from my income, how does each spending category consume my budget, and what's left?"

**Description:** Sequential bars that "walk" from income down through each spending category, showing the cumulative effect. Income bars go up (green), spending bars go down (red), and the final bar shows net remaining. Visually demonstrates the flow of money.

**Data requirements:**
- Total income for period
- Total spending per category for period (sorted by amount or user preference)
- Net remaining (calculated)

**ApexCharts type:** `rangeBar` (configured as waterfall) or custom via `bar` with calculated ranges

---

### 4. Scatter Plot — Transaction Anomaly Detection

**Budgeting question:** "Which transactions are unusual? Are there outliers I should investigate?"

**Description:** Each transaction plotted as a dot: X-axis = date, Y-axis = amount. Dot color encodes category. Outliers (transactions far from the category mean) are immediately visible. A reference line shows the average or median for the selected category.

**Data requirements:**
- Transaction date, amount, and category
- Optional: calculated mean/median per category for reference lines

**ApexCharts type:** `scatter`

---

### 5. Radar/Spider Chart — Category Budget Utilization

**Budgeting question:** "Am I balanced? Which budget categories am I over/under-utilizing?"

**Description:** Multi-axis chart where each axis represents a budget category. The shaded area shows actual spending as a percentage of budget for each category. A perfectly balanced month would show a regular polygon. Asymmetry reveals imbalance.

**Data requirements:**
- Budget amount per category
- Actual spending per category for selected period
- Percentage utilization (actual/budget × 100) per category

**ApexCharts type:** `radar`

---

### 6. Stacked Area — Spending Composition Over Time

**Budgeting question:** "How has my spending mix changed month over month? Which categories are growing?"

**Description:** Layered area chart where each colored band represents a spending category, stacked to show total spending. The height of each band shows the category's contribution over time. Growing bands indicate rising expenses.

**Data requirements:**
- Monthly spending per category over 6–12 months
- Categories ordered consistently (largest on bottom for readability)

**ApexCharts type:** `area` with `stacked: true`

---

### 7. Candlestick — Account Balance Range

**Budgeting question:** "How volatile is my account balance? What are the highs and lows each month?"

**Description:** Each "candle" represents one month: the body shows opening and closing balance, the wicks show the highest and lowest balance during the month. Green candles = balance increased; red = decreased. Reveals volatility and trends.

**Data requirements:**
- Daily account balance (or at least monthly open/high/low/close)
- Account selection

**ApexCharts type:** `candlestick`

---

### 8. Gauge/Radial Bar — Budget Utilization Dashboard

**Budgeting question:** "What percentage of my budget have I used this month? Am I on track?"

**Description:** Circular progress indicators showing budget utilization percentage per category or overall. Multiple radial bars in a single chart create a compact dashboard view. Color transitions from green (on track) → yellow (>75%) → red (>100%).

**Data requirements:**
- Budget amount and actual spending per category
- Utilization percentage (capped display at 150% to avoid visual confusion)

**ApexCharts type:** `radialBar`

**Note:** The existing hand-rolled `RadialGauge` component covers a simpler version of this. The ApexCharts `radialBar` adds animation, multiple rings, and configurable color transitions.

---

### 9. Box Plot — Spending Distribution by Category

**Budgeting question:** "What's the typical range of my spending in each category? What's normal vs. unusual?"

**Description:** For each category, a box plot shows median, quartiles (Q1/Q3), and whiskers (min/max excluding outliers). Outliers shown as individual dots. Immediately answers "is this month's grocery spending normal?" by showing historical distribution.

**Data requirements:**
- All transactions for a category over 6–12 months
- Statistical summary: min, Q1, median, Q3, max, outliers per category

**ApexCharts type:** `boxPlot`

---

### 10. Sunburst — Drill-Down Category Hierarchy

**Budgeting question:** "How does spending break down within each category? What specific merchants or subcategories drive the totals?"

**Description:** Concentric rings where the inner ring shows top-level categories and outer rings show subcategories and merchants. Click to drill into a category for details. Size of each arc segment is proportional to spending. Similar to treemap but emphasizes hierarchy depth.

**Data requirements:**
- Category hierarchy (category → subcategory → merchant)
- Spending amount at each level

**ApexCharts type:** `treemap` with hierarchical data (ApexCharts doesn't have native sunburst, but treemap with drill-down achieves the same insight). Alternatively, implement as a custom SVG component or evaluate a sunburst-specific micro-library.

---

## Visual Improvements to Existing Charts

### Theming Integration

- **ApexCharts theme bridge:** Create a `ChartThemeService` that reads CSS custom properties (`--color-brand-primary`, `--color-income`, `--color-expense`, etc.) and generates an `ApexChartOptions.Theme` configuration
- **Automatic theme switching:** Subscribe to theme change events; re-render charts with updated colors when the user switches themes
- **All 9 themes supported:** light, dark, accessible, crayons, geocities, macOS, monopoly, vscode-dark, win95

### Animation & Transitions

- **Load animation:** Charts animate in on first render (bars grow, lines draw, donut segments fan out)
- **Update animation:** When data changes (e.g., date range filter), charts smoothly transition rather than re-rendering
- **Hover effects:** Enhanced tooltip with category color swatch, formatted amount, percentage, and transaction count

### Responsiveness

- **Container-based sizing:** Charts resize with their container (ApexCharts has built-in `responsive` breakpoints)
- **Mobile optimization:** Simplified tooltips, larger touch targets, horizontal scrolling for wide charts
- **Print-friendly:** Charts render static (no animation) when `@media print` is active

### Interactivity

- **Zoom/Pan:** Enable on time-series charts (line, area, candlestick) for exploring large date ranges
- **Data point selection:** Click a bar/point to filter other charts on the same page (cross-chart filtering)
- **Legend toggle:** Click legend items to show/hide series (built into ApexCharts)
- **Export:** Download chart as PNG or SVG (built into ApexCharts toolbar)

### Color Palette

- **Semantic colors:** Budget charts use `--color-income` (green), `--color-expense` (red), `--color-transfer` (blue), `--color-recurring` (purple)
- **Category colors:** Persist user-assigned category colors from the Categories page
- **Colorblind-safe palette:** The `accessible` theme uses a colorblind-safe palette (already exists for the ChoroplethMap; extend to all charts)
- **Consistent gradients:** Where fill gradients are used, follow the same opacity ramp (35% → 5%) established by the current LineChart

### Dark Mode

- **Axis and grid colors** automatically adjust via CSS custom properties (already implemented in current charts; verify ApexCharts respects these)
- **Tooltip background** uses `--color-surface-elevated`
- **Shadow and glow effects** tone down in dark mode to avoid visual noise

---

## Architecture

### Component Structure

```
Components/Charts/
├── Shared/
│   ├── ChartThemeService.cs          # Bridges CSS vars → ApexCharts theme config
│   ├── ChartDataService.cs           # Aggregation, moving averages, statistics
│   ├── ChartColorProvider.cs         # Category color resolution + fallback palette
│   ├── ChartAxis.razor               # (legacy, retained during migration)
│   ├── ChartGrid.razor               # (legacy, retained during migration)
│   ├── ChartTooltip.razor            # (legacy, retained during migration)
│   └── ChartTick.cs                  # (legacy, retained during migration)
├── ApexCharts/
│   ├── BudgetBarChart.razor          # ApexCharts bar (replaces BarChart)
│   ├── BudgetDonutChart.razor        # ApexCharts donut (replaces DonutChart)
│   ├── BudgetLineChart.razor         # ApexCharts line (replaces LineChart)
│   ├── BudgetAreaChart.razor         # ApexCharts area (replaces AreaChart)
│   ├── BudgetTreemap.razor           # NEW: Treemap
│   ├── BudgetHeatmap.razor           # NEW: Heatmap
│   ├── BudgetWaterfall.razor         # NEW: Waterfall
│   ├── BudgetScatter.razor           # NEW: Scatter
│   ├── BudgetRadar.razor             # NEW: Radar/Spider
│   ├── BudgetStackedArea.razor       # NEW: Stacked Area
│   ├── BudgetCandlestick.razor       # NEW: Candlestick
│   ├── BudgetRadialBar.razor         # NEW: Gauge/Radial Bar
│   ├── BudgetBoxPlot.razor           # NEW: Box Plot
│   └── ChartExportButton.razor       # Download as PNG/SVG
├── BarChart.razor                    # (legacy, retained during migration)
├── DonutChart.razor                  # (legacy, retained during migration)
├── LineChart.razor                   # (legacy, retained during migration)
├── ... (other legacy components)
└── Models/
    ├── ChartThemeConfig.cs           # Theme configuration record
    ├── HeatmapDataPoint.cs           # Heatmap cell model
    ├── WaterfallSegment.cs           # Waterfall step model
    ├── CandlestickDataPoint.cs       # OHLC data model
    └── BoxPlotSummary.cs             # Statistical summary model
```

### Data Flow

```
Report Page → ChartDataService → Chart Component → ApexCharts (SVG/Canvas)
     ↓              ↓                    ↓
 Raw DTOs     Aggregated data      Theme config from ChartThemeService
              (moving avg,         Color palette from ChartColorProvider
               statistics,
               pivots)
```

1. **Report pages** fetch raw data from `IBudgetApiService` (already exists)
2. **`ChartDataService`** transforms raw DTOs into chart-ready models (aggregation, pivoting, statistical calculations)
3. **Chart components** receive prepared data and delegate rendering to ApexCharts
4. **`ChartThemeService`** provides theme configuration derived from active CSS custom properties
5. **`ChartColorProvider`** resolves category colors (user-defined or fallback palette)

### Service Layer (Testable)

```csharp
// All chart data preparation logic is unit-testable
public interface IChartDataService
{
    HeatmapDataPoint[][] BuildSpendingHeatmap(IEnumerable<TransactionDto> transactions, HeatmapGrouping grouping);
    WaterfallSegment[] BuildBudgetWaterfall(decimal income, IEnumerable<CategorySpendingDto> spending);
    CandlestickDataPoint[] BuildBalanceCandlesticks(IEnumerable<DailyBalanceDto> balances, CandlestickInterval interval);
    BoxPlotSummary[] BuildCategoryDistributions(IEnumerable<TransactionDto> transactions, int monthsBack);
    // ... additional methods per chart type
}
```

---

## Acceptance Criteria

### Foundation (Slices 1–2)

- **AC-127-01:** `Blazor-ApexCharts` NuGet package is added to `BudgetExperiment.Client.csproj` and builds without errors on .NET 10
- **AC-127-02:** A `ChartThemeService` reads CSS custom properties and generates an `ApexChartOptions` theme configuration matching the active theme
- **AC-127-03:** Charts rendered with ApexCharts respond to theme changes (switching from light to dark updates chart colors within 500ms)
- **AC-127-04:** The ApexCharts JS bundle adds no more than 200 KB gzipped to the published WASM payload
- **AC-127-05:** All existing chart components (`BarChart`, `DonutChart`, `LineChart`, etc.) continue to function without modification

### New Chart Types (Slices 3–6)

- **AC-127-06:** Treemap chart displays spending by category with rectangle sizes proportional to amounts; clicking a category drills into subcategories
- **AC-127-07:** Heatmap chart displays a 7-row (Mon–Sun) × N-column (weeks) grid colored by spending intensity for a selected date range
- **AC-127-08:** Waterfall chart starts at income, subtracts each spending category, and ends at net remaining — correctly handles negative net values
- **AC-127-09:** Scatter plot displays transactions as dots (date × amount), with outliers visually distinct (>2σ from category mean)
- **AC-127-10:** Radar chart displays budget utilization across all active budget categories on a single chart
- **AC-127-11:** Stacked area chart shows category spending composition over 6–12 months with consistent category ordering
- **AC-127-12:** Candlestick chart shows account balance open/high/low/close per month with green (increase) and red (decrease) coloring
- **AC-127-13:** Radial bar chart displays budget utilization percentage for up to 8 categories simultaneously with color transitions (green → yellow → red)
- **AC-127-14:** Box plot chart shows spending distribution (min, Q1, median, Q3, max) per category with outlier dots

### Data Service Layer (Slices 3–6)

- **AC-127-15:** `IChartDataService` is registered in DI and has a concrete implementation in the Client project
- **AC-127-16:** `BuildSpendingHeatmap` correctly aggregates transactions by day-of-week × week-of-month and returns a 7×N matrix
- **AC-127-17:** `BuildBudgetWaterfall` produces segments that sum from income to net remaining with running totals
- **AC-127-18:** `BuildBalanceCandlesticks` correctly computes open/high/low/close from daily balance data
- **AC-127-19:** `BuildCategoryDistributions` calculates correct quartiles and identifies outliers using 1.5×IQR method
- **AC-127-20:** All `IChartDataService` methods have unit tests with edge cases (empty data, single transaction, all same amount)

### Visual & Interactivity (Slice 7)

- **AC-127-21:** All ApexCharts-rendered charts animate on first load (bars grow, lines draw, segments fan out)
- **AC-127-22:** Tooltips display formatted currency values (using `CultureInfo.CurrentCulture`), category name, percentage of total, and transaction count
- **AC-127-23:** Time-series charts (line, area, candlestick) support zoom/pan via mouse drag or pinch gesture
- **AC-127-24:** Legend items are clickable to toggle series visibility on/off
- **AC-127-25:** Charts export to PNG via a toolbar button (ApexCharts built-in)

### Migration (Slice 8)

- **AC-127-26:** `MonthlyTrendsReport` uses ApexCharts bar chart with identical data display to the current SVG version
- **AC-127-27:** `BudgetComparisonReport` uses ApexCharts bar chart with identical data display
- **AC-127-28:** `MonthlyCategoriesReport` uses ApexCharts donut chart with identical data display
- **AC-127-29:** `CalendarInsightsPanel` uses ApexCharts donut chart in compact mode
- **AC-127-30:** `ComponentShowcase` page displays all new and migrated chart types

### Accessibility & Theme Compliance (Cross-cutting)

- **AC-127-31:** All ApexCharts-rendered charts include ARIA labels matching current accessibility standard
- **AC-127-32:** All 9 themes (light, dark, accessible, crayons, geocities, macOS, monopoly, vscode-dark, win95) render charts with correct colors — verified by manual inspection
- **AC-127-33:** The `accessible` theme uses a colorblind-safe palette for all chart types (not just ChoroplethMap)
- **AC-127-34:** Charts are keyboard-navigable (tab between data points, Enter to select)

### Cleanup (Slice 9)

- **AC-127-35:** All legacy SVG chart components removed from `Components/Charts/` after migration is verified complete
- **AC-127-36:** No dead code or unused supporting types remain (`BarChartGroup`, `BarChartSeries`, etc.)

---

## Implementation Slices

### Slice 1: Library Integration & Theme Bridge
**Commit:** `feat(charts): add Blazor-ApexCharts + ChartThemeService`
- Add `Blazor-ApexCharts` NuGet package
- Create `ChartThemeService` with JS interop to read CSS custom properties
- Create `ChartColorProvider` for category color resolution
- Add a proof-of-concept chart to `ComponentShowcase` to validate integration
- Verify bundle size impact
- Write unit tests for `ChartThemeService` and `ChartColorProvider`

### Slice 2: Chart Data Service Foundation
**Commit:** `feat(charts): add IChartDataService with core aggregation methods`
- Define `IChartDataService` interface with methods for all planned chart types
- Implement `ChartDataService` with aggregation, pivoting, and statistical calculation
- Register in DI
- Write comprehensive unit tests (edge cases, empty data, single item, large datasets)

### Slice 3: Treemap & Heatmap
**Commit:** `feat(charts): add treemap and heatmap chart types`
- `BudgetTreemap` component with category hierarchy drill-down
- `BudgetHeatmap` component with day-of-week × week grid
- Add to `ComponentShowcase`
- Unit tests for data preparation + bUnit tests for rendering

### Slice 4: Waterfall & Scatter
**Commit:** `feat(charts): add waterfall and scatter chart types`
- `BudgetWaterfall` component with income → spending → net flow
- `BudgetScatter` component with outlier highlighting
- Add to `ComponentShowcase`
- Unit tests for data preparation + bUnit tests for rendering

### Slice 5: Radar, Stacked Area & Radial Bar
**Commit:** `feat(charts): add radar, stacked area, and radial bar chart types`
- `BudgetRadar` component for budget utilization
- `BudgetStackedArea` component for spending composition over time
- `BudgetRadialBar` component for budget gauge dashboard
- Add to `ComponentShowcase`
- Unit tests + bUnit tests

### Slice 6: Candlestick & Box Plot
**Commit:** `feat(charts): add candlestick and box plot chart types`
- `BudgetCandlestick` component for account balance range
- `BudgetBoxPlot` component for spending distribution
- Add to `ComponentShowcase`
- Unit tests + bUnit tests

### Slice 7: Visual Polish & Interactivity
**Commit:** `feat(charts): add animations, zoom, export, and enhanced tooltips`
- Enable load animations for all ApexCharts components
- Configure zoom/pan for time-series charts
- Implement chart export (PNG/SVG) toolbar button
- Enhance tooltips with formatted values, percentages, and counts
- Verify all 9 themes render correctly

### Slice 8: Migrate Existing Report Charts
**Commit:** `refactor(charts): migrate report pages to ApexCharts`
- Replace `BarChart` usage in `MonthlyTrendsReport` and `BudgetComparisonReport`
- Replace `DonutChart` usage in `MonthlyCategoriesReport` and `CalendarInsightsPanel`
- Update bUnit tests for migrated components
- Visual regression verification per chart

### Slice 9: Legacy Cleanup
**Commit:** `refactor(charts): remove legacy SVG chart components`
- Remove all legacy SVG chart components (`BarChart`, `DonutChart`, `LineChart`, etc.)
- Remove supporting types no longer referenced
- Remove legacy `Shared/ChartAxis`, `ChartGrid`, `ChartTooltip` if no longer used
- Update `_Imports.razor` if needed
- Verify all tests still pass

### Slice 10: Reports Dashboard Enhancement
**Commit:** `feat(reports): add enhanced dashboard with new chart types`
- Create a Reports Dashboard page aggregating multiple chart types
- Add treemap, heatmap, and radar to appropriate report pages
- Cross-chart filtering: clicking a category in one chart filters others
- Responsive layout with chart sizing breakpoints

---

## Open Questions

1. **Sunburst chart:** ApexCharts does not have a native sunburst type. Should we implement sunburst as a custom SVG component (maintaining the project's existing SVG competency) or accept treemap with drill-down as an adequate substitute for hierarchical visualization?

2. **Candlestick data availability:** The candlestick chart requires daily account balance data. Does the app currently track daily balances, or does this require a new `DailyBalance` entity and a nightly snapshot job? If not available, should this chart type be deferred?

3. **Bundle size threshold:** The proposed 200 KB gzipped limit for ApexCharts is an estimate. Should we conduct a proof-of-concept spike (Slice 1) before committing to the full feature, with a go/no-go decision point after bundle size measurement?

4. **Cross-chart filtering UX:** When a user clicks a category in a treemap, should other charts on the same page filter to that category? This requires a shared selection state service. Is this in scope for v1 or a future enhancement?

5. **Chart type priority:** Are all 10 proposed chart types equally valued, or should we prioritize a subset? The heatmap (spending patterns), treemap (category breakdown), and waterfall (budget flow) deliver the most unique budgeting insights not available from existing chart types.
