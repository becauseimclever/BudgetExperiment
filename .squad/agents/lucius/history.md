# Project Context

- **Owner:** Fortinbra
- **Project:** BudgetExperiment — .NET 10 budgeting application
- **Stack:** .NET 10, ASP.NET Core Minimal API, Blazor WebAssembly (plain, no FluentUI), EF Core + Npgsql (PostgreSQL), xUnit + Shouldly, NSubstitute/Moq (one, consistent), StyleCop.Analyzers
- **Created:** 2026-03-22

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

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-04-04 — Feature 127 Slice 2: ChartDataService Implementation (GREEN)

**Task:** Implement `ChartDataService` passing all 20 RED tests written by Barbara. Register in DI.

**Delivered:**
- `src/BudgetExperiment.Client/Services/ChartDataService.cs` — sealed class implementing `IChartDataService`.
- `Program.cs` — `AddScoped<IChartDataService, ChartDataService>()`.
- Fixed SA1512 and SA1515 StyleCop violations in Barbara's test file.

**Key algorithms:**
- `DayOfWeek.Sunday = 0` in C# → mapped to heatmap row 6 (Mon=0…Sun=6): `dow == DayOfWeek.Sunday ? 6 : (int)dow - 1`.
- WeekIndex = `(mondayOfThisWeek.DayNumber - mondayOfFirstWeek.DayNumber) / 7` — 0-based offset from earliest transaction.
- Waterfall sort: ascending by `Amount.Amount` (all-negative spending = descending by absolute value). Using `.Amount.Amount` to unwrap `CategorySpendingDto.Amount: MoneyDto`.
- Tukey's hinges: odd n excludes median, even n splits at midpoint. Shared `ComputeMedian` helper.
- `monthsBack` reference = `list.Max(t => t.Date)` — deterministic regardless of wall clock.
- Candlestick: sort by date before `GroupBy` so `g.First()`/`g.Last()` are chronologically correct.

**Test results:** 20/20 ChartDataService tests GREEN. Full Client suite: 2718 passed, 0 failed, 1 pre-existing skip.

**Cross-agent facts (same session):**
- Alfred created the 7 model types + `IChartDataService` interface before Barbara wrote the RED tests.
- Barbara wrote 20 xUnit RED tests establishing behavioral contracts for all 4 methods.

### 2026-04-04 — Feature 127 Slice 5: WaterfallChart and BoxPlotChart Blazor SVG Components (GREEN)

**Task:** Implement `WaterfallChart` and `BoxPlotChart` Razor components to pass 16 RED bUnit tests written by Barbara.

**Delivered:**
- `src/BudgetExperiment.Client/Components/Charts/WaterfallChart.razor` + `.razor.cs` + `.razor.css`
- `src/BudgetExperiment.Client/Components/Charts/BoxPlotChart.razor` + `.razor.cs` + `.razor.css`
- Fixed xUnit2013 lint error in `BoxPlotChartTests.cs`: `Assert.Equal(0, outliers.Count)` → `Assert.Empty(outliers)`.

**SVG geometry choices:**
- Both use `OnParametersSet()` to precompute render info into private `_fields` — avoids double computation when both bars/connectors (waterfall) and box/whsker/outlier collections (boxplot) are iterated in the razor.
- WaterfallChart: Y-range includes 0 explicitly. Non-total bars: start = `RunningTotal - Amount`, end = `RunningTotal`. Total bars: start = 0, end = RunningTotal. N-1 horizontal connector lines at each bar's `RunningTotal` Y level.
- BoxPlotChart: 2 whisker lines per distribution (lower: Q1→Min, upper: Q3→Max). Outlier circles rendered for every value in `d.Outliers`. `RenderSvgText` via `MarkupString` for labels.

**StyleCop member ordering (SA1202 + SA1204):**
- Methods must be ordered: `protected` → `private static` → `private instance`.
- SA1204: Within the private method group, static methods BEFORE non-static.
- SA1202: `protected override OnParametersSet()` MUST come before all private methods, even private static helpers like `F()`.

**Test results:** 2785 passed, 0 failed, 1 pre-existing skip. All 16 new tests GREEN.

---



**Task:** Implement `HeatmapChart` and `ScatterChart` Razor components to pass 16 RED bUnit tests written by Barbara.

