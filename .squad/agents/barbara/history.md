# Project Context

- **Owner:** Fortinbra
- **Project:** BudgetExperiment — .NET 10 budgeting application
- **Stack:** .NET 10, ASP.NET Core Minimal API, Blazor WebAssembly (plain, no FluentUI), EF Core + Npgsql (PostgreSQL), xUnit + Shouldly, NSubstitute/Moq (one, consistent), StyleCop.Analyzers
- **Created:** 2026-03-22

## Test Stack & Conventions

- xUnit + Shouldly for assertions (NO FluentAssertions, NO AutoFixture)
- NSubstitute or Moq for mocking — one library, consistent across project
- Testcontainers for integration tests (PostgreSQL) — preferred over SQLite in-memory for EF fidelity
- WebApplicationFactory for API endpoint tests
- bUnit for Blazor component tests
- Always exclude `Category=Performance` unless explicitly requested: `--filter "Category!=Performance"`
- Culture-sensitive tests must set `CultureInfo.CurrentCulture` to a known culture (e.g., `en-US`)
- Arrange/Act/Assert structure, one assertion intent per test

## Architecture

Domain Tests, Application Tests, Infrastructure Tests, API Tests, Client Tests — all under `tests/`.

## Core Context

### Testcontainers Migration & Test Infrastructure

- **Infrastructure Tests:** Replaced SQLite in-memory with PostgreSQL Testcontainers (postgres:16). One container per collection via IAsyncLifetime; each test gets clean slate via TRUNCATE CASCADE.
- **API Tests:** Migrated to PostgreSQL; `ApiPostgreSqlFixture` + `[Collection("ApiDb")]` on all 33 test classes. `UserControllerTests` uses per-test `ResetDatabase()` for stateful isolation.
- **Real Bug Exposed:** `RecurringTransactionInstanceService.ModifyInstanceAsync` didn't call `MarkAsModified` on parent entity after `SetExpectedConcurrencyToken` — silently passed in-memory, broke PostgreSQL xmin concurrency. Fixed by adding `IUnitOfWork.MarkAsModified<T>`.
- **Key Learning:** `PostgreSqlBuilder` 4.11.0 requires image name parameter. `ExecuteSqlRaw` with interpolated strings triggers EF1002 — suppress only when table names come from EF model.

### Low-Value Test Cleanup (2026-03-22/23)

- Removed 29 tests: 17 framework behavior + 12 vanity enum tests (compile-time correctness only).
- Converted 3 duplicate test pairs to `[Theory]` (+18 InlineData scenarios, 0 net count change).
- Enhanced 4 mock-verification-only tests with behavioral result assertions.
- Filled 2 critical service gaps via Lucius (RecurringTransactionInstanceServiceTests + UserSettingsServiceTests = +37 tests).
- **Tip:** Decimal literals invalid in attribute args — use `double` + cast inside test. Verify which tests are already theory before claiming duplicates.
- **Net result:** 5,412 → 5,449 tests.

### Feature 127: bUnit Chart Test Patterns

**SVG charts (Tiers 1 & 2):** Extend `BunitContext` only (no `IAsyncLifetime`/ThemeService for pure SVG). Outer `<div role="img">` + inner `<svg aria-hidden="true">`. Empty state = `<div class="X-empty">`. Multi-class elements match base selector; use `.ClassName.Contains(...)` to differentiate. `data-day`/`data-week` attributes via `GetAttribute(...)`. `WaterfallSegment.IsPositive` is computed — NOT a constructor param (4-arg ctor only).

**ApexCharts (Tier 3 — BudgetTreemap, BudgetRadar):** `JSInterop.Mode = JSRuntimeMode.Loose` in constructor. Assert outer HTML only (div class + aria-label). Never assert SVG/canvas content. `Assert.Empty(cut.FindAll(".xxx-empty"))` for "non-empty" assertions.

**SA1512 rule:** NOTE comment blocks followed by a blank line before `[Fact]`/`using` statements violate SA1512. Lucius proactively fixes these during GREEN implementation.

**Models created across slices 3–7:** `ScatterDataPoint`, `StackedAreaDataPoint`, `StackedAreaSeries`, `RadialBarSegment`, `TreemapDataPoint`, `RadarDataSeries` — all `sealed record` in `BudgetExperiment.Client.Components.Charts.Models`.

**Suite progress:** 2745 (pre-127 slices 3–7) → **2804** (slices 3–7) → **2808** (Slice 10).

### ChartDataService Contracts (Slice 2)

- `BuildSpendingHeatmap`: Mon=row[0], Sun=row[6] (ISO day order). `TotalAmount` = absolute value of expenses. Same-day transactions aggregate to single point.
- `BuildBudgetWaterfall`: descending by largest absolute spend; last segment `IsTotal=true`. `CategorySpendingDto.Amount` is `MoneyDto` — access `.Amount.Amount` for decimal.
- `BuildBalanceCandlesticks`: Open=first, High=max, Low=min, Close=last per month. `IsBullish = Close >= Open` (computed property). `monthsBack` reference = `max(transaction.Date)` not `DateTime.UtcNow`.
- `BuildCategoryDistributions`: Tukey's hinges (exclude median for odd n, split at midpoint for even n). `Maximum` = largest non-outlier (not absolute max). Outlier detection = IQR × 1.5.

