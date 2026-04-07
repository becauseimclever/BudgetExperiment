# Squad Decisions

## Active Decisions

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

### 8. Decision: Enhanced Charts — Library Selection & Migration Strategy (2026-06-XX)

**Author:** Alfred (Lead/Architect)  
**Feature:** 127 — Enhanced Charts & Visualizations  
**Status:** Proposed

#### Context

The current charting system uses 11 hand-rolled SVG components (BarChart, DonutChart, LineChart, AreaChart, GroupedBarChart, StackedBarChart, SparkLine, RadialGauge, ProgressBar, ChartLegend, ChoroplethMap) with zero external dependencies. All are pure Blazor+SVG with CSS custom properties supporting 9 themes and 100% bUnit test coverage.

While functional, the system has hit its scaling limit: each new chart type requires 200–400 lines of custom SVG geometry code, interactivity is limited to basic hover tooltips, there are no animations, and chart types critical for budgeting insights (treemap, heatmap, waterfall, scatter, radar, candlestick, box plot) are missing entirely.

#### Decision

**Adopt Blazor-ApexCharts** as the chart rendering library via a parallel introduction + gradual migration strategy.

##### Why Blazor-ApexCharts

1. 20+ chart types covering all proposed new visualizations
2. ~80 KB gzipped JS payload — within budget for Raspberry Pi deployment
3. Actively maintained Blazor WASM wrapper with strongly-typed C# API
4. Built-in animations, zoom/pan, tooltips, legend interaction, and PNG export
5. Programmatic theming compatible with our CSS custom property design system
6. MIT licensed

##### What We Rejected

- **Chart.js (ChartJs.Blazor):** Blazor wrapper is stale (last updated 2022), .NET 10 support uncertain
- **Plotly.NET:** ~3 MB payload, designed for data science, overkill
- **D3.js via JS interop:** Effectively building a chart library from scratch
- **Radzen Blazor Charts:** Tied to Radzen component library
- **Keeping hand-rolled SVG:** Does not scale for 10+ new chart types

##### Migration Approach

- **No big bang.** ApexCharts and legacy SVG components coexist during migration.
- **New chart types** (treemap, heatmap, etc.) built exclusively with ApexCharts.
- **Existing charts** (BarChart, DonutChart, LineChart) migrated one-at-a-time in dedicated PRs.
- **Legacy components deleted only after** all consumers are migrated and tests pass.
- **ChartThemeService** bridges CSS custom properties → ApexCharts theme config.
- **IChartDataService** centralizes data aggregation/statistics for testability.

##### Risk Mitigations

- **Slice 1 is a spike:** Validates .NET 10 compatibility and measures actual bundle size before committing to full migration.
- **Go/no-go decision point** after Slice 1: if bundle >200 KB or .NET 10 incompatible, re-evaluate.
- **Legacy components retained** throughout migration — instant rollback by reverting component references.

#### Implications

- `BudgetExperiment.Client.csproj` gains a dependency on `Blazor-ApexCharts` NuGet package
- JS interop is introduced for charting (currently the Client has zero chart-related JS interop)
- ~80 KB added to published payload
- bUnit tests will need to account for ApexCharts JS interop (mock `IJSRuntime` calls)
- All 9 themes must be verified visually after each chart migration

#### Related

- Feature Doc: `docs/127-enhanced-charts-and-visualizations.md`
- Existing charts: `src/BudgetExperiment.Client/Components/Charts/`
- Theme system: `wwwroot/css/design-system/tokens.css` + 9 theme files

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

---

### Kakeibo + Kaizen Philosophy Embedded in Public Documentation (2026-01-09)

**Author:** Alfred

**Status:** Active

All new features and enhancements must support the **Kakeibo + Kaizen calendar-first philosophy**. This is not a feature — it is the application's identity.

**Kakeibo** (家計簿, "household ledger") — mindful, intentional recording. The app asks the four Kakeibo questions at the right moments: *How much did I receive? How much do I want to save? How much did I spend? How can I improve?*

**Kaizen** (改善, "continuous improvement") — small, consistent changes compound over time. Weekly micro-goals, not grand resolutions. Compare yourself to yourself. Progress is quiet and honest.

**The calendar is the centerpiece** — every financial decision happens on the calendar. Every day is a journal entry. Every week offers Kakeibo breakdowns. Every month closes with reflection.

#### What Changed

**README.md:**
- Opening tagline reframed: leads with Kakeibo + Kaizen philosophy, not feature list
- Purpose section reframed: WHY before HOW, calendar described as centerpiece
- Key Domain Concepts: added `KakeiboCategory`, `MonthlyReflection`, `KaizenGoal` entities; updated `BudgetCategory` and `Transaction` to mention Kakeibo routing
- Development Guidelines: added "Philosophy First" bullet pointing to `docs/128-kakeibo-kaizen-calendar-first.md`

