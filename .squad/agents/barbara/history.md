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

### Feature 116 Slice 2 — Strategy 3: Regex Alternation Tests Added

**Date:** Current session

**What was added:**
- 8 new failing tests appended to `tests/BudgetExperiment.Application.Tests/Categorization/RuleConsolidationAnalyzerTests.cs`
- Tests cover Strategy 3 (Regex Alternation): grouping multiple `Contains` rules for the same category into a single `Regex` alternation pattern

**Tests added:**
1. `AnalyzeAsync_RegexAlternation_TwoContainsRules_ReturnsSingleRegexSuggestion` — 2 Contains same category → 1 Regex suggestion with `|`
2. `AnalyzeAsync_RegexAlternation_ThreePlusContainsRules_MergesAll` — 3 Contains same category → all 3 IDs, 2 pipes
3. `AnalyzeAsync_RegexAlternation_SpecialCharactersEscaped` — `.` and `+` in patterns must appear as `\.` and `\+` in merged pattern
4. `AnalyzeAsync_RegexAlternation_CrossCategory_NotMerged` — different categories → no alternation
5. `AnalyzeAsync_RegexAlternation_NotAppliedToNonContainsTypes` — already-Regex rules → no Strategy 3 output
6. `AnalyzeAsync_RegexAlternation_SingleRule_NotSuggested` — 1 Contains rule → no suggestion
7. `AnalyzeAsync_RegexAlternation_PatternTooLong_SplitIntoMultipleSuggestions` — 15 rules, each ~50 chars → split, each suggestion ≤500 chars
8. `AnalyzeAsync_AlreadyExactDuplicate_NotAlsoAlternation` — exact duplicates handled by Strategy 1 only (Contains type, not Regex)

**Red/green status:**
- 4 tests FAIL (red): tests 1, 2, 3, 7 — require Strategy 3 implementation in `RuleConsolidationAnalyzer`
- 4 tests PASS (green): tests 4, 5, 6, 8 — current analyzer already satisfies these "no alternation" constraints
- All 13 Slice 1 tests remain green (no regressions)
- Total: 21 tests in `RuleConsolidationAnalyzerTests`, 17 passing, 4 failing

**Patterns observed:**
- StyleCop SA1124 (no regions) and SA1512 (no blank line after single-line comment) are enforced as errors — avoid `#region` blocks in test files
- Strategy 3 must: (a) skip rules already grouped by Strategy 1 or 2, (b) escape regex metacharacters, (c) split patterns exceeding 500 chars across multiple suggestions

### Feature 116 Slice 3 — ConsolidationPreviewResult & IRuleConsolidationPreviewService Tests

**Date:** Current session

**What was created:**
- `src/BudgetExperiment.Application/Categorization/ConsolidationPreviewResult.cs` — new Application-layer sealed record with: `TotalSamples`, `MatchedSamples`, `CoveragePercentage`, `MatchedDescriptions`, `UnmatchedDescriptions`
- `src/BudgetExperiment.Application/Categorization/IRuleConsolidationPreviewService.cs` — new interface with `Task<ConsolidationPreviewResult> PreviewConsolidationAsync(ConsolidationSuggestion, IReadOnlyList<string>)`
- `tests/BudgetExperiment.Application.Tests/Categorization/RuleConsolidationPreviewServiceTests.cs` — 9 failing tests (red phase)

**Tests added:**
1. `PreviewAsync_ContainsPattern_ReturnsCorrectCoverage` — WALMART contains, 2/3 match
2. `PreviewAsync_RegexPattern_ReturnsCorrectCoverage` — WALMART|KROGER alternation, 2/3 match
3. `PreviewAsync_ExactPattern_OnlyExactMatchCounted` — "WALMART" exact, only exact counts
4. `PreviewAsync_StartsWithPattern_CorrectMatches` — WALMART prefix, 2/3 match
5. `PreviewAsync_EndsWithPattern_CorrectMatches` — WALMART suffix, 2/3 match
6. `PreviewAsync_EmptySamples_ReturnsZeroCoverage` — empty list → 0/0/0%
7. `PreviewAsync_NoMatches_ReturnsZeroCoverage` — no matches → 0%, UnmatchedDescriptions.Count=2
8. `PreviewAsync_CaseInsensitive_MatchesRegardlessOfCase` — lowercase "walmart" vs "WALMART SUPERCENTER" → 1 match
9. `PreviewAsync_CoveragePercentage_CalculatedCorrectly` — 3/4 = 75.0%

