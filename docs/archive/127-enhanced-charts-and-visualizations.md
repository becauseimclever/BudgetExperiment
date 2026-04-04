# Feature 127: Enhanced Charts & Visualizations
> **Status:** Done  
> **Priority:** Medium  
> **Effort:** Large (new chart types, library integration for 2 types, theming overhaul, data service layer)

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

### Decision: **Hybrid Approach** (Self-Implement Tier 1 & 2, Use Blazor-ApexCharts for Tier 3)

The hybrid approach is the **primary strategy** for Feature 127. It balances the project's zero-JS-dependency strength with pragmatic outsourcing of genuinely complex chart types.

#### Hybrid Strategy Breakdown

**Tier 1 & 2 (Self-Implement — Zero New JS Dependencies):**
- **Tier 1 (Trivial → Simple):** Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick
- **Tier 2 (Medium):** Waterfall, Box Plot
- **Approach:** Continue the existing hand-rolled SVG + Blazor pattern using the team's proven skills and test infrastructure (bUnit, 100% coverage)
- **Bundle impact:** Zero
- **Effort:** 6–10 weeks combined

**Tier 3 (Library — Blazor-ApexCharts for Complex Algorithms):**
- **Types:** Treemap, Radar/Spider (and Sunburst if needed)
- **Why library:** Squarified treemapping algorithms and trigonometric radar geometry are not domain-specific to budgeting. Their maintenance burden outweighs the benefit of in-house ownership.
- **Bundle impact:** ~80 KB gzipped (acceptable within 200 KB budget)
- **Effort:** Spike + integration (2–3 weeks)

**Unified theming:** All charts (self-implemented and ApexCharts) share the same CSS custom property theming system, ensuring visual consistency across all 9 themes.

---

### Evaluated Options (for Context)

| Library | Pros | Cons | Verdict |
|---------|------|------|---------|
| **Blazor-ApexCharts** (thirstyape) | Beautiful defaults, 20+ chart types (treemap, heatmap, radar, candlestick, radialBar, etc.), actively maintained, strong Blazor WASM support, ~80 KB gzipped JS, built-in responsiveness, animation, zoom/pan, dark mode, MIT license | Depends on ApexCharts.js via JS interop; NuGet package wraps JS library | ✅ Recommended for Tier 3 only |
| **Chart.js via ChartJs.Blazor** | Most popular JS charting library, huge ecosystem | Blazor wrapper (`ChartJs.Blazor`) is stale (last update 2022), not actively maintained for .NET 8+. `PSC.Blazor.Components.ChartJs` is more active but less mature. Chart.js v4 bundles are larger. | ❌ Wrapper maintenance risk |
| **Plotly.NET** | Scientific charts, extremely powerful | Heavyweight (~3 MB), designed for data science, overkill for a budgeting app, poor Blazor WASM integration | ❌ Too heavy |
| **D3.js via JS interop** | Ultimate flexibility, any chart type possible | Requires significant custom JS, effectively building a chart library from scratch, huge maintenance burden | ❌ Too much custom JS |
| **Radzen Blazor Charts** | Native Blazor, no JS interop | Tied to Radzen component library, limited chart types, styling conflicts with custom design system | ❌ Component library dependency |
| **Keep hand-rolled SVG for all** | Zero dependencies, full control, existing test suite | Treemap (squarified algorithm) and Radar (trigonometry) add unsustainable custodial burden; other 7 types are feasible | ⚠️ Hybrid is better |

#### Hybrid Approach Rationale

1. **Preserves the project's zero-JS-dependency advantage for most charts.** 7 of 9 new chart types (Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick, Waterfall, Box Plot) require zero new JS dependencies.
2. **Pragmatic outsourcing of complex algorithms.** Treemap (squarified rectangle packing) and Radar (trigonometric polygon rendering) are error-prone if maintained in-house. The ~80 KB ApexCharts bundle is a reasonable trade-off.
3. **Leverages existing team skills.** The team has 11 production chart components with full bUnit coverage. Self-implementing Tier 1 & 2 charts follows a proven pattern.
4. **Unified visual language.** All charts (SVG-based and ApexCharts-based) share CSS custom property theming, ensuring consistency across all 9 themes.
5. **Flexible decision point.** After Slice 1 (ApexCharts integration spike), if bundle size exceeds acceptable limits, the team can pivot to self-implementing all Tier 1 & 2 charts while still using ApexCharts for Tier 3 only.

