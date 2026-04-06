# Feature 121: Testcontainers Migration for Integration Tests
> **Status:** Done

## Overview

Integration tests across both the Infrastructure and API test projects currently use SQLite in-memory databases as a substitute for PostgreSQL. This compromises test fidelity because SQLite and PostgreSQL differ in SQL dialect, constraint enforcement, index behaviour, and EF Core provider behaviour. Bugs that only manifest against PostgreSQL slip through. This work replaces both in-memory database strategies with Testcontainers-based PostgreSQL instances to give integration tests genuine production parity.

## Problem Statement

### Current State

- `BudgetExperiment.Infrastructure.Tests` uses `InMemoryDbFixture.cs` which spins up an `InMemoryDatabase` via EF Core's in-memory provider. This provider skips foreign-key constraints, ignores certain query translations, and has no support for PostgreSQL-specific types or functions.
- `BudgetExperiment.Api.Tests` uses `CustomWebApplicationFactory.cs` which overrides the real `AppDb` connection with `UseInMemoryDatabase()`. API integration tests therefore run against a fake database that doesn't represent production behaviour.

The engineering guidelines (section 15) explicitly state: *"prefer Testcontainers for fidelity"* and note that SQLite in-memory is only acceptable *"if behaviour parity is assured"* — which it is not here.

### Target State

- Both test projects use a Testcontainers-managed PostgreSQL container for all integration tests.
- The container lifecycle is managed via xUnit `IAsyncLifetime` fixtures so containers spin up once per test collection and are disposed cleanly.
- Schema is applied via EF Core migrations (not `EnsureCreated`) to exercise the real migration path.
- No test in either project references EF Core's in-memory provider.

---

## Acceptance Criteria

- [x] `InMemoryDbFixture.cs` in `BudgetExperiment.Infrastructure.Tests` is removed and replaced with a `PostgreSqlContainerFixture` using `Testcontainers.PostgreSql`.
- [x] `CustomWebApplicationFactory.cs` in `BudgetExperiment.Api.Tests` no longer calls `UseInMemoryDatabase()`; it configures Npgsql pointing at a Testcontainers-managed PostgreSQL instance.
- [x] EF Core migrations are applied to the test database at fixture startup — not `EnsureCreated`.
- [x] All existing passing integration tests continue to pass against PostgreSQL.
- [x] A single shared container fixture is reused across tests within a collection (no per-test container spin-up).
- [x] `Testcontainers.PostgreSql` NuGet package is added to both test projects via `dotnet add package`.
- [x] No remaining references to `UseInMemoryDatabase` or EF Core's in-memory provider in any test project.

---

## Technical Design

### Infrastructure Tests

Replace `InMemoryDbFixture.cs` with a `PostgreSqlContainerFixture` implementing `IAsyncLifetime`:

```csharp
// tests/BudgetExperiment.Infrastructure.Tests/PostgreSqlContainerFixture.cs
public sealed class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Apply migrations
        var options = new DbContextOptionsBuilder<BudgetDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;
        await using var context = new BudgetDbContext(options);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

Test classes that previously injected `InMemoryDbFixture` receive `PostgreSqlContainerFixture` via `IClassFixture<PostgreSqlContainerFixture>`.

### API Tests

`CustomWebApplicationFactory` overrides `ConfigureWebHost` to replace the registered Npgsql connection string:

```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureServices(services =>
    {
        // Remove the real DbContext registration
        var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<BudgetDbContext>));
        services.Remove(descriptor);

        // Register with Testcontainers connection string
        services.AddDbContext<BudgetDbContext>(options =>
            options.UseNpgsql(_fixture.ConnectionString));
    });
}
```

The factory holds a `PostgreSqlContainerFixture` instance started in `InitializeAsync` and stopped in `DisposeAsync`.

### NuGet Packages

```powershell
dotnet add tests\BudgetExperiment.Infrastructure.Tests\BudgetExperiment.Infrastructure.Tests.csproj package Testcontainers.PostgreSql
dotnet add tests\BudgetExperiment.Api.Tests\BudgetExperiment.Api.Tests.csproj package Testcontainers.PostgreSql
```

---

## Implementation Plan

### Phase 1: Infrastructure Tests — Testcontainers Fixture

**Objective:** Replace `InMemoryDbFixture` with a real PostgreSQL container fixture and migrate all infrastructure tests to use it.

**Tasks:**
- [x] Add `Testcontainers.PostgreSql` to `BudgetExperiment.Infrastructure.Tests`
- [x] Create `PostgreSqlContainerFixture` implementing `IAsyncLifetime`
- [x] Apply migrations in `InitializeAsync` using `MigrateAsync()`
- [x] Update all test classes that use `InMemoryDbFixture` to use `PostgreSqlContainerFixture`
- [x] Delete `InMemoryDbFixture.cs`
- [x] Run all infrastructure integration tests — confirm green

**Commit:**
```bash
git commit -m "test(infra): replace in-memory SQLite with Testcontainers PostgreSQL

- Add PostgreSqlContainerFixture with IAsyncLifetime
- Apply EF Core migrations in fixture InitializeAsync
- Remove InMemoryDbFixture
- All infrastructure integration tests now run against real PostgreSQL

Refs: #121"
```

---

### Phase 2: API Tests — Testcontainers Factory

**Objective:** Replace `UseInMemoryDatabase` in `CustomWebApplicationFactory` with a Testcontainers PostgreSQL instance.

**Tasks:**
- [x] Add `Testcontainers.PostgreSql` to `BudgetExperiment.Api.Tests`
- [x] Create or update `CustomWebApplicationFactory` to hold a `PostgreSqlContainerFixture`
- [x] Override `ConfigureWebHost` to swap the DbContext registration to use the test container connection string
- [x] Apply migrations in factory `InitializeAsync`
- [x] Remove all references to `UseInMemoryDatabase`
- [x] Run all API integration tests — confirm green

**Commit:**
```bash
git commit -m "test(api): replace in-memory database with Testcontainers PostgreSQL

- CustomWebApplicationFactory now uses PostgreSqlContainer
- DbContext registration overridden with test container connection string
- Migrations applied at factory startup
- Remove UseInMemoryDatabase calls

Refs: #121"
```

---

### Phase 3: Verification & Cleanup

**Objective:** Confirm no in-memory database references remain and CI passes.

**Tasks:**
- [x] Grep solution for `UseInMemoryDatabase` and `InMemoryDatabase` — confirm zero results outside production code
- [x] Confirm full test suite passes: `dotnet test --filter "Category!=Performance"`
- [x] Update any test documentation or README comments referencing the old fixtures

**Commit:**
```bash
git commit -m "test: verify Testcontainers migration complete

- Zero UseInMemoryDatabase references in test projects
- Full integration test suite green against PostgreSQL

Refs: #121"
```

---

## Notes

- Container startup adds ~10–15 s to the first test run in a collection; this is acceptable and matches the project's stated preference for fidelity over speed in integration tests.
- Use `[Collection("Database")]` xUnit collection attributes to share a single container across test classes where appropriate, avoiding redundant container starts.
- CI pipelines (GitHub Actions) support Docker-in-Docker; Testcontainers works out of the box on the existing CI infrastructure.