**Build status:** FAILS with 9× CS0246 (`RuleConsolidationPreviewService` not found) — correct TDD red phase. StyleCop is clean. All other tests unaffected.

**Lucius must implement:** `RuleConsolidationPreviewService` in `src/BudgetExperiment.Application/Categorization/RuleConsolidationPreviewService.cs`, implementing `IRuleConsolidationPreviewService`. Matching logic: Contains/StartsWith/EndsWith via `StringComparison.OrdinalIgnoreCase`; Exact via `.Equals(OrdinalIgnoreCase)`; Regex via `new Regex(pattern, RegexOptions.IgnoreCase)`. CoveragePercentage = `MatchedSamples / TotalSamples * 100.0` (0 if TotalSamples == 0).

### Feature 116 Slice 4 — IRuleConsolidationService Interface & Failing Tests

**Date:** Current session

**What was created:**
- `src/BudgetExperiment.Application/Categorization/IRuleConsolidationService.cs` — new interface with single method `Task<IReadOnlyList<RuleSuggestion>> AnalyzeAndStoreAsync(CancellationToken)`
- `tests/BudgetExperiment.Application.Tests/Categorization/RuleConsolidationServiceTests.cs` — 4 failing tests (RED)
- `tests/BudgetExperiment.Api.Tests/Categorization/AnalyzeConsolidationEndpointTests.cs` — 2 failing tests (RED, endpoint missing)
- `tests/BudgetExperiment.Api.Tests/Categorization/AnalyzeConsolidationAuthTests.cs` — 1 failing test (RED, endpoint missing)
- Added `Moq 4.20.72` to `BudgetExperiment.Api.Tests.csproj` (was missing; needed for service mock injection)

**Application Tests (4 RED):**
1. `AnalyzeAndStoreAsync_WithDuplicateRules_CreatesSuggestionsAndPersists` — 2 rules same pattern → 1 suggestion, AddRangeAsync called, Type=RuleConsolidation
2. `AnalyzeAndStoreAsync_NoRules_ReturnsEmpty` — empty repo → empty result, AddRangeAsync never called
3. `AnalyzeAndStoreAsync_NoConsolidationOpportunities_ReturnsEmpty` — single rule → empty result
4. `AnalyzeAndStoreAsync_MultipleConsolidations_PersistsAll` — 4 rules (2 pairs) → 2 suggestions, AddRangeAsync once with count=2

**API Tests (3 RED):**
1. `PostAnalyzeConsolidation_WhenNoSuggestions_Returns200WithEmptyList` — mock returns empty → 200 `[]`
2. `PostAnalyzeConsolidation_WhenSuggestionsFound_Returns200WithItems` — mock returns 1 suggestion → 200 with 1 item
3. `PostAnalyzeConsolidation_Unauthenticated_Returns401` — no auth header → 401

**Build status:** FAILS with exactly 4× CS0246 (`RuleConsolidationService` not found) — correct TDD red phase. All other projects compile cleanly. API tests compile because they reference only the interface.

**Blocker:** `RuleConsolidationService` class does not exist. All Application tests fail at compile time until Lucius creates it.

### Feature 116 Slice 5 — Accept & Dismiss Workflow Tests

**Date:** Current session

**What was done:**

**Interface update (`IRuleConsolidationService`):**
- Added `Task<CategorizationRule> AcceptConsolidationAsync(Guid suggestionId, CancellationToken)` with full XML docs
- Added `Task DismissConsolidationAsync(Guid suggestionId, CancellationToken)` with full XML docs

**Application tests (5 appended to `RuleConsolidationServiceTests.cs`):**
1. `AcceptConsolidationAsync_ValidSuggestion_CreatesNewRuleAndDeactivatesSources` — 2 source rules; asserts `AddAsync` called once, both source rules `IsActive == false`, return not null
2. `AcceptConsolidationAsync_SuggestionNotFound_ThrowsDomainException` — null from repo → `DomainException` with `DomainExceptionType.NotFound`
3. `AcceptConsolidationAsync_WrongSuggestionType_ThrowsDomainException` — `PatternOptimization` type → `DomainException`
4. `DismissConsolidationAsync_ValidSuggestion_MarksAsDismissed` — asserts `suggestion.Status == Dismissed`, `SaveChangesAsync` called
5. `DismissConsolidationAsync_SuggestionNotFound_ThrowsDomainException` — null from repo → `DomainException` with `DomainExceptionType.NotFound`

