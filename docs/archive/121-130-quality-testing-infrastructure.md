# Archive: Features 121–130

> Features completed and archived. Listed in completion order.

---

## Feature 123: Backend Code Quality Cleanup

> **Status:** Done

## Overview

A code quality review on `feature/code-quality-review` surfaced four categories of backend quality issues: an exception handler that uses fragile string matching instead of the existing `DomainException.ExceptionType` enum, three redundant dual-registration DI entries, six methods with excessive nesting depth, and a `DateTime.Now` usage in a Blazor component that violates the project's UTC-everywhere rule. None are blockers, but together they accumulate maintenance debt and introduce subtle risk. This work addresses all four categories.

## Problem Statement

### Current State

1. **`ExceptionHandlingMiddleware`** maps `DomainException` to HTTP status codes by calling `.Message.Contains("not found")`. The domain already has a `DomainException.ExceptionType` enum. String matching is fragile: it breaks on message rewordings, is case-sensitive by default, and obscures intent.

2. **Three redundant DI registrations** register each service twice — once as an interface and once as a concrete type — with a comment referencing "backward compatibility". No consumer in the solution references the concrete registration. Both entries resolve to the same implementation, so the duplicate is pure noise that misleads future readers about the DI contract.

3. **Six methods with 3+ nesting levels** are harder to read, test, and modify safely:
   - `TransactionListService` — recurring instance builder methods
   - `ImportExecuteRequestValidator`
   - `RuleSuggestionResponseParser`
   - `ImportRowProcessor.DetermineCategory`
   - `LocationParserService`

4. **`Reconciliation.razor`** uses `DateTime.Now` instead of `DateTime.UtcNow`. All `DateTime` values in this application must be UTC (engineering guidelines §30).

### Target State

- `ExceptionHandlingMiddleware` switches on `DomainException.ExceptionType` — zero string matching.
- Redundant DI registrations are removed; each service is registered once, by interface only.
- The six identified methods are refactored to ≤ 2 nesting levels using guard clauses, extracted private methods, or helper types.
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

- [x] `TransactionListService` recurring instance methods: max nesting ≤ 2.
- [x] `ImportExecuteRequestValidator`: max nesting ≤ 2.
- [x] `RuleSuggestionResponseParser`: max nesting ≤ 2.
- [x] `ImportRowProcessor.DetermineCategory`: max nesting ≤ 2.
- [x] `LocationParserService` (identified method(s)): max nesting ≤ 2.
- [x] Refactored methods preserve all existing unit test coverage and pass without modification.

### DateTime UTC

- [x] `Reconciliation.razor` uses `DateTime.UtcNow` in every location previously using `DateTime.Now`.
- [x] A grep of the Client project for `DateTime.Now` (excluding comments and string literals) returns zero results after this change.

---

## Technical Design

### Exception Handler Refactor

Replace the current pattern:

```csharp
// Before — fragile string matching
if (ex is DomainException domEx && domEx.Message.Contains("not found"))
    return StatusCodes.Status404NotFound;
```

With a switch on the enum:

```csharp
// After — typed dispatch
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

- **Guard clauses** — invert conditional and return/continue early to avoid one nesting level.
- **Extract method** — move an inner block to a well-named private method.
- **LINQ / pattern decomposition** — replace nested `if`/`foreach` with composable expressions where it improves clarity.

Do not reduce nesting in ways that obscure intent — prefer clarity over cleverness.

### DateTime Fix

```razor
@* Before *@
var today = DateTime.Now;

