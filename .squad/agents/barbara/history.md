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