## Recent Work

### 2026-04-04 — Feature 127 Slice 10: RED-Phase bUnit Tests for ReportsDashboard

**Task:** Write 6 RED-phase bUnit tests for the new `ReportsDashboard` aggregate page (Feature 127, Slice 10). Component did not exist yet.

**File created:** `tests/BudgetExperiment.Client.Tests/Pages/Reports/ReportsDashboardTests.cs`

**Tests (6):**
1. `ReportsDashboard_Renders_Container` — root `.reports-dashboard` div present without exception
2. `ReportsDashboard_Shows_LoadingState_Initially` — `.dashboard-loading` visible while `GetMonthlyCategoryReportAsync` blocked via `TaskCompletionSource`
3. `ReportsDashboard_Renders_TreemapSection` — `.dashboard-treemap` present after sync fakes load
4. `ReportsDashboard_Renders_WaterfallSection` — `.dashboard-waterfall` present
5. `ReportsDashboard_Renders_FilterSection` — `.dashboard-filter` present
6. `ReportsDashboard_Renders_RadialBarSection` — `.dashboard-radial-bar` present

**Fakes:** Local `FakeChartDataService` (sync, empty returns) + `FakeBudgetApiService` with `MonthlyCategoryTaskSource` (`TaskCompletionSource<MonthlyCategoryReportDto?>`) for loading-state test. Same isolation pattern as `BudgetComparisonReportTests.cs`. Culture set to `en-US` in `InitializeAsync`. DI: `IBudgetApiService` + `IChartDataService` + `ScopeService` (with JSInterop).

**Key spec for Lucius:** `GetMonthlyCategoryReportAsync` MUST be the FIRST await in `OnInitializedAsync` — blocking it keeps component in loading state. Test 2 locks on this assumption as spec.

**Result after Lucius implementation:** All 6 tests GREEN. Suite: 2808 passed, 0 failed, 1 pre-existing skip.

---

### 2026-04-06 — Financial Accuracy Test Suite

**Task:** Survey existing accuracy tests and fill gaps with a comprehensive financial accuracy suite.

**Survey findings (already covered — not duplicated):**
- `AccountBalanceAccuracyTests.cs` (Domain.Tests) — 13 tests covering zero balance, debit/credit, mixed transactions, remove, edit, large sequences.
- `BalanceCalculationAccuracyTests.cs` (Application.Tests/Accuracy) — 14 tests covering BalanceCalculationService with date boundaries, multi-account aggregation, per-account isolation.
- `TransferNetZeroAccuracyTests.cs` (Application.Tests/Accuracy) — 9 tests covering net-zero invariant, currency matching, transfer ID linkage.
- `MoneyValueTests.cs` (Domain.Tests) — 8 tests covering basic arithmetic, currency mismatch, Abs/Negate.
- `PaycheckAllocationCalculatorTests.cs` (Domain.Tests) — 18 tests covering all frequency combos, warnings, summaries.

**Files created (49 new tests):**
1. `tests/BudgetExperiment.Domain.Tests/ValueObjects/MoneyValueAccuracyTests.cs` (10 tests) — Long sequence accuracy, zero identity, rounding AwayFromZero, known decimal triple, Theory identity.
2. `tests/BudgetExperiment.Domain.Tests/Accuracy/KakeiboAccuracyTests.cs` (10 tests) — `GetEffectiveKakeiboCategory` override precedence, category fallback, default Wants, bucket totals reconciliation, SetKakeiboCategory domain validation.
3. `tests/BudgetExperiment.Application.Tests/Accuracy/AccountBalanceAccuracyTests.cs` (5 tests) — Order invariance, full journey scenario, mid-list removal, large balance precision, negative-to-zero convergence.
4. `tests/BudgetExperiment.Application.Tests/Accuracy/TransferAccuracyTests.cs` (4 tests) — Decimal precision with arbitrary amounts (Theory), exactly 2 transactions created, source negative/destination positive, absolute amounts match.
5. `tests/BudgetExperiment.Application.Tests/Accuracy/PaycheckAllocationAccuracyTests.cs` (7 tests) — Zero-amount bill, total-per-paycheck == sum of individuals, total annual bills == sum of individuals, rounding within half-cent-per-period bound (Theory), Theory for rounding error.
6. `tests/BudgetExperiment.Application.Tests/Accuracy/RecurringProjectionAccuracyTests.cs` (7 tests) — Monthly exact dates for 6 months, bi-weekly no drift over 26 periods, end date exclusive then inclusive, occurrence count, no projections before start date, skipped exception omits date from projector.

