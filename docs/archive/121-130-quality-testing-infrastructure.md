# Archive: Features 121ΓÇô130

> Features completed and archived. Listed in completion order.

---

## Feature 123: Backend Code Quality Cleanup

> **Status:** Done

## Overview

A code quality review on `feature/code-quality-review` surfaced four categories of backend quality issues: an exception handler that uses fragile string matching instead of the existing `DomainException.ExceptionType` enum, three redundant dual-registration DI entries, six methods with excessive nesting depth, and a `DateTime.Now` usage in a Blazor component that violates the project's UTC-everywhere rule. None are blockers, but together they accumulate maintenance debt and introduce subtle risk. This work addresses all four categories.

## Problem Statement

### Current State

1. **`ExceptionHandlingMiddleware`** maps `DomainException` to HTTP status codes by calling `.Message.Contains("not found")`. The domain already has a `DomainException.ExceptionType` enum. String matching is fragile: it breaks on message rewordings, is case-sensitive by default, and obscures intent.

2. **Three redundant DI registrations** register each service twice ΓÇö once as an interface and once as a concrete type ΓÇö with a comment referencing "backward compatibility". No consumer in the solution references the concrete registration. Both entries resolve to the same implementation, so the duplicate is pure noise that misleads future readers about the DI contract.

3. **Six methods with 3+ nesting levels** are harder to read, test, and modify safely:
   - `TransactionListService` ΓÇö recurring instance builder methods
   - `ImportExecuteRequestValidator`
   - `RuleSuggestionResponseParser`
   - `ImportRowProcessor.DetermineCategory`
   - `LocationParserService`

4. **`Reconciliation.razor`** uses `DateTime.Now` instead of `DateTime.UtcNow`. All `DateTime` values in this application must be UTC (engineering guidelines ┬º30).

### Target State

- `ExceptionHandlingMiddleware` switches on `DomainException.ExceptionType` ΓÇö zero string matching.
- Redundant DI registrations are removed; each service is registered once, by interface only.
- The six identified methods are refactored to Γëñ 2 nesting levels using guard clauses, extracted private methods, or helper types.
- `Reconciliation.razor` uses `DateTime.UtcNow`.
- All existing tests pass; no behavioural changes are introduced.

---

## Acceptance Criteria

### Exception Handler

- [x] `ExceptionHandlingMiddleware` contains no `.Message.Contains(...)` calls.
- [x] HTTP status code mapping is driven exclusively by `DomainException.ExceptionType` (or equivalent typed exception switch) .
- [x] All existing `DomainException` types map to the same status codes they did before.
- [x] Unit tests for `ExceptionHandlingMiddleware` cover every `ExceptionType` case.

### DI Cleanup

- [x] The three duplicate service registrations are identified and the concrete-type registrations are removed.
- [x] Existing consumers (if any exist) continue to resolve their dependencies correctly.
- [x] A grep of the solution confirms no remaining `// backward compatibility` DI comments.

### Method Nesting

- [x] `TransactionListService` recurring instance methods: max nesting Γëñ 2.
- [x] `ImportExecuteRequestValidator`: max nesting Γëñ 2.
- [x] `RuleSuggestionResponseParser`: max nesting Γëñ 2.
- [x] `ImportRowProcessor.DetermineCategory`: max nesting Γëñ 2.
- [x] `LocationParserService` (identified method(s)): max nesting Γëñ 2.
- [x] Refactored methods preserve all existing unit test coverage and pass without modification.

### DateTime UTC

- [x] `Reconciliation.razor` uses `DateTime.UtcNow` in every location previously using `DateTime.Now`.
- [x] A grep of the Client project for `DateTime.Now` (excluding comments and string literals) returns zero results after this change.

---

## Technical Design

### Exception Handler Refactor

Replace the current pattern:

```csharp
// Before ΓÇö fragile string matching
if (ex is DomainException domEx && domEx.Message.Contains("not found"))
    return StatusCodes.Status404NotFound;
```

With a switch on the enum:

```csharp
// After ΓÇö typed dispatch
if (ex is DomainException domEx)
{
    return domEx.ExceptionType switch
    {
        DomainExceptionType.NotFound       => StatusCodes.Status404NotFound,
        DomainExceptionType.Conflict       => StatusCodes.Status409Conflict,
        DomainExceptionType.Validation     => StatusCodes.Status422UnprocessableEntity,
        _                                  => StatusCodes.Status400BadRequest,
    };
}
```

Adjust the `DomainExceptionType` enum if any required cases are missing.

### DI Cleanup

Locate registrations of the form:

```csharp
services.AddScoped<IMyService, MyService>();
services.AddScoped<MyService>(); // backward compatibility
```

Remove the second line. Verify with a DI resolution test or manual inspection that no constructor requests the concrete type directly.

### Nesting Reduction Techniques

Use these standard approaches (choose per context):

- **Guard clauses** ΓÇö invert conditional and return/continue early to avoid one nesting level.
- **Extract method** ΓÇö move an inner block to a well-named private method.
- **LINQ / pattern decomposition** ΓÇö replace nested `if`/`foreach` with composable expressions where it improves clarity.

Do not reduce nesting in ways that obscure intent ΓÇö prefer clarity over cleverness.

### DateTime Fix

```razor
@* Before *@
var today = DateTime.Now;

@* After *@
var today = DateTime.UtcNow;
```

If the date is displayed to the user, convert to local time via `CultureService.CurrentTimeZone` (per engineering guidelines ┬º30 and ┬º38).

---

## Implementation Plan

### Phase 1: Exception Handler

**Objective:** Replace string-matching exception mapping with type-safe enum dispatch.

**Tasks:**
- [x] Audit `DomainExceptionType` enum ΓÇö add any missing cases needed for current status code mapping
- [x] Rewrite `ExceptionHandlingMiddleware` status code logic to switch on `ExceptionType`
- [x] Write/update unit tests for the middleware covering each `ExceptionType` case
- [x] Run tests ΓÇö confirm green

**Commit:**
```bash
git commit -m "refactor(api): replace string matching in ExceptionHandlingMiddleware with ExceptionType switch

- Remove .Message.Contains() calls
- Switch on DomainException.ExceptionType for status code mapping
- Unit tests cover all ExceptionType cases

Refs: #123"
```

---

### Phase 2: DI Cleanup

**Objective:** Remove the three redundant concrete-type service registrations.

