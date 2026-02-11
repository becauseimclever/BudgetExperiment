# 053 - Reporting & Data Portability Overhaul
> **Status:** 🚧 In Progress  
> **Priority:** High  
> **Dependencies:** Feature 050 (Calendar-Driven Reports), Existing Chart Components

## Goal
Deliver a modern, accessible, and highly interactive reporting experience for budget data, with a comprehensive chart component library, robust export options (CSV, Excel, PDF), and user-driven custom report composition.

## Motivation
- Current reports are functional but lack visual appeal, interactivity, and flexible export options.
- Users need actionable insights, not just static data.
- Data portability (for analysis, sharing, or backup) is a key user expectation.
- Customizable reporting empowers users to focus on what matters most to them.
- A unified chart component library ensures consistency across all visualization needs.

---

## Current State

**What Exists:**
- **DonutChart Component**: Pure SVG donut chart with segments, tooltips, hover effects, and legend (`Components/Charts/DonutChart.razor`)
- **ChartLegend Component**: Reusable legend for charts (`Components/Charts/ChartLegend.razor`)
- **DonutChartSegment Component**: Individual segment renderer (`Components/Charts/DonutChartSegment.razor`)
- **BarChart Component**: Pure SVG grouped bar chart with legend and tooltips (`Components/Charts/BarChart.razor`)
- **GroupedBarChart Component**: Multi-series grouped bar chart (`Components/Charts/GroupedBarChart.razor`)
- **StackedBarChart Component**: Stacked bar chart for composition views (`Components/Charts/StackedBarChart.razor`)
- **LineChart Component**: Pure SVG line chart with grid, axis labels, and tooltips (`Components/Charts/LineChart.razor`)
- **AreaChart Component**: Filled line chart with optional gradient fill (`Components/Charts/AreaChart.razor`)
- **SparkLine Component**: Inline trend indicator (`Components/Charts/SparkLine.razor`)
- **ProgressBar Component**: Horizontal progress indicator (`Components/Charts/ProgressBar.razor`)
- **RadialGauge Component**: Circular progress gauge (`Components/Charts/RadialGauge.razor`)
- **Shared Chart Primitives**: ChartAxis, ChartGrid, ChartTooltip (`Components/Charts/Shared/*`)
- **CSV Export Endpoint**: Monthly categories report export (`/api/v1/exports/categories/monthly`)
- **Monthly Categories Report**: Uses DonutChart to show category spending breakdown

**Current Gaps:**
1. Export limited to CSV (reports only)
2. No Excel/PDF exports
3. No custom report builder
4. Limited interactivity beyond tooltips (no zoom/filter)

---

## Features

### 1. Comprehensive Chart Component Library

#### 1.1 Bar Chart
A flexible bar chart supporting vertical and horizontal orientations with hover tooltips and click handlers.

**Use Cases:**
- Monthly spending by category
- Budget vs. actual comparison
- Account balance comparisons

**Features:**
- Vertical or horizontal orientation
- Optional value labels on bars
- Hover state with tooltip
- Click handler for drill-down
- Animated entry
- Responsive sizing

#### 1.2 Grouped Bar Chart
Multi-series bar chart for side-by-side comparisons.

**Use Cases:**
- Budget vs. Actual per category
- Income vs. Spending by month
- Multiple account comparisons

**Features:**
- Multiple series per group
- Legend for series identification
- Color-coded series
- Grouped with spacing

#### 1.3 Stacked Bar Chart
Multi-series bar chart where series stack on top of each other.

**Use Cases:**
- Spending composition over time
- Category breakdown within months
- Income sources stacked

**Features:**
- Stacked segments
- Segment tooltips showing individual and cumulative values
- Click to expand segment details

#### 1.4 Line Chart
Line chart for trends over time with optional multiple series.

**Use Cases:**
- Spending trend over 12 months
- Balance progression over time
- Budget adherence trend

**Features:**
- Single or multi-line
- Optional data points (dots)
- Smooth or straight line interpolation
- Area fill option (becomes area chart)
- Hover to highlight nearest point
- Optional reference lines (budget targets)