#### Risk Mitigation

- **JS interop latency (Tier 3 only):** ApexCharts uses JS interop under the hood. For Blazor WASM (in-browser), this is a local call — not cross-process. Latency is negligible.
- **.NET 10 compatibility:** Verify ApexCharts package compatibility early in Slice 1. If the NuGet package hasn't released a .NET 10 build, the `net9.0`/`net8.0` package may still work (Blazor WASM targets are forward-compatible).
- **Bundle size validation:** Measure actual gzipped payload after ApexCharts integration; fail-fast in Slice 1 if >200 KB gzipped.

#### License Notes (Tier 3 Libraries Only)

**ApexCharts.js + Blazor-ApexCharts Licensing**

- **ApexCharts.js:** Dual-license model
  - **Community License (Free):** Available for organizations with annual revenue < $2M USD — permits personal, educational, non-profit, and small-business commercial use without restriction
  - **Commercial License (Paid):** Required for organizations with annual revenue ≥ $2M USD
  - **Chart Type Coverage:** All chart types (including treemap, radar) are available under Community License; no "Pro" tier restrictions on specific chart types
  - **Attribution:** Yes, brief credit required (e.g., "Powered by ApexCharts")

- **Blazor-ApexCharts:** MIT License (Copyright 2020 Joakim Dangården)
  - Permits commercial and personal use, modification, and redistribution
  - Requires copyright notice preservation
  - No restrictions on self-hosted applications

**Recommendation for BudgetExperiment:**
✅ Safe and fully compliant for self-hosted personal budgeting application under Community License. No CLA or contributor agreement triggered by usage. Action: Add copyright/attribution notices to `THIRD-PARTY-LICENSES.md` once integration begins.

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
│   ├── ChartThemeService.cs          # Bridges CSS vars → ApexCharts theme config (Tier 3 only)
│   ├── ChartDataService.cs           # Aggregation, moving averages, statistics
│   ├── ChartColorProvider.cs         # Category color resolution + fallback palette
│   ├── ChartAxis.razor               # SVG axis renderer (Tier 1/2)
│   ├── ChartGrid.razor               # SVG grid lines (Tier 1/2)
│   ├── ChartTooltip.razor            # Tooltip overlay (Tier 1/2)
│   └── ChartTick.cs                  # Tick data model (Tier 1/2)
├── SelfImplemented/                  # Tier 1 & 2: Pure SVG + Blazor (zero JS dependency)
│   ├── HeatmapChart.razor            # Tier 1: Grid of colored cells
│   ├── ScatterChart.razor            # Tier 1: Point plotting with axes
│   ├── StackedAreaChart.razor        # Tier 1: Layered filled areas
│   ├── RadialBarChart.razor          # Tier 1: Multi-gauge dashboard
│   ├── CandlestickChart.razor        # Tier 1: OHLC bars with wicks
│   ├── WaterfallChart.razor          # Tier 2: Sequential bars with connectors
│   └── BoxPlotChart.razor            # Tier 2: Quartile visualization
├── ApexCharts/                       # Tier 3: Complex types via ApexCharts JS library
│   ├── BudgetTreemap.razor           # Tier 3: Hierarchical rectangle packing (algorithm outsourced)
│   ├── BudgetRadar.razor             # Tier 3: Polygonal axes (trigonometry outsourced)
│   └── ChartExportButton.razor       # Export helper for ApexCharts
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

### Theming Strategy: Unified Visual Language Across All Chart Types

**Tier 1 & 2 (Self-Implemented) Theming:**
- Use **CSS custom properties** for all colors, following the existing pattern established by BarChart, DonutChart, etc.
- Properties like `--color-brand-primary`, `--color-income`, `--color-expense`, `--color-surface-elevated` are already defined in all 9 themes
- Rendering via inline SVG; no JS interop needed
- Theme switching is automatic — no component re-render required (CSS handles it)