**Tasks:**
- [x] Locate all `// backward compatibility` DI comments in the solution
- [x] Confirm no constructor or service locator references the concrete type directly
- [x] Remove the redundant `AddScoped<ConcreteType>()` registrations
- [x] Run full test suite ΓÇö confirm no resolution failures

**Commit:**
```bash
git commit -m "refactor(api): remove redundant DI registrations

- Three services were registered as both interface and concrete type
- Concrete-type registrations removed; interface registrations remain
- No consumers reference concrete types

Refs: #123"
```

---

### Phase 3: Nesting Reduction

**Objective:** Refactor the six methods with 3+ nesting levels to Γëñ 2 levels.

**Tasks:**
- [x] `TransactionListService` recurring methods ΓÇö apply guard clauses / extract helper
- [x] `ImportExecuteRequestValidator` ΓÇö extract validation branches into named private methods
- [x] `RuleSuggestionResponseParser` ΓÇö flatten nested parsing logic
- [x] `ImportRowProcessor.DetermineCategory` ΓÇö guard-clause early returns for each condition
- [x] `LocationParserService` ΓÇö extract inner logic blocks
- [x] Confirm all existing unit tests pass unchanged after each refactor

**Commit:**
```bash
git commit -m "refactor(app): reduce method nesting depth in five service classes

- TransactionListService, ImportExecuteRequestValidator, RuleSuggestionResponseParser,
  ImportRowProcessor, LocationParserService all now <= 2 nesting levels
- Guard clauses and extracted private methods; no behaviour changes
- All unit tests pass

Refs: #123"
```

---

### Phase 4: DateTime UTC Fix

**Objective:** Replace `DateTime.Now` with `DateTime.UtcNow` in `Reconciliation.razor`.

**Tasks:**
- [x] Find all `DateTime.Now` uses in `Reconciliation.razor`
- [x] Replace with `DateTime.UtcNow`
- [x] If displayed to user, ensure conversion to local via `CultureService` (check existing pattern in other components)
- [x] Grep Client project for remaining `DateTime.Now` references and fix any others found
- [x] Run Client tests ΓÇö confirm green

**Commit:**
```bash
git commit -m "fix(client): replace DateTime.Now with DateTime.UtcNow in Reconciliation

- All DateTime values must be UTC per project conventions
- Convert to local time for display using CultureService

Refs: #123"
```

---

## Feature 122: Test Coverage Gaps and Vanity Test Cleanup

> **Status:** Done

## Overview

A code quality review on `feature/code-quality-review` identified meaningful test coverage gaps across two controllers and four repositories, plus approximately 20 vanity enum tests that verify integer values and can never detect a real regression. This work adds real tests where they are missing and removes tests that provide false confidence, leaving the suite trustworthy and actionable.

## Problem Statement

### Current State

**Untested controllers:**
- `RecurringChargeSuggestionsController` ΓÇö 4 endpoints with zero test coverage.
- `RecurringController` ΓÇö 2 endpoints (`GetPastDueAsync`, `RealizeBatchAsync`) with zero test coverage.

**Untested repositories:**
- `AppSettingsRepository`
- `CustomReportLayoutRepository`
- `RecurringChargeSuggestionRepository`
- `UserSettingsRepository`

**Vanity tests (~20):**
- Enum tests that assert `(int)SomeEnum.Value == 42`. These pass even if the enum member is renamed, the member is deleted and a new one added with the same integer, or the enum's semantic meaning changes entirely. They protect only against deliberate integer reassignment ΓÇö a scenario that never causes a real runtime regression.

### Target State

- All four repositories have integration tests exercising their primary read/write operations against a real database.
- All uncovered controller endpoints have API integration tests covering happy path, validation failure (400), and not-found (404) cases where applicable.
- Vanity enum integer-value tests are removed from the test suite. Any legitimate concern about enum serialisation is covered by a serialisation-contract test, not an integer assertion.

---

## Acceptance Criteria

### Controller Coverage

- [x] `RecurringChargeSuggestionsController` ΓÇö all 4 uncovered endpoints have tests covering: happy path (2xx), not-found (404), and invalid request (400) where applicable.
- [x] `RecurringController.GetPastDueAsync` ΓÇö tests cover: returns 200 with a list when past-due items exist; returns 200 with empty list when none exist.
- [x] `RecurringController.RealizeBatchAsync` ΓÇö tests cover: valid batch request returns 200/204; invalid/empty batch returns 400.

### Repository Coverage

- [x] `AppSettingsRepository` ΓÇö integration tests cover: get existing settings, get when not found, upsert/save.
- [x] `CustomReportLayoutRepository` ΓÇö integration tests cover: get by id, list by user/scope, create, update, delete.
- [x] `RecurringChargeSuggestionRepository` ΓÇö integration tests cover: get by id, list by account and status, create, update status.
- [x] `UserSettingsRepository` ΓÇö integration tests cover: get by user id, create, update.
- [x] All repository tests run against the Testcontainers PostgreSQL fixture (see Feature 121), not in-memory.

### Vanity Test Removal

- [x] All tests whose sole assertion is `Assert.Equal(<integer>, (int)SomeEnum.Member)` or equivalent are identified and removed.
- [x] No enum test file is left containing only vanity assertions; files are deleted if empty after cleanup.
- [x] If any enum is used in a JSON serialisation contract where the integer value genuinely matters (e.g., stored in DB as integer), a targeted serialisation-contract test replaces the vanity test.

### General

- [x] Overall test suite remains green after all additions and removals: `dotnet test --filter "Category!=Performance"`.
- [x] Code coverage does not regress on any already-covered area.

---

## Technical Design

### Controller Tests (API Integration)

Controller tests use `WebApplicationFactory` (already wired in `BudgetExperiment.Api.Tests`). After Feature 121 lands, the factory uses a real PostgreSQL container. Each controller test class:

1. Seeds the minimum required data in `InitializeAsync`.
2. Calls the endpoint via `HttpClient`.
3. Asserts status code and response body shape.

Relevant test classes to create:
- `RecurringChargeSuggestionsControllerTests` in `BudgetExperiment.Api.Tests`
- `RecurringControllerTests` (extend or add to existing) in `BudgetExperiment.Api.Tests`

### Repository Tests (Infrastructure Integration)

Repository tests use `PostgreSqlContainerFixture` (introduced in Feature 121). Each test:

1. Resolves the repository through a `BudgetDbContext` scoped to the test connection.
2. Seeds data directly via `DbContext` for arrange steps.
3. Calls the repository method under test.
4. Asserts the result without relying on EF Core tracking.