#### 1.5 Area Chart
Filled line chart for showing magnitude over time.

**Use Cases:**
- Cumulative spending over month
- Account balance progression
- Net worth over time

**Features:**
- Gradient fill
- Single or stacked areas
- Transparent overlays for multiple series

#### 1.6 SparkLine
Minimal inline chart for quick trend indication.

**Use Cases:**
- Trend indicator next to numeric values
- Mini chart in table cells
- Dashboard summary widgets

**Features:**
- Ultra-compact (fits in text line)
- No labels/axes
- Color indicates trend direction (green up, red down)
- Optional endpoint dots

#### 1.7 Progress Bar / Gauge
Visual progress indicator for budget consumption.

**Use Cases:**
- Category budget progress
- Overall budget utilization
- Goal tracking

**Features:**
- Horizontal or circular (radial) variant
- Color thresholds (green → yellow → red)
- Percentage and/or absolute value display
- Animated fill

### 2. Visually Appealing, Actionable Reports
- Refactor all existing reports for modern, clean, and accessible design.
- Use color, typography, and layout to highlight trends, anomalies, and actionable items.
- Add contextual callouts (e.g., "Overspent this month", "Uncategorized transactions high").
- Ensure all reports are fully keyboard navigable and screen reader friendly.

### 3. Interactive Graphs & Visualizations
- All chart components built in-house using pure SVG (like existing DonutChart).
- Enable tooltips, drill-down, zoom, and filter capabilities on all graphs.
- Support toggling data series, time ranges, and categories interactively.
- Ensure graphs are accessible (ARIA labels, color contrast, keyboard support).

### 4. Print & Export Options
- All reports and graphs can be exported as:
  - CSV (raw data tables)
  - Excel (with formatting, multiple sheets if needed)
  - PDF (print-optimized, with charts rendered as vector graphics)
- Print-friendly styles for all reports (auto-hide navigation, optimize for paper sizes).
- Export includes current filters/selections (WYSIWYG principle).

### 5. Custom Report Builder
- Users can select and arrange a mix of graphs, tables, and summary widgets to create their own report dashboard.
- Drag-and-drop interface for composing custom reports.
- Save/load custom report layouts per user.
- Export/print custom reports as above.

### 6. Data Portability & API
- Full data export (all transactions, budgets, categories, etc.) as CSV/Excel.
- Option to export filtered data from any report view.
- API endpoint for exporting report data (authenticated, respects user scope).

### 7. Accessibility & Internationalization
- All new/updated reports and exports meet WCAG 2.1 AA.
- Localized number/date/currency formats in exports and UI.

---