**CONTRIBUTING.md:**
- Added entire "Design Philosophy — Kakeibo + Kaizen" section explaining how philosophy affects contributions: calendar as centerpiece, reflection over data display, no gamification, small/consistent over large/occasional, categorization carries intention

#### Why This Matters

Every contributor — internal or external — must understand that this application is **not** a general-purpose transaction tracker that happens to have a calendar view. It is a **mindful budgeting application** where the calendar is the ledger and Kakeibo + Kaizen philosophy guides every design decision.

Without this framing in README and CONTRIBUTING, contributors default to "add more features" mode. With this framing, they ask: "Does this feature deepen the calendar experience? Does it invite reflection? Does it fit the daily/weekly/monthly rhythm?"

This is architectural guidance at the product level.

#### Constraints

- No gamification (streaks, badges, confetti)
- Calendar remains the primary interaction surface
- Features outside the calendar rhythm require justification
- New expense categories must specify Kakeibo routing

#### References

- Full spec: `docs/128-kakeibo-kaizen-calendar-first.md`
- README.md lines 11-26 (Purpose section), lines 177-186 (Development Guidelines), lines 190-206 (Key Domain Concepts)
- CONTRIBUTING.md new section after intro (Design Philosophy)


---

# Feature Specs 131–136: Kakeibo Foundation Decisions

**Created:** 2026-04-10  
**Author:** Alfred  
**Status:** Complete — all six specs are finalized and ready for implementation phase  
**Related:** Features 128, 129, 129b, 130

---

## Summary

Six coordinated feature specification documents (131–136) define the foundational implementation work for the Kakeibo + Kaizen philosophy established in Feature 128. These specs complete the architectural vision and provide implementation teams with sufficient detail to begin TDD-driven development immediately.

All documents follow Feature 128's format and have been reviewed for internal consistency, dependency ordering, and alignment with SOLID/Clean Code principles.

---

## Key Architectural Decisions

### 1. KakeiboCategory Placement and Routing

**Decision:** `KakeiboCategory` is an enum in the Domain layer. It is placed on `BudgetCategory` (not `Transaction`) as the primary routing point. Transactions can optionally override via `Transaction.KakeiboOverride` (nullable).

**Rationale:**
- **Single source of truth:** Category routing is the primary classification; one change point.
- **Retroactive changes:** Changing a category's Kakeibo routing automatically re-aggregates all past transactions via that category (no per-transaction mutations).
- **Per-transaction flexibility:** Individual transactions can override the category's routing for one-off exceptions (e.g., an unexpected medical bill in Healthcare stays in Unexpected for that specific transaction).
- **Computed property pattern:** Effective Kakeibo = `KakeiboOverride ?? BudgetCategory.KakeiboCategory ?? Wants` (fallback for safety).

**Implementation precedent:** This mirrors the existing `BudgetCategory` / `Transaction.Amount` relationship — category defines policy, transaction carries specific instance data.

---

### 2. Migration Seeding Strategy (No HasData)

**Decision:** Smart-defaulted migration (Groceries→Essentials, Dining→Wants, Education→Culture, unknown→Wants) is applied via a startup seeder using `ON CONFLICT (Name) DO NOTHING`, NOT via EF Core's `HasData()`.

**Rationale:**
- **Destructive HasData behavior:** `HasData()` generates migration SQL that overwrites existing rows on every `dotnet ef database update` and Docker startup. If users have already customized their category routing, the customization is lost when the app updates — unacceptable.
- **Non-destructive seeder:** INSERT-if-missing logic respects user changes. New defaults are seeded on first run; user customizations survive all future updates.
- **Precedent in codebase:** Feature 129b already recommends this pattern for feature flag defaults.

**Implementation:** New service `KakeiboDefaultSeeder` in Application layer, registered in startup chain before migrations (so seed is applied immediately after migration runs). Idempotent per-flag.

---

### 3. Feature Flag Defaults and Scope

**Decisions:**

| Feature | Flag | Default | Scope | Rationale |
|---------|------|---------|-------|-----------|
| 131 | None | N/A | Always on | Foundation; no opt-out |
| 132 | `Features:Kakeibo:TransactionOverride` | `true` | User choice | Override capability enabled by default; users can disable for pure category-driven routing |
| 133 | None | N/A | Always on | Onboarding is always enabled |
| 134 | `Features:Calendar:SpendingHeatmap` | `true` | User/UX preference | Spending heatmap visible by default; users can toggle in calendar settings |
| 134 | `Features:Kakeibo:CalendarOverlay` | `false` | Feature rollout | Kakeibo badges/breakdown bars default OFF during development; toggled ON when shipped |
| 134 | `Features:Kakeibo:MonthlyReflectionPrompts` | `true` | Feature rollout | Reflection prompts visible by default; can be disabled to hide all monthly reflection UI |
| 135 | `Features:Kakeibo:MonthlyReflectionPrompts` | `true` | Shared with 134 | One flag controls both calendar prompts and reflection panel visibility |
| 136 | `Features:Kaizen:MicroGoals` | `true` | User choice | Micro-goals visible by default; users can disable if they prefer non-ritual budgeting |