New test classes in `BudgetExperiment.Infrastructure.Tests`:
- `AppSettingsRepositoryTests`
- `CustomReportLayoutRepositoryTests`
- `RecurringChargeSuggestionRepositoryTests`
- `UserSettingsRepositoryTests`

### Vanity Enum Test Identification

Search pattern to locate vanity tests:

```powershell
Select-String -Path "tests\**\*Tests.cs" -Pattern "\(int\)\w+\.\w+\s*==" -Recurse
```

Review each hit. Remove the test if its only value is asserting an integer constant. If an enum drives JSON serialisation and its integer values are part of a stored contract, replace with a serialisation round-trip test:

```csharp
[Fact]
public void SomeEnum_SerializesAsExpectedJsonValues()
{
    var json = JsonSerializer.Serialize(SomeEnum.Pending);
    Assert.Equal("\"Pending\"", json); // string serialisation, not integer
}
```

---

## Implementation Plan

### Phase 1: Repository Integration Tests

**Objective:** Add integration tests for the four untested repositories.

**Tasks:**
- [x] Confirm Feature 121 `PostgreSqlContainerFixture` is available (or add a temporary in-project copy if 121 has not landed)
- [x] Write `AppSettingsRepositoryTests` ΓÇö get, upsert
- [x] Write `CustomReportLayoutRepositoryTests` ΓÇö CRUD by user/scope
- [x] Write `RecurringChargeSuggestionRepositoryTests` ΓÇö list by account+status, status transitions
- [x] Write `UserSettingsRepositoryTests` ΓÇö get by user, upsert
- [x] Run infrastructure tests ΓÇö confirm green

**Commit:**
```bash
git commit -m "test(infra): add integration tests for four untested repositories

- AppSettingsRepository: get, upsert
- CustomReportLayoutRepository: list/create/update/delete
- RecurringChargeSuggestionRepository: filter by status, transitions
- UserSettingsRepository: get, upsert

Refs: #122"
```

---

### Phase 2: Controller API Tests

**Objective:** Add API integration tests for uncovered endpoints on `RecurringChargeSuggestionsController` and `RecurringController`.

**Tasks:**
- [x] Write `RecurringChargeSuggestionsControllerTests` covering all 4 uncovered endpoints (happy path, 400, 404)
- [x] Extend `RecurringControllerTests` (or create if absent) for `GetPastDueAsync` and `RealizeBatchAsync`
- [x] Seed required fixtures in test setup
- [x] Run API tests ΓÇö confirm green

**Commit:**
```bash
git commit -m "test(api): add coverage for RecurringChargeSuggestions and Recurring endpoints

- RecurringChargeSuggestionsController: detect, list, accept, dismiss
- RecurringController: GetPastDueAsync, RealizeBatchAsync
- Happy path, 400, and 404 scenarios

Refs: #122"
```

---

### Phase 3: Remove Vanity Enum Tests

**Objective:** Identify and remove all enum integer-value tests; replace with serialisation tests where the contract genuinely matters.

**Tasks:**
- [x] Search for `(int)` casts inside test assertion expressions
- [x] Review each hit; remove pure vanity assertions
- [x] For any enum with a real serialisation contract, write a targeted replacement test
- [x] Delete now-empty test files
- [x] Run full test suite ΓÇö confirm green and coverage not regressed

**Commit:**
```bash
git commit -m "test: remove vanity enum integer-value tests

- ~20 tests asserting (int)Enum.Member == N removed
- No regression: enum integer values are not part of any stored contract
- Serialisation contract tests added where JSON shape matters

Refs: #122"
```

---

## Notes

- Feature 121 (Testcontainers migration) should land before or alongside Phase 1 of this feature so repository tests run against PostgreSQL from the start.
- The vanity enum tests give a misleadingly high line-coverage number. Removing them will lower the raw coverage percentage slightly but raise the quality of that metric.

---

## Feature 121: Testcontainers Migration for Integration Tests

> **Status:** Done  
> **Completed:** 2026-01-21

## Summary

Migrated both Infrastructure and API integration test projects from EF Core in-memory databases to Testcontainers-based PostgreSQL 18 instances. This ensures test fidelity by running integration tests against the same database engine used in production, eliminating false positives caused by SQLite/in-memory provider behavioral differences.

## What Changed

### Infrastructure Tests (`BudgetExperiment.Infrastructure.Tests`)
- **Before:** Used `InMemoryDbFixture` with EF Core in-memory provider (SQLite backend)
- **After:** Uses `PostgreSqlFixture` with Testcontainers PostgreSQL 18
- Renamed `InMemoryDbCollection` ΓåÆ `PostgreSqlDbCollection` (collection name: `"PostgreSqlDb"`)
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
1. `2fb3f5a` ΓÇö test(infra): upgrade to PostgreSQL 18 and use migrations
2. `9833e30` ΓÇö test(api): upgrade to PostgreSQL 18 and remove in-memory database

---

## Feature 124: Controller Abstractions Assessment and Style Consistency

> **Status:** Done

## Overview

Two related housekeeping items surfaced during the code quality review: three API controllers depend directly on concrete service classes rather than interfaces, and private field naming is inconsistent across Application services (`this._field` in some, `_field` in others). This document assesses the DIP concern pragmatically and defines a plan to enforce consistent field-access style via `.editorconfig`.

---

## DIP Assessment Results (2026-03-22)

### VERDICT A: TransactionsController, RecurringTransactionsController, RecurringTransfersController ΓåÆ Add Interface

**Rationale:** The interfaces (`ITransactionService`, `IRecurringTransactionService`, `IRecurringTransferService`) already exist and are registered in DI. The controllers simply inject the concrete types instead of the interfaces. This is a **zero-cost fix** ΓÇö no new interfaces need extraction, just change the constructor parameter types.

**Assessment Against Pragmatic Criteria:**

| Question | TransactionsController | RecurringTransactionsController | RecurringTransfersController |
|----------|----------------------|-------------------------------|------------------------------|
| Interface already exists? | Γ£à Yes (`ITransactionService`) | Γ£à Yes (`IRecurringTransactionService`) | Γ£à Yes (`IRecurringTransferService`) |
| Interface registered in DI? | Γ£à Yes | Γ£à Yes | Γ£à Yes |
| Realistic test substitution? | ΓÜá∩╕Å Integration tests use Testcontainers, but controller unit tests would benefit from mocking | ΓÜá∩╕Å Same | ΓÜá∩╕Å Same |
| Complexity cost to add? | **Zero** ΓÇö interface exists, just change type | **Zero** | **Zero** |
| Runtime swap scenario? | No | No | No |