**Test logic corrections during GREEN phase:**
- Initial `Allocation_PerPaycheckTimesPayPeriods_CoversOrMatchesAnnualAmount` assumed AwayFromZero always rounds up. It does not: $1000/12 = $83.333... rounds DOWN to $83.33, giving $999.96/year. Corrected assertion: total rounding error ≤ $0.005 × periodsPerYear.
- `Allocation_RoundingError_AtMostOnePennyPerPayPeriod` — same root cause. Corrected to use absolute error bound.

**Bugs found:** None. All financial logic is correct. The rounding behavior is expected and intentional.

**Production code note:** `PaycheckAllocationCalculator.CalculateAllocation` uses `MidpointRounding.AwayFromZero` which rounds the per-paycheck amount toward the nearest cent at the midpoint. For non-midpoint values like $83.333..., truncation (not rounding up) applies. This is correct behaviour — the total rounding error is bounded within half a cent per period.

**Key learnings:**
- `KakeiboCategory.Wants` is the fallback for uncategorized transactions (null override + null category routing).
- Reflection (`typeof(Transaction).GetProperty("Category")!.SetValue(...)`) is the established pattern to set navigation properties in unit tests (private set).
- `RecurringTransaction.GetOccurrencesBetween` is a direct domain method — its accuracy tests belong in Domain.Tests but were placed in Application.Tests/Accuracy per task specification.

**Suite result:** 5716 passed (was 5667 pre-task), 0 failed, 1 pre-existing skip.

---

## Learnings

### 2026-04-05 — Feature 120 Slice 1: RED Tests for Domain Event Foundation

**Task:** Write RED-phase tests for Transaction domain event functionality in preparation for Plugin system implementation.

**File created:** `tests/BudgetExperiment.Domain.Tests/Entities/TransactionDomainEventTests.cs`

**Transaction Current State Observations:**
- `_domainEvents` field is currently `List<object>` (line 17 of Transaction.cs)
- No public API to raise, clear, or read domain events
- No reference to `Plugin.Abstractions` namespace yet
- Transaction has robust factory methods (`Create`, `CreateFromRecurring`, `CreateTransfer`, `CreateFromRecurringTransfer`) and behavior methods (`UpdateDescription`, `UpdateAmount`, `MarkCleared`, `LockToReconciliation`, etc.)

**RED Tests Written (5 total):**
1. `DomainEvents_InitiallyEmpty` — asserts new Transaction has empty domain events collection
2. `RaiseDomainEvent_AddsToDomainEvents` — asserts `RaiseDomainEvent(IDomainEvent)` appends to collection
3. `DomainEvents_IsReadOnly` — asserts `DomainEvents` property returns `IReadOnlyList<IDomainEvent>`
4. `ClearDomainEvents_EmptiesCollection` — asserts `ClearDomainEvents()` method empties the collection
5. `RaiseDomainEvent_MultipleEvents_AllPresent` — asserts raising 3 events keeps all in order

**Build Status:** ✅ RED — Fails with 2 compilation errors:
- `CS0234: The type or namespace name 'Plugin' does not exist in the namespace 'BudgetExperiment'`
- `CS0246: The type or namespace name 'IDomainEvent' could not be found`

**Next Steps (for Lucius — GREEN phase):**
1. Create `BudgetExperiment.Plugin.Abstractions` project
2. Define `IDomainEvent` interface with `EventId` and `OccurredAtUtc` properties
3. Add project reference to `BudgetExperiment.Domain`
4. Add `RaiseDomainEvent(IDomainEvent)` method to Transaction
5. Add `ClearDomainEvents()` method to Transaction
6. Add `IReadOnlyList<IDomainEvent> DomainEvents { get; }` property to Transaction
7. Convert `_domainEvents` from `List<object>` to `List<IDomainEvent>`
8. Update `GlobalUsings.cs` in tests to include `BudgetExperiment.Plugin.Abstractions`

### 2026-04-05 — Feature 120 Slice 2: RED Tests for Domain Event Dispatch Wiring

**Task:** Write RED-phase tests for domain event dispatcher integration in BudgetDbContext.

**Files created:**
1. `tests/BudgetExperiment.Application.Tests/Events/DomainEventDispatcherTests.cs` — 3 tests for dispatcher contract
2. `tests/BudgetExperiment.Infrastructure.Tests/Data/BudgetDbContextDomainEventTests.cs` — 3 tests for DbContext integration

**New Interfaces Required (not yet defined):**
1. `IDomainEventHandler<TEvent>` in `BudgetExperiment.Plugin.Abstractions`
   - Generic handler interface for domain event subscribers
   - Single method: `Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default)`

2. `IDomainEventDispatcher` in `BudgetExperiment.Application`
   - Orchestrates dispatch of domain events to registered handlers
   - Method: `Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken cancellationToken = default)`

**RED Tests Written:**