**Naming convention:** Hierarchical PascalCase: `{Area}:{SubArea?}:{Feature}` (e.g., `Kakeibo:MonthlyReflectionPrompts`, not `monthly_reflection_prompts` or `KakeiboMonthlyReflectionPrompts`).

---

### 4. Entity Design — MonthlyReflection vs. UserSettings

**Decision:** Monthly reflection data (savings goal, intention, gratitude, improvement) is stored in a separate `MonthlyReflection` entity (one per user per month), not in `UserSettings`.

**Rationale:**
- **Bounded context:** Reflections are journal/ritual data, separate from user preferences (theme, locale, feature toggles). Keep concerns separated.
- **Queryability:** Reflection history can be queried (recent 12 months, all-time trend) without deserializing a large settings JSON blob.
- **Scalability:** Millions of monthly reflections per user over time; a dedicated table with indexes is correct schema design.
- **Audit trail:** CreatedAtUtc / UpdatedAtUtc fields track when reflections were created/modified (supports Reflection History view).

**Unique constraint:** `(UserId, Year, Month)` ensures one reflection per user per month.

---

### 5. Weekly Goal Identification (ISO Week Start)

**Decision:** `KaizenGoal.WeekStartDate` is the ISO 8601 week start (Monday, DateOnly type), used as the unique key for week identification.

**Rationale:**
- **International standard:** ISO 8601 unambiguous across locales (Monday = week start universally in ISO).
- **Natural key:** Uniqueness constraint is `(UserId, WeekStartDate)` — one goal per user per week, no compound surrogate keys.
- **DateOnly over DateTime:** Week is a date concept; `DateTime` adds no value and complicates comparisons.
- **API paths:** `POST /api/v1/goals/kaizen/week/{weekStart}` (format: `YYYY-MM-DD`) is clean and unambiguous.

---

### 6. Computed Savings in MonthlyReflection

**Decision:** `ActualSavings` (Income - Expenses) is computed on-demand from transaction data but can be cached in the `MonthlyReflection.ActualSavings` column for read access.

**Rationale:**
- **Computation cost:** Income and expense transactions for a month are <1000 items typically; aggregation is O(n) with minimal cost.
- **Cache pattern:** Cache the computed value in the reflection record on first read (or explicitly on month close). Subsequent reads are O(1).
- **Staleness acceptable:** Monthly reflections are not real-time; a few-minute staleness on the cached value is acceptable.
- **Optional caching:** Implementation can defer caching and compute on-demand initially; optimize later if needed.

---

### 7. Service Aggregation — KakeiboCalendarService

**Decision:** A new application service `IKakeiboCalendarService` is introduced to centralize Kakeibo-aware aggregations (month breakdown, week breakdown, predominant category per day).

**Rationale:**
- **Separation of concerns:** Calendar logic (display, navigation) stays in `ICalendarService`. Kakeibo-specific aggregations are distinct.
- **Reusability:** Reports, dashboard, and other features can consume the same aggregation methods without reimplementing.
- **Testing:** Aggregation logic is testable in isolation from calendar UI/state.

**Method signatures:**
```csharp
Task<KakeiboBreakdown> GetMonthBreakdownAsync(int year, int month, Guid userId);
Task<KakeiboBreakdown> GetWeekBreakdownAsync(DateOnly weekStart, Guid userId);
Task<KakeiboCategory> GetDominantCategoryAsync(DateOnly date, Guid userId);
```

---

### 8. No Gamification in Micro-Goals

**Decision:** `KaizenGoal` is entirely non-gamified. Achievements are marked with a quiet checkmark (✓/✗), no confetti, no streaks, no badges, no leaderboards.

**Rationale:**
- **Philosophy alignment:** Kaizen is about continuous self-improvement, not performance maximization or external validation. Gamification undermines this.
- **Sustainable practice:** Intrinsic motivation (personal growth) is more sustainable than extrinsic rewards (points, badges).
- **Simplicity:** Non-gamified design is simpler to build and maintain.
- **Future:** If gamification is desired later, it can be added as an opt-in toggle without breaking the core functionality.

---

### 9. Week-End Goal Achievement Prompt (Non-Blocking)

**Decision:** At week end, a non-blocking, non-judgmental prompt appears: "How did it go? Did you achieve your goal?" with buttons "✓ Yes" and "✗ No". Dismissal is allowed; no guilt language; both outcomes are equally valid.