## Technical Design

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  Components/Charts/                                                         │
│  ├── DonutChart.razor (existing)                                           │
│  ├── ChartLegend.razor (existing)                                          │
│  ├── BarChart.razor (NEW)                                                  │
│  ├── GroupedBarChart.razor (NEW)                                           │
│  ├── StackedBarChart.razor (NEW)                                           │
│  ├── LineChart.razor (NEW)                                                 │
│  ├── AreaChart.razor (NEW)                                                 │
│  ├── SparkLine.razor (NEW)                                                 │
│  ├── ProgressBar.razor (NEW)                                               │
│  ├── RadialGauge.razor (NEW)                                               │
│  └── Shared/                                                               │
│      ├── ChartAxis.razor (NEW)                                             │
│      ├── ChartGrid.razor (NEW)                                             │
│      ├── ChartTooltip.razor (NEW)                                          │
│      └── ChartAnimations.cs (NEW)                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          ▼                   ▼                   ▼
┌─────────────────┐ ┌─────────────────┐ ┌─────────────────┐
│ Export Services │ │ Report Services │ │ Custom Report   │
│                 │ │                 │ │ Builder         │
│ - CsvExport     │ │ - Chart Data    │ │                 │
│ - ExcelExport   │ │   Transformers  │ │ - Layout Engine │
│ - PdfExport     │ │ - Report DTOs   │ │ - Widget Library│
└─────────────────┘ └─────────────────┘ └─────────────────┘
```

### New/Modified Files

#### Chart Components

| File | Status | Description |
|------|--------|-------------|
| `Components/Charts/BarChart.razor` | New | Vertical/horizontal bar chart |
| `Components/Charts/BarChart.razor.css` | New | Bar chart styles |
| `Components/Charts/BarChartBar.razor` | New | Individual bar renderer |
| `Components/Charts/BarData.cs` | New | Bar chart data model |
| `Components/Charts/GroupedBarChart.razor` | New | Multi-series grouped bars |
| `Components/Charts/GroupedBarChart.razor.css` | New | Grouped bar styles |
| `Components/Charts/GroupedBarData.cs` | New | Grouped bar data model |
| `Components/Charts/StackedBarChart.razor` | New | Stacked bar chart |
| `Components/Charts/StackedBarChart.razor.css` | New | Stacked bar styles |
| `Components/Charts/LineChart.razor` | New | Line/trend chart |
| `Components/Charts/LineChart.razor.css` | New | Line chart styles |
| `Components/Charts/LineChartPoint.razor` | New | Data point renderer |
| `Components/Charts/LineData.cs` | New | Line chart data model |
| `Components/Charts/AreaChart.razor` | New | Filled area chart |
| `Components/Charts/AreaChart.razor.css` | New | Area chart styles |
| `Components/Charts/SparkLine.razor` | New | Inline mini chart |
| `Components/Charts/SparkLine.razor.css` | New | Sparkline styles |
| `Components/Charts/ProgressBar.razor` | New | Horizontal progress bar |
| `Components/Charts/ProgressBar.razor.css` | New | Progress bar styles |
| `Components/Charts/RadialGauge.razor` | New | Circular progress gauge |
| `Components/Charts/RadialGauge.razor.css` | New | Gauge styles |
| `Components/Charts/Shared/ChartAxis.razor` | New | Reusable axis renderer |
| `Components/Charts/Shared/ChartGrid.razor` | New | Background grid lines |
| `Components/Charts/Shared/ChartTooltip.razor` | New | Unified tooltip component |

#### Export Components

| File | Status | Description |
|------|--------|-------------|
| `Components/Export/ExportButton.razor` | New | Dropdown button for export options |
| `Components/Export/ExportButton.razor.css` | New | Export button styles |
| `Services/ExportService.cs` | New (Client) | Client-side CSV generation |

#### API Export Services

| File | Status | Description |
|------|--------|-------------|
| `Application/Export/IExportService.cs` | New | Export service interface |
| `Application/Export/ExportService.cs` | New | Export orchestration |
| `Application/Export/CsvExportService.cs` | New | CSV generation |
| `Application/Export/ExcelExportService.cs` | New | Excel generation (EPPlus) |
| `Application/Export/PdfExportService.cs` | New | PDF generation (QuestPDF) |
| `Controllers/ExportController.cs` | New | Export API endpoints |

#### Custom Report Builder

| File | Status | Description |
|------|--------|-------------|
| `Pages/Reports/CustomReportBuilder.razor` | New | Drag-and-drop builder page |
| `Components/Reports/WidgetPalette.razor` | New | Available widget list |
| `Components/Reports/ReportCanvas.razor` | New | Drop zone for widgets |
| `Components/Reports/ReportWidget.razor` | New | Base widget wrapper |
| `Domain/Entities/CustomReportLayout.cs` | New | Layout entity |
| `Contracts/Dtos/CustomReportLayoutDto.cs` | New | Layout DTO |

---

### Component Specifications

#### BarChart.razor Parameters

```csharp
/// <summary>
/// Pure SVG bar chart supporting vertical or horizontal orientation.
/// </summary>
[Parameter] public IReadOnlyList<BarData> Data { get; set; } = [];
[Parameter] public string Orientation { get; set; } = "vertical"; // vertical | horizontal
[Parameter] public bool ShowValues { get; set; } = true;
[Parameter] public bool ShowLabels { get; set; } = true;
[Parameter] public string ValueFormat { get; set; } = "C2"; // Currency format
[Parameter] public string CurrencyCode { get; set; } = "USD";
[Parameter] public decimal? MaxValue { get; set; } // Optional cap for scale
[Parameter] public string AriaLabel { get; set; } = "Bar chart";
[Parameter] public bool Animate { get; set; } = true;
[Parameter] public int BarSpacing { get; set; } = 4; // Gap between bars
[Parameter] public EventCallback<BarData> OnBarClick { get; set; }
[Parameter] public bool Compact { get; set; } = false;
```

#### BarData Model

```csharp
public sealed record BarData
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required decimal Value { get; init; }
    public string Color { get; init; } = "#3b82f6";
    public string? SecondaryLabel { get; init; }
    public string? Tooltip { get; init; }
}
```

#### GroupedBarChart.razor Parameters

```csharp
[Parameter] public IReadOnlyList<GroupedBarData> Data { get; set; } = [];
[Parameter] public IReadOnlyList<BarSeriesDefinition> Series { get; set; } = [];
[Parameter] public bool ShowValues { get; set; } = false; // Gets crowded with multi-series
[Parameter] public bool ShowLabels { get; set; } = true;
[Parameter] public bool ShowLegend { get; set; } = true;
[Parameter] public string AriaLabel { get; set; } = "Grouped bar chart";
[Parameter] public EventCallback<(GroupedBarData Group, string SeriesId)> OnBarClick { get; set; }
```

#### GroupedBarData Model

```csharp
public sealed record GroupedBarData
{
    public required string GroupId { get; init; }
    public required string GroupLabel { get; init; }
    public required IReadOnlyDictionary<string, decimal> Values { get; init; } // SeriesId → Value
}