**Application Tests (DomainEventDispatcherTests.cs):**
1. `DispatchAsync_EmptyList_DoesNothing` — dispatching empty list completes without error
2. `DispatchAsync_SingleEvent_CallsHandlers` — a single event is dispatched and reaches handlers
3. `DispatchAsync_MultipleEvents_AllDispatched` — 3 events dispatched in order reach handlers

**Infrastructure Tests (BudgetDbContextDomainEventTests.cs):**
1. `SaveChangesAsync_DispatchesDomainEvents_RaisedOnTransaction` — dispatcher receives events after SaveChangesAsync
2. `SaveChangesAsync_ClearsDomainEventsAfterDispatch` — Transaction.DomainEvents collection cleared post-dispatch
3. `SaveChangesAsync_NoEvents_DispatcherNotCalled` — dispatcher not invoked if no events raised

**Test Implementation Details:**
- Hand-written fake `FakeDomainEventDispatcher` (no Moq/NSubstitute) in Application tests
- Hand-written fake dispatcher in Infrastructure tests
- Infrastructure tests use in-memory SQLite (`UseSqlite(":memory:")`) for simplicity (not Testcontainers, as no DB migration needed for RED)
- Both test files include `TestDomainEvent` sealed class implementing `IDomainEvent`
- Added `Plugin.Abstractions` project reference to both test project `.csproj` files

**Build Status:** ✅ RED — Fails with 4 compilation errors (expected):
- `CS0234: The type or namespace name 'Events' does not exist in the namespace 'BudgetExperiment.Application'`
- `CS0246: The type or namespace name 'IDomainEventDispatcher' could not be found` (twice across both test files)

**Key Observations:**
- `BudgetDbContext` constructor currently accepts only `DbContextOptions<BudgetDbContext>` — RED tests expect it to also accept `IDomainEventDispatcher` parameter
- Infrastructure tests instantiate context with `new BudgetDbContext(options, dispatcher)` — will fail until constructor updated
- Both fakes accumulate events in a `_dispatchedEvents` list for assertion

**Next Steps (for Lucius — GREEN phase):**
1. Define `IDomainEventDispatcher` interface in `BudgetExperiment.Application.Events` namespace
2. Define `IDomainEventHandler<TEvent>` interface in `BudgetExperiment.Plugin.Abstractions` namespace
3. Update `BudgetDbContext` constructor to accept `IDomainEventDispatcher` parameter (optional/null-coalescing for existing usage)
4. Implement `SaveChangesAsync` override to:
   - Call base `SaveChangesAsync()`
   - Collect all domain events from tracked Transaction entities
   - Dispatch via `IDomainEventDispatcher.DispatchAsync()`
   - Clear events from entities via `ClearDomainEvents()`
5. Register `IDomainEventDispatcher` implementation in DI container (API Program.cs)

### 2026-04-05 — Feature 120 Slice 3: RED Tests for Full Plugin SDK

**Task:** Write RED-phase contract tests for the complete Plugin.Abstractions SDK, verifying exact API surface (types, member signatures, constraints).

**File created:** `tests/BudgetExperiment.Domain.Tests/Plugin/PluginAbstractionsContractTests.cs`

**Contracts Verified (20 tests):**
1. `IPlugin_HasName_Property` — Name property (string)
2. `IPlugin_HasVersion_Property` — Version property (string)
3. `IPlugin_HasDescription_Property` — Description property (string)
4. `IPlugin_ConfigureServices_AcceptsServiceCollection` — ConfigureServices(IServiceCollection) method
5. `IPlugin_InitializeAsync_AcceptsPluginContext` — InitializeAsync(IPluginContext, CancellationToken) async method
6. `IPlugin_ShutdownAsync_Exists` — ShutdownAsync(CancellationToken) async method
7. `IPluginContext_ExposeServiceProvider` — Services property returns IServiceProvider
8. `IPluginContext_ExposeConfiguration` — Configuration property returns IConfiguration
9. `IPluginContext_ExposeLoggerFactory` — LoggerFactory property returns ILoggerFactory
10. `IPluginNavigationProvider_GetNavItems_ReturnsNavigationEntries` — GetNavItems() returns IReadOnlyList<PluginNavItem>
11. `PluginNavItem_HasRequiredProperties` — PluginNavItem record with Label, Route, IconCssClass properties
12. `PluginNavItem_HasOrderProperty_WithDefault` — Order property with default=100
13. `IImportParser_HasName_Property` — Name property (string)
14. `IImportParser_HasSupportedExtensions_Property` — SupportedExtensions property returns IReadOnlyList<string>
15. `IImportParser_ParseAsync_AcceptsStreamAndCancellationToken` — ParseAsync(Stream, CancellationToken) returns Task<IReadOnlyList<object>>
16. `IReportBuilder_HasReportName_Property` — ReportName property (string)
17. `IReportBuilder_HasReportDescription_Property` — ReportDescription property (string)
18. `IReportBuilder_BuildAsync_AcceptsContextAndCancellationToken` — BuildAsync(IPluginContext, CancellationToken) returns Task<object>
19. `PluginControllerBase_InheritsFromControllerBase` — abstract class : ControllerBase
20. `PluginControllerBase_CanBeInherited` — concrete subclass instantiates cleanly

