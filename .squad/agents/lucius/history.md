# Project Context

- **Owner:** Fortinbra
- **Project:** BudgetExperiment — .NET 10 budgeting application
- **Stack:** .NET 10, ASP.NET Core Minimal API, Blazor WebAssembly (plain, no FluentUI), EF Core + Npgsql (PostgreSQL), xUnit + Shouldly, NSubstitute/Moq (one, consistent), StyleCop.Analyzers

## Architecture

Clean/Onion hybrid. Projects: Domain, Application, Infrastructure, Api, Client, Contracts, Shared.
Tests under `tests/` mirror the src structure.

## Key Conventions

- TDD: RED → GREEN → REFACTOR
- `dotnet add <csproj> package <name> --version <ver>` — never hand-edit PackageReference blocks
- Warnings as errors, StyleCop enforced, nullable reference types enabled
- One top-level type per file
- Private fields: `_camelCase`, async methods end with `Async`
- REST: `/api/v{version}/{resource}`, URL segment versioning, v1 start
- All DateTime UTC; DateOnly for date-only fields
- No FluentAssertions, no AutoFixture, no AutoMapper
- Migrations live in Infrastructure; EF types never leave Infrastructure

## Core Context

### Backend Code Quality & Architecture (2026-03-22)

- **Architecture: B+.** Domain: rich entities, no infrastructure leakage, 21 repo interfaces, 4 domain services. Application: 87 services, explicit mapping; 54 methods exceed 20-line guideline (6 with 3+ nesting). Infrastructure: excellent EF fluent config, scope filtering. API: textbook REST, DTOs-only, RFC 7807, ETags, OpenAPI+Scalar.
- **Fixes:** ExceptionHandlingMiddleware → enum routing. 9 deeply nested methods flattened. 1,474 `this._` usages removed. 2 service interfaces expanded (+8 methods). 3 controllers switched to interface injection. Duplicate concrete DI registrations removed.

### Feature 111: Performance Optimizations (2026-03-22)

- Added `AsNoTracking`/`AsNoTrackingWithIdentityResolution` to all read-only queries.
- Parallelized hot paths: CalendarGridService (9+ sequential → parallel), TransactionListService, DayDetailService.
- Registered `DbContextFactory` for future parallel support (but did NOT wire into services yet — dead registration removed later).
- Bounded eager loading: AccountRepository 90-day lookback. Added `IAccountTransactionRangeRepository`, `IAccountNameLookupRepository`.

### CI Fixes & Infrastructure (2026-03-22 → 2026-03-23)

- Fixed performance CI action versions (checkout@v6, upload-artifact@v7 → all @v4).
- Committed NBomber baselines from stress CSV (1335 requests, p99=7.3ms). CI loop fixed to compare all scenario CSVs.
- PostgreSQL 18 upgrade in docker-compose.demo.yml (dhi.io/postgres:18 hardened image).

### Code Quality Fixes (2026-03-22)

- ExceptionHandlingMiddleware: `switch (domainEx.ExceptionType)` enum routing. Created `DomainExceptionType` (Validation=0, NotFound=1).
- `DateTime.Now → DateTime.UtcNow` in Reconciliation.razor (3 occurrences).
- Interface expansion: `IRecurringTransactionService` + `IRecurringTransferService` expanded (+8 methods total).

### Test Gaps Filled (2026-03-23)

- `RecurringTransactionInstanceServiceTests` — 20 new tests (GetInstancesAsync, ModifyInstanceAsync, SkipInstanceAsync, GetProjectedInstancesAsync).
- `UserSettingsServiceTests` — 17 new tests. Net: Application.Tests 982 → 1,019. Full suite: 5,412 → 5,449.

### Feature 127: Chart Component StyleCop Rules

- **SA1204:** `private static` methods MUST appear BEFORE `private` instance members in class body. Applies to `.razor.cs` partials. Violation = build error.
- **SA1407:** Mixed arithmetic requires explicit parentheses: `a + (b * c)` not `a + b * c`.
- **SA1202:** `protected override OnParametersSet()` must come BEFORE all private methods.
- **SA1201 (ApexCharts):** Fields → Properties (`[Parameter]` before `[Inject]`) → Methods → Nested types.
- **`@namespace` directive:** Required for `.razor` files in subdirectories (e.g., `ApexCharts/`).
- **`[Inject]` nullable:** Nullable `[Inject]` still throws in .NET 10 + bUnit. Workaround: inject `IServiceProvider`, call `GetService<T>()`.