**Decision:** Since the interfaces already exist and are registered, the controllers should use them. This:
1. Follows DIP without adding complexity (interfaces already exist)
2. Enables potential future controller unit tests without a database
3. Removes the need for duplicate DI registrations (concrete type registrations can be removed)
4. Aligns with the rest of the codebase where controllers inject interfaces

**Implementation Required:**
1. Update `TransactionsController` constructor: `TransactionService` ΓåÆ `ITransactionService`
2. Update `RecurringTransactionsController` constructor: `RecurringTransactionService` ΓåÆ `IRecurringTransactionService`
3. Update `RecurringTransfersController` constructor: `RecurringTransferService` ΓåÆ `IRecurringTransferService`
4. Remove redundant concrete type registrations from `DependencyInjection.cs`
5. Add missing methods to `IRecurringTransactionService`: `SkipNextAsync`, `UpdateFromDateAsync`
6. Add missing methods to `IRecurringTransferService`: `UpdateAsync`, `DeleteAsync`, `PauseAsync`, `ResumeAsync`, `SkipNextAsync`, `UpdateFromDateAsync`
7. Update test mocks in `ChatActionExecutorTests.cs` to implement new interface methods

**Note:** The assessment revealed that the interfaces are incomplete ΓÇö the concrete classes have methods the interfaces don't define. The interfaces need to be expanded to match the concrete implementations' public API.

---

## Problem Statement

### Current State

**DIP: Concrete dependencies in controllers**

`TransactionsController`, `AccountsController`, and `RecurringTransactionsController` inject concrete service classes (e.g., `TransactionService`, `AccountService`) directly via constructor parameters rather than interfaces. This was noted in the March 2026 architecture review.

The project convention (per engineering guidelines ┬º7, DIP) is that higher layers depend on abstractions. However, Fortinbra's stated directive for this review is: *"Apply SOLID principles judiciously. Add interfaces/abstractions when they earn their weight; skip when the added complexity doesn't justify the benefit. A single concrete service with no realistic substitution scenario doesn't need an interface just to satisfy DIP."*

Assessment is therefore required before work begins: do these services have realistic substitution scenarios that justify extraction?

**Style: `this._field` vs `_field`**

Some Application service classes access private fields with the explicit `this.` qualifier (`this._repository`, `this._logger`). Others use the bare form (`_repository`, `_logger`). Both compile and StyleCop does not flag one over the other by default, but the inconsistency makes the codebase feel unowned and slightly complicates future code generation / refactoring.

### Target State

- A documented decision exists for each of the three controllers: either introduce an interface (with justification) or record the explicit decision not to (with rationale), keeping the concrete dependency.
- Private field access is consistent across all Application services. The project convention (`_camelCase` per ┬º5) is enforced by `.editorconfig`; the `this.` qualifier is not used for field access.

---

## Acceptance Criteria

### DIP Assessment

- [x] Each of the three controllers (`TransactionsController`, `RecurringTransactionsController`, `RecurringTransfersController`) is assessed: does a realistic substitution scenario exist?
  - A scenario is "realistic" if it is needed for testability (e.g., mocking in unit tests), pluggability (e.g., swapping implementations based on config), or is already anticipated by another feature doc.
  - **Result:** Interfaces already exist; zero-cost to use them. Verdict A for all three.
- [x] Controllers updated to inject interfaces (implementation required)
- [x] Interfaces expanded to include missing methods (implementation required)
- [x] DI registrations cleaned up (remove duplicate concrete registrations) (implementation required)

### Style Consistency

- [x] `.editorconfig` is updated to add the `dotnet_style_qualification_for_field = false:warning` rule (or equivalent) so that `this._field` style is flagged.
- [x] All existing `this._field` usages in Application services are updated to `_field`.
- [x] `dotnet format --verify-no-changes` passes after the style update.
- [x] No StyleCop or analyzer warnings introduced.

---

## Technical Design

### DIP Pragmatic Framework

Before extracting an interface, answer these questions for each service:

| Question | If Yes ΓåÆ | If No ΓåÆ |
|----------|----------|---------|
| Is the controller unit-tested or planned to be? | Interface aids mocking | No benefit |
| Does any other feature doc plan a second implementation? | Interface required | No benefit |
| Is the service a domain boundary that Infrastructure could implement differently? | Interface appropriate | Concrete is fine |

For application services that are simple orchestrators with no realistic alternative implementation and no current unit test isolation need, the concrete dependency is the pragmatic choice. The engineering guidelines already acknowledge this in ┬º25: *"A single concrete service with no realistic substitution scenario doesn't need an interface just to satisfy DIP."*

### Style Fix

Add to `.editorconfig` in the repository root (under `[*.cs]`):

```ini
# Do not qualify field access with 'this.'
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning
```

Then run:

```powershell
dotnet format c:\ws\BudgetExperiment\BudgetExperiment.sln --diagnostics IDE0003
```

This will automatically remove `this.` qualifiers where the rule fires.

---

## Implementation Plan

### Phase 1: DIP Assessment and Decision

**Objective:** Evaluate each controller and record the decision; implement interface extraction only where justified.

**Tasks:**
- [ ] Review `TransactionsController` ΓÇö does it have (or need) unit tests? Is a second `ITransactionService` implementation plausible?
- [ ] Review `AccountsController` ΓÇö same assessment
- [ ] Review `RecurringTransactionsController` ΓÇö same assessment
- [ ] For each controller where an interface is justified:
  - [ ] Extract `IXxxService` interface in the Application layer
  - [ ] Update DI registration: `services.AddScoped<IXxxService, XxxService>()`
  - [ ] Update controller constructor to inject the interface
  - [ ] Write or update controller unit/integration tests using a test double
- [ ] For each controller where the concrete dependency is retained:
  - [ ] Record the decision in `.squad/decisions.md` with rationale
- [ ] Run full test suite ΓÇö confirm green

**Commit (if interface extracted):**
```bash
git commit -m "refactor(api): extract service interfaces for controllers where substitution is justified

- ITransactionService / IAccountService / IRecurringTransactionService extracted as applicable
- Controllers depend on interfaces; DI wiring updated
- Concrete-dependency decisions recorded in decisions.md

Refs: #124"
```

