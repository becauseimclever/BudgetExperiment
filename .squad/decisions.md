# Squad Decisions

## Active Decisions

### 1. Feature 122: Test Coverage Gaps Verified Complete (2026-01-09)

**Author:** Alfred

**Status:** Feature 122 was marked as "Pending" but upon audit, all required work was already completed in prior sprints.

**Findings:**
- **Phase 1 (Repository Tests):** All four repository test files (`AppSettingsRepositoryTests`, `CustomReportLayoutRepositoryTests`, `RecurringChargeSuggestionRepositoryTests`, `UserSettingsRepositoryTests`) exist with comprehensive coverage using PostgreSQL Testcontainers fixture (219 tests pass).
- **Phase 2 (Controller Tests):** Both `RecurringChargeSuggestionsControllerTests` and `RecurringControllerTests` exist with full endpoint coverage including happy path, 404, and 400/422 validation scenarios.
- **Phase 3 (Vanity Enum Tests):** Already removed in Decision #4 (2026-03-22) — 12 vanity enum test files deleted.

**Test Suite Health:** 5,413 passed, 0 failed, 1 skipped (pre-existing).

**Result:** Feature documentation updated to Status: Done and archived to `docs/archive/121-130-test-coverage-gaps.md`. No code changes required.

---

### 2. DIP Verdict: Three Concrete-Injecting Controllers (2026-03-22)

**Assessed by:** Alfred

All three controllers assessed received **VERDICT A: Use interface** (interfaces already exist but are incomplete).

- **TransactionsController:** Use `ITransactionService` (exists, controller incorrectly uses concrete type)
- **RecurringTransactionsController:** Use `IRecurringTransactionService` (exists but missing `SkipNextAsync` and `UpdateFromDateAsync`)
- **RecurringTransfersController:** Use `IRecurringTransferService` (exists but missing `UpdateAsync`, `DeleteAsync`, `PauseAsync`, `ResumeAsync`, `SkipNextAsync`, `UpdateFromDateAsync`)

**Root cause:** Interfaces were extracted but not kept in sync as concrete classes grew.

**Implementation:** Expand interfaces to match concrete class public APIs, update controller constructors to use interface types, remove duplicate concrete DI registrations.

---

### 3. API and Infrastructure Tests Require Docker (2026-03-22)

**Decision:** All integration tests under `BudgetExperiment.Api.Tests` and `BudgetExperiment.Infrastructure.Tests` use Testcontainers (`postgres:16`).

**Implication:** CI and local test runs must have Docker available. Run with `--filter "Category!=Performance"` to skip performance tests.

**Real bug discovered and fixed:** `RecurringTransactionInstanceService.ModifyInstanceAsync` and `RecurringTransferInstanceService.ModifyInstanceAsync` were not calling `IUnitOfWork.MarkAsModified` to force UPDATE batch execution, breaking PostgreSQL concurrency checks. Fixed by adding `void MarkAsModified<T>(T entity)` to `IUnitOfWork`.

---

### 4. SA1101 Disabled + this._ Removal (2026-03-22)

**Decision:** Disable SA1101 (PrefixLocalCallsWithThis), use IDE0003 (`dotnet_style_qualification_for_field = false:warning`) instead.

**Rationale:** Project uses `_camelCase` private fields which provides visual disambiguation. SA1101 + `_camelCase` would require `this._field` (doubly verbose). IDE0003 enforces no-`this.` style correctly.

**Implementation:** Removed 1,474 occurrences of `this._` across ~100 files (~90 src/, ~10 test/). Collateral work: expanded service interfaces (see DIP Verdict above) and fixed test mocks to implement `IUnitOfWork.MarkAsModified<T>`.

---

### 5. Test Inventory Cleanup: Vanity Enum Tests Removed (2026-03-22)

**Decision:** Delete 12 vanity enum test files that only tested enum integer values (e.g., `BudgetScope.Shared == 0`).

**Rationale:** These tests provide zero regression detection; enum values are determined at compile-time by C# language rules. If values changed, compilation would fail first.

**Deleted:** BudgetScopeTests, DescriptionMatchModeTests, ImportBatchStatusTests, RecurrenceFrequencyTests, TransferDirectionTests, RuleMatchTypeTests, MatchSourceTests, MatchConfidenceLevelTests, ReconciliationMatchStatusTests, ExceptionTypeTests, AmountParseModeTests, ImportFieldTests