**Test Implementation Pattern:**
- Concrete test implementations: TestPlugin, TestPluginContext, TestPluginNavigationProvider, TestImportParser, TestReportBuilder, TestPluginController
- Located after test methods in private sealed classes (SA1201 ordering)
- Full XML docs on class + test method summaries
- xUnit + Shouldly (no FluentAssertions, no AutoFixture)

**Build Status:** ✅ RED — 14 compilation errors (expected):
- Missing ASP.NET Core / Configuration / Logging NuGet package references in Domain.Tests
- Missing types not yet defined: `IPlugin`, `IPluginContext`, `IPluginNavigationProvider`, `PluginNavItem`, `IImportParser`, `IReportBuilder`, `PluginControllerBase`

**Interface Signatures from Feature Spec (docs/120-plugin-system.md):**
- `IPlugin`: Name, Version, Description (properties); ConfigureServices(IServiceCollection), InitializeAsync(IPluginContext, CancellationToken), ShutdownAsync(CancellationToken)
- `IPluginContext`: Services (IServiceProvider), Configuration (IConfiguration), LoggerFactory (ILoggerFactory)
- `IPluginNavigationProvider`: GetNavItems() → IReadOnlyList<PluginNavItem>
- `PluginNavItem`: sealed record(Label: string, Route: string, IconCssClass: string, Order: int = 100)
- `IImportParser`: Name (string), SupportedExtensions (IReadOnlyList<string>), ParseAsync(Stream, CancellationToken) → Task<IReadOnlyList<object>>
- `IReportBuilder`: ReportName (string), ReportDescription (string), BuildAsync(IPluginContext, CancellationToken) → Task<object>
- `PluginControllerBase`: abstract class : ControllerBase

**Next Steps (for Lucius — GREEN phase):**
1. Add NuGet package references to Domain.Tests.csproj: `Microsoft.AspNetCore.Mvc`, `Microsoft.Extensions.Configuration`, `Microsoft.Extensions.Logging`
2. Create `BudgetExperiment.Plugin.Abstractions/IPlugin.cs`
3. Create `BudgetExperiment.Plugin.Abstractions/IPluginContext.cs`
4. Create `BudgetExperiment.Plugin.Abstractions/IPluginNavigationProvider.cs`
5. Create `BudgetExperiment.Plugin.Abstractions/PluginNavItem.cs` (sealed record)
6. Create `BudgetExperiment.Plugin.Abstractions/IImportParser.cs`
7. Create `BudgetExperiment.Plugin.Abstractions/IReportBuilder.cs`
8. Create `BudgetExperiment.Plugin.Abstractions/PluginControllerBase.cs`

### 2026-04-05 — Feature 120 Slice 4: RED Tests for Plugin.Hosting (Loader + Registry)

**Task:** Write RED-phase tests for the Plugin.Hosting infrastructure layer, verifying PluginLoader and PluginRegistry contract compliance.

**Files created:**
1. `tests/BudgetExperiment.Plugin.Hosting.Tests/BudgetExperiment.Plugin.Hosting.Tests.csproj` — new test project
2. `tests/BudgetExperiment.Plugin.Hosting.Tests/GlobalUsings.cs` — project-wide usings
3. `tests/BudgetExperiment.Plugin.Hosting.Tests/PluginLoaderTests.cs` — 3 tests for loader
4. `tests/BudgetExperiment.Plugin.Hosting.Tests/PluginRegistryTests.cs` — 5 tests for registry

**Projects created:**
- `src/BudgetExperiment.Plugin.Hosting/BudgetExperiment.Plugin.Hosting.csproj` — new project (stub for RED)

**RED Tests Written (8 total):**

**PluginLoader Tests (3):**
1. `PluginLoader_EmptyFolder_ReturnsNoPlugins` — loader returns empty collection when scanning empty folder
2. `PluginLoader_FolderWithNonPluginDll_ReturnsNoPlugins` — non-plugin DLL (no IPlugin impl) is ignored
3. `PluginLoader_FolderWithValidPlugin_LoadsPlugin` — DLL with IPlugin implementation is discovered and loaded

**PluginRegistry Tests (5):**
4. `PluginRegistry_InitiallyEmpty` — new registry has empty Plugins collection
5. `PluginRegistry_Register_AddsPlugin` — Register() adds plugin to Plugins list
6. `PluginRegistry_Disable_MarksPluginDisabled` — Disable(pluginName) sets IsEnabled = false
7. `PluginRegistry_Enable_MarksPluginEnabled` — Enable(pluginName) sets IsEnabled = true
8. `PluginRegistry_GetPlugin_ByName_ReturnsCorrectPlugin` — GetPlugin(name) returns correct PluginRegistration