public sealed record BarSeriesDefinition
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Color { get; init; }
}
```

#### LineChart.razor Parameters

```csharp
[Parameter] public IReadOnlyList<LineData> Data { get; set; } = [];
[Parameter] public IReadOnlyList<LineSeriesDefinition>? Series { get; set; } // Multi-line support
[Parameter] public bool ShowPoints { get; set; } = true;
[Parameter] public bool ShowArea { get; set; } = false; // Fill under line
[Parameter] public string Interpolation { get; set; } = "linear"; // linear | smooth
[Parameter] public bool ShowGrid { get; set; } = true;
[Parameter] public bool ShowXAxis { get; set; } = true;
[Parameter] public bool ShowYAxis { get; set; } = true;
[Parameter] public string XAxisLabel { get; set; } = "";
[Parameter] public string YAxisLabel { get; set; } = "";
[Parameter] public decimal? MinY { get; set; } // Optional Y-axis min
[Parameter] public decimal? MaxY { get; set; } // Optional Y-axis max
[Parameter] public IReadOnlyList<ReferenceLine>? ReferenceLines { get; set; }
[Parameter] public string AriaLabel { get; set; } = "Line chart";
[Parameter] public EventCallback<LineData> OnPointClick { get; set; }
```

#### LineData Model

```csharp
public sealed record LineData
{
    public required string Label { get; init; } // X-axis label
    public required decimal Value { get; init; } // Single series
    public IReadOnlyDictionary<string, decimal>? SeriesValues { get; init; } // Multi-series
}

public sealed record LineSeriesDefinition
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required string Color { get; init; }
    public bool Dashed { get; init; } = false;
}

public sealed record ReferenceLine
{
    public required decimal Value { get; init; }
    public required string Label { get; init; }
    public string Color { get; init; } = "#9ca3af";
    public bool Dashed { get; init; } = true;
}
```

#### SparkLine.razor Parameters

```csharp
[Parameter] public IReadOnlyList<decimal> Values { get; set; } = [];
[Parameter] public int Width { get; set; } = 80;
[Parameter] public int Height { get; set; } = 20;
[Parameter] public bool ShowEndpoints { get; set; } = true;
[Parameter] public string PositiveColor { get; set; } = "#22c55e"; // Green
[Parameter] public string NegativeColor { get; set; } = "#ef4444"; // Red
[Parameter] public string NeutralColor { get; set; } = "#6b7280"; // Gray
[Parameter] public bool ColorByTrend { get; set; } = true; // Color based on start→end direction
[Parameter] public string AriaLabel { get; set; } = "Trend indicator";
```

#### ProgressBar.razor Parameters

```csharp
[Parameter] public decimal Value { get; set; }
[Parameter] public decimal MaxValue { get; set; } = 100;
[Parameter] public bool ShowLabel { get; set; } = true;
[Parameter] public bool ShowPercentage { get; set; } = true;
[Parameter] public string Label { get; set; } = "";
[Parameter] public string Size { get; set; } = "medium"; // small | medium | large
[Parameter] public IReadOnlyList<ThresholdColor>? Thresholds { get; set; }
[Parameter] public string DefaultColor { get; set; } = "#3b82f6";
[Parameter] public bool Animate { get; set; } = true;

