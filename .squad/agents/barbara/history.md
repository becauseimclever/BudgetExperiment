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
- bUnit for Blazor component tests (optional)
- Always exclude `Category=Performance` unless explicitly requested: `--filter "Category!=Performance"`
- Culture-sensitive tests must set `CultureInfo.CurrentCulture` to a known culture (e.g., `en-US`)
- Arrange/Act/Assert structure, one assertion intent per test

## Architecture

Domain Tests, Application Tests, Infrastructure Tests, API Tests, Client Tests — all under `tests/`.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-01-09 - Test Coverage & Quality Review

**Test Volume:** 5,018 total tests across 5 core projects (Domain: 757, Application: 904, Infrastructure: 183, API: 595, Client: 2,579). Additional performance tests in dedicated project.

**Coverage Assessment:**
- **Strong:** Application services (93%), API endpoints (92%), Core domain entities (well-tested)
- **Gaps:** 2 untested controllers (RecurringChargeSuggestionsController, RecurringController), 4 untested repositories (AppSettingsRepository, CustomReportLayoutRepository, RecurringChargeSuggestionRepository, UserSettingsRepository)
- **Vanity tests:** ~20 enum value tests (BudgetScopeTests, DescriptionMatchModeTests, ImportBatchStatusTests, etc.) test enum integer values which can never break

**Test Infrastructure Issues:**
- **CRITICAL:** Infrastructure tests use SQLite in-memory (InMemoryDbFixture), not Testcontainers as required by engineering guide
- **CRITICAL:** API tests use EF Core InMemoryDatabase (CustomWebApplicationFactory), not Testcontainers as required
- Engineering guide specifies Testcontainers for PostgreSQL fidelity; current approach risks missing PostgreSQL-specific bugs

**Test Quality:**
- ✅ **Excellent hygiene:** No FluentAssertions, no AutoFixture (banned libraries respected)
- ✅ **Consistent mocking:** Moq used throughout, no NSubstitute mixed in
- ✅ **Structure:** Arrange/Act/Assert pattern followed consistently
- ✅ **Culture handling:** CultureServiceTests correctly sets CultureInfo.CurrentCulture
- ✅ **Performance tests:** Properly isolated with [Trait("Category", "Performance")] using NBomber framework

**Behavioral Test Gaps (partial coverage):**
- TransactionService: Missing tests for UpdateAsync(), ClearLocationAsync(), ClearAllLocationDataAsync(), GetByDateRangeAsync()
- AccountService: Missing tests for GetAllAsync()
- Several domain entities (BudgetCategory, BudgetGoal, BudgetProgress) have test files but may lack complete behavioral coverage

**Integration Test Strategy:**
- API tests use WebApplicationFactory ✅
- Infrastructure tests use in-memory SQLite ❌ (should use Testcontainers)
- Performance tests exist with baseline thresholds ✅

**Top Priority Fixes:**
1. Migrate Infrastructure and API tests to Testcontainers (PostgreSQL) for production fidelity
2. Add tests for RecurringChargeSuggestionsController and RecurringController
3. Add tests for 4 untested repositories
4. Remove vanity enum tests (or document rationale)
5. Fill behavioral test gaps in TransactionService and AccountService

### 2026-07-15 — Testcontainers Migration: Infrastructure Tests

**What changed:**
- Replaced `InMemoryDbFixture` (SQLite in-memory) with `PostgreSqlFixture` (Testcontainers, `postgres:16`)
- All 16 repository test classes updated to inject `PostgreSqlFixture` instead of `InMemoryDbFixture`
- `InMemoryDbCollection` collection definition updated to use `PostgreSqlFixture` (collection name "InMemoryDb" retained to minimise diff)
- `InMemoryDbFixture.cs` deleted; `PostgreSqlFixture.cs` created
- `Microsoft.EntityFrameworkCore.Sqlite` removed from test project; `Testcontainers.PostgreSql 4.11.0` added

**Isolation strategy:** One container per collection (`IAsyncLifetime` on the collection fixture). Each `CreateContext()` call truncates all tables via `TRUNCATE ... CASCADE` before returning the context, giving each test a clean slate without spawning a new container. `CreateSharedContext()` simply opens a second context to the same PostgreSQL database (no truncation) — identical semantics to the old shared-connection SQLite approach because tests always call `SaveChangesAsync()` before the shared context reads.