**Tier 3 (ApexCharts) Theming:**
- `ChartThemeService` bridges CSS custom properties → `ApexChartOptions.Theme` configuration
- ApexCharts components read theme config and apply colors programmatically
- On theme change, `ChartThemeService` notifies ApexCharts components to re-render with new colors

**Visual Consistency Guarantee:**
All charts (Tier 1, 2, and 3) use the **same semantic color values** from CSS custom properties. A Heatmap (Tier 1) and a Treemap (Tier 3) on the same page will render with identical color palettes across all 9 themes (light, dark, accessible, crayons, geocities, macOS, monopoly, vscode-dark, win95).

### Data Flow

```
Report Page → ChartDataService → Chart Component → SVG (Tier 1/2) OR ApexCharts (Tier 3)
     ↓              ↓                    ↓
 Raw DTOs     Aggregated data      Theme config from ChartThemeService (Tier 3 only)
              (moving avg,         Color palette from ChartColorProvider
               statistics,
               pivots)
```

1. **Report pages** fetch raw data from `IBudgetApiService` (already exists)
2. **`ChartDataService`** transforms raw DTOs into chart-ready models (aggregation, pivoting, statistical calculations) — shared across all tiers
3. **Chart components** receive prepared data:
   - **Tier 1/2:** Render inline SVG using C# geometry calculations
   - **Tier 3:** Delegate rendering to ApexCharts via JS interop
4. **`ChartThemeService`** provides theme configuration derived from active CSS custom properties (Tier 3 only)
5. **`ChartColorProvider`** resolves category colors (Tier 1/2 and Tier 3)

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

- **AC-127-01:** ✅ `Blazor-ApexCharts` NuGet package is added to `BudgetExperiment.Client.csproj` and builds without errors on .NET 10
- **AC-127-02:** ✅ A `ChartThemeService` reads CSS custom properties and generates `ApexChartOptions` theme configuration for Tier 3 charts
- **AC-127-03:** ✅ ApexCharts-rendered charts (Treemap, Radar) respond to theme changes (switching themes updates colors within 500ms)
- **AC-127-04:** ✅ The ApexCharts JS bundle adds no more than 200 KB gzipped to the published WASM payload
- **AC-127-05:** ✅ All existing chart components (`BarChart`, `DonutChart`, `LineChart`, etc.) continue to function without modification

### New Chart Types — Self-Implemented (Slices 3–5)

- **AC-127-06:** ✅ Heatmap chart displays a 7-row (Mon–Sun) × N-column (weeks) grid colored by spending intensity
- **AC-127-07:** ✅ Scatter plot displays transactions as dots (date × amount), with outliers visually distinct (>2σ from category mean)
- **AC-127-08:** ✅ Stacked area chart shows category spending composition over 6–12 months with consistent category ordering
- **AC-127-09:** ✅ Radial bar chart displays budget utilization percentage for up to 8 categories with color transitions (green → yellow → red)
- **AC-127-10:** ✅ Candlestick chart shows account balance open/high/low/close per month with green (increase) and red (decrease) coloring
- **AC-127-11:** ✅ Waterfall chart starts at income, subtracts each spending category, and ends at net remaining — correctly handles negative net values
- **AC-127-12:** ✅ Box plot chart shows spending distribution (min, Q1, median, Q3, max) per category with outlier dots

### New Chart Types — ApexCharts (Slice 6)

- **AC-127-13:** ✅ Treemap chart displays spending by category with rectangle sizes proportional to amounts; clicking a category drills into subcategories
- **AC-127-14:** ✅ Radar chart displays budget utilization across all active budget categories on a single chart

### Data Service Layer (Slices 2–6)

- **AC-127-15:** ✅ `IChartDataService` is registered in DI and has a concrete implementation in the Client project
- **AC-127-16:** ✅ `BuildSpendingHeatmap` correctly aggregates transactions by day-of-week × week-of-month and returns a 7×N matrix
- **AC-127-17:** ✅ `BuildBudgetWaterfall` produces segments that sum from income to net remaining with running totals
- **AC-127-18:** ✅ `BuildBalanceCandlesticks` correctly computes open/high/low/close from daily balance data
- **AC-127-19:** ✅ `BuildCategoryDistributions` calculates correct quartiles and identifies outliers using 1.5×IQR method
- **AC-127-20:** ✅ All `IChartDataService` methods have unit tests with edge cases (empty data, single transaction, all same amount)