// Default thresholds for budget progress: 0-70% green, 70-90% yellow, 90%+ red
```

#### ThresholdColor Model

```csharp
public sealed record ThresholdColor
{
    public required decimal Threshold { get; init; } // Percentage threshold
    public required string Color { get; init; }
}
```

#### RadialGauge.razor Parameters

```csharp
[Parameter] public decimal Value { get; set; }
[Parameter] public decimal MaxValue { get; set; } = 100;
[Parameter] public int Size { get; set; } = 120; // Diameter in pixels
[Parameter] public bool ShowValue { get; set; } = true;
[Parameter] public bool ShowPercentage { get; set; } = false;
[Parameter] public string Label { get; set; } = "";
[Parameter] public string ValueFormat { get; set; } = "N0";
[Parameter] public IReadOnlyList<ThresholdColor>? Thresholds { get; set; }
[Parameter] public string TrackColor { get; set; } = "#e5e7eb";
[Parameter] public string DefaultColor { get; set; } = "#3b82f6";
[Parameter] public int StrokeWidth { get; set; } = 12;
```

---

### API Endpoints

#### Export Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/export/transactions` | Export transactions as CSV/Excel |
| GET | `/api/v1/export/transactions/csv` | Export transactions as CSV |
| GET | `/api/v1/export/transactions/excel` | Export transactions as Excel |
| GET | `/api/v1/export/report/{reportType}` | Export specific report data |
| GET | `/api/v1/export/report/{reportType}/pdf` | Export report as PDF |
| GET | `/api/v1/export/all` | Full data export (ZIP with multiple files) |

#### Export Query Parameters

```
GET /api/v1/export/transactions?format=csv&startDate=2026-01-01&endDate=2026-01-31&categoryId=...

format: csv | excel (default: csv)
startDate: DateOnly (optional, filters transactions)
endDate: DateOnly (optional, filters transactions)
categoryId: Guid (optional, filter by category)
accountId: Guid (optional, filter by account)
includeHeaders: bool (default: true, for CSV)
```

#### Export Response Headers

```
Content-Type: text/csv | application/vnd.openxmlformats-officedocument.spreadsheetml.sheet | application/pdf
Content-Disposition: attachment; filename="transactions_2026-01-01_2026-01-31.csv"
```

---

### Custom Report Builder Specification

#### Layout Entity

```csharp
public sealed class CustomReportLayout : EntityBase
{
    public required Guid UserId { get; init; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string LayoutJson { get; set; } // Serialized widget positions
    public bool IsDefault { get; set; } = false;
    public DateTime CreatedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; set; }
}
```

#### Layout JSON Structure

```json
{
  "version": 1,
  "widgets": [
    {
      "id": "widget-1",
      "type": "donut-chart",
      "title": "Category Spending",
      "config": {
        "reportType": "categories",
        "dateRange": "current-month"
      },
      "position": { "x": 0, "y": 0, "width": 6, "height": 4 }
    },
    {
      "id": "widget-2",
      "type": "bar-chart",
      "title": "Budget vs Actual",
      "config": {
        "reportType": "budget-comparison",
        "showLegend": true
      },
      "position": { "x": 6, "y": 0, "width": 6, "height": 4 }
    }
  ]
}
```

#### Widget Types