**Commit (if no interface extracted for a given controller):**
```bash
git commit -m "docs: record DIP assessment decisions for controller service dependencies

- Assessed TransactionsController, AccountsController, RecurringTransactionsController
- Concrete dependencies retained where no realistic substitution scenario exists
- Rationale documented in .squad/decisions.md

Refs: #124"
```

---

### Phase 2: Style Consistency ΓÇö Remove `this.` Qualifiers

**Objective:** Enforce consistent `_field` (no `this.`) access across Application services via `.editorconfig` and `dotnet format`.

**Tasks:**
- [ ] Add `dotnet_style_qualification_for_field = false:warning` (and related rules) to `.editorconfig`
- [ ] Run `dotnet format --diagnostics IDE0003` to auto-fix existing violations
- [ ] Review the diff ΓÇö confirm only `this.` removal, no logic changes
- [ ] Run `dotnet build` ΓÇö confirm zero new warnings
- [ ] Run tests ΓÇö confirm green

**Commit:**
```bash
git commit -m "style: enforce no this. qualifier for field access

- .editorconfig: dotnet_style_qualification_for_field = false:warning
- dotnet format applied to remove this._field usages in Application services
- No logic changes

Refs: #124"
```

---

## Notes

- The `this.` qualifier issue spans Application services primarily; check Infrastructure services as a secondary sweep.
- If StyleCop's `SA1101` rule (`PrefixLocalCallsWithThis`) is currently enabled in `stylecop.json`, it must be disabled ΓÇö it conflicts with the `IDE0003` rule enforcing the opposite style. Review `stylecop.json` before applying the `.editorconfig` change.
- Do not introduce interfaces speculatively. The goal is consistent, justifiable architecture ΓÇö not maximum indirection.

---

## Feature 128: Calendar Daily Amount and Running Total Correctness Audit

# Feature 128: Calendar Daily Amount and Running Total Correctness Audit
> **Status:** Done
> **Priority:** High
> **Scope:** Correctness across all accounts

## Problem Statement

Calendar day amounts and running totals must be mathematically correct for the selected scope, including multi-account views. Today, behavior is not documented as a formal audit target with explicit invariants and traceability from invariant to tests to defect classes. Before any implementation changes, we need an audit-first baseline that defines correctness, validates current behavior, and identifies gaps.

## Goals / Non-Goals

### Goals

- Define exact correctness rules for day amount and running total calculations.
- Audit behavior across single-account and all-accounts views.
- Validate edge behavior (transfers, deletes, corrections, boundaries).
- Establish invariant-driven tests across unit, integration, API, and E2E layers.
- Produce a defect-oriented traceability matrix to guide fixes after audit.

### Non-Goals

- No feature redesign or UI redesign in this phase.
- No schema or domain model redesign in this phase.
- No performance tuning in this phase unless a correctness defect depends on it.
- No unrelated refactors.

## Audit Scope

This audit is limited to correctness for:

- Day amount accuracy for a calendar date under a selected account scope.
- Running total correctness over ordered dates under the same scope.
- Inclusion/exclusion rules for transaction state (deleted, pending, corrected).
- Multi-account aggregation behavior, including transfers.
- Day-boundary attribution rules for date-based grouping.

Out of scope for this document:

- Chart rendering, visual styling, and component layout concerns.
- New filtering features beyond what already exists.

## Data Scenarios

1. Single account, same-day mixed transactions.
2. Multiple accounts, same-day transactions, aggregate view.
3. Multiple accounts with account filter applied.
4. Transfer between two included accounts on same day.
5. Transfer where only one side account is in the selected scope.
6. Correction workflow: amount edited after initial posting.
7. Soft delete and restore flow.
8. Zero-amount transaction (if supported).
9. Date boundary case around midnight attribution.

For each scenario, record:

- Inputs: account scope, transaction list, day under test.
- Expected day amount.
- Expected running total series up to and including that day.
- Notes on whether behavior is current-pass or current-fail.

## Edge Cases

- No transactions on a day must yield day amount 0 and unchanged running total.
- Initial balance interaction with first transaction day.
- Duplicate ingestion protection (same external transaction imported twice).
- Reordered transaction arrival (late imports for earlier dates).
- Transfer symmetry when both accounts are included vs partially included.
- Negative and positive corrections on prior dates and their downstream impact.
- Leap day and month boundary continuity.

## Invariants

I1. Day Reconciliation

For day D and selected scope S:

calendar_day_amount(D, S) = sum(amount of all included transactions on D within S)

I2. Running Total Continuity

running_total(D, S) = running_total(previous_day(D), S) + calendar_day_amount(D, S)

I3. Prefix Sum Correctness

running_total(D, S) = initial_balance(S) + sum(calendar_day_amount(d, S)) for all d <= D

I4. Scope Commutativity

Selecting accounts [A, B, C] must produce same results regardless of order.

I5. Transfer Net-Zero in Full Aggregate

If both source and destination transfer legs are included in S, net transfer contribution across S is 0 for that day.

I6. Deletion Idempotence

After deletion is applied, recomputation excludes deleted transactions; re-applying same delete changes nothing further.

I7. Deterministic Recompute

Given identical transaction set and scope, recomputation returns identical day amounts and running totals.

## Current Implementation Baseline

Audit snapshot date: 2026-04-04.

### Code Path Map (Current)

- API entry: [src/BudgetExperiment.Api/Controllers/CalendarController.cs](../src/BudgetExperiment.Api/Controllers/CalendarController.cs)
	- `GetCalendarGridAsync(...)` delegates to `ICalendarGridService.GetCalendarGridAsync(...)`.
- Application orchestrator: [src/BudgetExperiment.Application/Calendar/CalendarGridService.cs](../src/BudgetExperiment.Application/Calendar/CalendarGridService.cs)
	- `GetCalendarGridAsync(...)` computes `gridStartDate/gridEndDate`, loads daily totals, recurring projections, opening balance, and in-grid initial balances.
	- `BuildGridDays(...)` sets per-day `ActualTotal`, `ProjectedTotal`, and `CombinedTotal`.
	- `CalculateRunningBalances(...)` computes `EndOfDayBalance` as running accumulation.
- Actual per-day amount source: [src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs](../src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs)
	- `GetDailyTotalsAsync(year, month, accountId, ...)` filters by month bounds and optional account, then `GroupBy(t => t.Date)` and `Sum(t => t.Amount.Amount)`.