**Test Implementation Details:**
- Hand-written test fakes: `TestPlugin` (implements IPlugin)
- PluginLoader tests use temporary file system folder (System.IO.Path.GetTempPath) for assembly scanning
- PluginRegistry tests use in-memory fakes without file I/O
- xUnit + Shouldly (no FluentAssertions, no AutoFixture)
- Arrange/Act/Assert structure

**Types Required (not yet defined in Plugin.Hosting):**
1. `PluginLoader` — scans folder, loads assemblies, discovers IPlugin implementations
   - Method: `IReadOnlyList<IPlugin> LoadPlugins(string folderPath)`
2. `PluginRegistry` — manages loaded plugins and state
   - Property: `IReadOnlyList<PluginRegistration> Plugins { get; }`
   - Methods: `void Register(PluginRegistration registration)`, `void Disable(string pluginName)`, `void Enable(string pluginName)`, `PluginRegistration GetPlugin(string pluginName)`
3. `PluginRegistration` — value type (record) holding plugin instance + metadata
   - Constructor: `PluginRegistration(IPlugin plugin, bool isEnabled, string loadedFromPath)`
   - Properties: `IPlugin Plugin { get; }`, `bool IsEnabled { get; }`, `string LoadedFromPath { get; }`

**Build Status:** ✅ RED — 26 compilation errors (expected):
- Multiple `CS0246: The type or namespace name 'PluginLoader' could not be found`
- Multiple `CS0246: The type or namespace name 'PluginRegistry' could not be found`
- Multiple `CS0246: The type or namespace name 'PluginRegistration' could not be found`

**Next Steps (for Lucius — GREEN phase):**
1. Create `BudgetExperiment.Plugin.Hosting/PluginLoader.cs` — loads assemblies from folder, discovers IPlugin implementations
2. Create `BudgetExperiment.Plugin.Hosting/PluginRegistry.cs` — tracks loaded plugins and their enabled/disabled state
3. Create `BudgetExperiment.Plugin.Hosting/PluginRegistration.cs` — record holding (IPlugin, IsEnabled, LoadedFromPath)
4. Implement PluginLoader.LoadPlugins(string) to scan folder for .dll files, load assemblies, find IPlugin implementations
5. Implement PluginRegistry with Register/Disable/Enable/GetPlugin methods
6. Add project reference from Plugin.Hosting to Plugin.Abstractions

### 2026-04-05 — Feature 120 Slice 5: RED Tests for PluginsController API Integration

**Task:** Write RED-phase API integration tests for the PluginsController management endpoints. Tests validate the exact API contract for listing, retrieving, enabling, and disabling plugins.

**Files created:**
1. `tests/BudgetExperiment.Api.Tests/PluginsControllerTests.cs` — 7 integration tests for Plugins API
2. `src/BudgetExperiment.Contracts/Dtos/PluginDto.cs` — DTO for plugin responses
3. `PluginsTestWebApplicationFactory` class within test file — custom factory for seeding plugins

**RED Tests Written (7 total):**
1. `GetPlugins_ReturnsOk_WithEmptyList` — GET /api/v1/plugins returns 200 with empty array when no plugins registered
2. `GetPlugins_ReturnsOk_WithRegisteredPlugins` — GET /api/v1/plugins returns 200 with all registered plugins as PluginDto array
3. `GetPlugin_ExistingPlugin_ReturnsOk` — GET /api/v1/plugins/{name} returns 200 with single plugin details for existing plugin
4. `GetPlugin_NonExistentPlugin_ReturnsNotFound` — GET /api/v1/plugins/{name} returns 404 for non-existent plugin
5. `EnablePlugin_ExistingPlugin_Returns204` — POST /api/v1/plugins/{name}/enable returns 204 No Content for existing plugin
6. `EnablePlugin_NonExistentPlugin_ReturnsNotFound` — POST /api/v1/plugins/{name}/enable returns 404 for non-existent plugin
7. `DisablePlugin_ExistingPlugin_Returns204` — POST /api/v1/plugins/{name}/disable returns 204 No Content for existing plugin

**Test Implementation Details:**
- xUnit + plain Assert (no FluentAssertions, no AutoFixture)
- Hand-written `TestPlugin` class implementing IPlugin interface
- `PluginsTestWebApplicationFactory` extends `WebApplicationFactory<Program>` with per-test plugin seeding via `IAsyncLifetime`
- Each test creates a fresh factory instance with specific seed plugins, avoiding shared state
- Uses `ConfigureWebHost` to override `PluginRegistry` registration with test instance
- Follows existing API test pattern from `VersionControllerTests.cs` and `AccountsControllerTests.cs`

**DTO Created (PluginDto):**
- Name (string)
- Version (string)
- Description (string)
- IsEnabled (bool)
- LoadedFromPath (string)

**Build Status:** ✅ RED — 2 compilation errors (expected):
- `CS0234: The type or namespace name 'Hosting' does not exist in the namespace 'BudgetExperiment.Plugin'` (appears twice, once per reference)