**Compatibility notes:**
- `PostgreSqlBuilder` 4.11.0 requires passing the image name to the constructor: `new PostgreSqlBuilder("postgres:16")`. The parameterless constructor is marked `[Obsolete]` and was treated as an error.
- `ExecuteSqlRaw` with an interpolated string triggers EF1002 (escalated to error). Suppressed with a scoped `#pragma warning disable EF1002` since the table names come from the EF model, not user input.
- No SQLite-specific test patterns were found; all 183 tests passed against PostgreSQL without any logic changes.

**Result:** 183/183 tests pass. Docker must be running for the Infrastructure test suite.

### Cross-Agent Note: Testcontainers API & DI Findings (2026-03-22T10-04-29)

**API Changes:** Testcontainers 4.11.0 requires explicit image name in PostgreSqlBuilder constructor.

**From Lucius (Backend):** The concrete DI registrations investigated in Fix 2 are all load-bearing — controllers inject directly. This is important context for future Feature 124 (Controller Abstractions) work: assess DIP per controller, noting that changes require updating both service registration and controller injection sites.

### 2026-07-15 — Testcontainers Migration: API Tests

**What changed:**
- `CustomWebApplicationFactory` and `AuthEnabledWebApplicationFactory` migrated from `UseInMemoryDatabase` to real PostgreSQL Testcontainer
- `ApiPostgreSqlFixture` created: single container shared across the `"ApiDb"` collection (same pattern as Infrastructure tests)
- `ApiDbCollection` collection definition created
- Both factories implement `IAsyncLifetime`: `InitializeAsync` calls `EnsureCreatedAsync` then truncates all tables; `DisposeAsync` calls base dispose
- `TruncateAllTables` (private static) extracted inside each factory — same TRUNCATE CASCADE pattern as Infrastructure
- `[Collection("ApiDb")]` attribute added to all 33 test classes that use the factories
- `UserControllerTests` refactored from per-test inline factory creation to `IClassFixture<CustomWebApplicationFactory>` + `ResetDatabase()` called in the constructor (preserves per-test isolation required by those stateful settings tests)
- `VersionControllerTests` and `DebugLogControllerTests` had inline `new CustomWebApplicationFactory()` constructions — fixed to use the injected factory instance
- `Testcontainers.PostgreSql 4.11.0` added to API test project; `Microsoft.EntityFrameworkCore.InMemory` retained (still needed by auth/provider integration tests that create private inline factories)

**Real bug exposed by PostgreSQL:**
- `RecurringTransactionInstanceService.ModifyInstanceAsync` and `RecurringTransferInstanceService.ModifyInstanceAsync` both called `SetExpectedConcurrencyToken` on the parent recurring entity then NEVER updated that entity (only the child exception was modified). PostgreSQL's xmin-based concurrency check only triggers when EF executes an UPDATE on the row. With InMemory EF the check silently succeeded.
- Fix: added `MarkAsModified<T>` to `IUnitOfWork` interface and `BudgetDbContext`; both services now call `_unitOfWork.MarkAsModified(recurring)` immediately after `SetExpectedConcurrencyToken`. This forces EF to include the parent entity in the UPDATE batch, making the WHERE xmin=expected check execute.

**Isolation strategy:** One container per collection (`ApiDb`). Each `CustomWebApplicationFactory.InitializeAsync` truncates all tables — giving each test CLASS a clean slate. `UserControllerTests` additionally truncates in the test class constructor (once per test method) because its tests depend on exact default values.

**Result:** 626/626 API tests pass. Docker must be running for the API test suite.

### 2026-07-20 — Repository Coverage Added

- Added InfraDb-scoped integration tests for AppSettings, CustomReportLayout, RecurringChargeSuggestion, and UserSettings repositories.
- Added detached-entity SaveAsync coverage for UserSettings to ensure Update attaches and persists.

### 2026-03-22 — Performance Test Audit: 45 Tests Categorized

Completed comprehensive audit of all performance test files across the solution. **Findings:** 45 total tests categorized as:
- **17 genuine performance tests:** Real-world latency assertions with meaningful thresholds
- **28 noise tests:** 12 vanity enum tests + 2 correctness tests mislabeled as performance + 21 unit tests of helper infrastructure