**Rationale:**
- **Kakeibo philosophy:** Reflection without judgment. The goal is learning, not perfection.
- **UX respect:** Users should not feel pressured or ashamed if a goal is not met.
- **Optional:** Users who don't set goals are not prompted; the feature is entirely optional.
- **Psychological safety:** The app's tone should be supportive, not evaluative.

---

### 10. Reflection Panel Read-Only vs. Editable Fields

**Decision:**

- **Current month:** All fields (Savings Goal, Intention, Gratitude, Improvement) are editable.
- **Past months:** Savings Goal is read-only (audit trail — it was the original goal); Gratitude and Improvement are editable (allows users to add reflections retroactively).
- **Future months:** Only Savings Goal and Intention are shown (non-editable; shown as placeholders/previews).

**Rationale:**
- **Audit trail:** The original savings goal should not change (historical record). Users can see their original intention vs. actual outcome.
- **Journaling flexibility:** Gratitude and improvement can be written/updated later if the user reflects on a past month.
- **Future clarity:** Future months are scaffolding only; no data collection.

---

### 11. KakeiboSelector Component in Transaction Modal

**Decision:** The `KakeiboSelector` component is always visible (not hidden) in the transaction add/edit modal if feature flag `Features:Kakeibo:TransactionOverride` is enabled. If disabled, the selector is hidden entirely (read-only UI).

**Rationale:**
- **Simplicity:** One flag controls visibility and editability; no partially-visible/disabled UI states.
- **Users in disabled mode:** Still get the benefit of Kakeibo routing via their category selection (which has a default Kakeibo value); they just can't override on a per-transaction basis.
- **Phased rollout:** Default on, but can be disabled for users who want simpler transaction entry.

---

### 12. Kakeibo Setup Wizard (Feature 131) vs. Onboarding Kakeibo Step (Feature 133)

**Decision:** 

- **Feature 131** includes a one-time **Kakeibo Setup Wizard** accessible from category settings and triggered on first login post-migration.
- **Feature 133** integrates Kakeibo setup as **Step 5 of the onboarding flow** for new users.

Both use the same or similar UI components (e.g., `KakeiboSetupStep.razor` component).

**Rationale:**
- **New users:** Get Kakeibo introduction as part of standard onboarding (Step 5).
- **Existing users (post-migration):** Get wizard on next login if they haven't completed setup yet (`HasCompletedKakeiboSetup == false`).
- **Component reuse:** Same setup UI serves both flows, reducing duplication.
- **Flag:** `HasCompletedKakeiboSetup` on `UserSettings` prevents re-display for users who have already completed setup.

---

## Implementation Order (Recommended)

1. **Feature 131** (Budget Categories — Kakeibo Routing)
   - Core foundation; all downstream features depend on this
   - Estimated effort: 3–4 days (entity, migration, seeder, API, UI)

2. **Feature 132** (Transaction Entry — Kakeibo Selector)
   - Depends on Feature 131
   - Estimated effort: 2–3 days (entity field, component, modal integration)

3. **Feature 133** (Onboarding — Kakeibo Setup Step)
   - Depends on Feature 131
   - Can run in parallel with Feature 132
   - Estimated effort: 2–3 days (UI component, flow integration)

4. **Features 134–136** (Calendar, Reflection, Micro-Goals)
   - Depend on Features 131–133
   - Can be parallelized (134 and 135 are more tightly coupled; 136 can proceed independently)
   - Estimated effort: 5–7 days per feature

**Total estimated effort:** 3–4 weeks for foundational work (131–136), assuming 1–2 developer teams working in parallel.

---

## Testing Strategy Implications

### Unit Tests
- `KakeiboCategory` enum values and naming
- `Transaction.GetEffectiveKakeiboCategory()` computed property (all override combinations)
- `IKakeiboCalendarService` aggregation methods
- `IReflectionService` CRUD and authorization
- `IKaizenGoalService` CRUD and authorization

### Integration Tests
- Database migrations (seeding defaults, no HasData destructiveness)
- Repository queries with Kakeibo aggregations
- API endpoints (create, read, update, delete, list for reflections and goals)
- Feature flag toggles affecting endpoint visibility and UI rendering

### End-to-End Tests (Blazor/WebApplicationFactory)
- Onboarding flow with Kakeibo setup step
- Transaction entry with Kakeibo selector
- Calendar rendering with heatmap, breakdown bars, badges
- Reflection panel editing and history view
- Micro-goal setting and achievement marking

### Exclusions
- Performance tests for aggregation queries (can be deferred)
- Load testing for concurrent reflection/goal updates (low priority)

---

## Open Questions / Deferred Decisions

1. **Reflection History UI:** Should it be timeline view, table view, or both? Consider deferring to Feature 135 implementation phase.
2. **Kaizen Dashboard:** Should it show 12-week rolling or calendar-year view? Consider user research or MVP with rolling window.
3. **Backward Compatibility:** If migration adds Kakeibo fields to existing categories, should we run data cleanup (e.g., audit log) for compliance? Defer if not required.
4. **Goal Computation:** Should `KaizenGoal.IsAchieved` be auto-computed from actual spend (if `TargetAmount` is set) or always manual? Defer to Feature 136 implementation; allow both patterns.