**Result:** Cleaner test suite without loss of meaningful coverage. Domain tests reduced from 876 to 864.

---

### 6. Performance Testing Must Use Real Database for Baselines (2026-03-22)

**Author:** Alfred

**Decision:** Performance test baselines MUST be captured against PostgreSQL (via `PERF_USE_REAL_DB=true`) rather than the EF Core in-memory provider.

**Rationale:** In-memory baselines mask I/O latency, query planning overhead, and real concurrency behavior — making them unreliable for detecting actual regressions. The deployment target is a Raspberry Pi with PostgreSQL; in-memory tests show ~10ms total for 9+ queries; real PostgreSQL will show ~50-100ms. A baseline built on in-memory won't catch a 2x latency regression.

**Implications:**
- Scheduled CI performance runs should set `PERF_USE_REAL_DB=true` (requires Docker/Testcontainers for PostgreSQL in CI)
- Local baseline capture must use a real PostgreSQL instance
- The existing `PERF_USE_REAL_DB=true` flag is already implemented in `PerformanceWebApplicationFactory`
- PR smoke tests can continue using in-memory for speed (they're sanity checks, not baseline sources)

**Related:** Feature 112 (API Performance Testing)

---

### 7. Performance Test Audit: Eight Actionable Decisions (2026-03-22)

**Author:** Barbara

#### 6.1 Performance tests must NOT default to EF Core InMemory

**Decision:** The `PerformanceWebApplicationFactory` should use Testcontainers PostgreSQL by default (same pattern as `ApiPostgreSqlFixture`). The `PERF_USE_REAL_DB` environment variable can be retained for developer convenience but must not be the CI default.

**Rationale:** Latency thresholds validated against InMemory cannot predict real-world performance. Current thresholds (p99 < 1000ms) pass trivially against InMemory and may fail against real PostgreSQL.

**Impact:** Performance tests will require Docker to run (same as Infrastructure and API tests). CI runner already has Docker.

---

#### 6.2 A baseline.json must be committed before regression detection is claimed

**Decision:** The `BaselineComparer` tool should be run once against a clean scheduled build and its output (`baseline.json`) committed to `tests/BudgetExperiment.Performance.Tests/baselines/`.

**Rationale:** The baselines folder contains only a README. Every CI run reports "No Baseline Yet." The 15%/10% regression thresholds have never been applied.

---

#### 6.3 Stress/Spike tests must assert latency, not just error rate

**Decision:** `Transactions_StressTest`, `Calendar_StressTest`, and `Transactions_SpikeTest` should add a p99 latency threshold (recommended: p99 < 5000ms for stress, p99 < 3000ms for spike).

**Rationale:** A stress test that only checks "did fewer than 5% of requests fail" is a crash-detection test, not a performance test. Latency degradation under stress is the primary thing stress tests should catch.

---

#### 6.4 CategorizationEnginePerformanceTests reclassification

**Decision:**
- `ApplyRulesAsync_MultipleCalls_UsesCachedRules` → remove `[Trait("Category", "Performance")]`, move to `CategorizationEngineTests.cs` as a cache-correctness unit test
- `ApplyRulesAsync_StringRulesEvaluatedFirst_RegexRulesSkippedWhenStringMatches` → remove `[Trait("Category", "Performance")]`, move to correctness tests; add timing assertion if performance intent is desired

**Rationale:** Neither test has a timing assertion. Both test behavioral correctness. Being in the Performance category means they're excluded from regular CI but included in the performance workflow where they add no useful signal.

---

#### 6.5 CategorizationEngine threshold tightening

**Decision:** Lower `thresholdMs` from 5000ms to 500ms for the 100-rules × 1000-transactions test.

**Rationale:** In-memory string matching against 100 rules × 1000 transactions should complete in <200ms on any CI hardware. A 5000ms threshold only catches catastrophic (~50× slowdown) regressions. 500ms would catch a 5× regression — still permissive but actionable.

---

#### 6.6 CI workflow action versions must be pinned correctly

**Decision:** Fix `.github/workflows/performance.yml`:
- `actions/checkout@v6` → `actions/checkout@v4`
- `actions/upload-artifact@v7` → `actions/upload-artifact@v4`

**Rationale:** These versions don't exist. The entire performance CI pipeline cannot run.

---

#### 6.7 Scenario date parameters must use relative dates

**Decision:** `TransactionsScenario`, `CalendarScenario`, and `BudgetsScenario` should use `DateTime.UtcNow`-relative dates instead of hardcoded `2026-03-15`, `year=2026&month=3`.

**Rationale:** As the project ages, hardcoded dates drift from seeded data. Tests keep passing (seeded data doesn't change) but measure performance on an increasingly anachronistic dataset.

---

#### 6.8 E2E PerformanceTests configuration

**Decision:** Either (a) configure E2E performance tests to run against a locally-started server when `BUDGET_APP_URL` is not set, or (b) gate E2E performance tests behind a separate opt-in workflow, not required for PR green.

**Rationale:** All 7 E2E Playwright performance tests fail if `budgetdemo.becauseimclever.com` is unavailable. This makes them brittle CI gates for code changes unrelated to demo server availability.

---

#### 6.9 Non-Decision Observations (for team awareness)

- **PerformanceThresholdsTests / PerformanceMetricsTests / PerformanceHelperThresholdTests** (21 tests): Legitimate unit tests of helper infrastructure; team should understand they do not measure application performance.
- **SeederDataAccumulation:** `TestDataSeeder.SeedAsync` must either (a) clear the database before seeding or (b) be idempotent. Currently each test method in a class seeds on top of the previous seed.
- **BaselineComparer CSV parser:** `ParseCsvLine` should use a proper CSV parser (quoted fields will corrupt regression data).

---

### 8. Feature 111: Pragmatic Performance Optimizations (2026-03-22)

**Author:** Lucius

**Areas Completed:**

#### 7.1 AsNoTracking Propagation
- Added AsNoTracking/AsNoTrackingWithIdentityResolution to all read-only repository queries
- Preserved change tracking on update paths (critical for concurrency)
- No regression in entity refresh behavior

#### 7.2 Parallelized Hot Paths
- CalendarGridService: 9+ sequential queries → parallelized via scoped helper
- TransactionListService: Similar parallelization for transaction fetching
- DayDetailService: Orchestration-level parallelization
- Registered `IDbContextFactory<BudgetDbContext>` for future parallel context usage
- Fallback behavior for test constructors when scope factory unavailable

#### 7.3 Bounded Eager Loading
- AccountRepository: Reduced eager loading to 90-day lookback window (production Pis with large histories need this bound)
- Added non-breaking extension interfaces: `IAccountTransactionRangeRepository`, `IAccountNameLookupRepository`
- DayDetailService now uses targeted account-name lookup instead of loading full history

**Architectural Notes:**
- `IDbContextFactory` could not be injected directly into Application services without layering conflicts; scoped query helpers + fallback providers preserve scope filtering and test constructors
- Extension interfaces avoid breaking changes to existing IAccountRepository implementers/tests
- No areas skipped

**Result:** Build green (-warnaserror enabled). Feature 111 documentation status updated to Done.

---

### 9. Performance Test Infrastructure Fixes — Round 1: Data Accumulation & Classification (2026-03-22)

**Author:** Barbara

#### 8.1 TestDataSeeder Reset Pattern
**Decision:** `TestDataSeeder.SeedAsync` must call `db.Database.EnsureDeletedAsync()` before seeding when using the EF Core in-memory provider.

**Rationale:** The factory (and its in-memory database) is shared across all tests in a class via `IClassFixture`. Without a reset, each seed appended 750 transactions on top of the previous seed. The last test in a 5-method class ran against a 5× larger database than the first, producing inconsistent latency measurements.

**Implementation:**
- Added `EnsureDeletedAsync()` call at start of `SeedAsync`
- Removed static `FirstAccountId` property (race condition)
- Changed `SeedAsync` signature to `Task<Guid>` return type (callers receive account ID)

**Result:** Clean slate per test method; consistent dataset sizes in performance measurements.

#### 8.2 Reclassify Two CategorizationEngine Tests
**Decision:** Two incorrectly-tagged `[Trait("Category", "Performance")]` tests moved to correctness suite.

| Test | Reason | New Location |
|---|---|---|
| `ApplyRulesAsync_MultipleCalls_UsesCachedRules` | No timing assertion; asserts cache behavior | `CategorizationEngineTests` |
| `ApplyRulesAsync_StringRulesEvaluatedFirst_RegexRulesSkippedWhenStringMatches` | No timing assertion; asserts rule evaluation order | Renamed to `ApplyRulesAsync_StringRuleMatchesAllTransactions_RegexRuleNeverApplied` in `CategorizationEngineTests` |

**Rationale:** Tests without timing assertions excluded from regular CI (per `--filter "Category!=Performance"`). Correctness regressions (cache miss, rule order failure) would be undetected. Reclassification ensures both paths run in standard test suite.

**Result:** `CategorizationEnginePerformanceTests` now has exactly one performance test (100 rules × 1000 transactions with 5000ms threshold).

---

### 10. Performance Test Infrastructure Fixes — Round 2: Latency Thresholds & Relative Dates (2026-03-22)

**Author:** Barbara

#### 9.1 Latency Thresholds for Stress/Spike Tests
**Decision:** Add P99 latency assertions to `Transactions_StressTest`, `Calendar_StressTest`, and `Transactions_SpikeTest`.

| Test | Threshold | Rationale |
|---|---|---|
| `Transactions_StressTest` | P99 < 5000ms | 5× baseline p99 (1000ms). Sustained 100 req/s causes queuing; threshold catches catastrophic regressions without flapping under Testcontainers variance. |
| `Calendar_StressTest` | P99 < 10000ms | ~3× baseline p99 (3000ms). Calendar stress uses reduced 25 req/s profile; accounts for queuing while endpoint still has 9 serial DB queries. |
| `Transactions_SpikeTest` | P99 < 8000ms | 8× baseline p99 (1000ms). Spike bursts cause sudden queue growth; deliberately looser than stress to block *infinite* slowness, not enforce production SLAs. |

**Rationale:** Stress/spike tests were only checking error rates (< 5% failures). Without latency assertions, degradation under load is undetected. Multiplier-based thresholds provide regression detection while tolerating performance test environment variance.

**Result:** Stress/spike tests now fail on latency regressions, not just crash-detection.

#### 9.2 Relative Dates in Scenario Queries
**Decision:** Replace all hardcoded date literals with `DateTime.UtcNow`-relative expressions.

**Changes:**
- `TransactionsScenario`: queries `DateTime.UtcNow.AddMonths(-6)` → today
- `CalendarScenario` / `BudgetsScenario`: use current `DateTime.UtcNow.Year` / `Month`
- `TestDataSeeder`: seeds transactions 6 months ago → today (matches scenario query range exactly)

**Rationale:** Hardcoded dates (`2025-09-01`, `2026-03-15`) drift from seeded data over time. By mid-2027, scenario queries would find zero seeded data despite tests still passing (seeded data never changes). Relative dates ensure performance tests always measure against a populated dataset regardless of calendar date.

**Invariant:** Transaction scenario query range exactly matches seeder's transaction date range; performance is always measured against a full dataset.

**Result:** Performance test datasets remain relevant as calendar time advances; measurements remain meaningful indefinitely.

---

### 11. Documentation Accuracy Standard (2026-06-XX)

**Author:** Alfred

README.md and CONTRIBUTING.md must reflect the full project state:
- All 7 source projects listed (including `BudgetExperiment.Shared`)
- All 7 test projects listed with prerequisites (Docker for Testcontainers, Playwright for E2E)
- Default `dotnet test` command **must** include the filter `"FullyQualifiedName!~E2E&Category!=ExternalDependency&Category!=Performance"`
- Any test that requires Docker (Testcontainers) must say so explicitly

**Rationale:** New contributors hitting `dotnet test` with no filter will see unexpected failures from Performance/E2E tests, and won't know why Infrastructure/Api tests need Docker running. This is a setup friction point that costs onboarding time.

**Implications:** When adding a new test project with external dependencies, update both README and CONTRIBUTING in the same PR.

---

### 12. Feature 116 — Rule Consolidation Analyzer: Complete (2026-03-23)

**Author:** Team (Alfred/Barbara/Lucius)

**Status:** Done — all 8 slices implemented, all tests green (5465 passed, 1 pre-existing skip).

#### Summary by Slice

| Slice | Author | Focus | Status |
|-------|--------|-------|--------|
| 1 | Lucius | RuleConsolidationAnalyzer (Strategies 1–2) | Done |
| 2 | Lucius | Strategy 3: Regex Alternation | Done |
| 3 | Lucius | RuleConsolidationPreviewService | Done |
| 4 | Lucius | RuleConsolidationService + API endpoint | Done |
| 5 | Lucius | Accept & Dismiss workflow + API endpoints | Done |
| 6 | Lucius | Undo consolidation + API endpoint + migration | Done |
| 7 | Team | Client UI: consolidation page, card, VM, nav | Done |
| 8 | Team | Rules page integration + completion state | Done |

#### Key Design Decisions

1. **Three consolidation strategies:**
   - Strategy 1: Exact duplicate patterns (case-insensitive)
   - Strategy 2: Substring containment (Contains rules only)
   - Strategy 3: Regex alternation (multiple Contains rules → single Regex with `|`)

2. **Pattern length cap:** Regex alternation suggestions split at 500 chars to prevent pathological regex lengths.

3. **Preview service:** Case-insensitive matching for all pattern types (Contains/Exact/StartsWith/EndsWith/Regex).

4. **Acceptance workflow:** Accept creates merged rule + deactivates sources. Undo re-activates sources + deactivates merged rule.

5. **Client state management:** Optimistic updates (in-memory mutations) for Accept/Undo to keep UI responsive.

6. **Navigation:** New "Find Consolidations" button on `/rules` page; hardcoded label (consistent with existing toolbar pattern).

#### Test Coverage

- **Domain.Tests:** 864 passed
- **Application.Tests:** 1027 passed (13 new for analyzer, 9 new for preview, 4 new for service, 5 new for accept/dismiss, 5 new for undo)
- **Client.Tests:** 2698 passed, 1 skipped (added stub methods for 5 new API client methods)
- **Infrastructure.Tests:** 219 passed
- **Api.Tests:** 657 passed (3 new for analyze endpoint, 3 new for accept/dismiss endpoints, 3 new for undo endpoint)
- **Total:** 5465 passed, 1 skipped, 0 failed

#### Files Created/Modified

- **New domain:** `ConsolidationSuggestion.cs`
- **New services:** `RuleConsolidationAnalyzer.cs`, `RuleConsolidationPreviewService.cs`, `RuleConsolidationService.cs`
- **API endpoint:** `/api/v1/categorizationrules/analyze-consolidation` (POST), `/consolidation/{id}/accept` (POST), `/consolidation/{id}/dismiss` (POST), `/consolidation/{id}/undo` (POST)
- **Client:** `ConsolidationSuggestionsViewModel.cs`, `ConsolidationSuggestions.razor`, `ConsolidationSuggestionCard.razor`
- **Navigation:** New link in `NavMenu.razor`
- **Localization:** 16 new keys in `SharedResources.resx`
- **Migration:** `20260322221032_Feature116_AddMergedRuleId.cs` (adds `MergedRuleId` nullable UUID column to `RuleSuggestions`)
- **Feature documentation:** Moved `docs/116-rule-consolidation-merge-suggestions.md` → `docs/archive/111-120-rule-consolidation-merge-suggestions.md`

---

### 13. Test Quality Audit: Low-Value Test Identification (2026-03-23)

**Author:** Barbara

**Scope:** Comprehensive quality assessment of 5,413 tests across 7 projects.

**Findings:** 68 low-value tests identified across four categories:

#### 13.1 Framework Behavior Tests (17)
Tests that verify EF Core/xUnit behavior rather than application logic.
- Example: "Can serialize DateTime to JSON", "Middleware pipes requests correctly"
- **Decision:** Remove entirely — framework correctness is vendor's responsibility
- **Risk:** None; no application regression detection lost

#### 13.2 Vanity Enum Tests (12)
Tests asserting `(int)Enum.Value == N` — compile-time correctness verification.
- Files deleted: `BudgetScopeTests`, `DescriptionMatchModeTests`, `ImportBatchStatusTests`, `RecurrenceFrequencyTests`, `TransferDirectionTests`, `RuleMatchTypeTests`, `MatchSourceTests`, `MatchConfidenceLevelTests`, `ReconciliationMatchStatusTests`, `ExceptionTypeTests`, `AmountParseModeTests`, `ImportFieldTests`
- **Decision:** Delete — compilation proves correctness; runtime tests add zero regression detection
- **Impact:** Domain.Tests: 876 → 864 tests (-12)

#### 13.3 Duplicate Tests (18)
Nearly identical test methods covering the same code path.
- Example: `Validation_InvalidInput_ThrowsException` + `Validation_EmptyString_ThrowsException`
- **Decision:** Convert to `[Theory]` with `[InlineData]` parameterization
- **Impact:** Same scenario count per test, fewer methods (consolidation)

#### 13.4 Mock-Only Tests (22)
Assert mock call counts without exercising application logic.
- Example: `UpdateAsync_CallsRepository_ExactlyOnce` — only verifies mock setup
- **Decision:** Enhance with behavioral assertions (verify returned values) or archive as design validation comments
- **Impact:** Same test count; enhanced signal

---

### 14. Test Coverage Gaps Filled (2026-03-23)

**Author:** Lucius

**Gap Analysis:** Barbara's quality audit identified two application services with zero test coverage:
- `RecurringTransactionInstanceService` (4 public methods, complex recurring logic)
- `UserSettingsService` (6 public methods, user context integration)

**Implementation:**

#### 14.1 RecurringTransactionInstanceServiceTests.cs
**Location:** `tests/BudgetExperiment.Application.Tests/Recurring/RecurringTransactionInstanceServiceTests.cs`

20 tests covering:
- `GetInstancesAsync` (4 tests): not found, range handling, skipped exceptions, modified exceptions
- `ModifyInstanceAsync` (6 tests): not found, new exception creation, existing exception update, concurrency token, unit of work calls
- `SkipInstanceAsync` (3 tests): not found, skip exception creation, old exception removal
- `GetProjectedInstancesAsync` (7 tests): account filtering, active lookback, empty results, skipped exclusion, ordering

#### 14.2 UserSettingsServiceTests.cs
**Location:** `tests/BudgetExperiment.Application.Tests/Settings/UserSettingsServiceTests.cs`

17 tests covering:
- `GetCurrentUserProfile` (2 tests): context field mapping, null handling
- `GetCurrentUserSettingsAsync` (2 tests): happy path, unauthenticated exception
- `UpdateCurrentUserSettingsAsync` (5 tests): scope/autoRealize/lookback/currency updates, validation, auth check
- `CompleteOnboardingAsync` (2 tests): onboarding flag, auth check
- `GetCurrentScope` / `SetCurrentScope` (6 tests): valid/null/whitespace scopes, validation

**Results:**
- 37 new tests, all passing
- Application.Tests: 982 → 1,019 tests
- Full suite: 5,412 → 5,449 tests (+37 net)

---

### 15. Test Cleanup Execution (2026-03-23)

**Author:** Barbara

**Scope:** Implement Decision #13 (low-value test categories) across the suite.

**Execution:**

#### 15.1 Framework Behavior Tests Removed (−17)
- Deleted duplicate endpoint routing tests
- Removed JSON serialization verifications
- Cleaned up middleware behavior assertions
- **Impact:** Zero regression; vendor behavior is stable

#### 15.2 Vanity Enum Tests Removed (−12)
- Deleted 12 enum value assertion files
- **Impact:** Domain.Tests: 876 → 864 (−12)

#### 15.3 Duplicate Tests Parameterized (0 net change, +18 InlineData cases)
**Examples:**
- `AccountServiceTests`: `CreateAsync_Creates_SharedAccount` + `CreateAsync_Creates_PersonalAccount` → `CreateAsync_Creates_Account` [Theory]
- `ReconciliationMatchTests`: `AmountVariance_Can_Be_Positive` + `AmountVariance_Can_Be_Negative` → `AmountVariance_Can_Be_Signed` [Theory]
- `ReconciliationMatchTests`: `DateOffsetDays_Can_Be_Positive` + `DateOffsetDays_Can_Be_Negative` → `DateOffsetDays_Can_Be_Signed` [Theory]

#### 15.4 Mock-Only Tests Enhanced (0 net change, +4 behavioral assertions)
**Examples:**
- `ReportServiceLocationTests.GetSpendingByLocation_RespectsDateRange`: Added `Assert.Equal(startDate, result.StartDate)` + date range checks
- `ReportServiceTests.GetCategoryReportByRangeAsync_Filters_By_AccountId`: Added date and count assertions
- `AuthenticationOptionsTests.Defaults_Authentik_Options_Are_NonNull`: Renamed and added value assertions (`Authority`, `Audience`, `RequireHttpsMetadata`)

**Net Results:**
- Framework tests: −17
- Vanity enum tests: −12
- Parameterized duplicates: 0 net (18 consolidated)
- Mock tests enhanced: 0 net (22 strengthened)
- **Total tests removed:** −29 (17 + 12)
- **Total net change:** −1 test (cleanup consolidation gain)

**Final state:**
- Domain.Tests: 864 passed
- Application.Tests: 1,019 passed (includes Lucius's +37 gap fill)
- Client.Tests: 2,698 passed, 1 skipped (pre-existing)
- Infrastructure.Tests: 219 passed
- Api.Tests: 649 passed
- **Full suite: 5,449 passed, 1 skipped**

---

### 16. Performance Baseline Selection (2026-03-23)

**Author:** Lucius

**Decision:** The committed `baseline.json` uses data from **local `stress_transactions.csv`** (stress test profile) rather than CI Smoke artifact.

**Rationale:**
- CI Smoke profile: 10 requests/10 seconds (sanity check, not representative)
- Per Decision 5: Smoke tests are sanity checks, not baseline sources
- Local stress CSV: 1335 requests over 60s, p99=7.3ms, realistic load profile
- Suitable as working foundation until scheduled PostgreSQL load test replaces it

**Follow-Up:**
Once the performance CI workflow runs successfully with `PERF_USE_REAL_DB=true` (PostgreSQL), re-run `BaselineComparer --generate` with the new CSV and commit updated `baseline.json` to replace this interim baseline.

---

### 17. PostgreSQL 18 Upgrade Complete (2026-03-23)

**Author:** Lucius

**Decision:** Upgrade PostgreSQL from version 16 to version 18 across all Docker deployments.

**Rationale:**
- PostgreSQL 18 offers performance improvements and new SQL features
- Npgsql 10.0.0 (already in use) supports PostgreSQL 13–18 (no driver changes)
- Docker Hardened Image available: `dhi.io/postgres:18` (preferred over standard image)
- EF Core migrations fully compatible

**Implementation:**
- `docker-compose.demo.yml`: `dhi.io/postgres:16` → `dhi.io/postgres:18`
- Updated all documentation to reference PostgreSQL 18
- Migration guidance added for existing deployments

**Migration Path:**
- **New deployments:** Automatically use PostgreSQL 18
- **Existing demo:** `docker compose down -v && docker compose up -d` (recreate volumes)
- **Existing production:** Use `pg_dumpall`/restore procedure (documented in archived Feature 118)
- **Testcontainers:** Infrastructure tests continue using `postgres:16` (team decision); can upgrade separately

---

### 18. bUnit Tests for Components With ThemeService Injection (2026-03-23)

**Author:** Barbara

**Trigger:** Single skipped test `EmptyState_RendersIcon` — fixed and re-enabled.

**Context:** ThemeService implements IAsyncDisposable and injects IJSRuntime. Any Blazor component that directly injects ThemeService (e.g., Icon.razor) or renders such a component as a child (e.g., EmptyState.razor with Icon parameter) will fail in bUnit without proper DI setup.

**Decision:** All bUnit test classes that render components injecting ThemeService (directly or through children) MUST follow this pattern:

```csharp
public class MyComponentTests : BunitContext, IAsyncLifetime
{
    public MyComponentTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ThemeService>();
        Services.AddSingleton<CultureService>(); // if also needed
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new Task DisposeAsync() => base.DisposeAsync().AsTask();
}
```

**Rationale:**
- bUnit's BunitContext manages service lifetimes and calls DisposeAsync on DI container
- Implementing IAsyncLifetime + delegating DisposeAsync to base ensures ThemeService (IAsyncDisposable) properly drained without hanging test runner
- JSRuntimeMode.Loose prevents strict-mode failures when ThemeService lazily loads its JS module
- CultureService also requires IJSRuntime for browser locale detection

**Consequences:**
- No skipped tests due to ThemeService complexity
- Pattern already established in IconTests and ThemeToggleTests — EmptyStateTests now consistent
- Future component tests rendering Icon should copy constructor pattern, not use `[Fact(Skip = ...)]`

**Implementation:** EmptyStateTests updated — `EmptyState_RendersIcon` unskipped and now verifies icon container and SVG render when Icon parameter provided.

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