### Visual & Interactivity (Slice 7)

- **AC-127-21:** ✅ All ApexCharts-rendered charts (Treemap, Radar) animate on first load
- **AC-127-22:** ✅ Tooltips display formatted currency values, category name, percentage of total, and transaction count (applies to all charts)
- **AC-127-23:** ✅ Time-series self-implemented charts (Scatter, Stacked Area, Candlestick) support zoom/pan via mouse drag or pinch gesture
- **AC-127-24:** ✅ Self-implemented chart legends are interactive (toggle series visibility where applicable)
- **AC-127-25:** ✅ ApexCharts export to PNG via toolbar button (built-in feature for Treemap, Radar)

### Migration (Slice 8)

- **AC-127-26:** ✅ Existing chart usage in reports is evaluated: keep SVG or migrate to new chart components (decision per report)
- **AC-127-27:** ✅ New chart types (Heatmap, Treemap, Waterfall, Radar, etc.) are introduced to appropriate report pages as value-adding features
- **AC-127-28:** ✅ `ComponentShowcase` page displays all 9 new chart types (7 self-implemented + 2 ApexCharts)
- **AC-127-29:** ✅ All charts work across all 9 themes with correct colors and readability

### Accessibility & Theme Compliance (Cross-cutting)

- **AC-127-30:** ✅ All self-implemented charts include ARIA labels and keyboard navigation matching current standards
- **AC-127-31:** ✅ All ApexCharts charts include ARIA labels matching current standards
- **AC-127-32:** ✅ All 9 themes (light, dark, accessible, crayons, geocities, macOS, monopoly, vscode-dark, win95) render all charts with correct colors
- **AC-127-33:** ✅ The `accessible` theme uses a colorblind-safe palette for all chart types

### Cleanup (Slice 9 — Optional)

- **AC-127-34:** ✅ If legacy SVG components are fully migrated, they are removed from `Components/Charts/` after verification
- **AC-127-35:** ✅ No dead code or unused supporting types remain (only if cleanup slice executed)

---

## Implementation Slices

### Slice 1: Library Integration & Theme Bridge (ApexCharts for Tier 3 Only)
**Commit:** `feat(charts): add Blazor-ApexCharts spike for treemap/radar validation`
- Add `Blazor-ApexCharts` NuGet package to `BudgetExperiment.Client`
- Create `ChartThemeService` to bridge CSS custom properties → ApexCharts theme configuration
- Create `ChartColorProvider` for category color resolution
- Implement proof-of-concept treemap to validate integration and bundle size impact
- Verify gzipped payload is <200 KB (fail-fast if exceeded)
- Write unit tests for `ChartThemeService` and `ChartColorProvider`

### Slice 2: Chart Data Service Foundation
**Commit:** `feat(charts): add IChartDataService with aggregation methods`
- Define `IChartDataService` interface with methods for all chart types (self-implemented and ApexCharts)
- Implement `ChartDataService` with aggregation, pivoting, and statistical calculations
- Register in DI
- Write comprehensive unit tests (edge cases, empty data, single item, large datasets)

### Slice 3: Self-Implement Tier 1 Charts — Part 1 (Heatmap, Scatter)
**Commit:** `feat(charts): add heatmap and scatter chart components`
- `BudgetHeatmap` component: day-of-week × week grid, colored by spending intensity
- `BudgetScatter` component: transaction points (date × amount), outlier highlighting
- Add to `ComponentShowcase`
- Full bUnit test coverage (grid rendering, color mapping, tooltips, keyboard navigation)

### Slice 4: Self-Implement Tier 1 Charts — Part 2 (Stacked Area, Radial Bar, Candlestick)
**Commit:** `feat(charts): add stacked area, radial bar, and candlestick chart components`
- `BudgetStackedArea` component: layered areas showing category composition over time
- `BudgetRadialBar` component: multi-gauge dashboard for budget utilization
- `BudgetCandlestick` component: OHLC bars for account balance range
- Add to `ComponentShowcase`
- Full bUnit test coverage