@* After *@
var today = DateTime.UtcNow;
```

If the date is displayed to the user, convert to local time via `CultureService.CurrentTimeZone` (per engineering guidelines §30 and §38).

---

## Implementation Plan

### Phase 1: Exception Handler

**Objective:** Replace string-matching exception mapping with type-safe enum dispatch.

**Tasks:**
- [x] Audit `DomainExceptionType` enum — add any missing cases needed for current status code mapping
- [x] Rewrite `ExceptionHandlingMiddleware` status code logic to switch on `ExceptionType`
- [x] Write/update unit tests for the middleware covering each `ExceptionType` case
- [x] Run tests — confirm green

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
- [x] Run full test suite — confirm no resolution failures

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

**Objective:** Refactor the six methods with 3+ nesting levels to ≤ 2 levels.

**Tasks:**
- [x] `TransactionListService` recurring methods — apply guard clauses / extract helper
- [x] `ImportExecuteRequestValidator` — extract validation branches into named private methods
- [x] `RuleSuggestionResponseParser` — flatten nested parsing logic
- [x] `ImportRowProcessor.DetermineCategory` — guard-clause early returns for each condition
- [x] `LocationParserService` — extract inner logic blocks
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
- [x] Run Client tests — confirm green

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
- `RecurringChargeSuggestionsController` — 4 endpoints with zero test coverage.
- `RecurringController` — 2 endpoints (`GetPastDueAsync`, `RealizeBatchAsync`) with zero test coverage.

**Untested repositories:**
- `AppSettingsRepository`
- `CustomReportLayoutRepository`
- `RecurringChargeSuggestionRepository`
- `UserSettingsRepository`

**Vanity tests (~20):**
- Enum tests that assert `(int)SomeEnum.Value == 42`. These pass even if the enum member is renamed, the member is deleted and a new one added with the same integer, or the enum's semantic meaning changes entirely. They protect only against deliberate integer reassignment — a scenario that never causes a real runtime regression.

### Target State

- All four repositories have integration tests exercising their primary read/write operations against a real database.
- All uncovered controller endpoints have API integration tests covering happy path, validation failure (400), and not-found (404) cases where applicable.
- Vanity enum integer-value tests are removed from the test suite. Any legitimate concern about enum serialisation is covered by a serialisation-contract test, not an integer assertion.

---

## Acceptance Criteria

### Controller Coverage

- [x] `RecurringChargeSuggestionsController` — all 4 uncovered endpoints have tests covering: happy path (2xx), not-found (404), and invalid request (400) where applicable.
- [x] `RecurringController.GetPastDueAsync` — tests cover: returns 200 with a list when past-due items exist; returns 200 with empty list when none exist.
- [x] `RecurringController.RealizeBatchAsync` — tests cover: valid batch request returns 200/204; invalid/empty batch returns 400.

### Repository Coverage

- [x] `AppSettingsRepository` — integration tests cover: get existing settings, get when not found, upsert/save.
- [x] `CustomReportLayoutRepository` — integration tests cover: get by id, list by user/scope, create, update, delete.
- [x] `RecurringChargeSuggestionRepository` — integration tests cover: get by id, list by account and status, create, update status.
- [x] `UserSettingsRepository` — integration tests cover: get by user id, create, update.
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
- [x] Write `AppSettingsRepositoryTests` — get, upsert
- [x] Write `CustomReportLayoutRepositoryTests` — CRUD by user/scope
- [x] Write `RecurringChargeSuggestionRepositoryTests` — list by account+status, status transitions
- [x] Write `UserSettingsRepositoryTests` — get by user, upsert
- [x] Run infrastructure tests — confirm green

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
- [x] Run API tests — confirm green

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
- [x] Run full test suite — confirm green and coverage not regressed

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

---

## Feature 124: Controller Abstractions Assessment and Style Consistency

> **Status:** Done

## Overview

Two related housekeeping items surfaced during the code quality review: three API controllers depend directly on concrete service classes rather than interfaces, and private field naming is inconsistent across Application services (`this._field` in some, `_field` in others). This document assesses the DIP concern pragmatically and defines a plan to enforce consistent field-access style via `.editorconfig`.

---

## DIP Assessment Results (2026-03-22)

### VERDICT A: TransactionsController, RecurringTransactionsController, RecurringTransfersController → Add Interface

**Rationale:** The interfaces (`ITransactionService`, `IRecurringTransactionService`, `IRecurringTransferService`) already exist and are registered in DI. The controllers simply inject the concrete types instead of the interfaces. This is a **zero-cost fix** — no new interfaces need extraction, just change the constructor parameter types.

**Assessment Against Pragmatic Criteria:**

| Question | TransactionsController | RecurringTransactionsController | RecurringTransfersController |
|----------|----------------------|-------------------------------|------------------------------|
| Interface already exists? | ✅ Yes (`ITransactionService`) | ✅ Yes (`IRecurringTransactionService`) | ✅ Yes (`IRecurringTransferService`) |
| Interface registered in DI? | ✅ Yes | ✅ Yes | ✅ Yes |
| Realistic test substitution? | ⚠️ Integration tests use Testcontainers, but controller unit tests would benefit from mocking | ⚠️ Same | ⚠️ Same |
| Complexity cost to add? | **Zero** — interface exists, just change type | **Zero** | **Zero** |
| Runtime swap scenario? | No | No | No |

**Decision:** Since the interfaces already exist and are registered, the controllers should use them. This:
1. Follows DIP without adding complexity (interfaces already exist)
2. Enables potential future controller unit tests without a database
3. Removes the need for duplicate DI registrations (concrete type registrations can be removed)
4. Aligns with the rest of the codebase where controllers inject interfaces

**Implementation Required:**
1. Update `TransactionsController` constructor: `TransactionService` → `ITransactionService`
2. Update `RecurringTransactionsController` constructor: `RecurringTransactionService` → `IRecurringTransactionService`
3. Update `RecurringTransfersController` constructor: `RecurringTransferService` → `IRecurringTransferService`
4. Remove redundant concrete type registrations from `DependencyInjection.cs`
5. Add missing methods to `IRecurringTransactionService`: `SkipNextAsync`, `UpdateFromDateAsync`
6. Add missing methods to `IRecurringTransferService`: `UpdateAsync`, `DeleteAsync`, `PauseAsync`, `ResumeAsync`, `SkipNextAsync`, `UpdateFromDateAsync`
7. Update test mocks in `ChatActionExecutorTests.cs` to implement new interface methods

**Note:** The assessment revealed that the interfaces are incomplete — the concrete classes have methods the interfaces don't define. The interfaces need to be expanded to match the concrete implementations' public API.

---

## Problem Statement

### Current State

**DIP: Concrete dependencies in controllers**

`TransactionsController`, `AccountsController`, and `RecurringTransactionsController` inject concrete service classes (e.g., `TransactionService`, `AccountService`) directly via constructor parameters rather than interfaces. This was noted in the March 2026 architecture review.

The project convention (per engineering guidelines §7, DIP) is that higher layers depend on abstractions. However, Fortinbra's stated directive for this review is: *"Apply SOLID principles judiciously. Add interfaces/abstractions when they earn their weight; skip when the added complexity doesn't justify the benefit. A single concrete service with no realistic substitution scenario doesn't need an interface just to satisfy DIP."*

Assessment is therefore required before work begins: do these services have realistic substitution scenarios that justify extraction?

**Style: `this._field` vs `_field`**

Some Application service classes access private fields with the explicit `this.` qualifier (`this._repository`, `this._logger`). Others use the bare form (`_repository`, `_logger`). Both compile and StyleCop does not flag one over the other by default, but the inconsistency makes the codebase feel unowned and slightly complicates future code generation / refactoring.

### Target State

- A documented decision exists for each of the three controllers: either introduce an interface (with justification) or record the explicit decision not to (with rationale), keeping the concrete dependency.
- Private field access is consistent across all Application services. The project convention (`_camelCase` per §5) is enforced by `.editorconfig`; the `this.` qualifier is not used for field access.

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

| Question | If Yes → | If No → |
|----------|----------|---------|
| Is the controller unit-tested or planned to be? | Interface aids mocking | No benefit |
| Does any other feature doc plan a second implementation? | Interface required | No benefit |
| Is the service a domain boundary that Infrastructure could implement differently? | Interface appropriate | Concrete is fine |

For application services that are simple orchestrators with no realistic alternative implementation and no current unit test isolation need, the concrete dependency is the pragmatic choice. The engineering guidelines already acknowledge this in §25: *"A single concrete service with no realistic substitution scenario doesn't need an interface just to satisfy DIP."*

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
- [ ] Review `TransactionsController` — does it have (or need) unit tests? Is a second `ITransactionService` implementation plausible?
- [ ] Review `AccountsController` — same assessment
- [ ] Review `RecurringTransactionsController` — same assessment
- [ ] For each controller where an interface is justified:
  - [ ] Extract `IXxxService` interface in the Application layer
  - [ ] Update DI registration: `services.AddScoped<IXxxService, XxxService>()`
  - [ ] Update controller constructor to inject the interface
  - [ ] Write or update controller unit/integration tests using a test double
- [ ] For each controller where the concrete dependency is retained:
  - [ ] Record the decision in `.squad/decisions.md` with rationale
- [ ] Run full test suite — confirm green

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

### Phase 2: Style Consistency — Remove `this.` Qualifiers

**Objective:** Enforce consistent `_field` (no `this.`) access across Application services via `.editorconfig` and `dotnet format`.

**Tasks:**
- [ ] Add `dotnet_style_qualification_for_field = false:warning` (and related rules) to `.editorconfig`
- [ ] Run `dotnet format --diagnostics IDE0003` to auto-fix existing violations
- [ ] Review the diff — confirm only `this.` removal, no logic changes
- [ ] Run `dotnet build` — confirm zero new warnings
- [ ] Run tests — confirm green

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
- If StyleCop's `SA1101` rule (`PrefixLocalCallsWithThis`) is currently enabled in `stylecop.json`, it must be disabled — it conflicts with the `IDE0003` rule enforcing the opposite style. Review `stylecop.json` before applying the `.editorconfig` change.
- Do not introduce interfaces speculatively. The goal is consistent, justifiable architecture — not maximum indirection.