**Critical Issues Found:**

1. **`PerformanceWebApplicationFactory` defaults to EF Core InMemory** — must switch to Testcontainers PostgreSQL to capture realistic baselines. Current approach cannot predict real production performance.

2. **Baseline infrastructure inactive** — No `baseline.json` committed. Every CI run reports "No Baseline Yet." 15%/10% regression thresholds have never been applied.

3. **Stress/Spike tests incomplete** — check error rate only; missing p99 latency thresholds.

4. **CI workflow actions broken** — `.github/workflows/performance.yml` pins actions to non-existent versions (v6, v7). Entire performance CI pipeline cannot run.

5. **Hardcoded scenario dates** — Over time, drift from seeded data, measuring performance on increasingly anachronistic datasets.

6. **E2E tests brittle** — All 7 Playwright tests fail if demo server unavailable, breaking PR gates for unrelated code changes.

7. **CategorizationEnginePerformanceTests reclassified** — Two tests lack timing assertions; test behavioral correctness, not performance. Should move to core test suite.

8. **CategorizationEngine threshold too loose** — 5000ms threshold only catches 50× regressions. Recommend lowering to 500ms for 5× detection.

**Deliverable:** 8 actionable decisions merged to `decisions.md` with rationale and implementation guidance.

### 2026-07-20 — Performance Test Infrastructure Fixes

**Task:** Fix two bugs reported by Fortinbra.

#### Fix 1: Seeder data accumulation across tests

**Problem:** `TestDataSeeder.SeedAsync` was called in `IAsyncLifetime.InitializeAsync` (per-test) while the factory was shared via `IClassFixture`. The in-memory EF Core database persisted between calls, so each test in a class seeded on top of the previous test's data. The last test in a 5-test class ran against a 5× larger database.

**Fix:**
- Added `await db.Database.EnsureDeletedAsync()` immediately before seeding in the in-memory path. This drops and recreates the in-memory database to a clean slate before each test.
- Removed the static `FirstAccountId` property from `TestDataSeeder` (static fields are logical races when multiple test classes use the same type). Changed `SeedAsync` to return `Task<Guid>` instead.
- Updated `StressTests` (the only caller that needed `FirstAccountId`) to capture the returned Guid as an instance field `_firstAccountId`.
- `SmokeTests` and `LoadTests` already discarded the return value — no changes needed.

#### Fix 2: Reclassify mislabelled CategorizationEngine tests

**Problem:** Two tests in `CategorizationEnginePerformanceTests` wore `[Trait("Category", "Performance")]` (via class-level attribute) but had no timing assertions:
1. `ApplyRulesAsync_MultipleCalls_UsesCachedRules` — asserted `ruleRepo` called `Times.Once`. Pure cache-correctness test.
2. `ApplyRulesAsync_StringRulesEvaluatedFirst_RegexRulesSkippedWhenStringMatches` — had a `Stopwatch` declared but never asserted against (dead code). Was a correctness test.

**Fix:**
- Removed class-level `[Trait("Category", "Performance")]` from `CategorizationEnginePerformanceTests`.
- Added `[Trait("Category", "Performance")]` directly to `ApplyRulesAsync_100Rules_1000Transactions_CompletesWithinThreshold` (the sole genuine timing test).
- Removed both mislabelled tests from the performance file and moved them to `CategorizationEngineTests`.
  - Cache test rewritten without reflection (shared-cache-via-reflection approach replaced by calling the same engine instance twice — cleaner, less fragile).
  - String-rules test renamed `ApplyRulesAsync_StringRuleMatchesAllTransactions_RegexRuleNeverApplied`; dead `Stopwatch` removed; uses `Assert.Equal/Assert.All` consistent with the host file.
- Removed `CreateEngineWithSharedCache` helper (no longer needed after removing its only caller).

**Verification:**
- `--filter "Category!=Performance"`: 982 tests pass; both reclassified tests appear and pass.
- `--filter "Category=Performance"`: Only 1 test runs (`ApplyRulesAsync_100Rules_1000Transactions_CompletesWithinThreshold`, 111ms).
- Full solution build: 0 errors, 0 warnings.

### 2026-07-20 — Stress/Spike Latency Thresholds + Relative Scenario Dates