### Slice 5: Self-Implement Tier 2 Charts (Waterfall, Box Plot)
**Commit:** `feat(charts): add waterfall and box plot chart components`
- `BudgetWaterfall` component: floating bars showing income → spending → net flow
- `BudgetBoxPlot` component: statistical distribution (quartiles, outliers) per category
- Add to `ComponentShowcase`
- Comprehensive unit tests for coordinate math and statistical calculations
- Full bUnit test coverage

### Slice 6: ApexCharts Tier 3 Integration (Treemap, Radar)
**Commit:** `feat(charts): complete treemap and radar chart implementation`
- `BudgetTreemap` component: category hierarchy with drill-down interaction (final polish from Slice 1 spike)
- `BudgetRadar` component: budget utilization polygon across all categories
- Add to `ComponentShowcase` with all 9 themes verified
- Full bUnit test coverage

### Slice 7: Visual Polish & Interactivity (All Charts)
**Commit:** `feat(charts): add animations, zoom, export, and enhanced tooltips`
- Enable load animations for all ApexCharts components (Treemap, Radar)
- Enhance tooltips across all charts with formatted currency, percentage, and counts
- Configure zoom/pan for time-series self-implemented charts (Scatter, Stacked Area, Candlestick)
- Implement chart export (PNG/SVG) for ApexCharts charts
- Verify all 9 themes render correctly across all 9 chart types

### Slice 8: Migrate Existing Report Charts to Hybrid Approach
**Commit:** `refactor(charts): migrate report pages to new chart components`
- For existing usage (BarChart, DonutChart, LineChart, AreaChart, etc.): decide on per-chart basis whether to keep self-implemented SVG or migrate to new self-implemented/ApexCharts equivalents
- `MonthlyTrendsReport` and `BudgetComparisonReport`: keep existing `BarChart` (SVG) or migrate to `BudgetBarChart` (SVG equivalent in new architecture)
- `MonthlyCategoriesReport` and `CalendarInsightsPanel`: keep existing `DonutChart` (SVG) or migrate to `BudgetDonutChart`
- Introduce new chart types (heatmap, treemap, waterfall, radar, etc.) to appropriate report pages
- Update bUnit tests; verify visual parity
- Verify all 9 themes remain correct after changes

### Slice 9: Optional Legacy Cleanup
**Commit:** `refactor(charts): remove legacy SVG chart components (optional)`
- If Slice 8 migrations are complete and all consumers updated: remove old SVG chart components (`BarChart`, `DonutChart`, `LineChart`, etc.)
- Remove supporting types no longer referenced (`BarChartGroup`, `BarChartSeries`, etc.)
- Remove legacy `Shared/ChartAxis`, `ChartGrid`, `ChartTooltip` if no longer used
- Update `_Imports.razor` if needed
- Verify all tests still pass
- **Alternative:** Retain legacy components alongside new ones for incremental migration over time (lower-risk approach)

### Slice 10: Reports Dashboard Enhancement
**Commit:** `feat(reports): add enhanced dashboard with new chart types`
- Create a Reports Dashboard page aggregating multiple chart types
- Add treemap, heatmap, and radar to appropriate report pages
- Cross-chart filtering: clicking a category in one chart filters others
- Responsive layout with chart sizing breakpoints

---

## Implementation Approach: Self-Implement vs. Library

### Honest Assessment: Can These Charts Be Self-Implemented in Pure SVG + Blazor?

The user asked whether the new chart types could be implemented without adopting Blazor-ApexCharts, continuing the hand-rolled SVG approach that has served the project well (11 chart components, full bUnit coverage, zero JS dependencies). This section provides a frank technical assessment for each proposed chart type, ultimately supporting the **hybrid approach as the chosen strategy** (see Library Recommendation above).

#### Methodology

For each chart type, we evaluate:
1. **Feasibility** — Can it be done in pure SVG + Blazor?
2. **Complexity** — Low / Medium / High / Very High (relative to BarChart/DonutChart effort)
3. **Key Challenges** — What makes it hard or easy?
4. **Time Estimate** — Developer weeks for implementation + testing (using existing 11 charts as a baseline: BarChart ≈ 1 week with tests)
5. **Maintenance Burden** — Ongoing cost to support this type alongside existing charts

