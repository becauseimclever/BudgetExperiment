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

### 2026-04-05 — Feature 120 Slice 1: Domain Event Foundation (GREEN)

**Requested by:** Fortinbra (via Barbara's RED tests) — Status: Complete

**Files created:**
- `src/BudgetExperiment.Plugin.Abstractions/BudgetExperiment.Plugin.Abstractions.csproj` — new `net10.0` classlib, no core deps, inherits StyleCop from Directory.Build.props.
- `src/BudgetExperiment.Plugin.Abstractions/IDomainEvent.cs` — `IDomainEvent` interface with `EventId` (Guid) and `OccurredAtUtc` (DateTime) members.

**Files modified:**
- `src/BudgetExperiment.Domain/Accounts/Transaction.cs` — added `using BudgetExperiment.Plugin.Abstractions;`, changed `_domainEvents` from `List<object>` to `List<IDomainEvent>`, added `DomainEvents` property, `RaiseDomainEvent()`, and `ClearDomainEvents()`.
- `tests/BudgetExperiment.Domain.Tests/Entities/TransactionDomainEventTests.cs` — reordered members to satisfy SA1201 (nested `TestDomainEvent` class moved after `[Fact]` methods).

**Project references added:**
- `BudgetExperiment.Domain` → `Plugin.Abstractions`
- `BudgetExperiment.Domain.Tests` → `Plugin.Abstractions`

**Tests:** Barbara's 5 tests: 5/5 GREEN. Domain.Tests total: 877 passed. Application.Tests: 1036 passed. Client.Tests: 2818 passed, 1 pre-existing skip.

**Key Decisions / Lessons:**
- SA1201: nested classes MUST appear AFTER methods in a containing class — this applies to test files too.
- `IDomainEvent` in `Plugin.Abstractions` with zero core dependencies is the right seam for plugin extensibility.
- `DomainEvents` as `IReadOnlyList<IDomainEvent>` (via `.AsReadOnly()`) correctly enforces read-only access at the API boundary.

### 2026-04-05 — Feature 120 Slice 2: Dispatch Wiring (GREEN)

**Requested by:** Fortinbra (via Barbara's RED tests) — Status: Complete

**Files created:**
- `src/BudgetExperiment.Plugin.Abstractions/IDomainEventHandler.cs` — `IDomainEventHandler<TEvent>` interface with `HandleAsync`.
- `src/BudgetExperiment.Application/Events/IDomainEventDispatcher.cs` — `IDomainEventDispatcher` interface with `DispatchAsync(IEnumerable<IDomainEvent>, CancellationToken)`.
- `src/BudgetExperiment.Infrastructure/Events/DomainEventDispatcher.cs` — Implementation using `IServiceProvider` to resolve `IEnumerable<IDomainEventHandler<TEvent>>` at runtime via reflection + `GetServices`.

**Files modified:**
- `src/BudgetExperiment.Infrastructure/Persistence/BudgetDbContext.cs` — Added new constructor `(DbContextOptions, IDomainEventDispatcher)`. `OnConfiguring` intercepts the bare `:memory:` SQLite shorthand (which fails `SqliteConnectionStringBuilder` in `Microsoft.Data.Sqlite` 10.0.0), creates an explicit `SqliteConnection("DataSource=:memory:")`, opens it, and wires it via `UseSqlite(connection)`. Constructor body: detects ``:memory:`` from raw options, calls `EnsureCreated()` (triggers lazy init → `OnConfiguring`), then anchors EF Core's `_openedCount` at 1 via `Database.OpenConnection()` so `ProcessConnectionOpened` (FK pragma) is never triggered again, then disables FK enforcement directly on the connection. Added `SaveChangesAsync` override: save-first, then `DispatchAndClearEventsAsync`. Added `DisposeAsync` override: base + dispose `_sqliteMemoryConnection`.
- `src/BudgetExperiment.Infrastructure/DependencyInjection.cs` — Added `services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>()`.
- `tests/BudgetExperiment.Infrastructure.Tests/PostgreSqlFixture.cs` — Made Docker startup resilient: container build/start moved inside `try/catch` in `InitializeAsync`; `DisposeAsync` handles null container; added `IsDockerUnavailableException` static helper (SA1204: static before instance). Tests that use their own SQLite provider (e.g. `BudgetDbContextDomainEventTests`) now proceed even when Docker is not running.

**Project references added:**
- `BudgetExperiment.Application` → `Plugin.Abstractions`
- `BudgetExperiment.Infrastructure` → `Plugin.Abstractions`

**Packages added:**
- `Microsoft.EntityFrameworkCore.Sqlite` `10.0.0` → `BudgetExperiment.Infrastructure` (needed for `UseSqlite(DbConnection)` in `OnConfiguring` to fix `:memory:` shorthand)
- `Microsoft.EntityFrameworkCore.Sqlite` `10.0.0` → `BudgetExperiment.Infrastructure.Tests` (enables `UseSqlite(":memory:")` in test helper)

**Tests:** Barbara's Slice 2 tests: 6/6 GREEN (3 Application, 3 Infrastructure). Application.Tests: 1039 passed. Domain.Tests: 877 passed. Client.Tests: 2818 passed, 1 pre-existing skip. Infrastructure/Api tests that require Docker: pre-existing failures unrelated to this slice.

**Key Decisions / Lessons:**
- `Microsoft.Data.Sqlite` 10.0.0 BREAKS `UseSqlite(":memory:")` — bare `:memory:` fails `SqliteConnectionStringBuilder` (requires `Key=Value` ADO.NET format). Workaround: intercept in `OnConfiguring`, open a real `SqliteConnection("DataSource=:memory:")`, and pass it via `UseSqlite(connection)`.
- EF Core's `RelationalConnection.OpenAsync` calls `ProcessConnectionOpened` (runs `PRAGMA foreign_keys = ON;`) whenever `_openedCount` transitions from 0→1, even for already-open external connections. Fix: anchor `_openedCount` at 1 via `Database.OpenConnection()` after schema creation, then disable FKs on the raw connection.
- `PostgreSqlFixture` must lazily build/start the container (inside `InitializeAsync` try-catch, not as a field initializer) so test classes that use their own provider don't fail when Docker is unavailable.
- `DomainEventDispatcher` resolves `IEnumerable<IDomainEventHandler<TEvent>>` via `IServiceProvider.GetServices(handlerType)` using `typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType())` — correct pattern for open-generic DI resolution.
- Domain events are collected from `ChangeTracker.Entries<Transaction>()` after a successful save. Only `Transaction` entities are checked (the only aggregate in scope for Slice 2); extend to an `IHasDomainEvents` interface when more aggregates need this.

### 2026-04-05 — Feature 120 Slice 3: Plugin.Abstractions Full SDK (GREEN)

**Requested by:** Fortinbra (via Barbara's RED tests) — Status: Complete

**Files created:**
- `src/BudgetExperiment.Plugin.Abstractions/IPlugin.cs` — `IPlugin` interface: `Name`, `Version`, `Description` (string), `ConfigureServices(IServiceCollection)`, `InitializeAsync(IPluginContext, CancellationToken)`, `ShutdownAsync(CancellationToken)`.
- `src/BudgetExperiment.Plugin.Abstractions/IPluginContext.cs` — `IPluginContext` interface: `Services` (IServiceProvider), `Configuration` (IConfiguration), `LoggerFactory` (ILoggerFactory).
- `src/BudgetExperiment.Plugin.Abstractions/PluginNavItem.cs` — `sealed record PluginNavItem(string Label, string Route, string IconCssClass, int Order = 100)` — positional record with default Order of 100.
- `src/BudgetExperiment.Plugin.Abstractions/IPluginNavigationProvider.cs` — `IPluginNavigationProvider` interface: `GetNavItems() → IReadOnlyList<PluginNavItem>`.
- `src/BudgetExperiment.Plugin.Abstractions/IImportParser.cs` — `IImportParser` interface: `Name`, `SupportedExtensions`, `ParseAsync(Stream, CancellationToken)`.
- `src/BudgetExperiment.Plugin.Abstractions/IReportBuilder.cs` — `IReportBuilder` interface: `ReportName`, `ReportDescription`, `BuildAsync(IPluginContext, CancellationToken)`.
- `src/BudgetExperiment.Plugin.Abstractions/PluginControllerBase.cs` — `abstract class PluginControllerBase : ControllerBase` with `[ApiController]` and `[Route("api/v1/plugins/{pluginName}/[controller]")]`.

**Files modified:**
- `src/BudgetExperiment.Plugin.Abstractions/BudgetExperiment.Plugin.Abstractions.csproj` — Added `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to give the classlib access to `ControllerBase`, `IConfiguration`, `ILoggerFactory`, etc. without individual NuGet packages.
- `tests/BudgetExperiment.Domain.Tests/BudgetExperiment.Domain.Tests.csproj` — Added `<FrameworkReference Include="Microsoft.AspNetCore.App" />` so the test project can directly reference `ControllerBase` (needed by `PluginAbstractionsContractTests`).
- `tests/BudgetExperiment.Domain.Tests/Plugin/PluginAbstractionsContractTests.cs` — Fixed 5 StyleCop violations in Barbara's test file: added `<returns>` XML doc to 4 async test methods (SA1615), removed `#region`/`#endregion` block (SA1124).

**Tests:** Barbara's 20 Slice 3 tests: **20/20 GREEN**. Domain.Tests total: 897 passed (was 877; +20 new). Application.Tests: 1039 passed (unchanged).

**Key Decisions / Lessons:**
- `PluginNavItem` MUST be a `positional record` — Barbara's tests use `new PluginNavItem("Label", "/route", "icon-css")` and `new PluginNavItem("Label", "/route", "icon-css", 50)`. A class with property setters would not satisfy these constructor calls.
- For `net10.0` classlibs that need ASP.NET Core types (ControllerBase, IConfiguration, etc.), use `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in the csproj — NOT individual NuGet packages. This is the canonical approach for ASP.NET Core class libraries.
- FrameworkReference is NOT transitively available to referencing projects. If a test project directly uses ASP.NET Core types (e.g., `typeof(ControllerBase)`), it needs its own FrameworkReference even if its Plugin.Abstractions reference has one.
- StyleCop SA1124 (no regions) and SA1615 (async methods need `<returns>`) are enforced even in test files — fix them in the test file directly when caught by the build.

### 2026-04-05 — Feature 120 Slice 4: Plugin.Hosting (GREEN)

**Requested by:** Fortinbra (via Barbara's RED tests) — Status: Complete

**Files created:**
- `src/BudgetExperiment.Plugin.Hosting/PluginRegistration.cs` — `sealed class` (not record) with `IPlugin Plugin`, `bool IsEnabled { get; set; }`, `string LoadedFromPath`. Mutable `IsEnabled` required for Enable/Disable mutations.
- `src/BudgetExperiment.Plugin.Hosting/PluginRegistry.cs` — `Register(PluginRegistration)` (takes full registration, not just IPlugin+path), `Disable(string)`, `Enable(string)`, `GetPlugin(string) → PluginRegistration?`, `Plugins` as `IReadOnlyList<PluginRegistration>`.
- `src/BudgetExperiment.Plugin.Hosting/PluginLoader.cs` — `LoadPlugins(string folderPath)` scans folder for `*.dll`, uses `Assembly.LoadFrom`, catches outer `Exception` to skip unloadable DLLs, catches inner `ReflectionTypeLoadException` (via `GetExportedTypes` private static helper) to handle partial assemblies. Static helper placed before instance methods (SA1204).
- `src/BudgetExperiment.Plugin.Hosting/ServiceCollectionExtensions.cs` — `AddPluginHosting(IServiceCollection, string pluginsFolder = "plugins")` registers `PluginLoader` and `PluginRegistry` as singletons.

**Packages added:**
- `Microsoft.Extensions.DependencyInjection.Abstractions` `10.0.0-preview.4.25258.110` → `BudgetExperiment.Plugin.Hosting` (for `IServiceCollection` in DI extension).

**Test file fixes (Barbara's files):**
- `GlobalUsings.cs` — Added `global using Shouldly;`, fixed SA1210 (alphabetical order).
- `PluginRegistryTests.cs` — Fixed SA1516 (blank lines between properties in `TestPlugin`). Fixed CS8602: captured `ShouldNotBeNull()` return value for null-flow (`var nonNullResult = retrieved.ShouldNotBeNull(); nonNullResult.Plugin.Name.ShouldBe(...)`).
- `PluginLoaderTests.cs` — Fixed SA1516 (blank lines between properties in `TestPlugin`).

**Tests:** Barbara's 8 Slice 4 tests: **8/8 GREEN**. Domain.Tests: 897. Application.Tests: 1039. Client.Tests: 2818 passed, 1 pre-existing skip. Plugin.Hosting.Tests: 8 passed.

**Key Decisions / Lessons:**
- `PluginRegistry.Register` takes a `PluginRegistration` (not `IPlugin + path string`) — the tests construct `PluginRegistration` themselves and pass it to `Register`. Always read the tests, not just the spec description.
- `PluginLoader` must catch `ReflectionTypeLoadException` inside `GetTypes()` calls, not just the outer `Assembly.LoadFrom`. When loading a test assembly that references missing deps (xUnit, Shouldly, etc.), `GetTypes()` throws; use `ex.Types.Where(t => t is not null)` to still find valid types.
- Shouldly 4.x does NOT auto-add `global using Shouldly;` — it must be explicitly added to `GlobalUsings.cs`. The test project was missing it.
- `ShouldNotBeNull()` returns `T` (non-nullable) in Shouldly 4.x — capture the return value to satisfy the nullable flow analyzer: `var result = retrieved.ShouldNotBeNull()`.
- SA1516: All properties in a class must be separated by blank lines, even in test inner classes.

### 2026-04-05 — Feature 120 Slice 5: PluginsController API (GREEN)

**Requested by:** Fortinbra (via Barbara's RED tests) — Status: Complete

**Files created:**
- `src/BudgetExperiment.Api/Controllers/PluginsController.cs` — `[Route("api/v1/plugins")]` controller with `GetAll()`, `GetByName(string)`, `Enable(string)`, `Disable(string)`. Uses `PluginRegistry` singleton injected via constructor. Static `ToDto(PluginRegistration)` helper maps to positional `PluginDto` record using constructor syntax.
- `tests/BudgetExperiment.Api.Tests/PluginsTestWebApplicationFactory.cs` — Extracted from Barbara's test file to satisfy SA1402 (one type per file). Added auth bypass: registers `AutoAuthenticatingTestHandler` under `"TestAuto"` scheme. Seeds `PluginRegistry` with test `IPlugin` instances. Added `using Microsoft.Extensions.Configuration;` for `AddInMemoryCollection`.

**Files modified:**
- `src/BudgetExperiment.Api/Program.cs` — Added `using BudgetExperiment.Plugin.Hosting;` and `builder.Services.AddPluginHosting();` after `AddInfrastructure`.
- `tests/BudgetExperiment.Api.Tests/PluginsControllerTests.cs` — Removed `PluginsTestWebApplicationFactory` class (moved to its own file per SA1402). Fixed SA1518 (trailing newline).
- `src/BudgetExperiment.Api/BudgetExperiment.Api.csproj` — Added reference to `BudgetExperiment.Plugin.Hosting`.
- `tests/BudgetExperiment.Api.Tests/BudgetExperiment.Api.Tests.csproj` — Added reference to `BudgetExperiment.Plugin.Hosting`.

**Tests:** Barbara's 7 Slice 5 tests: **7/7 GREEN**. Domain.Tests: 897. Application.Tests: 1039. Infrastructure.Tests: 226. Api.Tests: 667 passed (includes the 7 new). Client.Tests: 2818 passed, 1 pre-existing skip.

**Key Decisions / Lessons:**
- `PluginDto` is a positional record — use constructor syntax in `ToDto`: `new PluginDto(r.Plugin.Name, r.Plugin.Version, ...)` not object initializer `new() { Name = ... }`.
- SA1402 applies to test files too — `PluginsTestWebApplicationFactory` had to be extracted from `PluginsControllerTests.cs` into its own file.
- `WebApplicationFactory.ConfigureServices` runs AFTER `Program.cs`, so a re-registered `AddSingleton<PluginRegistry>` in the test factory correctly overrides the one from `AddPluginHosting()` (DI resolves last registration wins).
- `PluginsTestWebApplicationFactory` must register `AutoAuthenticatingTestHandler` under `"TestAuto"` scheme — otherwise all requests return 401. Pattern matches `CustomWebApplicationFactory`.
- `AddInMemoryCollection` requires `using Microsoft.Extensions.Configuration;` — not in implicit usings for test project.


### 2026-04-05 — Feature 129: Feature Flag Implementation Research (GREEN)

**Requested by:** Fortinbra (via Alfred's architecture task)

**Research Scope:**
- Survey existing config infrastructure (Program.cs, appsettings.json, *Options.cs patterns)
- Evaluate .NET Options pattern (IOptions<T>, IOptionsMonitor<T>, IOptionsSnapshot<T>)
- Evaluate Microsoft.FeatureManagement NuGet package vs hand-rolled approach
- Recommend Blazor WASM client delivery pattern (bake into HTML vs /api/v1/features endpoint)
- Produce concrete implementation code sketches

**Deliverable:** docs/129b-feature-flag-implementation.md — 10 sections, 450+ lines, complete implementation proposal.

**Key Findings:**

1. **Existing Config Patterns:**
   - DatabaseOptions, AuthenticationOptions, ClientConfigOptions all follow const string SectionName, IOptions<T> injection, property initializers for defaults.
   - DI registration: builder.Services.Configure<TOptions>(builder.Configuration.GetSection(TOptions.SectionName)) in API Program.cs.
   - Client config delivered via /api/v1/config endpoint (ConfigController), fetched at Client startup before DI registration.
   - No usage of IOptionsMonitor or IOptionsSnapshot in the codebase — all config is startup-bound, not runtime-hot-reloadable.

2. **Recommended Approach: Hand-Rolled IOptions<T>**
   - Why not Microsoft.FeatureManagement? It's excellent for complex scenarios (A/B testing, targeting, external providers, percentage rollouts), but we don't need that complexity yet. Our flags control gradual rollout of backend-completed features to client UI. Simple on/off switches are sufficient.
   - Benefits: Zero external dependencies, aligns with no magic principle (§3, §8 copilot-instructions.md), trivial to test (mock IOptions<T>), matches existing config patterns exactly.
   - Extensibility: If we need runtime toggles later, upgrade IOptions<T> → IOptionsMonitor<T> (file-watch reload). If we need targeting/A/B, migrate to Microsoft.FeatureManagement (~2 hours effort).

3. **Layer Placement:**
   - Shared (BudgetExperiment.Shared): FeatureFlagOptions.cs POCO. Rationale: Both API and Client need same shape; avoids DTO mapping; matches existing BudgetScope, CategorySource enum pattern.
   - API (BudgetExperiment.Api): FeaturesController exposing /api/v1/features endpoint, [AllowAnonymous] (client needs flags before auth), ResponseCache(Duration = 3600) (flags change at deployment, not per request).
   - Client (BudgetExperiment.Client): IFeatureFlagService + FeatureFlagService (fetches flags at startup, caches them, exposes IsEnabled(string) method for components).

4. **Client Delivery Pattern: /api/v1/features Endpoint (Option B)**
   - Why not bake into HTML? Matches existing /api/v1/config pattern (simpler deployment, consistent approach). Client fetches flags at startup (line ~142 in Program.cs), before RunAsync().
   - Graceful Degradation: If API unavailable (network failure, dev environment), client defaults to all-false flags except completed features (AdvancedCharts, RecurringChargeSuggestions) which use property initializers to default true.

5. **Migration Path for Existing Features:**
   - Problem: We already ship advanced charts, recurring charge suggestions. Setting flags to false by default breaks production.
   - Solution: Default-on via property initializers (public bool AdvancedCharts { get; set; } = true;). New features (Kakeibo, Kaizen, MonthlyReflection) default to false.
   - Rollout: Wrap existing nav links in @if (FeatureFlagService.IsEnabled("AdvancedCharts")) — no behavior change (always true). New features hidden by default, enabled via appsettings.json override.

6. **Code Sketches Provided:**
   - FeatureFlagOptions.cs (11 properties, XML docs, default values)
   - FeaturesController.cs (GET endpoint, IOptions<T> injection, [AllowAnonymous], ResponseCache)
   - DI registration in API Program.cs (1 line: Configure<FeatureFlagOptions>)
   - IFeatureFlagService.cs + FeatureFlagService.cs (Client-side service with LoadFlagsAsync(), IsEnabled(string), graceful degradation)
   - Client DI registration + startup flag loading (replace await builder.Build().RunAsync() with var host = builder.Build(); await LoadFlags(); await host.RunAsync())

**Open Questions for Alfred (§8 of proposal):**
- Flag naming convention: feature-based (Kakeibo) vs page-based (KakeiboCategorizationPage)?
- Inventory management: code-first (FeatureFlagOptions is canonical) vs separate docs/feature-flag-inventory.md?
- Blazor component pattern: inline @if (IsEnabled("Flag")) vs code-behind property?
- Backend flag usage: Should API endpoints check flags (return 404 if disabled) or only client hides UI?

**Estimated Effort:**
- Lucius (implementation): 2 hours (POCO, controller, service, DI wiring)
- Barbara (tests): 3 hours (unit + integration tests)
- Alfred (review): 1 hour
- Total: ~6 hours

**Status:** Proposal complete, awaiting Alfred's architecture alignment and answer to open questions. No implementation yet — coordinate with Alfred first.