- Running total opening/injection inputs: [src/BudgetExperiment.Application/Accounts/BalanceCalculationService.cs](../src/BudgetExperiment.Application/Accounts/BalanceCalculationService.cs)
	- `GetOpeningBalanceForDateAsync(date, accountId, ...)` includes initial balances for accounts with `InitialBalanceDate < date` and prior transactions.
	- `GetInitialBalancesByDateRangeAsync(startDate, endDate, accountId, ...)` returns per-date initial-balance additions for accounts starting inside the grid range.
- Transfer handling (actual + projected):
	- Domain transfer primitives: [src/BudgetExperiment.Domain/Accounts/Transaction.cs](../src/BudgetExperiment.Domain/Accounts/Transaction.cs) via `CreateTransfer(...)` and `CreateFromRecurringTransfer(...)`.
	- Manual transfer realization: [src/BudgetExperiment.Application/Accounts/TransferService.cs](../src/BudgetExperiment.Application/Accounts/TransferService.cs) creates paired source negative + destination positive transactions.
	- Recurring transfer projection: [src/BudgetExperiment.Application/Recurring/RecurringTransferInstanceProjector.cs](../src/BudgetExperiment.Application/Recurring/RecurringTransferInstanceProjector.cs) emits both legs (source negative, destination positive) unless filtered to one account.
- Scope / multi-account aggregation:
	- Transaction scope filter: `TransactionRepository.ApplyScopeFilter(...)` in [src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs](../src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs).
	- With `accountId == null`, aggregation is across all scope-visible accounts.
- Day-boundary grouping behavior:
	- Transaction date key is `DateOnly` (`Transaction.Date`) in [src/BudgetExperiment.Domain/Accounts/Transaction.cs](../src/BudgetExperiment.Domain/Accounts/Transaction.cs).
	- Calendar grouping is by stored `DateOnly` value; no time-of-day/time-zone conversion exists in the calendar aggregation path.

### Top Likely Defect Risks vs I1-I7

1. I1/I2/I3 risk: `GetDailyTotalsAsync(...)` only loads transactions for the target month, while running balances iterate full 42-day grid. Out-of-month days can contribute `0` actuals even when transactions exist, causing running-balance drift at month edges.
2. I1/I2/I3 risk: running accumulation uses `CombinedTotal` (actual + projected recurring) in `CalculateRunningBalances(...)`. If invariants are defined against included realized transactions only, projected values may violate reconciliation and prefix-sum expectations.
3. I5 risk: transfer net-zero holds only when both legs are present in the selected scope/account set. With account filtering (or partial visibility), one-sided transfer impact is expected but can be misinterpreted as correctness drift unless explicitly tested and documented.
4. I6 risk: transaction deletion is hard-delete (`TransactionService.DeleteAsync(...)` -> `RemoveAsync(...)`) with no soft-delete state in aggregation path. Scenario coverage for delete/restore idempotence may fail or be undefined for restore workflows.
5. I7 risk: daily totals select currency from `g.First().Amount.Currency` per date group. Mixed-currency same-day data (if introduced) creates non-deterministic/ambiguous currency selection and invalid sums.

## Test Strategy (unit/integration/api/e2e)

### Unit

- Calculation helpers: day aggregation, running accumulation, inclusion filters.
- Invariant-level tests for I1-I7 with minimal synthetic datasets.
- Order-independence tests for multi-account selection.

### Integration

- Repository plus database-backed aggregation correctness with realistic transaction graphs.
- Transfer cases across multiple accounts and date windows.
- Delete/correction re-read consistency from persistence.

### API

- Endpoint-level verification that response totals match persistence sums for same scope/date.
- Contract checks for day and running totals under account filters.
- Regression tests for known defect patterns (double-counting, skipped days).

### E2E

- Seed known scenarios and verify calendar day values and running total progression end-to-end.
- Include all-accounts view plus filtered-account view transitions.
- Validate user-visible values against authoritative expected data fixture.

## Test Usefulness and Anti-Pattern Guidance

Useful tests:

- Assert mathematical properties (invariants), not only snapshots.
- Use small fixtures where expected sums are obvious and reviewable.
- Include at least one defect-reproduction test per discovered bug class.
- Verify both positive and negative paths (include/exclude behavior).

Anti-patterns to avoid:

- Asserting implementation details instead of observable totals.
- Over-mocking persistence paths in integration-level correctness tests.
- Giant opaque fixtures where expected values are hard to validate manually.
- Duplicate tests that cover same invariant without new defect signal.

## Traceability Matrix (Invariant -> Test -> Defect Class)

| Invariant | Primary Test IDs (planned) | Defect Class Prevented |
|--------|--------------------------|-------------------------|
| I1 Day Reconciliation | UT-128-I1, IT-128-I1, API-128-I1 | Day mismatch, dropped transaction, duplicate count |
| I2 Running Continuity | UT-128-I2, API-128-I2, E2E-128-I2 | Running jumps, skipped-day propagation bug |
| I3 Prefix Sum Correctness | UT-128-I3, IT-128-I3 | Initial balance drift, cumulative drift |
| I4 Scope Commutativity | UT-128-I4, API-128-I4 | Account-order-dependent totals |
| I5 Transfer Net-Zero (full aggregate) | UT-128-I5, IT-128-I5, E2E-128-I5 | Transfer double-counting in all-accounts view |
| I6 Deletion Idempotence | UT-128-I6, IT-128-I6, API-128-I6 | Ghost transaction after delete, repeated delete drift |
| I7 Deterministic Recompute | UT-128-I7, IT-128-I7 | Non-deterministic totals, race-sensitive recompute |

## Current Test Coverage Baseline

Quick baseline from existing tests before implementing Feature 128 fixes.

### By Layer

- Unit (Application):
	- `CalendarGridServiceTests` validates 42-day grid shape, basic daily total inclusion, and end-of-day balance accumulation from opening balance.
	- `BalanceCalculationServiceTests` validates opening balance behavior across account start-date combinations and date-range initial balance injection behavior.
	- `DayDetailServiceTests` validates day-level actual/projected merge and transfer cancellation behavior in day detail responses.
- Integration (Infrastructure):
	- `TransactionRepositoryTests` validates `GetDailyTotalsAsync` aggregation and month/account filtering using PostgreSQL test infrastructure.
	- No integration tests currently assert full calendar grid running-total correctness across multi-day/multi-account transfer scenarios.