**Handler tests (new file `SuggestionAcceptanceHandlerConsolidationTests.cs` in Categorization/):**
1. `HandleAsync_ConsolidationSuggestion_DelegatesToConsolidationService` — uses FUTURE 4-param constructor with `IRuleConsolidationService`; asserts `AcceptConsolidationAsync` called once, result not null
2. `HandleAsync_ConsolidationSuggestion_DoesNotThrowManualReviewException` — regression guard; asserts no exception thrown

**API tests (new file `ConsolidationAcceptDismissEndpointTests.cs` in Categorization/):**
1. `PostAccept_ValidId_Returns200` — mocks consolidation service + rule service; POST `.../consolidation/{id}/accept` → 200
2. `PostDismiss_ValidId_Returns204` — mocks consolidation service; POST `.../consolidation/{id}/dismiss` → 204
3. `PostAccept_NotFound_Returns404` — service throws `DomainException(NotFound)` → 404

**Build status:** FAILS with exactly 2× CS0535:
- `RuleConsolidationService` does not implement `AcceptConsolidationAsync`
- `RuleConsolidationService` does not implement `DismissConsolidationAsync`

**Cascading red:** After Lucius implements the two service methods, the Application project compiles. Then test projects will expose additional CS1501 (wrong constructor arg count) on the handler tests, signalling that `SuggestionAcceptanceHandler` also needs a 4th `IRuleConsolidationService` parameter. This is intentional.

**Key patterns:**
- No `#region` blocks (SA1124 prohibited)
- No blank lines after single-line comments (SA1512)
- `using Moq; using Shouldly;` explicit; domain types via global usings
- Mock setup with `It.IsAny<CancellationToken>()` consistently
- Shouldly for assertions in all new tests (not bare `Assert`)

### Feature 116 Slice 6 — Undo Consolidation Tests

**Date:** Current session

**Domain changes added (to unblock test contracts):**
- `RuleSuggestion.MergedRuleId { get; private set; }` — `Guid?` property; populated via `RecordMergedRuleId(Guid)`
- `RuleSuggestion.RecordMergedRuleId(Guid)` — records merged rule ID; throws `DomainException` if empty
- `RuleSuggestion.Reopen()` — resets status from `Accepted → Pending`, nulls `ReviewedAtUtc`; throws `DomainException(InvalidState)` if not Accepted
- `DomainExceptionType.InvalidState = 2` — new enum value for invalid state transitions (maps to HTTP 422)
- `ExceptionHandlingMiddleware` updated: `InvalidState → 422 Unprocessable Entity`

**Interface update (`IRuleConsolidationService`):**
- Added `Task UndoConsolidationAsync(Guid suggestionId, CancellationToken)` with full XML docs

**Application tests (5 appended to `RuleConsolidationServiceTests.cs`):**
1. `UndoConsolidationAsync_AcceptedSuggestion_ReactivatesSourceRules` — 2 source rules inactive before undo; asserts each `IsActive == true` after, merged rule `IsActive == false`
2. `UndoConsolidationAsync_AcceptedSuggestion_ResetsSuggestionState` — asserts `suggestion.Status == Pending`, `SaveChangesAsync` called
3. `UndoConsolidationAsync_SuggestionNotFound_ThrowsDomainException` — null from repo → `DomainException(NotFound)`
4. `UndoConsolidationAsync_SuggestionNotAccepted_ThrowsDomainException` — pending suggestion → `DomainException`
5. `UndoConsolidationAsync_SourceRulesReactivated_AndMergedRuleDeactivated` — combined end-to-end; source rules active, merged rule inactive, `SaveChangesAsync` called `Times.Once`

**API tests (new file `ConsolidationUndoEndpointTests.cs`):**
1. `PostUndo_ValidId_Returns204` — service completes → 204 No Content + service called once
2. `PostUndo_NotFound_Returns404` — service throws `DomainException(NotFound)` → 404
3. `PostUndo_NotAccepted_Returns422` — service throws `DomainException(InvalidState)` → 422

**Build status:** FAILS with exactly 1× CS0535:
- `RuleConsolidationService` does not implement `UndoConsolidationAsync` — correct TDD red phase

**Key test setup pattern for accepted suggestions:**
```csharp
var suggestion = RuleSuggestion.CreateConsolidationSuggestion(...);
suggestion.RecordMergedRuleId(mergedRuleId);  // must call before Accept()
suggestion.Accept();
// mock: _mockRuleRepo.Setup(r => r.GetByIdAsync(mergedRuleId, ...)).ReturnsAsync(mergedRule)
// mock: _mockRuleRepo.Setup(r => r.GetByIdsAsync(...)).ReturnsAsync([sourceRule1, sourceRule2])
```