### Feature 127: Slice Implementation Summary

| Slice | Delivered | Key Decision |
|-------|-----------|--------------|
| 2: ChartDataService | `ChartDataService` GREEN (20 tests). DayOfWeek.Sunday=0→row[6]; candlestick sort by date before GroupBy. | `DailyBalanceDto` lives in Client `Charts.Models` — not Contracts |
| 3: HeatmapChart + ScatterChart | Both GREEN (8+8 tests). ViewBox 400×200 for scatter; jagged array for heatmap. | `RenderSvgText` via `MarkupString` for SVG `<text>` labels |
| 4: StackedAreaChart + RadialBarChart + CandlestickChart | All GREEN (24 tests). Cumulative stacking via baselines[]. DashOffset = circ*(1-pct/100). Static helper for candle class. | SA1204: static helpers BEFORE instance methods |
| 5: WaterfallChart + BoxPlotChart | Both GREEN (16 tests). Non-total bars: start=RunningTotal-Amount. Whiskers at last non-outlier. | SA1202: `protected override` before private |
| 6: BudgetTreemap + BudgetRadar | Both GREEN (12 tests). First ApexCharts components in `ApexCharts/` subdirectory. `@namespace` directive required. | `[Inject]` nullable workaround via IServiceProvider |
| 7: Visual Polish | ExportChartButton + AnimationsEnabled on ScatterChart. 7 new tests (5+2). | `@if (Visible)` cleaner than CSS `display:none` for conditional render |
| 8: Wire into reports | BudgetComparisonReport (Waterfall+Radial+Radar), MonthlyCategoriesReport (Treemap), MonthlyTrendsReport (StackedArea). ComponentShowcase updated with all 9. | HeatmapChart/ScatterChart deferred (need raw transactions) |
| 9: AreaChart removal | Deleted AreaChart.razor, .razor.cs, AreaChartTests.cs. Zero consumers confirmed. No Models orphans. | GroupedBarChart/StackedBarChart deferred (shared axis infra) |

## Recent Work

### 2026-04-04 — Feature 127 Slice 10: ReportsDashboard Page (GREEN)

**Requested by:** Fortinbra — Status: Complete

**Files created:**
- `src/BudgetExperiment.Client/Pages/Reports/ReportsDashboard.razor` — Route `/reports/dashboard`. Loading: `<div class="dashboard-loading">` while `_isLoading`. Loaded: `.dashboard-treemap`, `.dashboard-waterfall`, `.dashboard-radar`, `.dashboard-heatmap`, `.dashboard-radial-bar`, `.dashboard-filter`.
- `src/BudgetExperiment.Client/Pages/Reports/ReportsDashboard.razor.cs` — Injects `IBudgetApiService` + `IChartDataService`. Fields first (SA1201), then `[Inject]` properties. `LoadDashboardAsync`: first await = `GetMonthlyCategoryReportAsync` (required for loading-state test), then `GetTransactionsAsync`, then `GetBudgetSummaryAsync`. `_isLoading = false` at end. Individual try-catch blocks per API call.
- `src/BudgetExperiment.Client/Pages/Reports/ReportsDashboard.razor.css` — Grid, 1-col mobile, 2-col ≥1024px, treemap/heatmap full width.

**Modified:** `ReportsIndex.razor` — replaced "Year in Review – Coming Soon" with "Financial Dashboard" card linking to `/reports/dashboard`.

**Build:** 0 errors, 0 warnings. **Tests:** 2808 passed, 0 failed, 1 pre-existing skip. All 6 new tests GREEN.

**Key Decisions / Lessons:**
- SA1201 in code-behind: fields MUST come before `[Inject]` properties.
- Loading-state test: `GetMonthlyCategoryReportAsync` MUST be the FIRST await in `OnInitializedAsync`.
- `ReportsDashboard` is first reports page using separate `.razor.cs` code-behind (others use inline `@code {}`).
- `IChartDataService` has no `BuildTreemapData` — treemap data built directly in code-behind from `MonthlyCategoryReportDto.Categories`.
- `HeatmapChart` parameter is `Data` (not `DataPoints`).
- `_Imports.razor` — added `@using BudgetExperiment.Client.Components.Charts.Models` globally in Slice 8.
