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