- API:
	- `CalendarControllerTests` currently validate parameter handling/status codes for summary endpoint only.
	- No API tests currently assert `GET /api/v1/calendar/grid` day totals or running total correctness.
- E2E:
	- Functional calendar tests validate rendering, navigation, and account filter request behavior.
	- E2E currently does not verify mathematically expected day amounts or running totals against known fixtures.

### Invariant Coverage Snapshot (Current)

- I1 Day Reconciliation: Partial (unit + repository aggregation checks exist; no API/E2E value assertions).
- I2 Running Total Continuity: Partial (unit running-balance accumulation exists in `CalendarGridServiceTests`; no end-to-end invariant assertion).
- I3 Prefix Sum Correctness: Partial (opening-balance unit tests exist; no explicit prefix-sum invariant test at calendar grid level).
- I4 Scope Commutativity: Missing (no account-order commutativity tests found).
- I5 Transfer Net-Zero in Full Aggregate: Partial (day-detail transfer cancellation assertions exist; grid-level/all-accounts calendar invariant not asserted).
- I6 Deletion Idempotence: Missing (no calendar-specific delete/re-delete recompute assertions found).
- I7 Deterministic Recompute: Missing (no repeated recompute deterministic equality assertions found).

### Initial Gap Checklist

- [x] UT: Add explicit I3 prefix-sum test on calendar grid (`running_total == opening + cumulative day totals`).
- [x] UT: Add I4 account-scope commutativity test (same account set in different order yields identical outputs).
- [x] UT: Add I5 transfer net-zero test on calendar grid for all-accounts scope (both legs included).
- [x] UT: Add I7 deterministic recompute test (same inputs produce identical grid outputs).
- [x] IT: Add multi-account daily aggregation integration test for `GetDailyTotalsAsync` (null account scope).
- [x] IT: Add paired transfer integration test verifying net-zero aggregate day total.
- [x] IT: Add recompute-stability integration test after delete (`GetDailyTotalsAsync` read consistency).
- [x] IT: Add late-insert integration test verifying recompute includes earlier inserted date.
- [x] API: Add `GET /api/v1/calendar/grid` invariant assertions for day totals and running balances with seeded deterministic data.
- [x] API: Add I6 deletion idempotence regression (`delete`, recompute; repeated `delete`, unchanged output).
- [x] E2E: Add deterministic calendar assertions for visible day total and running balance progression (UI values matched against scoped grid response).
- [x] E2E: Add account-filter transition assertions that validate visible values against each selected account response.

## Audit Checklist and Exit Criteria

### Audit Checklist

- [x] Confirm and document authoritative inclusion rules for transaction states.
- [x] Confirm day-boundary attribution rule used by calendar calculations.
- [x] Execute planned tests for I1-I7 at required layers.
- [x] Record pass/fail per data scenario and per invariant.
- [x] Log defects with reproduction dataset and expected vs actual outputs.
- [x] Identify minimal fix set required for each failing invariant.

### Execution Snapshot (2026-04-04)

| Layer | Tests | Passed | Failed |
|-------|-------|--------|--------|
| Application unit (CalendarGridServiceTests) | 18 | 18 | 0 |
| API (CalendarControllerTests) | 7 | 7 | 0 |
| Infrastructure (TransactionRepositoryTests) | 24 | 24 | 0 |
| E2E LocalOnly (CalendarTests) | 2 | 2 | 0 |

### Pass/Fail Per Invariant

| Invariant | Test Coverage | Result | Notes |
|-----------|-------------|--------|-------|
| I1 Day Reconciliation | UT, IT, API | ✅ Pass | AccumulatesEndOfDayBalance, IT multi-account, API grid invariant |
| I2 Running Total Continuity | UT, API | ✅ Pass | PrefixSum test (I3 subsumes I2), API running continuity |
| I3 Prefix Sum Correctness | UT, IT | ✅ Pass | Explicit I3 test in CalendarGridServiceTests |
| I4 Scope Commutativity | UT (additivity) | ✅ Pass | Modelled as aggregate == sum of parts (API surface supports single or all; order permutation is inherently satisfied by SQL SUM) |
| I5 Transfer Net-Zero (full aggregate) | UT, IT | ✅ Pass | I5 test in CalendarGridServiceTests; IT paired transfer |
| I6 Deletion Idempotence | IT, API | ✅ Pass | AfterDelete_RecomputeIsStable; API I6 deletion test |
| I7 Deterministic Recompute | UT | ✅ Pass | I7 parallel call test in CalendarGridServiceTests |

### Defect Log

No defects discovered during audit execution. All invariant tests pass against the current implementation.

**Pre-existing defect resolved during audit (Risk 1):** `GetDailyTotalsAsync` previously queried only the target calendar month; a 42-day grid spanning two months would produce `0` actuals for out-of-month days, causing I2/I3 running-balance drift at month edges. Fixed in this audit phase by implementing `GetGridDailyTotalsAsync` in `CalendarGridService`, which queries all months covered by the grid range and merges results. No current-state defects remain.

### Exit Criteria

Audit phase is complete only when all are true:

- [x] All invariants mapped to at least one implemented test.
- [x] All planned layers include meaningful coverage for core invariants.
- [x] Every failing case has a tracked defect and owner.
- [x] Team agrees on correctness specification and boundary rules.
- [x] Implementation tasks below are ready to execute.

## Initial Implementation Tasks (After Audit)

These tasks are intentionally deferred until audit completion.

- [x] Implement fixes for invariant failures discovered in I1-I7 audit.
  - Risk 1 (I2/I3 month-edge drift): Fixed via `GetGridDailyTotalsAsync` in `CalendarGridService`.
  - No additional invariant failures found.
- [x] Add/adjust calculation service logic only where audit proved defects.
- [x] Add regression tests tied to each resolved defect class.
  - E2E, API, infrastructure, and unit regression coverage added across all discovered risk areas.
- [ ] Update API mapping/serialization if output contract mismatches correctness spec.
  - No mismatches found; no changes required.
- [ ] Add targeted E2E scenario seeds for previously failing real-world cases.
  - No real-world reproducers confirmed; E2E guard tests cover existing live data patterns.
- [x] Update docs status and archive this feature document after implementation is done.

---

## Feature 127: Enhanced Charts & Visualizations

> **Status:** Done

## Overview

Upgraded the charting system from 11 hand-rolled SVG components to a hybrid model: 7 new self-implemented SVG chart types (Tier 1/2) plus 2 ApexCharts-backed types (Tier 3) for complex algorithms. Added a testable `IChartDataService` data layer, wired new charts into all existing report pages, and delivered a `ReportsDashboard` aggregate page. Zero regression to existing SVG charts.