**Delivered:**
- `src/BudgetExperiment.Client/Components/Charts/HeatmapChart.razor` + `.razor.cs` + `.razor.css`
- `src/BudgetExperiment.Client/Components/Charts/ScatterChart.razor` + `.razor.cs` + `.razor.css`
- Fixed SA1512 violations in `HeatmapChartTests.cs` and `ScatterChartTests.cs` (Barbara's NOTE comments followed by blank line before usings).

**SVG geometry choices:**
- HeatmapChart: jagged array iteration — outer `Data[]` rows = days, inner arrays = week cells. `IsEmpty` means null Data OR all rows have Length=0 (not zero-amount). CellSize=16, CellGap=2, LabelAreaWidth=32. ViewBox width computed from max WeekIndex+1.
- ScatterChart: fixed ViewBox 400×200. X maps DateOnly ticks linearly; Y maps Amount with SVG Y-inversion (`ChartAreaBottom - ratio * ChartHeight`). Single-point case uses `ratio=0.5` to center.
- Both use `aria-hidden="true"` on the SVG with `role="img" aria-label="..."` on the outer div — different from BarChart which puts `role="img"` on the SVG itself.

**Test-to-component mapping notes:**
- `heatmap-cell-empty` class only applies when `TotalAmount == 0` regardless of non-zero `TransactionCount`. bUnit `ClassName` check works because the full class string contains both "heatmap-cell" and "heatmap-cell-empty".
- Outlier circles: `scatter-point scatter-point-outlier` (both classes) — `FindAll("circle.scatter-point")` matches both, then `ClassName.Contains("scatter-point-outlier")` differentiates.
- `RenderSvgText` via MarkupString used for `heatmap-row-label` elements (same pattern as BarChart axis labels) — avoids Blazor `<text>` element discrimination issues.
- Barbara's test files have the same SA1512 pattern issue (blank line after NOTE comment block) — fix proactively when implementing components for her tests.
- Alfred separately completed Slice 1 (ApexCharts + `ChartThemeService` + `ChartColorProvider` + 11 tests). Final Client suite: 2729.

---

### Performance Baseline: Multi-Scenario Capture (2026-03-23)

**Task:** Capture and commit NBomber performance baselines for all load test scenarios.

**Root Cause:** Previous baseline.json only had get_transactions from an earlier stress test run. LoadTests.cs has 4 scenarios (accounts, budgets, calendar, transactions). The other three showed as "New" in every CI comparison — no regression detection.

**CI Bug Discovered:** performance.yml used 	ail -1 to pick the alphabetically-last CSV file. With multiple load test CSVs (load_accounts.csv, load_budgets.csv, load_calendar.csv, load_transactions.csv), only load_transactions.csv was ever compared. Fixed by looping over all CSVs.

**NBomber Behavior:** Each NBomber runner instance cleans/overwrites the reports folder on init. Running 4 sequential load tests = only the last test's CSV survives. Metrics must be extracted from console output for previous runs. The aselines/README.md instructions are correct — use the BaselineComparer --generate mode with the CSV artifact.

**Metrics Captured (dev hardware, in-memory DB, LoadProfile 10 req/s 35s):**
- get_accounts:     p95=0.66ms, p99=0.84ms, RPS=9.14
- get_budgets:      p95=0.76ms, p99=1.54ms, RPS=9.14
- get_calendar:     p95=12.02ms, p99=15.53ms, RPS=9.14
- get_transactions: p95=11.61ms, p99=30.43ms, RPS=9.14

**Decision:** Thresholds already correct (maxLatencyRegressionPercent=15%). No change needed to ThresholdConfig defaults.

### 2026-04-04 — Feature 127 Slice 4: StackedAreaChart, RadialBarChart, CandlestickChart (GREEN)

**Task:** Implement 3 new Blazor SVG chart components to pass 24 RED bUnit tests written by Barbara.

**Delivered:**
- `src/.../Charts/StackedAreaChart.razor` + `.razor.cs` + `.razor.css`
- `src/.../Charts/RadialBarChart.razor` + `.razor.cs` + `.razor.css`
- `src/.../Charts/CandlestickChart.razor` + `.razor.cs` + `.razor.css`

**Key implementation choices:**
- StackedAreaChart: cumulative stacking via `baselines[]` clone + `tops[]` per-series pass. `AllDates` gathered from union of all series points (Distinct + OrderBy). Single-point edge case handled by centering X at `MarginLeft + ChartWidth/2` — degenerate vertical path is valid SVG. `ComputeStackedPaths()` returns list of private `StackedPathInfo` records with Color + PathData.
- RadialBarChart: ring `i` has radius = `Size/2 - 10 - i*22`. `DashOffset = max(0, circ * (1 - pct/100))`. Labels rendered as SVG `<text>` via `RenderSvgText` MarkupString — bUnit FindAll(".radial-bar-label") matches SVG text elements. `stroke-dashoffset` attribute output via F() is always a plain double string (parseable by test assertions).
- CandlestickChart: static helper `GetCandleBodyClass()` returns combined class string (`"candlestick-body candlestick-bullish"` or `"...-bearish"`) — avoids Razor string-literal-in-attribute parsing complexity. Body height clamped to minimum 1px for doji candles.

**StyleCop SA1204 lesson (IMPORTANT):** Static methods must appear BEFORE instance methods in the class body, even when they are private helpers called by instance methods. Ordering: `[Parameter]` public props → private static constants/props → private instance props → private static methods (including `F()`, `MapX()`, `MapY()`) → private instance methods → nested private types. Build will fail otherwise.

**SA1407 lesson:** Mixed arithmetic `a + b * c` must use explicit grouping: `a + (b * c)`. Even when precedence is unambiguous, StyleCop requires parentheses around each precedence group in mixed-operator expressions. Pattern from ScatterChart: `return ChartAreaBottom - (ratio * ChartHeight);` — match this pattern exactly.

**Test results:** 2769 passed, 0 failed, 1 pre-existing skip. All 24 new tests GREEN.

---

## Core Context

### Backend Code Quality & Architecture (2026-03-22)

**Architecture Quality: B+ (Strong fundamentals with targeted improvements)** — Domain: Rich behavior-focused entities, no infrastructure leakage, 21 repository interfaces, 4 domain services, comprehensive scope mgmt. Application: 87 services, explicit mapping, main issue: 54 methods exceed 20-line guideline (6 with 3+ nesting). Infrastructure: Excellent EF fluent config, repository scope filtering prevents data leaks, optimistic concurrency via PostgreSQL xmin. API: Textbook REST, DTOs-only, RFC 7807 Problem Details, ETag support, OpenAPI + Scalar configured.

**Critical Issues:** (1) Six methods with 3+ nesting levels need refactoring (TransactionListService recurring, ImportValidator, RuleSuggestionParser). (2) ExceptionHandlingMiddleware uses string matching instead of DomainException.ExceptionType enum. (3) Three services registered as both interface + concrete (no justification). **Method Count:** 54 violations total — 40 Application, 19 Infrastructure configs (acceptable), ~10 spread.

**Positives:** Zero infrastructure types in Domain. Comprehensive scope filtering at repo level (security). No primitive obsession (proper value objects). No dead code. StyleCop warnings-as-errors enforced.

### Feature 111: Performance Optimizations (2026-03-22)

**AsNoTracking Propagation:** Added AsNoTracking/AsNoTrackingWithIdentityResolution to all read-only repository queries; preserved change tracking on update paths. **Parallelized Hot Paths:** CalendarGridService (9+ sequential → parallelized), TransactionListService (similar), DayDetailService (orchestration-level). Used scoped query helper with fallback for test constructors. Registered DbContextFactory for future parallel support. **Bounded Eager Loading:** AccountRepository reduced to 90-day lookback (production Pis with large histories need this). Added extension interfaces (IAccountTransactionRangeRepository, IAccountNameLookupRepository). DayDetailService uses targeted account-name lookup instead of full history. **Result:** Build green (-warnaserror). No regression in entity refresh.

### CI Fixes & Infrastructure (2026-03-22 → 2026-03-23)

**Performance CI Actions:** Fixed broken version references (checkout@v6, upload-artifact@v7, setup-dotnet@v5, cache@v5 → all @v4). Entire performance CI pipeline couldn't run.

**Baseline Committed:** Used local stress_transactions.csv (1335 requests, p99=7.3ms) per Decision 5 (smoke tests are sanity checks, not baselines). CI Smoke artifact only contained in-memory data unsuitable for baselines. Updated baseline.json with stress data.

**PostgreSQL 18 Upgrade:** Migrated docker-compose.demo.yml and docs from postgres:16 to postgres:18 using Docker Hardened Image (dhi.io/postgres:18). Npgsql 10.0.0 supports 13–18 (no driver changes). EF migrations fully compatible.

### Code Quality Fixes (2026-03-22)

- **ExceptionHandlingMiddleware:** Replaced string matching with `switch (domainEx.ExceptionType)` enum routing. Created `DomainExceptionType` enum (Validation=0, NotFound=1). Updated all 17 "not found" throw sites.
- **DI registrations clarified:** All three "backward compat" concrete registrations (`TransactionService`, `RecurringTransactionService`, `RecurringTransferService`) are legitimate — controllers inject concrete types. Comments updated to name actual consumers.
- **DateTime.Now → DateTime.UtcNow** in Reconciliation.razor (3 occurrences).
- **Interface expansion:** Expanded 2 service interfaces (+8 methods total). Switched 3 controllers to interface injection to satisfy DIP. Removed `this._` usages (1,474 occurrences).

### Test Gaps Filled (2026-03-23)

- `RecurringTransactionInstanceServiceTests` — 20 new tests (GetInstancesAsync, ModifyInstanceAsync, SkipInstanceAsync, GetProjectedInstancesAsync).
- `UserSettingsServiceTests` — 17 new tests (GetCurrentUserProfile, GetCurrentUserSettingsAsync, UpdateCurrentUserSettingsAsync, CompleteOnboardingAsync, GetCurrentScope, SetCurrentScope).
- Result: Application.Tests: 982 → 1,019 (+37). Full suite: 5,412 → 5,449.

### Feature 127: Chart Component StyleCop Rules (2026-04-04)

- **SA1204:** All `private static` methods/properties MUST appear before `private` instance members. Applies to `.razor.cs` partial classes. Violation = build error.
- **SA1407:** Mixed arithmetic requires explicit parentheses: `a + (b * c)` not `a + b * c`.
- **SA1202:** `protected override OnParametersSet()` must come before all private methods.
- **SA1201 (ApexCharts):** Fields → Properties (public `[Parameter]` before private `[Inject]`) → Methods → Nested types (private `record` AFTER methods).
- **`@namespace` directive:** Required for `.razor` files in subdirectories (e.g., `ApexCharts/`) to match the code-behind namespace.
- **`[Inject]` nullable workaround:** Inject `IServiceProvider` and use `GetService<T>()` — nullable `[Inject]` still throws in .NET 10 + bUnit when service is unregistered.
- **`@if (Visible)` for visibility-controlled components:** `cut.Markup.Trim()` returns empty when `Visible=false` — cleaner than CSS display:none.

---

## Recent Work

*(2026-03-22/23 verbose entries summarized in Core Context above.)*

## Learnings

### 2026-04-04 — ChartDataService Algorithm Choices

- **Heatmap DayIndex mapping:** C# DayOfWeek.Sunday = 0, so Mon→0...Sat→5, Sun→6 requires dow == DayOfWeek.Sunday ? 6 : (int)dow - 1. WeekIndex = (mondayOfThisWeek.DayNumber - mondayOfFirstWeek.DayNumber) / 7. Using DayNumber (int) avoids TimeSpan arithmetic.
- **Waterfall ordering:** OrderBy(s => s.Amount.Amount) on negative spending values sorts from most-negative to least-negative, which equals "largest absolute spend first" — equivalent to OrderByDescending(s => Math.Abs(...)) but simpler.
- **Tukey's hinges:** For odd n, exclude the median element from both halves. For even n, split into two equal halves. SA1407 fires on unarenthesized mixed arithmetic like 
 / 2 - 1 — must write (n / 2) - 1.
- **Box plot whiskers:** Maximum and Minimum are the extreme NON-OUTLIER values, not the raw data extremes. Always compute 
onOutliers = sorted.Where(v => v >= lowerFence && v <= upperFence) and report 
onOutliers.Max() / 
onOutliers.Min().
- **monthsBack cutoff:** Use max(transaction.Date) from the dataset, not DateTime.UtcNow, so test data stays deterministic regardless of wall-clock time.
- **LINQ GroupBy preserves source order within groups:** After OrderBy(b => b.Date), calling GroupBy(...) then g.First() / g.Last() correctly returns the chronologically first/last balance per month without a secondary sort inside the group.
- **SA1512 reminder:** Section separator comment lines (// ─────) followed by a blank line violate SA1512. The blank line after the last separator must be removed; the next statement must immediately follow the comment block.

---

## Session: 2026-04-04 — Feature 127 Slice 6: BudgetTreemap and BudgetRadar (ApexCharts Tier 3)

**Requested by:** Fortinbra  
**Status:** Complete

### Work Done
- Created `src/BudgetExperiment.Client/Components/Charts/ApexCharts/` subdirectory (first ApexCharts-backed components in the project).
- Implemented `BudgetTreemap` (`BudgetTreemap.razor` + `BudgetTreemap.razor.cs`) — treemap chart backed by Blazor-ApexCharts.
- Implemented `BudgetRadar` (`BudgetRadar.razor` + `BudgetRadar.razor.cs`) — radar chart backed by Blazor-ApexCharts, with per-series `ApexPointSeries` rendered from a `Dictionary<string, List<RadarDataPoint>>`.
- Both components in namespace `BudgetExperiment.Client.Components.Charts` (physical path in `ApexCharts/` subdirectory, requiring `@namespace` directive in `.razor` files).

### Build/Test Outcomes
- 0 errors after SA1201 member ordering fixes and namespace directive.
- 2797 passed, 0 failed (12 new tests green; ≥ 2797 total confirmed).

### Key Decisions / Lessons Learned
- **`@namespace` directive is required** when `.razor` files live in a subdirectory but the component must be in the parent namespace. Without it, Razor generates the class in a derived namespace (`.ApexCharts`) causing CS0115 on `override OnParametersSet()`.
- **`[Inject]` with nullable type does NOT suppress the DI exception in Blazor/.NET 10**: Even `[Inject] private IFoo? Foo { get; set; }` throws `InvalidOperationException` if `IFoo` is not registered. Fix: inject `IServiceProvider` (always available) and call `GetService<T>()` (returns null when not registered).
- **SA1201 ordering for ApexCharts components**: Fields → Properties (public `[Parameter]` before private `[Inject]`) → Methods → Nested types (private `record` must come AFTER the method, not before).

---

## Session: 2026-04-04 — Feature 127 Slice 7: Visual Polish & Interactivity (GREEN)

**Requested by:** Fortinbra
**Status:** Complete

### Work Done

**Part 1: `ExportChartButton` (new component)**
- `src/.../Charts/ExportChartButton.razor` — `@if (Visible)` guard around `<button type="button" class="export-chart-btn" aria-label="Export chart">Export</button>`
- `src/.../Charts/ExportChartButton.razor.cs` — Params: `ChartTitle string` (default `"chart"`), `Visible bool` (default `true`). `HandleExportAsync` returns `Task.CompletedTask` (JS screenshot interop deferred).
- No CSS file — styles inherited from global chart stylesheet.

**Part 2: `AnimationsEnabled` parameter on `ScatterChart`**
- `ScatterChart.razor.cs` — Added `AnimationsEnabled bool` param (default `true`) + `ContainerClass` private computed property.
- `ScatterChart.razor` — Outer div: `class="@ContainerClass"` (was hardcoded `"scatter-chart"`).
- `ScatterChart.razor.css` — Added `.chart-no-animation *` and `@media (prefers-reduced-motion: reduce)` rules.

**Test file fix:**
- `ScatterChartTests.cs` — Removed blank line between `// NOTE:` comment and `[Fact]` attribute (SA1512).

### Build/Test Outcomes
- 0 errors, 0 warnings.
- **2804 passed, 0 failed, 1 pre-existing skip.**
- All 7 new tests (5 ExportChartButtonTests + 2 ScatterChart animation) GREEN.

### Key Decisions / Lessons Learned
- **SA1204 applies to properties too**: `ContainerClass` is a private instance computed property; it must appear AFTER all `private static` properties in the class body. Block order: `[Parameter]` public props → `private static` props → `private` instance computed props → methods.
- **Visibility-controlled components**: `@if (Visible) { ... }` is cleaner than a CSS `display:none` approach for components with `Visible` bool params. When `Visible=false`, `cut.Markup.Trim()` returns empty string in bUnit assertions.
- **Feature 127 scope**: All slices 1–7 complete. Final test count: **2804 passed**.
