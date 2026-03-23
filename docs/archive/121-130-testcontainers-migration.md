# Feature 121: Testcontainers Migration for Integration Tests
> **Status:** Done  
> **Completed:** 2026-01-21

## Summary

Migrated both Infrastructure and API integration test projects from EF Core in-memory databases to Testcontainers-based PostgreSQL 18 instances. This ensures test fidelity by running integration tests against the same database engine used in production, eliminating false positives caused by SQLite/in-memory provider behavioral differences.

## What Changed

### Infrastructure Tests (`BudgetExperiment.Infrastructure.Tests`)
- **Before:** Used `InMemoryDbFixture` with EF Core in-memory provider (SQLite backend)
- **After:** Uses `PostgreSqlFixture` with Testcontainers PostgreSQL 18
- Renamed `InMemoryDbCollection` → `PostgreSqlDbCollection` (collection name: `"PostgreSqlDb"`)
- All 16 repository test classes updated to use new collection
- Schema applied via `MigrateAsync()` instead of `EnsureCreatedAsync()`
- 219 tests pass against real PostgreSQL

### API Tests (`BudgetExperiment.Api.Tests`)
- **Before:** Multiple factories using `UseInMemoryDatabase()` with unique database names
- **After:** All factories use shared `ApiPostgreSqlFixture` (PostgreSQL 18 container)
- Updated 2 primary factories:
  - `CustomWebApplicationFactory`
  - `AuthEnabledWebApplicationFactory`
- Converted 6 authentication integration test classes to PostgreSQL:
  - `AuthenticationBackwardCompatTests` (1 factory)
  - `NoAuthIntegrationTests` (1 factory)
  - `GenericOidcProviderIntegrationTests` (3 factories)
  - `GoogleProviderIntegrationTests` (2 factories)
  - `MicrosoftProviderIntegrationTests` (3 factories)
  - `ProviderSwitchingIntegrationTests` (1 factory)
- All factories implement `IAsyncLifetime` with `MigrateAsync()` + table truncation
- 657 API tests pass (5413 total across all test projects)

### Key Technical Changes
- PostgreSQL version: `postgres:18` (was `postgres:16` in pre-existing fixtures)
- Migration strategy: `context.Database.MigrateAsync()` (was `EnsureCreatedAsync()`)
- Test isolation: Table truncation via `TRUNCATE TABLE ... CASCADE` between tests
- Container lifecycle: Shared per collection (xUnit `IAsyncLifetime`)

## Verification

- **In-memory database references:** Zero in Infrastructure and API test projects (Performance tests excluded per feature scope)
- **Test results:** 5413 passed, 1 skipped (pre-existing), 0 failed
- **Build:** Clean with `-warnaserror` enabled

## Notes

- Performance tests (`BudgetExperiment.Performance.Tests`) intentionally retain in-memory database option for fast PR smoke tests; they have a separate `PERF_USE_REAL_DB` flag for scheduled baseline runs (see Squad Decision #5)
- Testcontainers package was already present in both test projects; no new NuGet dependencies added
- CI (GitHub Actions) supports Docker-in-Docker; no infrastructure changes required

---

**Commits:**
1. `2fb3f5a` — test(infra): upgrade to PostgreSQL 18 and use migrations
2. `9833e30` — test(api): upgrade to PostgreSQL 18 and remove in-memory database