| Type | Description | Config Options |
|------|-------------|----------------|
| `donut-chart` | Category breakdown donut | reportType, dateRange |
| `bar-chart` | Single series bar chart | reportType, orientation, showValues |
| `grouped-bar-chart` | Multi-series comparison | reportType, series |
| `line-chart` | Trend over time | reportType, months, showArea |
| `sparkline` | Mini inline trend | metric, comparison |
| `progress-bar` | Budget progress | categoryId, showLabel |
| `summary-card` | KPI with value | metric, comparison |
| `data-table` | Tabular data | reportType, columns, pageSize |

---

## Out of Scope
- Real-time collaboration on custom reports (future consideration).
- Third-party integrations (e.g., Google Sheets) – revisit after core export is stable.
- Scheduled/emailed reports – future enhancement.
- Report sharing between users.

## Acceptance Criteria
- All existing and new reports are visually modern, actionable, and accessible.
- All chart components render correctly with proper accessibility.
- Users can export any report or custom dashboard as CSV, Excel, or PDF.
- Graphs are interactive and accessible.
- Users can build, save, and export custom report layouts.
- All exports reflect current filters/selections.
- All features covered by unit/integration tests.
- Chart components have comprehensive bUnit tests.

---

## Licensing & Library Policy
- All third-party libraries must be licensed under Apache, MIT, or similarly permissive free/open source licenses.
- No paid or commercial-only libraries are permitted.
- Prefer building in-house solutions over third-party libraries when plausible, especially for core features.
- All libraries must be free or free and open source.
- **Charts**: Built in-house using pure SVG (following DonutChart pattern) – no external charting libraries.
- **Excel/PDF Export**: Deferred until Phase 5 expands beyond CSV.

---

## Implementation Plan

### Phase 1: Bar Chart Component
> **Commit:** `feat(client): add BarChart component for single-series bar charts`

**Objective:** Create a reusable bar chart component following the DonutChart pattern.

**Tasks:**
- [x] Create `BarData.cs` data model
- [x] Create `BarChart.razor` component with SVG rendering
- [x] Implement vertical and horizontal orientations
- [x] Add hover states and tooltips
- [x] Add click handler for drill-down
- [x] Add CSS animations for bar entry
- [x] Write bUnit tests for rendering and interaction
- [ ] Add to component documentation

**Validation:**
- Renders correctly with various data sets
- Tooltips show on hover
- Accessible via keyboard
- Responsive sizing works

---

### Phase 2: Grouped & Stacked Bar Charts
> **Commit:** `feat(client): add GroupedBarChart and StackedBarChart components`

**Objective:** Extend bar chart capabilities for multi-series data.

**Tasks:**
- [x] Create `GroupedBarData.cs` and `BarSeriesDefinition.cs` models
- [x] Create `GroupedBarChart.razor` with side-by-side bars
- [x] Create `StackedBarChart.razor` with stacked segments
- [x] Integrate with `ChartLegend` component
- [x] Add segment/bar tooltips with series information
- [x] Write bUnit tests
- [ ] Document component usage

**Validation:**
- Multiple series display correctly
- Legend correctly identifies series
- Stacking order is consistent

---

### Phase 3: Line & Area Charts
> **Commit:** `feat(client): add LineChart and AreaChart components`

**Objective:** Create line-based charts for trend visualization.

**Tasks:**
- [x] Create `LineData.cs` and `LineSeriesDefinition.cs` models
- [x] Create shared `ChartAxis.razor` and `ChartGrid.razor` components
- [x] Create shared `ChartTooltip.razor` component
- [x] Create `LineChart.razor` with SVG path rendering
- [x] Implement linear and smooth (Catmull-Rom) interpolation
- [x] Add optional area fill (gradient)
- [x] Add data point markers with hover interaction
- [x] Implement reference lines for targets/thresholds
- [x] Create `AreaChart.razor` (extends LineChart with fill)
- [x] Write bUnit tests
- [x] Test with large datasets (performance)

**Validation:**
- Lines render smoothly
- Multi-line charts distinguish series
- Grid and axes scale correctly

---

### Phase 4: SparkLine & Progress Components
> **Commit:** `feat(client): add SparkLine, ProgressBar, and RadialGauge components`

**Objective:** Create compact visualization components for dashboard use.