---

## Related Documentation

- `docs/128-kakeibo-kaizen-calendar-first.md` — Philosophy and four Kakeibo questions
- `docs/129-feature-audit-kakeibo-alignment.md` — Alignment audit and feature flag candidates
- `docs/129b-feature-flag-implementation.md` — Feature flag architecture (database + cache)
- `docs/131-budget-categories-kakeibo-routing.md` — Feature 131 full spec
- `docs/132-transaction-entry-kakeibo-selector.md` — Feature 132 full spec
- `docs/133-onboarding-kakeibo-setup.md` — Feature 133 full spec
- `docs/134-calendar-kakeibo-enhancements.md` — Feature 134 full spec
- `docs/135-monthly-reflection-panel.md` — Feature 135 full spec
- `docs/136-kaizen-micro-goals.md` — Feature 136 full spec

---

## Sign-Off

**Alfred (Lead):** These specs are complete, internally consistent, and ready for implementation. Feature 131 is the blocking dependency; teams can begin TDD-driven development immediately after Feature 129b (feature flag infrastructure) is merged.


---

# Lucius: Feature Specifications 137–144 — Implementation Decisions

**Date:** 2026-04-10  
**Author:** Lucius (Backend Dev)  
**Charter Task:** Create 8 feature spec documents for Kakeibo alignment (Reports, AI, Settings, utilities)

---

## Summary

Created 8 comprehensive feature specification documents (137–144) defining backend and frontend requirements for Kakeibo + Kaizen alignment across Reports, AI Chat, Settings, and utility pages. All specs follow the standard format: Status (Planned), Prerequisites, Feature Flag (where applicable), Overview, Domain Model Changes, API Changes, UI Changes, Acceptance Criteria, Implementation Notes.

---

## Feature Breakdown & Key Decisions

### Feature 137: Kaizen Dashboard Report

**Type:** New Report / Data Visualization  
**Scope:** Backend (aggregation service, API endpoint), Frontend (chart component)

**Key Decisions:**
- **Flag:** `Features:Kaizen:Dashboard` (default: `false` during dev, `true` when shipped)
- **Depends On:** 131 (KakeiboCategory), 136 (KaizenGoal entity), 129b (Feature Flag system)
- **New Endpoint:** `GET /api/v1/reports/kaizen-dashboard?weeks=12`
- **DTO:** `KaizenDashboardDto` with `List<WeeklyKakeiboSummary>` — each week contains `Essentials`, `Wants`, `Culture`, `Unexpected` amounts + `KaizenGoalDescription?` and `KaizenGoalAchieved?`
- **Aggregation:** Weekly grouping of transactions by effective Kakeibo category; join with `KaizenGoal` for outcomes
- **Caching:** 1-hour `IMemoryCache` per `userId:weeks` to avoid repeated aggregation
- **Chart:** Stacked area chart with month boundaries marked; Kaizen badges (✓/✗) overlaid on week columns

---

### Feature 138: Transactions List — Kakeibo Filter and Badge

**Type:** UI Enhancement + API Filter  
**Scope:** Backend (filtering logic), Frontend (dropdown + badge component)

**Key Decisions:**
- **Flag:** `Features:Kakeibo:TransactionFilter` (default: `true` — on by default)
- **Depends On:** 131 (KakeiboCategory), 132 (KakeiboOverride), 129b
- **New Query Param:** `GET /api/v1/transactions?kakeiboCategory=Wants` (optional)
- **DTO Change:** `TransactionSummaryDto` gains `EffectiveKakeiboCategory: string?` (resolved server-side)
- **Effective Category Logic:** `Transaction.KakeiboOverride ?? BudgetCategory.KakeiboCategory`
- **Filter Options:** All / Essentials / Wants / Culture / Unexpected
- **UI:** Dropdown filter + colored badge per transaction row (Expense only; Income/Transfer → no badge)
- **State:** Filter selection persisted to `localStorage` across page reloads

---

### Feature 139: AI Chat — Kakeibo Awareness

**Type:** AI Service Enhancement  
**Scope:** AI action builder logic, chat UI

**Key Decisions:**
- **Flag:** None (enhances existing `Features:AI:ChatAssistant`)
- **Depends On:** 131, 132, 138 (for Kakeibo query support)
- **New Action Type:** `ClarificationNeededAction.AskKakeiboCategory` — prompts user to confirm/select bucket when determinism unclear
- **Behavior:**
  - AI includes Kakeibo intent in confirmation messages: "Dinner at Olive Garden — Dining (Wants). Confirm?"
  - If category's `KakeiboCategory` is null or default (Wants), asks clarification: "Is this Essentials, Wants, Culture, or Unexpected?"
  - Supports natural language Kakeibo queries: "How much on Wants this week?" — queries via `GET /api/v1/transactions?kakeiboCategory=Wants`