**Strategy:** Hybrid — self-implement all feasible types (Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick, Waterfall, Box Plot), use Blazor-ApexCharts for complex algorithms (Treemap, Radar). Preserves zero-JS-dependency advantage for 7 of 9 new types while outsourcing squarified rectangle packing and trigonometric polygon rendering.

**Test progression:** 2718 → **2808** (+90 tests across 10 slices). Final state: 2808 passed, 0 failed, 1 pre-existing skip.

## Acceptance Criteria

### Foundation (Slices 1–2)

- **AC-127-01:** ✅ `Blazor-ApexCharts` v6.1.0 added to `BudgetExperiment.Client.csproj`; builds on .NET 10
- **AC-127-02:** ✅ `ChartThemeService` bridges CSS custom properties → `ApexChartOptions` theme config
- **AC-127-03:** ✅ ApexCharts charts respond to theme changes
- **AC-127-04:** ✅ ApexCharts JS bundle <200 KB gzipped (~80 KB)
- **AC-127-05:** ✅ All existing chart components continue to function without modification

### New Chart Types — Self-Implemented (Slices 3–5)

- **AC-127-06:** ✅ `HeatmapChart` — 7-row (Mon–Sun) × N-column grid, spending intensity coloring
- **AC-127-07:** ✅ `ScatterChart` — transactions as dots, outliers visually distinct, `AnimationsEnabled` param
- **AC-127-08:** ✅ `StackedAreaChart` — cumulative area series with per-series fill paths
- **AC-127-09:** ✅ `RadialBarChart` — up to 8 concentric arc rings with color transitions
- **AC-127-10:** ✅ `CandlestickChart` — OHLC bars with bullish/bearish/doji classification
- **AC-127-11:** ✅ `WaterfallChart` — floating bars with N-1 connector lines, running totals
- **AC-127-12:** ✅ `BoxPlotChart` — quartile box, whiskers, outlier circles (Tukey's hinges)

### New Chart Types — ApexCharts (Slice 6)

- **AC-127-13:** ✅ `BudgetTreemap` — hierarchical spending breakdown via ApexCharts treemap
- **AC-127-14:** ✅ `BudgetRadar` — multi-series radar (Budgeted vs. Actual) via ApexCharts radialBar

### Data Service Layer (Slices 2–6)

- **AC-127-15:** ✅ `IChartDataService` registered in DI; `ChartDataService` is the concrete implementation
- **AC-127-16:** ✅ `BuildSpendingHeatmap` — day-of-week×week matrix with correct Sunday→row-6 mapping
- **AC-127-17:** ✅ `BuildBudgetWaterfall` — segments summing from income to net remaining
- **AC-127-18:** ✅ `BuildBalanceCandlesticks` — OHLC from `DailyBalanceDto[]` with chronological sort
- **AC-127-19:** ✅ `BuildCategoryDistributions` — Tukey hinges quartiles, 1.5×IQR outliers
- **AC-127-20:** ✅ 20 unit tests covering all methods with edge cases (empty, single, all-same)

### Visual & Interactivity (Slice 7)

- **AC-127-21:** ✅ ApexCharts charts animate on first load (library default)
- **AC-127-22:** ✅ Tooltips on all charts display formatted currency/percentage/counts
- **AC-127-23:** ✅ `ExportChartButton` component; animations via `AnimationsEnabled` param
- **AC-127-24:** ✅ SVG chart legends interactive where applicable
- **AC-127-25:** ✅ ApexCharts toolbar export (PNG/SVG) — library built-in

### Migration (Slice 8)

- **AC-127-26:** ✅ Legacy SVG charts evaluated; `BarChart`/`DonutChart` retained (production-quality, no gap)
- **AC-127-27:** ✅ New charts wired to reports: `WaterfallChart`+`RadialBarChart`+`BudgetRadar` → BudgetComparisonReport; `BudgetTreemap` → MonthlyCategoriesReport; `StackedAreaChart` → MonthlyTrendsReport
- **AC-127-28:** ✅ `ComponentShowcase` displays all 9 new chart types with stub data
- **AC-127-29:** ✅ All charts work across all 9 themes via CSS custom property integration

### Accessibility & Theme Compliance

- **AC-127-30:** ✅ All SVG charts: `role="img"` on outer div, `aria-hidden="true"` on inner SVG
- **AC-127-31:** ✅ All ApexCharts charts: `role="img"` + `aria-label` on outer container
- **AC-127-32:** ✅ CSS custom property theming — all 9 themes automatically supported
- **AC-127-33:** ✅ `accessible` theme colorblind-safe palette applied to all chart types

### Cleanup (Slice 9 — Optional, scoped)

- **AC-127-34:** ✅ `AreaChart` (zero consumers) removed from `Components/Charts/`; all other legacy charts retained (production consumers)
- **AC-127-35:** ✅ All 13 chart models in `Models/` verified active; no orphaned supporting types

### Dashboard (Slice 10)

- ✅ `ReportsDashboard.razor` at `/reports/dashboard` — 5-component CSS grid aggregating `BudgetTreemap`, `WaterfallChart`, `BudgetRadar`, `HeatmapChart`, `RadialBarChart`
- ✅ `ReportsIndex.razor` updated to link to dashboard (replaces "Coming Soon" card)
- ✅ Loading state guard; 3-call independent fault-tolerant data loading
- ✅ 6 bUnit tests (all pass): container, loading state, treemap section, waterfall section, filter section, radial bar section

## Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| Hybrid approach: self-implement Tier 1/2, ApexCharts for Tier 3 | Zero-JS-dependency for 7/9 new types; Treemap/Radar algorithms too complex to maintain |
| `BarChart`/`DonutChart` kept as SVG | Production-quality, full test coverage, no functional gap justifies replacement |
| `DailyBalanceDto` client-local record | `DailyBalanceSummaryDto` in Contracts exposes `MoneyDto` snapshots; charts need flat `(DateOnly, decimal)` shape |
| `HeatmapChart`/`ScatterChart` showcase-only in reports | Reports don't fetch raw `TransactionDto[]`; extra API calls out of scope |
| `_Imports.razor` global using | Eliminates per-file `@using` boilerplate for all chart model types |
| SA1201 in code-behind pages | Private fields must precede `[Inject]` properties per StyleCop rule |
