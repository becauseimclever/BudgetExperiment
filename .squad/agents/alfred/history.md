# Project Context

- **Owner:** Fortinbra
- **Project:** BudgetExperiment — .NET 10 budgeting application
- **Stack:** .NET 10, ASP.NET Core Minimal API, Blazor WebAssembly (plain, no FluentUI), EF Core + Npgsql (PostgreSQL), xUnit + Shouldly, NSubstitute/Moq (one, consistent), StyleCop.Analyzers
- **Created:** 2026-03-22

## Architecture

Clean/Onion hybrid. Layers (outer → inner, dependencies flow inward only):
- `BudgetExperiment.Client` — Blazor WebAssembly UI
- `BudgetExperiment.Api` — REST API, DI wiring, OpenAPI + Scalar, versioning, error handling
- `BudgetExperiment.Application` — use cases, services, validators, mapping, domain event handlers
- `BudgetExperiment.Domain` — entities, value objects, enums, domain events, interfaces (NO EF types)
- `BudgetExperiment.Contracts` — shared DTOs/request/response types
- `BudgetExperiment.Shared` — shared enums (BudgetScope, CategorySource, DescriptionMatchMode)
- `BudgetExperiment.Infrastructure` — EF Core DbContext, repository implementations, migrations

Tests mirror structure under `tests/`.

## Key Conventions

- TDD: RED → GREEN → REFACTOR, always
- Warnings as errors, StyleCop enforced
- One top-level type per file, filename matches type name
- REST endpoints: `/api/v{version}/{resource}` (URL segment versioning, start at v1)
- All DateTime UTC, use `DateTime.UtcNow`
- No FluentAssertions, no AutoFixture
- Exclude `Category=Performance` tests by default
- Private fields: `_camelCase`

## Core Context

### Architecture Quality Assessment (2026-03-22)

- **Overall: B+.** Domain: rich entities, no infrastructure leakage, 21 repo interfaces. Application: 87 services; 54 methods exceed 20-line target. Infrastructure: excellent EF fluent config, scope filtering prevents data leaks. API: textbook REST, DTOs-only, RFC 7807, ETags, OpenAPI+Scalar.
- **Fixes shipped:** ExceptionHandlingMiddleware now uses `DomainException.ExceptionType` enum (not string matching). `this._` prefix removed (~1,474 occurrences). 3 controllers switched to interface injection.
- **DIP verdict (3 controllers):** Interfaces (`ITransactionService`, `IRecurringTransactionService`, `IRecurringTransferService`) already existed and were registered; controllers just used wrong concrete types. Expanded interfaces (+8 methods). Duplicate concrete DI registrations removed.
- **Performance exclusion is correct:** 13 performance tests take 4–5 min. CI separation is optimal (standard ~70s, performance ~4 min, E2E ~10 min).
- **Test suite audit:** 5,413 tests; 2 critical service gaps (RecurringTransactionInstanceService, UserSettingsService); 68 low-value tests cleaned up; vanity enum tests removed; net: 5,449.

### Feature 127: Enhanced Charts & Visualizations — COMPLETE (2808 tests)

**Existing baseline:** 11 hand-rolled SVG chart components, 100% bUnit coverage, 9-theme CSS custom property integration. Active: BarChart + DonutChart. Unused/showcase: LineChart, SparkLine, RadialGauge, AreaChart (removed), GroupedBarChart, StackedBarChart.

**Hybrid approach (chosen):**
- Tier 1 & 2 self-implemented (SVG): Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick, Waterfall, Box Plot
- Tier 3 library (Blazor-ApexCharts): Treemap, Radar — squarified/trig algorithms outsourced
- ApexCharts.js community license (free <$2M revenue) + wrapper MIT. Bundle: ~80 KB gzipped.

**Chart → report page wiring (Slice 8):**
- BudgetComparisonReport: WaterfallChart + RadialBarChart + BudgetRadar
- MonthlyCategoriesReport: BudgetTreemap (HeatmapChart/ScatterChart deferred — need raw transactions)
- MonthlyTrendsReport: StackedAreaChart (income vs spending totals only — no per-category data in DTO)
- CandlestickChart: ComponentShowcase only (daily balance data not in any current report)
- BarChart + DonutChart: untouched

**Slice 9:** AreaChart deleted (zero consumers confirmed by grep). GroupedBarChart/StackedBarChart/LineChart/SparkLine remain (no consumers found, deferred cleanup).

**Slice 10:** ReportsDashboard (`/reports/dashboard`) added; 2804 → 2808 tests.

### ApexCharts Technical Notes (Slice 1)

- v6.x: ES module lazy loading — no `<script>` tag needed; `AddApexCharts()` in `Program.cs` sufficient.
- `ThemeService` has no interface — inject concrete directly (consistent with DI registration).
- CSS vars not readable from C# — `ChartColorProvider` hardcodes design-token values; update on `tokens.css` changes.
- Dark themes: only `dark` and `vscode-dark`; `system` defaults to light (no async JS sync available).
- `@namespace` directive required for `.razor` files in a subdirectory (e.g., `ApexCharts/`).
- `[Inject]` nullable workaround (Blazor/.NET 10): inject `IServiceProvider` and call `GetService<T>()`.
- `GetHashCode()` for category colour cycling is session-stable (acceptable for in-session visual assignment).

### Documentation Fixes (README/CONTRIBUTING)

- Added `BudgetExperiment.Shared` to project list (was missing).
- Test run command requires `Category!=Performance` filter — updated README and CONTRIBUTING.
- Docker prerequisite for Testcontainers tests (Infrastructure.Tests, Api.Tests) now documented.
- Typo: `cd BudgetExpirement` → `cd BudgetExperiment` fixed.

## Learnings

### Feature 120: Plugin System Planning (2026-03-22)

**Domain Event Scaffolding Discovery:**
- `Transaction._domainEvents` exists at line 17 (`List<object>`) but is never dispatched or consumed.
- No `IDomainEvent` interface exists — must be created in Domain layer.
- No Events folder exists in Domain — needs creation.
- `BudgetDbContext` does not override `SaveChangesAsync` to dispatch events — wiring required.

**Plugin Architecture Boundary Issue:**
- `IDomainEventHandler<TEvent>` requires constraint `where TEvent : IDomainEvent`.
- Plugin.Abstractions must have zero core deps per spec — but needs `IDomainEvent`.
- **Resolution:** Recommend moving `IDomainEvent` marker to Plugin.Abstractions; Domain references Abstractions for the marker only. This inverts typical layering but keeps plugin authoring simple (single SDK reference).

**Blazor WASM Limitation:**
- WASM cannot dynamically load assemblies at runtime.
- Plugin Blazor pages cannot be hot-pluggable in current architecture.
- **MVP Scope:** Plugin UI limited to navigation links and management page. Full page support deferred.

**Existing Patterns Observed:**
- Report builders use `ITrendReportBuilder`, `ILocationReportBuilder` — not a unified `IReportBuilder` interface yet.
- Import service uses composition of sub-services (`IImportRowProcessor`, etc.) — no `IImportParser` abstraction exists.
- NavMenu renders static nav items — no dynamic plugin section yet.

**Test Infrastructure:**
- All API/Infrastructure tests use PostgreSQL Testcontainers.
- Integration tests for plugin loading will need sample plugin DLL fixture.
