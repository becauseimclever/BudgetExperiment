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