**Key Observations:**
- `IPlugin` interface requires `ShutdownAsync(CancellationToken)` method (discovered during test implementation)
- Test factory uses `IAsyncLifetime` pattern to integrate with xUnit's lifecycle
- Each test manually disposes its factory after use to ensure clean state
- PluginRegistry and PluginRegistration types not yet defined — will cause compilation failure until GREEN phase

**Next Steps (for Lucius — GREEN phase):**
1. Ensure `BudgetExperiment.Plugin.Hosting` project is created with PluginRegistry, PluginRegistration types
2. Create `BudgetExperiment.Api/Controllers/PluginsController.cs` with 4 endpoints:
   - `GET /api/v1/plugins` → returns `List<PluginDto>` with 200 OK
   - `GET /api/v1/plugins/{name}` → returns `PluginDto` with 200 OK or 404 Not Found
   - `POST /api/v1/plugins/{name}/enable` → returns 204 No Content or 404 Not Found
   - `POST /api/v1/plugins/{name}/disable` → returns 204 No Content or 404 Not Found
3. Wire `PluginRegistry` into DI container in `Program.cs`
4. Ensure PluginRegistry is registered as singleton so plugins can be pre-populated for testing

### 2026-04-05 — Feature 120 Slice 6: RED Tests for ImportParserService (Plugin Extension Point)

**Task:** Write RED-phase tests for the new `ImportParserService` in `BudgetExperiment.Application` that orchestrates file parsing across registered `IImportParser` implementations (core + plugins).

**File created:** `tests/BudgetExperiment.Application.Tests/Import/ImportServiceTests.cs`

**RED Tests Written (6 total):**
1. `ParseFileAsync_NoRegisteredParsers_ThrowsNotSupportedException` — with empty parser collection, throws NotSupportedException containing the extension (.csv)
2. `ParseFileAsync_NoMatchingParser_ThrowsNotSupportedException` — .xyz extension, no parser supports it, throws error
3. `ParseFileAsync_MatchingParser_DelegatesToParser` — .csv file with matching CsvParser triggers ParseAsync call
4. `ParseFileAsync_MatchingParser_ReturnsParserResults` — parser result object passed through to caller
5. `ParseFileAsync_MultipleRegisteredParsers_SelectsCorrectOne` — two parsers (.csv and .ofx), correct one selected by extension
6. `ParseFileAsync_ParserNameMatchesExtension_CaseInsensitive` — .CSV and .csv both match the same parser

**Test Implementation Details:**
- Hand-written fake `FakeImportParser` (no Moq/NSubstitute) with configurable SupportedExtensions and return values
- `ParseAsyncWasCalled` boolean tracking for delegation verification
- xUnit [Fact] + Shouldly assertions
- Arrange/Act/Assert structure
- File extension extraction from filename (e.g., "data.csv" → ".csv")
- Case-insensitive extension matching (.CSV = .csv)

**Service Signature Expected (not yet implemented):**
- `ImportParserService(IEnumerable<IImportParser> parsers)` — DI constructor
- `Task<IReadOnlyList<object>> ParseFileAsync(Stream fileStream, string filename, CancellationToken cancellationToken)` — async parsing method
  - Extracts file extension from filename
  - Finds parser supporting that extension (case-insensitive match)
  - Delegates to parser.ParseAsync()
  - Throws `NotSupportedException` if no parser matches the extension

**Build Status:** ✅ RED — 6 compilation errors (expected):
- `CS0246: The type or namespace name 'ImportParserService' could not be found` (all 6 test methods)

**Next Steps (for GREEN phase):**
1. Create `BudgetExperiment.Application/Import/ImportParserService.cs`
2. Implement constructor accepting `IEnumerable<IImportParser>`
3. Implement `ParseFileAsync(Stream, string, CancellationToken)` method:
   - Extract extension from filename (Path.GetExtension)
   - Normalize to lowercase for comparison
   - Find first parser where SupportedExtensions contains matching extension (case-insensitive)
   - Delegate to parser.ParseAsync() if found
   - Throw NotSupportedException(string containing extension) if not found
4. Register service in DI container (Application layer extension method or API Program.cs)

---

## Feature 120 Slice 7: Report Builder Service (RED Phase) — 2026-04-05

**Task:** Write RED (failing) tests for ReportBuilderService that will orchestrate report builders in the Application layer.

**Files Created:**
1. 	ests/BudgetExperiment.Application.Tests/Reports/ReportBuilderServiceTests.cs (116 lines)
2. 	ests/BudgetExperiment.Application.Tests/Reports/FakeReportBuilder.cs (35 lines)
3. 	ests/BudgetExperiment.Application.Tests/Reports/FakePluginContext.cs (20 lines)

**Tests Written (6 total, all RED):**
1. GetAvailableReports_NoBuilders_ReturnsEmptyList — empty builder list → empty report list
2. GetAvailableReports_WithBuilders_ReturnsReportNames — multiple builders → returns all report names
3. BuildReportAsync_UnknownReportName_ThrowsKeyNotFoundException — unknown report → throws KeyNotFoundException
4. BuildReportAsync_MatchingBuilder_CallsBuildAsync — correct builder invoked
5. BuildReportAsync_MatchingBuilder_ReturnsResult — builder result returned to caller
6. BuildReportAsync_MultipleBuilders_SelectsCorrectOne — multiple builders, correct one selected