#### Chart-by-Chart Assessment

---

##### 1. **Treemap** — Hierarchical Rectangle Packing

| Criterion | Assessment |
|-----------|------------|
| **Feasibility** | ✅ Yes, but requires algorithm knowledge |
| **Complexity** | **High** |
| **SVG Difficulty** | Rectangle layout is straightforward; hierarchical data packing is the challenge |
| **Algorithm Required** | Squarified treemapping (or simpler slice-and-dice) — not trivial math, but well-documented. Needs recursive hierarchy traversal. |
| **Interactivity** | Click-to-drill requires state management in the component (tracking "current level" in hierarchy). Moderate complexity. |
| **Time Estimate** | 1.5–2 weeks (algorithm development + testing + drill-down interaction logic) |
| **Maintenance Cost** | Medium — algorithm-heavy code needs documentation; drill-down interaction adds complexity |
| **Recommendation** | **Library (ApexCharts)** — While feasible, the squarified algorithm and hierarchy drill-down are not domain-specific to budgeting. Library amortizes this complexity across many users. Self-implementation is possible but adds custodial burden. **Tier 3.** |

---

##### 2. **Heatmap** — Grid of Colored Cells

| Criterion | Assessment |
|-----------|------------|
| **Feasibility** | ✅ Yes, straightforward SVG |
| **Complexity** | **Low** |
| **SVG Difficulty** | Trivial — `<rect>` elements in a grid with color scaling. No geometry calculation required. |
| **Interactivity** | Hoverable cells with formatted tooltips. Same pattern as existing DonutChart segments. |
| **Time Estimate** | 0.5–0.75 weeks (grid layout + color scale + tooltips) |
| **Maintenance Cost** | Low — simple component, minimal code |
| **Recommendation** | **Self-implement** — This is one of the easiest chart types. Requires ~150 lines of component code + CSS. Fits naturally with the existing architecture. **Tier 1 candidate.** |

---

##### 3. **Waterfall Chart** — Sequential Bars with Connectors

| Criterion | Assessment |
|-----------|------------|
| **Feasibility** | ✅ Yes, but requires careful coordinate math |
| **Complexity** | **Medium-High** |
| **SVG Difficulty** | Drawing bars is trivial; the challenge is drawing connector lines (floating bars) and calculating cumulative positions. Coordinates must track running totals carefully. |
| **Interactivity** | Tooltips + hover highlighting. Standard pattern. |
| **Time Estimate** | 1–1.5 weeks (coordinate logic + connector rendering + thorough testing of edge cases) |
| **Maintenance Cost** | Medium — coordinate tracking can be error-prone if refactored carelessly. Needs good comments. |
| **Recommendation** | **Self-implement possible, but library preferred** — The coordinate logic is not complex, but waterfall is more "statistical visualization" than a simple domain-specific chart. Library handles floating-bar semantics correctly. If bundle size is critical, this is a **Tier 2** candidate (could self-implement). |

---

##### 4. **Scatter Plot** — Point Plotting with Axis Scaling

| Criterion | Assessment |
|-----------|------------|
| **Feasibility** | ✅ Yes, uses existing ChartAxis infrastructure |
| **Complexity** | **Low** |
| **SVG Difficulty** | Plot points at (x, y) coordinates with axes. Same approach as LineChart (which already plots multi-point series). Only difference: render circles instead of lines. Outlier detection is a data preparation concern (handled in application layer). |
| **Interactivity** | Hoverable points + tooltips. Same as LineChart. |
| **Time Estimate** | 0.5–0.75 weeks (reuse LineChart infrastructure + modify rendering logic) |
| **Maintenance Cost** | Low — straightforward point-rendering component |
| **Recommendation** | **Self-implement** — This is nearly a variant of the existing LineChart. Fits naturally with the architecture. **Tier 1 candidate.** |

---

##### 5. **Radar/Spider Chart** — Polygonal Axes with Shaded Area