**Task (from Fortinbra):** Two remaining performance test infrastructure fixes.

#### Fix 1: Latency thresholds added to stress/spike tests

**What changed in `StressTests.cs`:**
- `Transactions_StressTest`: Added `stats.Ok.Latency.Percent99 < 5000` — 5× the baseline load p99 (1 000 ms). Stress tests sustain 100 req/s; queuing is expected but infinite slowness must not pass.
- `Calendar_StressTest`: Added `stats.Ok.Latency.Percent99 < 10000` — ~3× the baseline load p99 (3 000 ms). Calendar already runs a reduced profile (25 req/s); the 10 s ceiling is generous enough to tolerate Testcontainers latency while still catching catastrophic regressions.
- `Transactions_SpikeTest`: Added `stats.Ok.Latency.Percent99 < 8000` — 8× the baseline load p99 (1 000 ms). Spike bursts cause request queuing, so the threshold is deliberately looser than stress. The goal is to block infinite slowness, not enforce SLAs.

All three scenarios already had the `stats.Fail.Request.Percent < 5` error-rate assertion; the latency assertions were added alongside it.

#### Fix 2: Relative dates in scenario files and seeder

**Scenarios updated:**
- `TransactionsScenario.cs`: `startDate=2025-09-01&endDate=2026-03-15` → `DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6))` to `DateOnly.FromDateTime(DateTime.UtcNow)`, formatted `yyyy-MM-dd`. Computed once at `Create()` time for consistent query range within a run.
- `CalendarScenario.cs`: `year=2026&month=3` → `DateTime.UtcNow.Year` / `DateTime.UtcNow.Month`.
- `BudgetsScenario.cs`: same as Calendar.

**Seeder updated (`TestDataSeeder.cs`):**
- Account open date: `DateTime.UtcNow.AddMonths(-8)` (first of month).
- Transaction range: 6 months ago to today.
- Recurring transaction start: 6 months ago.
- Budget goals: `offset = -2 to +3` months around now (6 goals per category, spanning 2 months past → 3 months forward), ensuring the current month always has goals for the budget scenario query.

**Verification:** Build clean (0 errors, 0 warnings). 5,409 non-performance tests pass (1 pre-existing unrelated failure in `SuggestionsControllerTests.GetMetrics_Returns_200_WithMetrics` — not caused by these changes).

**Commit:** `d325b44`



**Task:** Verify Feature 111 (AsNoTracking, CalendarGridService parallelism, bounded eager loading) doesn't break existing tests.

**Result:** 5409/5410 passed (1 skipped), 0 failed — after fixing one DI bug introduced by Feature 111.

**Bug Found & Fixed:** `DependencyInjection.cs` added `services.AddDbContextFactory<BudgetDbContext>(...)` (Singleton by default), but neither `CalendarGridService` nor `DayDetailService` actually uses `IDbContextFactory` — both use `IServiceScopeFactory` for parallel scope creation. The unused Singleton factory registration cannot consume the Scoped `DbContextOptions` registered by `AddDbContext`, causing DI validation to fail across all 458 API tests.

**Fix:** Removed the dead `AddDbContextFactory` line from `DependencyInjection.cs` (commit `599483a`). No behavior change — pure dead code removal.

**Root Cause Classification:** Feature 111 bug — incorrect DI registration added for an abstraction that was planned but never wired up in the services.

**Pre-existing issues:** None. The 5409 passing tests confirm all other Feature 111 changes (AsNoTracking, bounded eager loading) are safe.

### 2026-03-22T18-23-42Z — Session Close: Batch 2+3 Complete

**Cross-Team Summary:**

- **From Lucius:** Flattened 9 deeply nested methods (improved readability), removed 1,474 `this._` usages (reduced verbosity), expanded 2 service interfaces (+8 methods total), switched 3 controllers to interface injection. Service interfaces now complete for DIP assessment.
- **From Alfred:** DIP verdict complete — all 3 controllers received VERDICT A (interfaces exist but are incomplete). Interfaces expanded by Lucius to match concrete class APIs.
- **From Coordinator:** 5,409 tests passing, 0 build warnings. All assertion bugs fixed (JSONB spacing, HTTP status logic). PR ready for merge.

**Session Outcome:** Infrastructure modernization + architectural alignment complete. All merge requirements met.