**Test Infrastructure:**
- FakeReportBuilder: Tracks BuildAsyncWasCalled, supports configurable return value via SetReturnValue()
- FakePluginContext: Minimal implementation, all members throw NotImplementedException (test doesn't use them)
- xUnit [Fact] + Shouldly assertions (no FluentAssertions, no AutoFixture)

**Compilation Status:** ✅ RED — 12 compilation errors (expected):
- CS0246: The type or namespace name 'ReportBuilderService' could not be found (6 instances per test initialization)
- Multiple downstream errors from constructor/method calls on non-existent type

**ServiceInterface Specification (from docs/120-plugin-system.md, US-120-004):**
- Constructor: ReportBuilderService(IEnumerable<IReportBuilder> builders)
- Method: IReadOnlyList<string> GetAvailableReports() — returns report names only
- Method: Task<object> BuildReportAsync(string reportName, IPluginContext context, CancellationToken cancellationToken) — throws KeyNotFoundException if report not found

**Next Steps (for Lucius — GREEN phase):**
1. Create BudgetExperiment.Application/Reports/IReportBuilderService.cs interface
2. Create BudgetExperiment.Application/Reports/ReportBuilderService.cs implementation
3. Register service in DI container (Program.cs or AddApplication() extension)
4. Tests should pass with minimal GetAvailableReports + BuildReportAsync logic

### 2026-04-09 — Response Compression Middleware Tests

**Task:** Write integration tests verifying that `AddResponseCompression()` / `UseResponseCompression()` middleware correctly compresses HTTP responses.

**File created:** `tests/BudgetExperiment.Api.Tests/CompressionTests.cs`

**Tests Written (3 total, RED until Lucius wires up middleware):**
1. `GetCategories_WithBrotliAcceptEncoding_ReturnsBrotliContentEncoding` — `Accept-Encoding: br` → `Content-Encoding: br`
2. `GetCategories_WithGzipAcceptEncoding_ReturnsGzipContentEncoding` — `Accept-Encoding: gzip` → `Content-Encoding: gzip`
3. `GetCategories_WithNoAcceptEncoding_ReturnsNoContentEncodingHeader` — no Accept-Encoding → no Content-Encoding header

**Key Technical Decision:**
Use `_factory.Server.CreateHandler()` to obtain the raw in-process handler and wrap it in `new HttpClient(handler)`. The `TestServer` handler does NOT perform automatic decompression, so `Content-Encoding` headers survive intact for assertion. This is cleaner than `HttpClientHandler { AutomaticDecompression = DecompressionMethods.None }` which would require a delegating-handler chain to connect to the test server.

**Pattern Used:**
- `[Collection("ApiDb")]` + `IClassFixture<CustomWebApplicationFactory>` (standard API test pattern)
- Auth header manually added (`TestAuto authenticated`) since we bypass `CreateApiClient()`
- Client created and disposed per-test via `using var client = CreateRawClient()`
- Endpoint: `GET /api/v1/categories` (stable, always 200 OK, returns JSON)

**Build Status:** ✅ Compiles clean (0 errors, 0 warnings). Tests are RED pending Lucius wiring middleware.


---

## 2026-04-09: Financial Accuracy Test Implementation (Barbara)

**Date:** 2026-04-09T02:38:31Z
**Status:** Testing Complete — 49 New Tests, All Passing

### Test Campaign Summary

**Objective:** Validate 10 financial accuracy invariants (INV-1 through INV-10) across domain and application layers.

**Test Files Written (6 total):**
1. AccuracyTests for Domain entities (account balance, transfers, money value, budgets, paycheck allocation, category assignment)
2. AccuracyTests for Application services (recurring projection, report consistency, reconciliation integrity)
3. Accuracy test organization: Accuracy/ folder within test project

**Tests Delivered:** 49 new tests, all passing

**Code Quality:** No production bugs discovered in existing code paths

### Technical Decision — Compression Header Testing

**.squad/decisions/inbox/barbara-compression-tests.md** — Pattern documented:
- Use TestServer.CreateHandler() for compression header inspection in API tests
- Avoids automatic decompression by HttpClientHandler
- Applied to BudgetExperiment.Api.Tests compression verification

### Open Items Flagged for Future Production (3 items)

1. [Future production enhancement — detailed in lucius/history.md]
2. [Future production enhancement — detailed in lucius/history.md]
3. [Future production enhancement — detailed in lucius/history.md]

### Deliverables

- 49 passing accuracy tests across Domain.Tests and Application.Tests
- Test location convention established (Accuracy/ folder)
- Integration test pattern documented for compression tests
- No regressions in existing 5,400+ test suite