| Criterion | Assessment |
|-----------|------------|
| **Feasibility** | ⚠️ Possible, but requires advanced SVG geometry |
| **Complexity** | **Very High** |
| **SVG Difficulty** | Must generate N axes radiating from center (N = number of categories). Each axis has ticks and labels positioned at angles. Area fill is a polygon with N vertices. Angle calculations and coordinate transforms are error-prone. |
| **Geometry** | All coordinates must be rotated around a center point. Requires `Math.Sin()`/`Math.Cos()` for every point — much more trigonometry than BarChart. |
| **Interactivity** | Hoverable polygon areas + tooltips. Challenging due to arbitrary polygon shapes (hit detection not trivial). |
| **Testing** | Hard to visually verify by eye; need snapshot tests + manual inspection across all category counts. |
| **Time Estimate** | 2–3 weeks (trigonometry + polygon rendering + robust testing + interaction logic) |
| **Maintenance Cost** | Very High — trigonometric code is fragile. Future refactors risk breaking angle calculations. Requires domain expertise or deep documentation. |
| **Recommendation** | **Library (ApexCharts)** — While theoretically feasible, the trigonometry and polygon rendering are error-prone and unmaintainable. This is the chart type most likely to accumulate bugs in self-implementation. Library mitigates all risk. **Tier 3.** |

---

##### 6. **Stacked Area** — Filled Areas Stacked on X-Axis

| Criterion | Assessment |
|-----------|------------|
| **Feasibility** | ✅ Yes, moderate math |
| **Complexity** | **Medium** |
| **SVG Difficulty** | Build on the existing AreaChart (which already renders filled lines). The challenge: calculate cumulative Y values as you move up the stack. Each band's Y coordinates depend on all bands below it. Requires careful coordinate tracking. |
| **Interactivity** | Hoverable bands + tooltips. Same pattern as AreaChart. |
| **Time Estimate** | 0.75–1.25 weeks (stacking math + rendering refactoring + edge case testing) |
| **Maintenance Cost** | Medium — stacking logic can be refactored incorrectly; good tests needed. |
| **Recommendation** | **Self-implement** — This is a natural evolution of the existing AreaChart. Fits the architecture well. **Tier 1 candidate.** |

---

##### 7. **Radial Bar / Gauge** — Arc-Based Progress Indicator

| Criterion | Assessment |
|-----------|------------|
| **Feasibility** | ✅ Yes, the project already has `RadialGauge` |
| **Complexity** | **Low-Medium** |
| **SVG Difficulty** | The existing `RadialGauge` component shows a single circular progress bar. A multi-gauge (several concentric rings for different categories) requires parametrizing the radius and angle per gauge. Moderate coordinate work. |
| **Interactivity** | Non-interactive or simple tooltips on hover. |
| **Time Estimate** | 0.5–1 week (extend RadialGauge to support multiple rings + theming) |
| **Maintenance Cost** | Low — simple extension of existing code |
| **Recommendation** | **Self-implement** — The project already has a radial gauge. Extending it to multi-gauge is low-risk. **Tier 1 candidate.** |

---

##### 8. **Candlestick** — OHLC Bars with Wicks

| Criterion | Assessment |
|-----------|------------|
| **Feasibility** | ✅ Yes, uses existing bar rendering |
| **Complexity** | **Low** |
| **SVG Difficulty** | Each candlestick is a vertical bar (body) with two lines (wicks) extending above and below. Rendering is trivial; the challenge is ensuring correct color semantics (green = close > open, red = close < open) and handling overlapping wicks. |
| **Interactivity** | Tooltips on hover. Standard pattern. |
| **Time Estimate** | 0.5–1 week (bar + wick rendering + color logic + bUnit tests) |
| **Maintenance Cost** | Low — straightforward rendering |
| **Recommendation** | **Self-implement** — This is simple SVG work. Fits naturally with the architecture. **Tier 1 candidate.** |

---

##### 9. **Box Plot** — Statistical Quartile Visualization