- **UI:** Clarification dialog with four buttons (one per bucket); color-coded badges in messages

---

### Feature 140: AI Rule Suggestions — Kakeibo Display

**Type:** UI Information Display  
**Scope:** Frontend (badge addition), Backend (minor DTO field)

**Key Decisions:**
- **Flag:** None (enhances existing `Features:AI:RuleSuggestions`)
- **Depends On:** 131 (KakeiboCategory)
- **DTO Change:** `CategorySuggestionDto` gains `SuggestedKakeiboCategory: string?` (from suggested category's `KakeiboCategory`)
- **Optional Enhancement:** `KakeiboOverrideSuggestion: string?` + `KakeiboOverrideReasoning: string?` — AI can suggest alternative buckets based on merchant context
- **UI:** Kakeibo badge next to category name (e.g., "Dining → **Wants**"); optional override callout with reasoning
- **Interaction:** User accepts category + override in a single action if override is suggested

---

### Feature 141: Settings — Kakeibo/Kaizen Preferences

**Type:** User Settings / Data Model  
**Scope:** Domain (new fields), API (DTO extension), Frontend (settings UI)

**Key Decisions:**
- **Flag:** None (settings page always available; individual toggles control feature visibility)
- **Depends On:** 134 (Calendar heatmap), 135 (Monthly Reflection), 136 (Kaizen Goals)
- **New Fields on `UserSettings`:**
  - `ShowSpendingHeatmap: bool = true`
  - `ShowMonthlyReflectionPrompts: bool = true`
  - `EnableKaizenMicroGoals: bool = true`
  - `ShowKakeiboCalendarBadges: bool = true`
- **Migration:** Add 4 columns to `UserSettings` table with `DEFAULT TRUE`
- **DTO Change:** `UserSettingsDto` extended with 4 bool fields
- **API:** No new endpoints; `GET/PUT /api/v1/settings` extended
- **UI:** New "Kakeibo & Kaizen Preferences" section with 4 toggle switches
- **Semantics:** These are **user preferences** controlling visibility. **Server feature flags** (129b) control whether feature exists at all.

---

### Feature 142: Uncategorized Transactions — Kakeibo Display

**Type:** UI Information Display  
**Scope:** Frontend (category dropdown enhancement)

**Key Decisions:**
- **Flag:** None (informational enhancement, always useful)
- **Depends On:** 131 (KakeiboCategory)
- **No API Changes:** Category dropdown already fetches category list; enrich client-side with `KakeiboCategory`
- **UI:** When user hovers/selects category, display Kakeibo badge preview (e.g., "Dining → **Wants**")
- **Confirmation:** After categorization, brief feedback message shows (1–2 sec): "✓ Dining (Wants)"
- **Optional:** Direct Kakeibo override button during categorization (deferred to follow-up if needed)

---

### Feature 143: Reports — Kakeibo Grouping

**Type:** Report Enhancement / Aggregation Toggle  
**Scope:** Backend (grouping logic, query param), Frontend (toggle UI)

**Key Decisions:**
- **Flag:** None (toggles are UI controls within existing report pages)
- **Depends On:** 131 (KakeiboCategory)
- **Affected Reports:**
  1. Monthly Categories Report: toggle "Group by: [Categories] [Kakeibo Buckets]"
  2. Budget Comparison Report: dropdown "Variance by: [Category] [Kakeibo Bucket]"
  3. Monthly Trends Report: toggle "Trend by: [Categories] [Kakeibo Buckets]"
- **New Query Params:** All three report endpoints accept `groupByKakeibo: bool?` (optional, default: false)
- **Aggregation:** When `true`, group by effective Kakeibo category (override checked first); sum amounts per bucket
- **Response DTO:** Restructured to group by bucket name when `groupByKakeibo=true`; backward-compatible (optional param)
- **UI State:** Grouping selection persisted to `localStorage` per report
- **Color Consistency:** Four Kakeibo colors applied to chart segments/lines

---

### Feature 144: Custom Reports Builder — Feature Flag

**Type:** Philosophical Gate / Feature Toggle  
**Scope:** Backend (flag check middleware/guard), Frontend (nav item visibility, route guard)

**Key Decisions:**
- **Flag:** `Features:Reports:CustomReportBuilder` (default: `false` — off by default)
- **Depends On:** 129b (Feature Flag system)
- **Rationale:** Custom Reports Builder creates tension with Kakeibo philosophy (calendar-first, intentional reflection) by encouraging endless data exploration. Default-off but available for power users who opt in.
- **Route Guard:** `GET /api/v1/reports/custom/{...}` checks flag; returns 404 if disabled
- **Nav Item:** "Custom Report Builder" menu link hidden unless flag enabled; controlled by `IFeatureFlagClientService`
- **When Enabled:** Page displays educational note at top: "The calendar is your primary reflection surface. Custom reports are for deep-dive analysis only."
- **Dismissal:** Note can be dismissed; dismissal stored in `localStorage`
- **Runtime Toggle:** Admin can enable/disable via `PUT /api/v1/features/Features:Reports:CustomReportBuilder` without restart

---

## Consistent Decisions Across All 8 Features

### 1. Feature Flag Strategy

| Feature | Flag Name | Default | Rationale |
|---------|-----------|---------|-----------|
| 137 | `Features:Kaizen:Dashboard` | false | Behind flag during dev; shipped = true |
| 138 | `Features:Kakeibo:TransactionFilter` | true | On by default; users see immediately |
| 139 | (reuse `Features:AI:ChatAssistant`) | — | Enhances existing flag |
| 140 | (reuse `Features:AI:RuleSuggestions`) | — | Enhances existing flag |
| 141 | (none) | — | Settings always available; toggles per-user |
| 142 | (none) | — | Informational, always useful |
| 143 | (none) | — | Report toggles are UI controls |
| 144 | `Features:Reports:CustomReportBuilder` | false | Philosophical gate; users opt in |

### 2. Kakeibo Color Scheme (Universal)

Applied to badges, chart lines, toggles, and report segments across all features:

```
Essentials: #3b82f6 (blue)
Wants: #10b981 (green)
Culture: #a855f7 (purple)
Unexpected: #f97316 (orange/red)
```

### 3. Server-Side Resolution Pattern

All features resolve **effective Kakeibo category** server-side:

```csharp
EffectiveKakeiboCategory = Transaction.KakeiboOverride ?? BudgetCategory.KakeiboCategory
```

This ensures:
- Consistency across API, filtering, aggregation
- Client doesn't need to manage routing logic
- API-level filtering and grouping work correctly

### 4. DTO & API Field Additions

**New Fields (across all specs):**

| DTO | Field | Type | Purpose |
|-----|-------|------|---------|
| `TransactionSummaryDto` | `EffectiveKakeiboCategory` | `string?` | Show user which Kakeibo bucket transaction belongs to |
| `KaizenDashboardDto` | `Weeks` | `List<WeeklyKakeiboSummary>` | Weekly spending aggregations + goal outcomes |
| `CategorySuggestionDto` | `SuggestedKakeiboCategory` | `string?` | Show suggested category's Kakeibo bucket |
| `CategorySuggestionDto` | `KakeiboOverrideSuggestion` | `string?` | Optional AI-driven override suggestion |
| `UserSettingsDto` | `ShowSpendingHeatmap` | `bool` | User preference |
| `UserSettingsDto` | `ShowMonthlyReflectionPrompts` | `bool` | User preference |
| `UserSettingsDto` | `EnableKaizenMicroGoals` | `bool` | User preference |
| `UserSettingsDto` | `ShowKakeiboCalendarBadges` | `bool` | User preference |

**New Query Parameters:**

| Endpoint | Param | Type | Purpose |
|----------|-------|------|---------|
| `GET /api/v1/transactions` | `kakeiboCategory` | `string?` | Filter by Kakeibo bucket (138) |
| `GET /api/v1/reports/monthly-categories` | `groupByKakeibo` | `bool?` | Group by bucket instead of category (143) |
| `GET /api/v1/reports/budget-comparison` | `groupByKakeibo` | `bool?` | Show variance by bucket (143) |
| `GET /api/v1/reports/monthly-trends` | `groupByKakeibo` | `bool?` | Show trend lines per bucket (143) |

### 5. Dependency Hierarchy

```
All 8 features depend on:
  └─ 129b (Feature Flag Implementation) ✓

Additionally:
  137 ← 131 (KakeiboCategory) + 136 (KaizenGoal)
  138 ← 131 + 132 (KakeiboOverride)
  139 ← 131 + 132 + 138
  140 ← 131
  141 ← 134 (Heatmap) + 135 (Monthly Reflection) + 136 (Goals)
  142 ← 131
  143 ← 131
  144 ← (none, only 129b)
```

### 6. UI State Persistence Pattern

Features that accept user selections (filter, toggle, dismissal) persist to `localStorage`:

- **138:** `transaction-filter:kakeiboCategory` (filter choice)
- **143:** `report:{reportName}:groupBy=Kakeibo` (grouping choice per report)
- **144:** `customReportBuilderEducationalNoteDismissed` (dismissal flag)

### 7. Standards Adherence

All 8 specs follow:
- Clean code & SOLID principles (backend services properly abstracted)
- TDD format (Acceptance Criteria written as testable statements)
- Consistent naming (Kakeibo features use consistent terminology and color codes)
- Documentation (Implementation Notes guide backend & frontend devs)
- OpenAPI spec updates required for API changes

---

## Implementation Sequence (Recommended)

1. **Foundation:** Merge 129b (Feature Flag Implementation) first — all 8 features depend on it
2. **Domain Model:** Complete 131 (KakeiboCategory), 132 (KakeiboOverride), 136 (KaizenGoal) before implementing any feature
3. **Settings:** Implement 141 (UserSettings new fields + settings UI) early — provides per-user control layer
4. **Transactions:** Implement 138 (filter + badge) — customers see Kakeibo immediately when viewing transactions
5. **Reports:** Implement 137 (dashboard), 143 (grouping) — provides reflection/analysis surface
6. **AI:** Implement 139, 140 — builds intelligence into suggestions and transactions
7. **Utility:** Implement 142 (uncategorized display) — polish on transaction entry
8. **Philosophy:** Implement 144 (custom reports gating) — reinforces calendar-first design

---

## Acceptance Criteria Highlights

All 8 specs include comprehensive acceptance criteria covering:
- Feature flag behavior (if applicable)
- API correctness (endpoints, params, response shape)
- Service-layer aggregation (effective category resolution, grouping logic)
- UI elements (dropdowns, badges, toggles, charts)
- State persistence (localStorage, if applicable)
- Test coverage (unit, integration, API)
- Documentation (OpenAPI spec updates)

---

## Notes for Implementation Teams

### Backend (Lucius) Priorities

1. **Effective Kakeibo Resolution:** Build a single, reusable function to resolve `Transaction.KakeiboOverride ?? BudgetCategory.KakeiboCategory`. Used by all features.
2. **Aggregation Services:** Implement `IKakeiboAggregationService` (weekly summaries, category grouping, variance calculations) — used by 137, 143.
3. **Caching:** Use `IMemoryCache` with appropriate TTLs (1 hour for weekly summaries, 5 min for category/transaction lists).
4. **Filtering:** Implement Kakeibo filter at the repository level in `TransactionRepository.GetAsync(kakeiboCategory)` — ensures query efficiency.
5. **DTOs:** Carefully version DTOs to ensure backward compatibility; new fields should be optional where possible.

### Frontend (Client) Priorities

1. **Color Scheme:** Define Kakeibo color constants in a shared client utility; use consistently across all 8 features.
2. **Feature Flag Service:** Integrate `IFeatureFlagClientService` into layout and route guards; check flags before rendering nav items and allowing route access.
3. **localStorage Strategy:** Use a consistent key naming scheme (e.g., `feature:{name}:{state}`) to avoid conflicts.
4. **Accessibility:** Ensure all badges, tooltips, and toggle switches are keyboard-accessible and screen-reader friendly.
5. **Chart Library:** Verify chart library (Chart.js, Plotly, etc.) supports stacked area charts with custom colors and overlaid markers (for 137).

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| **Feature flag cascade:** Many features depend on 129b being rock-solid | 129b must be thoroughly tested before any of 137–144 are implemented. Recommend 2–3 code reviews. |
| **Effective Kakeibo resolution bugs:** If override logic is wrong, all filtering/grouping cascades fail | Implement unit tests for `EffectiveKakeiboCategory` resolution with explicit test cases (override set, override null, category null, etc.) |
| **Performance:** Weekly aggregations could be slow for users with 10k+ transactions | Implement caching early (1-hour TTL for weekly summaries). Monitor query performance in CI stress tests. Consider denormalization if aggregation remains slow post-optimization. |
| **Backward Compatibility:** Adding fields to existing DTOs could break old clients | New fields should be optional (nullable or default values). Existing endpoints should work unchanged when new params are omitted. |
| **Color scheme inconsistency:** If colors vary across reports/badges, Kakeibo philosophy is diluted | Define color constants in shared utility; code review checklist must include "Kakeibo colors match spec." |

---

## Success Criteria (Completed Task)

✅ **8 feature specification documents created** (137–144) in `docs/` directory  
✅ **All specs follow standard format:** Status, Prerequisites, Feature Flag (if applicable), Overview, Domain Model Changes, API Changes, UI Changes, Acceptance Criteria, Implementation Notes  
✅ **Feature flags defined** with clear defaults and rationale  
✅ **Dependencies documented** (all depend on 129b; some depend on 131, 132, 134, 135, 136, etc.)  
✅ **DTOs and API endpoints specified** with request/response examples  
✅ **Color scheme standardized** (blue/green/purple/orange across all features)  
✅ **Acceptance criteria testable** (all are actionable, measurable statements)  
✅ **Implementation notes comprehensive** (guide both backend and frontend developers)  
✅ **Consistent naming & terminology** across all 8 docs (Kakeibo, bucket, override, effective category)  

---

**Status:** ✅ **GREEN** — All 8 feature specs created and ready for implementation planning.