**Tasks:**
- [x] Create `SparkLine.razor` ultra-compact trend line
- [x] Create `ProgressBar.razor` with threshold coloring
- [x] Create `RadialGauge.razor` circular progress indicator
- [x] Add `ThresholdColor.cs` model for color thresholds
- [x] Ensure all components work at small sizes
- [x] Add animations for progress components
- [x] Write bUnit tests

**Validation:**
- SparkLine fits inline with text
- Progress colors change at thresholds
- Radial gauge renders arc correctly

---

### Phase 5: Export Infrastructure
> **Commit:** `feat(api): add CSV export service infrastructure`

**Objective:** Build server-side export capabilities.

**Tasks:**
- [x] Create `IExportService` interface
- [x] Implement `CsvExportService` for CSV generation
- [x] Create `ExportController` with endpoints
- [x] Add proper Content-Disposition headers
- [x] Write unit tests for export services
- [x] Write integration tests for export endpoints
- [x] Update OpenAPI documentation

**Validation:**
- CSV downloads with correct encoding

---

### Phase 6: Export UI Integration
> **Commit:** `feat(client): add export button component and report integration`

**Objective:** Add export functionality to existing reports.

**Tasks:**
**Tasks:**
- [x] Create `ExportButton.razor` dropdown component
- [x] Integrate into `MonthlyCategoriesReport.razor`
- [x] Integrate into other report pages
- [x] Add loading state during export
- [x] Handle errors gracefully
- [ ] Test file download in various browsers
- [x] Add keyboard accessibility

**Validation:**
- Export button shows format options
- Files download correctly
- Export reflects current filters

---

### Phase 7: Custom Report Builder - Basic
> **Commit:** `feat(client): add custom report builder foundation`

**Objective:** Create the custom report builder page with widget palette.

**Tasks:**
- [ ] Create `CustomReportLayout` domain entity
- [ ] Create `CustomReportLayoutDto` contract
- [ ] Add API endpoints for CRUD operations
- [ ] Create `CustomReportBuilder.razor` page
- [ ] Create `WidgetPalette.razor` component
- [ ] Create `ReportCanvas.razor` drop zone
- [ ] Implement basic drag-and-drop (native HTML5 or minimal JS interop)
- [ ] Create `ReportWidget.razor` wrapper
- [ ] Write tests

**Validation:**
- Widgets can be dragged from palette to canvas
- Layout persists to server
- Saved layouts can be loaded

---

### Phase 8: Custom Report Builder - Advanced
> **Commit:** `feat(client): enhance custom report builder with widget configuration`

**Objective:** Add widget configuration and refinements.

**Tasks:**
- [ ] Add widget configuration panel
- [ ] Implement resize handles for widgets
- [ ] Add grid snapping for layout
- [ ] Enable widget title editing
- [ ] Add duplicate and delete widget actions
- [ ] Implement undo/redo (optional enhancement)
- [ ] Add preset layouts as starting points
- [ ] Export custom report as PDF

**Validation:**
- Widgets can be configured
- Layout is responsive
- Export captures full layout

---

### Phase 9: Testing & Documentation
> **Commit:** `test: add comprehensive tests for chart components and export`

**Objective:** Ensure quality and documentation.

**Tasks:**
- [ ] Add E2E tests for all chart interactions
- [ ] Add E2E tests for export functionality
- [ ] Add E2E tests for custom report builder
- [ ] Run accessibility audit on all chart components
- [ ] Update COMPONENT-STANDARDS.md with chart guidelines
- [ ] Add storybook-style documentation page
- [ ] Performance test with large datasets
- [ ] Update README with feature documentation

**Validation:**
- All tests pass
- No accessibility violations
- Documentation complete

---

## Testing Strategy

### Unit Tests (Components - bUnit)