| Criterion | Assessment |
|-----------|------------|
| **Feasibility** | ✅ Yes, uses existing bar rendering |
| **Complexity** | **Medium** |
| **SVG Difficulty** | Each box plot is a vertical bar (representing Q1–Q3), a horizontal line inside (median), and whiskers extending to min/max. Outliers rendered as circles. The challenge: ensuring correct statistical calculations in the data service layer (quartiles, outlier detection via 1.5×IQR). Rendering is straightforward. |
| **Interactivity** | Tooltips showing quartile values. Standard pattern. |
| **Data Prep** | The heavy lifting is in the application service (`BuildCategoryDistributions` in `IChartDataService`), not the component. |
| **Time Estimate** | 0.75–1.5 weeks (statistical calculations + SVG rendering + comprehensive unit tests for quartiles and outlier detection) |
| **Maintenance Cost** | Medium — statistical code must be robust; edge cases (single value, all same value) are critical. |
| **Recommendation** | **Self-implement possible, but library preferred for robustness** — While feasible, the statistical calculations (quartiles, IQR) are prone to subtle bugs if implemented ad hoc. Library handles these robustly. If statistical rigor is critical, use library. If speed matters more, this is a **Tier 2** candidate. |

---

#### Tiered Recommendation

**Tier 1: Self-Implement (Low Risk, Fits Architecture)**
1. **Heatmap** — Trivial grid rendering, no complex math
2. **Scatter Plot** — Natural evolution of LineChart, reuses axis infrastructure
3. **Stacked Area** — Natural evolution of AreaChart, familiar stacking math
4. **Radial Bar/Gauge** — Extends existing RadialGauge component minimally
5. **Candlestick** — Simple bar + wick rendering, straightforward color logic

**Tier 2: Self-Implement with Caveats (Medium Risk, Extra Testing)**
1. **Waterfall** — Feasible coordinate math, but needs careful edge-case testing
2. **Box Plot** — Feasible, but statistical calculations (quartiles, outlier detection) demand rigor

**Tier 3: Library Recommended (High Risk of Self-Implementation)**
1. **Treemap** — Squarified algorithm + drill-down interaction add custodial burden
2. **Radar/Spider** — Trigonometric geometry is error-prone; requires domain expertise or deep documentation
3. **Sunburst** — Combine treemap algorithm + sunburst layout math; not worth maintaining in-house

---

### Chosen Approach: Hybrid (Tier 1 & 2 Self-Implement + Tier 3 ApexCharts)

**This is the primary recommendation and adopted strategy for Feature 127.**

**Rationale:**
- **Tier 1 & 2** (Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick, Waterfall, Box Plot) are all feasible and fit naturally with the existing SVG+Blazor architecture. Combined effort: 6–10 weeks.
- **Tier 3** (Treemap, Radar) have complex algorithms (squarified rectangle packing, trigonometry) that are error-prone when maintained in-house. ApexCharts handles these robustly.
- **Zero JS dependency for 7 of 9 chart types** — preserves the project's architectural advantage.
- **Unified theming** — all charts (self-implemented and ApexCharts-based) share CSS custom property integration, ensuring visual consistency.
- **Pragmatic balance** — the team leverages its proven SVG skills while outsourcing genuinely difficult algorithms.

See **Library Recommendation** section above for full hybrid strategy details.



1. **Sunburst chart:** ApexCharts does not have a native sunburst type. Should we implement sunburst as a custom SVG component (maintaining the project's existing SVG competency) or accept treemap with drill-down as an adequate substitute for hierarchical visualization?

2. **Candlestick data availability:** The candlestick chart requires daily account balance data. Does the app currently track daily balances, or does this require a new `DailyBalance` entity and a nightly snapshot job? If not available, should this chart type be deferred?

3. **Bundle size threshold:** The proposed 200 KB gzipped limit for ApexCharts is an estimate. Should we conduct a proof-of-concept spike (Slice 1) before committing to the full feature, with a go/no-go decision point after bundle size measurement?

4. **Cross-chart filtering UX:** When a user clicks a category in a treemap, should other charts on the same page filter to that category? This requires a shared selection state service. Is this in scope for v1 or a future enhancement?

5. **Chart type priority:** Are all 10 proposed chart types equally valued, or should we prioritize a subset? The heatmap (spending patterns), treemap (category breakdown), and waterfall (budget flow) deliver the most unique budgeting insights not available from existing chart types.