- [ ] `BarChart` renders correct number of bars
- [ ] `BarChart` applies colors correctly
- [ ] `BarChart` handles empty data gracefully
- [ ] `BarChart` click events fire with correct data
- [ ] `GroupedBarChart` renders multiple series per group
- [ ] `StackedBarChart` calculates segment heights correctly
- [ ] `LineChart` renders path with correct points
- [ ] `LineChart` smooth interpolation produces valid SVG path
- [ ] `AreaChart` fills below line correctly
- [ ] `SparkLine` colors based on trend direction
- [ ] `ProgressBar` changes color at thresholds
- [ ] `RadialGauge` calculates arc correctly

### Unit Tests (Export Services)

- [ ] `CsvExportService` produces valid CSV with headers
- [ ] `CsvExportService` escapes special characters correctly
- [ ] `ExcelExportService` creates valid XLSX file
- [ ] `ExcelExportService` applies formatting correctly
- [ ] `PdfExportService` generates valid PDF
- [ ] Export services handle empty data

### Integration Tests (API)

- [ ] `GET /api/v1/export/transactions/csv` returns valid CSV
- [ ] `GET /api/v1/export/transactions/excel` returns valid XLSX
- [ ] Export endpoints require authentication
- [ ] Export respects user scope (only user's data)
- [ ] Date filters work correctly

### E2E Tests (Playwright)

- [ ] Click export button, select CSV, verify download
- [ ] Chart tooltips appear on hover
- [ ] Chart click navigates to detail view
- [ ] Custom report builder: drag widget to canvas
- [ ] Custom report builder: save and reload layout

---

## Accessibility Considerations

### Charts
- All charts have `role="img"` and `aria-label`
- SVG `<title>` element provides accessible name
- Data tables provided as alternative to visual charts
- Color is not the only indicator (patterns, labels)
- Focus indicators for interactive elements
- Keyboard navigation for drill-down

### Export
- Export button accessible via keyboard
- Loading state announced to screen readers
- Error messages announced appropriately

---

## Performance Considerations

- **SVG Rendering**: Charts use SVG for vector graphics (scales well, prints cleanly)
- **Virtual DOM**: Blazor's diffing handles updates efficiently
- **Large Datasets**: Implement data windowing for charts with 1000+ points
- **PDF Generation**: Generate on server to avoid client-side memory issues
- **Lazy Loading**: Report widgets load data on demand
- **Caching**: Cache report data for short periods (5 minutes)

---

## Security Considerations

- Export endpoints require authentication
- Users can only export their own data
- Rate limiting on export endpoints (prevent abuse)
- File downloads use proper Content-Type headers
- PDF generation runs in sandboxed environment
- No user-provided code execution in charts

---

## Future Enhancements

- **Heatmap Chart**: For daily spending intensity visualization
- **Waterfall Chart**: For income/expense flow
- **Sankey Diagram**: For category flow visualization
- **Treemap**: For hierarchical category breakdown
- **Candlestick Chart**: For account balance ranges
- **Comparative Analysis**: Side-by-side period comparison
- **Scheduled Exports**: Automatic weekly/monthly exports
- **Report Templates**: Pre-built report configurations
- **Collaborative Editing**: Share custom reports with family members

---

## References

- [Feature 050 - Calendar-Driven Reports](./050-calendar-driven-reports-analytics.md) - Related report enhancements
- [Existing DonutChart](../src/BudgetExperiment.Client/Components/Charts/DonutChart.razor) - Pattern to follow
- [Component Standards](./COMPONENT-STANDARDS.md) - Component design patterns
- [QuestPDF Documentation](https://www.questpdf.com/) - PDF generation library
- [ClosedXML Documentation](https://closedxml.github.io/ClosedXML/) - Excel generation library

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-01 | Initial draft | @becauseimclever |
| 2026-02-02 | Expanded with comprehensive chart library, export specs, and implementation phases | @copilot |
| 2026-02-10 | Added LineChart implementation notes, shared chart primitives, and Phase 3 progress | @copilot |
| 2026-02-10 | Added grouped and stacked bar chart implementations and Phase 2 progress | @copilot |
| 2026-02-10 | Completed Phase 3 area chart and gradient fills | @copilot |
| 2026-02-10 | Completed Phase 4 sparkline and progress components | @copilot |
| 2026-02-10 | Started Phase 5 with CSV-only export infrastructure | @copilot |

---
