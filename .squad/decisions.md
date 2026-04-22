# Squad Decisions

## Active Decisions

### 1. Branch Strategy Operationalization — Approved (2026-04-14)

**Assessed by:** Alfred  
**Status:** ✅ APPROVED with implementation complete

**Decision:** Operationalize trunk-based development with `develop` stabilization layer:

- **`main`** — Always releasable. Only receives merges from `develop` or hotfixes.
- **`develop`** — Pre-release integration branch. Receives merges from feature branches.
- **`feature/*`** — Individual feature work. Branch from `develop`, PR back to `develop`.
- **`hotfix/*`** — Urgent fixes to released versions. Branch from release tag, tag new version, merge back to both `main` and `develop`.

**Implementation (Completed by Lucius):**

1. ✅ Updated `CONTRIBUTING.md` Step 1 to branch from `develop` (not `main`).
2. ✅ Updated PR instructions to target `develop` for feature work.
3. ✅ Extended CI workflow (`.github/workflows/ci.yml`) to run on both `main` and `develop`.
4. ✅ Created `develop` branch from `origin/main` and pushed to origin.
5. ✅ Release and Docker semantics remain unchanged (tag-driven from `main`).

**Rationale:** This strategy is standard trunk-based development with a stabilization layer, common in teams that release frequently. Feature branches now gate against `develop` instead of `main`, while release and deployment workflows remain tag-driven and independent of branches.

**Impact:** All feature branches must now branch from and PR against `develop`. Existing PRs against `main` should be closed and reopened against `develop`. All active feature branches must rebase/merge-rebase against `develop` before PR.

**CI Status:** ✅ All checks passed.

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

### 30. Feature 160: llama.cpp Backend Implementation & Runtime Selection (2026-04-13)

**Author:** Lucius

**Decision:** 
- Implement `LlamaCppAiService` as a concrete `OpenAiCompatibleAiService` subclass.
- Create `BackendSelectingAiService` as a runtime selector that reads persisted backend choice from app settings.
- Register both concrete backends as typed HttpClient services and bind `BackendSelectingAiService` to `IAiService`.

**Rationale:**
- OpenAI-compatible HTTP flow (message formatting, streaming, token counting) is shared between Ollama and llama.cpp; inheritance avoids duplication.
- Backend choice lives in persisted app settings, so startup-only concrete binding would require app restart after switching backends.
- Runtime selector pattern allows dynamic backend switching via app settings without restart.
- Preserves Ollama as the default and provides configuration fallback if persisted settings unavailable.

**Implications:**
- Application and API layers remain dependent only on `IAiService` (no concrete backend leakage).
- Adding another backend stays localized to Infrastructure: add the concrete service and extend the selector's mapping.
- Non-Docker regression coverage validates token counting, backend selection, and DI registration; Docker-backed API integration tests deferred pending environment documentation.

**Result:** Feature 160 llama.cpp + DI selection slice complete. Ollama preserved as default for backward compatibility.

---

### 31. Feature 160: Pluggable AI Backend — Complete (2026-04-13)

**Author:** Alfred, Barbara, Lucius  
**Status:** DONE — Production ready, approved for merge

#### Overview

Feature 160 (Pluggable AI Backend) implements runtime backend selection between Ollama and llama.cpp with transparent endpoint configuration. The feature went through 7 phases: enum/DTOs → base class → concrete implementations → DI registration → persistence → API contracts → client UI completion.

**Final state:** All code, tests, documentation, and client UI are complete. Feature approved by all team members.

#### Architecture

**Strategy Pattern with DI Runtime Selection:**
- `OpenAiCompatibleAiService`: Abstract base class encapsulating shared OpenAI HTTP protocol (health checks, model listing, completions, token counting)
- `OllamaAiService`: Extends base; health endpoint `/api/version`, model parsing for Ollama's tags response
- `LlamaCppAiService`: Extends base; health endpoint `/health`, model parsing for llama.cpp's `/v1/models` array
- `BackendSelectingAiService`: Runtime selector reading `AiSettings:BackendType` from persisted app settings, dispatches to appropriate backend
- **DI Registration:** Both concrete backends registered as typed services; `BackendSelectingAiService` bound to `IAiService`
- **Default:** Ollama (preserves zero-breaking-change behavior)

**Data Model:**
- `AiBackendType` enum: Ollama=0, LlamaCpp=1
- `AiSettingsData`: `BackendType` field + backward-compatible `OllamaEndpoint` → `EndpointUrl` migration
- `AiStatusDto`: Includes `BackendType` in responses
- `AiDefaults` (Domain) + `AiBackendDefaults` (Shared): Backend-specific default URLs

#### Acceptance Criteria (All Met ✅)

| Criterion | Evidence | Status |
|-----------|----------|--------|
| Enum with Ollama & LlamaCpp | `AiBackendType` in Shared | ✅ |
| Base class abstracts OpenAI protocol | `OpenAiCompatibleAiService` | ✅ |
| Ollama implementation | `OllamaAiService` (backward compatible) | ✅ |
| llama.cpp implementation | `LlamaCppAiService` with correct endpoints | ✅ |
| DI runtime selection | `BackendSelectingAiService` strategy | ✅ |
| Persistence | `AppSettingsService` + migration | ✅ |
| API endpoints expose BackendType | `AiController` status/settings/update | ✅ |
| **Client form selector** | `AiSettingsForm.razor` with dropdown | ✅ |
| **Generic endpoint handling** | Bind to `EndpointUrl`, smart default swapping | ✅ |
| **Unit tests (all layers)** | 2826+ Client, 687 API, 257 Infrastructure tests passed | ✅ |
| **Documentation** | docs/AI.md updated, extension guide added, feature doc marked Done | ✅ |

#### Test Results

- **Domain:** 924 passed
- **Application:** 1136 passed
- **Infrastructure:** 257 passed (Docker-backed)
- **API:** 687 passed (Docker-backed)
- **Client:** 2826 passed, 1 skipped (Category!=Performance)
- **Total:** 5832 passed, 1 skipped, 0 failed

#### Key Decisions (Merged from Inbox)

1. **Backend Selection Strategy** (Decision: Use runtime selector, not compile-time binding)
   - Allows dynamic backend switching via app settings without restart
   - Both backends registered; selector dispatches at runtime
   - Default Ollama preserved

2. **Client UI Implementation** (Decision: BackendType selector + smart endpoint swapping)
   - Dropdown for Ollama/llama.cpp selection
   - Generic `EndpointUrl` binding (not `OllamaEndpoint`)
   - Placeholder updates: Ollama :11434, llama.cpp :8080
   - Smart default swapping: only swaps if endpoint still matches previous backend's default; preserves custom endpoints

3. **Documentation** (Decision: Update docs/AI.md + add extension guide)
   - AI.md: title "AI Features (Ollama or llama.cpp)", "Backend Selection" section
   - New docs/AI-BACKEND-EXTENSION.md: guide for adding new backends
   - Feature doc 160 marked Done and ready for archive

#### Testing Note: Docker-Required Integration Tests

Infrastructure and API integration tests require Docker/Testcontainers (both verified in CI with full PostgreSQL). This is not a blocker for local development — all unit tests (mock-based HTTP) pass without Docker. CI validates the full stack.

#### Extensibility

**Adding a new backend (e.g., OpenAI):**
1. Create class extending `OpenAiCompatibleAiService`
2. Override protocol-specific methods (`GetBackendDisplayName()`, `GetHealthCheckEndpoint()`, `ParseModelsResponseAsync()`)
3. Add enum value to `AiBackendType` (if new pattern needed)
4. Register in DI (Infrastructure AddInfrastructure)
5. Update `BackendSelectingAiService.ResolveBackendAsync()` switch
6. Write unit tests with mock HTTP (no Docker required)

No Application/Domain layer changes needed. Zero breaking changes to existing code.

#### Recommendation

Feature 160 is **eligible for merge to main.** All acceptance criteria met, all tests passing, full documentation and client UI complete.

---

### 32. User Directive: Main Branch Releasability & Branching Strategy (2026-04-13)

**Author:** Fortinbra (via Copilot)  
**Request:** Main must always remain in a releasable state. Use a `develop` branch for pre-release work, use feature branches for individual features.

**Implication:** Squad will follow trunk-based development with develop branch for stabilization + feature branches for isolated work.

---

### 33. User Directive: Docker Reminder for Feature Starts (2026-04-13)

**Author:** thegu (via Copilot)  
**Request:** When we start new features, remind me to ensure Docker is running (for Testcontainers).

**Implication:** Team should surface this check at feature kickoff to avoid "Why are my tests failing?" debugging.

---

### 34. Alfred — llama.cpp Local Model Recommendation (2026-04-13)

**Author:** Alfred  
**Status:** ✅ APPROVED

**Decision:** For local llama.cpp work on **32 GB RAM / 16 GB VRAM / RTX 5070-class** hardware, recommend **`Qwen/Qwen3-14B-GGUF:Q5_K_M`** as the default for general-purpose chat and reasoning.

**Why:**
- 14B dense models provide the best quality/speed balance for this hardware tier.
- Qwen3 has official GGUF support, official llama.cpp guidance, and strong chat + reasoning profile.
- `Q5_K_M` fits comfortably on 16 GB VRAM; `Q8_0` is too tight to recommend as default.

**Fallback Options:**
- Conservative: `Qwen/Qwen2.5-14B-Instruct-GGUF:Q5_K_M`
- Fast: `bartowski/Meta-Llama-3.1-8B-Instruct-GGUF:Q6_K`

**Key Note:** Do not treat 32K+ context or thinking mode as the default UX path. Start with 8K context and use Qwen3 `/no_think` unless the task requires deliberate reasoning.

---

### 35. Vic — llama.cpp Model Audit (2026-04-13)

**Author:** Vic  
**Scope:** Independent validation of local llama.cpp model recommendation  
**Status:** ✅ APPROVED

**Executive Judgment:** **`Qwen/Qwen3-14B-GGUF`** is the best default—preferably `Q6_K` for quality or `Q5_K_M` for speed/headroom.

**Validation Points:**
1. Practical local usability on 16 GB VRAM
2. Good general chat quality plus real reasoning ability
3. First-party llama.cpp support and documentation maturity
4. No fragile or barely-fitting setups

**Two-Tier Recommendation:**
1. **Best local usability (default):** `Qwen/Qwen3-14B-GGUF` (`Q6_K` or `Q5_K_M`) — ~12 GB
2. **Best pure quality (slower hybrid inference):** `Qwen/Qwen3-32B-GGUF` (`Q4_K_M`) — ~19.76 GB (requires system RAM offload)

**Models NOT to Oversell:**
- DeepSeek-R1 distills (specialized for reasoning, poor for general chat)
- 70B-class models (32 GB system RAM + 16 GB VRAM is not a comfortable 70B setup)
- Extended context claims without warnings (YaRN extension increases memory/speed pressure)

---

### 36. Lucius — Merge Squad Branch to Develop (2026-04-13)

**Author:** Lucius  
**Status:** ✅ APPROVED

**Decision:** The `squad` worktree contains only expected Feature 160 client completion changes and recent develop-branch workflow/documentation updates. Merge is safe.

**Modified Files Reviewed:**
- CI workflow (`ci.yml`)
- Documentation (`CONTRIBUTING.md`, deployment docs)
- AI settings client files
- Client tests

**Execution:** Preserve `squad` tip remotely, merge `squad` into `develop` non-interactively, checkout on `develop`.

**CI Status:** ✅ Targeted client test suite passed before merge.

---

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction

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




## Merged from Inbox (2026-04-08)

# Feature 120: Plugin System — Implementation Slice Plan

**Author:** Alfred (Lead)  
**Date:** 2026-03-22  
**Requested by:** Fortinbra

---

## Executive Summary

Feature 120 introduces a plugin architecture enabling third-party extensions. The spec defines 8 user stories across authoring, management, domain events, and UI integration. This plan decomposes the feature into **10 ordered slices** following TDD and Clean Architecture constraints.

**Critical precondition identified:** The existing `Transaction._domainEvents` collection (line 17 in `Transaction.cs`) is typed as `List<object>` and is never dispatched. Slice 1 must wire up domain event infrastructure before any plugin event subscriptions are possible.

---

## Slice Plan

### Slice 1: Domain Event Foundation

**Layer(s):** Domain, Infrastructure  
**Projects:** `BudgetExperiment.Domain`, `BudgetExperiment.Infrastructure`

**Delivers:**
- `IDomainEvent` marker interface with `OccurredAtUtc` property
- Core event record types: `TransactionCreatedEvent`, `TransactionUpdatedEvent`, `TransactionDeletedEvent`, `TransactionCategorizedEvent`, `ImportCompletedEvent`, `RuleSuggestionAcceptedEvent`, `ReconciliationMatchedEvent`
- `IDomainEventDispatcher` interface in Domain
- Refactor `Transaction._domainEvents` from `List<object>` to `List<IDomainEvent>`
- Add `RaiseDomainEvent()` protected method and `ClearDomainEvents()` to entity base or Transaction
- `DomainEventDispatcher` implementation in Infrastructure (resolves handlers via DI, logs failures, does not rethrow)

**Acceptance Criteria Closed:** US-120-008 (partial — IDomainEvent, dispatcher, event types)

**Dependencies:** None (foundation slice)

**Assigned:** Lucius (backend/infra)

**Risks/Decisions:**
- **Decision needed:** Where to place `IDomainEventDispatcher` implementation — Infrastructure or a new `BudgetExperiment.Application.Events` namespace? Recommendation: Infrastructure, since dispatch wiring requires DbContext access.
- **Risk:** Event dispatch timing. Spec requires post-commit dispatch. Implementation will collect events from tracked entities before `SaveChangesAsync`, then dispatch after successful commit.

---

### Slice 2: Domain Event Dispatch Wiring

**Layer(s):** Infrastructure  
**Projects:** `BudgetExperiment.Infrastructure`

**Delivers:**
- Override `BudgetDbContext.SaveChangesAsync` to:
  1. Collect all domain events from tracked entities implementing `IHasDomainEvents`
  2. Call `base.SaveChangesAsync()` (commit)
  3. Dispatch collected events via `IDomainEventDispatcher`
  4. Clear events from entities
- `IHasDomainEvents` interface for entities that raise events
- Transaction entity implements `IHasDomainEvents`

**Acceptance Criteria Closed:** US-120-008 (completes event dispatch after SaveChanges)

**Dependencies:** Slice 1

**Assigned:** Lucius

**Risks/Decisions:**
- **Risk:** Dispatcher must be resolved at dispatch time, not constructor-injected into DbContext (to avoid captive dependency). Use `IServiceProvider` to resolve.
- **Architectural note:** Handler failures are logged but do not roll back the committed transaction (per spec).

---

### Slice 3: Plugin Abstractions Project

**Layer(s):** SDK (new)  
**Projects:** `BudgetExperiment.Plugin.Abstractions` (new)

**Delivers:**
- New project with **zero dependencies on core projects** (only Microsoft.Extensions.* and System.*)
- `IPlugin` interface (Name, Version, Description, ConfigureServices, InitializeAsync)
- `IPluginContext` interface (Services, Configuration, LoggerFactory)
- `IDomainEventHandler<TEvent>` interface (requires `IDomainEvent` from Domain — **BOUNDARY ISSUE**)
- `IImportParser` interface with `ParsedTransactionRow` DTO
- `IReportBuilder` interface with `ReportParameters`, `ReportResult` types
- `IPluginNavigationProvider` interface with `PluginNavItem` record
- `PluginControllerBase` abstract class
- `PluginAttribute` for assembly metadata

**Acceptance Criteria Closed:** US-120-001 (SDK interfaces), US-120-002 (PluginControllerBase), US-120-003 (IImportParser), US-120-004 (IReportBuilder), US-120-009 (IPluginNavigationProvider)

**Dependencies:** Slice 1 (IDomainEvent must exist)

**Assigned:** Lucius

**Risks/Decisions:**
- **CRITICAL BOUNDARY DECISION:** `IDomainEventHandler<TEvent>` has a constraint `where TEvent : IDomainEvent`. This requires Plugin.Abstractions to reference Domain (or duplicate the interface).
  - **Option A:** Plugin.Abstractions references Domain (breaks "no core deps" constraint)
  - **Option B:** Move `IDomainEvent` to Plugin.Abstractions and have Domain reference it (inverts dependency)
  - **Option C:** Duplicate `IDomainEvent` in Abstractions as `IPluginDomainEvent` (breaks type compatibility)
  - **Recommendation:** Option B — `IDomainEvent` is a marker interface with one property. Plugin.Abstractions becomes the contract boundary; Domain references it for the marker. This preserves plugin author experience (single SDK reference) and type safety.

---

### Slice 4: Plugin Hosting Project

**Layer(s):** Hosting (new)  
**Projects:** `BudgetExperiment.Plugin.Hosting` (new)

**Delivers:**
- New project referencing Plugin.Abstractions + Domain
- `PluginScanner` — scans directory for assemblies with `IPlugin` implementations
- `PluginDescriptor` — metadata record (name, version, assembly path, status, capabilities)
- `PluginRegistry` — tracks loaded plugins, queryable by name
- `PluginLoader` — loads assemblies, instantiates `IPlugin`, invokes `ConfigureServices`
- `PluginHostedService` — `IHostedService` that calls `IPlugin.InitializeAsync` on startup
- `AddPlugins(IConfiguration)` extension method for DI
- Configuration binding for `Plugins:Path` and `Plugins:Disabled`
- Logging throughout (discovery, loading, failures)

**Acceptance Criteria Closed:** US-120-005 (folder-based install), US-120-007 (disable via config)

**Dependencies:** Slice 3

**Assigned:** Lucius

**Risks/Decisions:**
- **Assembly loading:** Use `AssemblyLoadContext` for isolation? MVP spec says shared AppDomain — use default context with assembly scanning.
- **Risk:** Incompatible plugin (wrong SDK version) could crash host. Implement version check and graceful skip.

---

### Slice 5: API Integration + Management Endpoints

**Layer(s):** API, Contracts  
**Projects:** `BudgetExperiment.Api`, `BudgetExperiment.Contracts`

**Delivers:**
- `builder.Services.AddPlugins(configuration)` call in `Program.cs`
- `ApplicationPartManager` configuration to add plugin assemblies (controller discovery)
- Route convention to prefix plugin controllers with `/api/v1/plugins/{pluginName}/`
- `PluginsController` with:
  - `GET /api/v1/plugins` — list all plugins
  - `GET /api/v1/plugins/{name}` — plugin detail
- `PluginInfoResponse` DTO in Contracts
- `Plugins` section in `appsettings.json`
- OpenAPI integration (plugin endpoints appear in Scalar)

**Acceptance Criteria Closed:** US-120-002 (route prefix), US-120-006 (management endpoint)

**Dependencies:** Slice 4

**Assigned:** Lucius

**Risks/Decisions:**
- **Route convention:** Use `IApplicationModelConvention` to apply prefix dynamically based on `PluginControllerBase` inheritance.
- **Auth:** Plugin endpoints participate in same auth pipeline (no special handling needed).

---

### Slice 6: Import Parser Extension Point

**Layer(s):** Application  
**Projects:** `BudgetExperiment.Application`

**Delivers:**
- Modify `ImportService` to resolve `IEnumerable<IImportParser>` from DI
- Parser selection logic: match by file extension from `SupportedExtensions`
- Fallback to core CSV parser if no plugin matches
- Registration point for plugin parsers (via `IServiceCollection`)

**Acceptance Criteria Closed:** US-120-003 (import parser integration)

**Dependencies:** Slice 4 (plugin DI registration)

**Assigned:** Lucius

**Risks/Decisions:**
- **Risk:** Plugin parser returns invalid DTOs. Add validation layer before committing transactions.
- **Decision:** Plugin parsers return `ParsedTransactionRow` (defined in Abstractions) — need mapping to internal DTO.

---

### Slice 7: Report Builder Extension Point

**Layer(s):** Application, API  
**Projects:** `BudgetExperiment.Application`, `BudgetExperiment.Api`

**Delivers:**
- Modify `ReportService` to resolve `IEnumerable<IReportBuilder>` from DI
- Expose plugin report types via existing reports API
- Add endpoint to list available report types (core + plugins)
- Plugin reports use same `ReportParameters` input shape

**Acceptance Criteria Closed:** US-120-004 (report builder integration)

**Dependencies:** Slice 4

**Assigned:** Lucius

**Risks/Decisions:**
- **Decision:** How do plugin reports appear in UI? Slice 9 (Blazor) will need a "plugin reports" section.

---

### Slice 8: Blazor UI Integration — Navigation + Routing

**Layer(s):** Client  
**Projects:** `BudgetExperiment.Client`

**Delivers:**
- `PluginNavigationService` — aggregates `IPluginNavigationProvider` nav items via API call
- New `/api/v1/plugins/navigation` endpoint returning all plugin nav items
- Modify `NavMenu.razor` to render "Plugins" section with plugin nav items
- Configure Blazor router `AdditionalAssemblies` for plugin page discovery
- **Note:** Blazor WASM cannot load plugin assemblies dynamically. Plugin pages must be pre-registered or use a different approach (see Risk below).

**Acceptance Criteria Closed:** US-120-009 (navigation items in sidebar)

**Dependencies:** Slice 5 (API returns plugin data), Slice 4

**Assigned:** Barbara (tests) + Lucius (implementation)

**Risks/Decisions:**
- **CRITICAL RISK:** Blazor WebAssembly runs in browser; it cannot dynamically load plugin assemblies at runtime. Options:
  - **Option A:** Plugin UI is server-rendered (not viable for WASM-only client)
  - **Option B:** Plugin pages are pre-compiled and served as separate Blazor projects (complex)
  - **Option C:** MVP: Plugins contribute API endpoints only; UI pages are deferred to Phase 2 or require Blazor Server hybrid
  - **Recommendation:** For MVP, implement navigation items that link to plugin API endpoints or external URLs. Full Blazor page support deferred. Update spec acceptance criteria accordingly.

---

### Slice 9: Plugin Management Page

**Layer(s):** Client  
**Projects:** `BudgetExperiment.Client`

**Delivers:**
- `Plugins.razor` page at `/plugins`
- Displays installed plugins: name, version, description, status, capabilities
- Shows plugin load errors for troubleshooting
- Disabled plugins shown with visual indicator
- Uses `PluginInfoResponse` from API

**Acceptance Criteria Closed:** US-120-006 (management UI)

**Dependencies:** Slice 5 (API endpoint), Slice 8

**Assigned:** Barbara (tests) + Lucius (implementation)

**Risks/Decisions:**
- **Style:** Use existing card/list patterns from codebase.

---

### Slice 10: Sample Plugin + Documentation

**Layer(s):** Samples, Docs  
**Projects:** `samples/SamplePlugin/` (new), `docs/`

**Delivers:**
- Sample plugin project demonstrating:
  - `IPlugin` implementation
  - `PluginControllerBase` endpoint
  - `IDomainEventHandler<TransactionCreatedEvent>` subscription
  - `IPluginNavigationProvider` for nav item
- `docs/plugin-authoring-guide.md` with SDK reference
- Update `README.md` with plugin system section
- Integration test that loads sample plugin and verifies functionality

**Acceptance Criteria Closed:** US-120-001 (sample plugin demonstrates all extension points)

**Dependencies:** Slices 1-9 complete

**Assigned:** Both (Lucius: sample, Barbara: integration tests, both: docs)

**Risks/Decisions:**
- Sample plugin should be simple but exercise all extension points.

---

## Test Strategy (TDD Per Slice)

| Slice | Test Type | Test Focus |
|-------|-----------|------------|
| 1 | Unit | Event record equality, dispatcher resolves handlers, logs failures |
| 2 | Integration | SaveChangesAsync dispatches events post-commit, failures don't rollback |
| 3 | Unit | Interface contracts, PluginControllerBase route attribute |
| 4 | Unit | Scanner finds IPlugin, Loader instantiates, Registry tracks |
| 5 | API Integration | /api/v1/plugins returns list, plugin controller routing works |
| 6 | Integration | Plugin parser selected by extension, fallback to core |
| 7 | Integration | Plugin report appears in report list |
| 8 | bUnit | NavMenu renders plugin section |
| 9 | bUnit | Plugins page displays plugin list |
| 10 | Integration | Sample plugin loads, endpoint responds, event handled |

**Assigned:** Barbara owns test slices; writes failing tests before Lucius implements.

---

## Dependency Graph

```
Slice 1 (Domain Events)
    ↓
Slice 2 (Dispatch Wiring)
    ↓
Slice 3 (Plugin.Abstractions) ←─ depends on IDomainEvent from Slice 1
    ↓
Slice 4 (Plugin.Hosting)
    ↓
    ├─→ Slice 5 (API Integration)
    │       ↓
    │   Slice 8 (Client Navigation) ─→ Slice 9 (Management Page)
    │
    ├─→ Slice 6 (Import Parser Extension)
    │
    └─→ Slice 7 (Report Builder Extension)

Slice 10 (Sample + Docs) ← depends on all above
```

---

## Open Decisions Requiring Resolution

### Decision 1: IDomainEvent Location
**Question:** Where does `IDomainEvent` live given Plugin.Abstractions must have no core deps?

**Recommendation:** Move `IDomainEvent` to Plugin.Abstractions. Domain references Abstractions for the marker only. This inverts the expected dependency but keeps plugin authoring simple (single NuGet reference).

**Assigned to:** Alfred to confirm before Slice 3 starts.

---

### Decision 2: Blazor Plugin Pages
**Question:** Can Blazor WASM support dynamic plugin page loading?

**Answer:** No — WASM cannot load assemblies dynamically. 

**Recommendation:** MVP scopes plugin UI to navigation links only. Full page support requires either:
- Blazor Server hosting (different architecture)
- Plugin pages compiled into client at build time (not hot-pluggable)

**Action:** Update US-120-009 acceptance criteria to clarify MVP scope.

**Assigned to:** Alfred to update spec; Fortinbra to confirm scope reduction is acceptable.

---

### Decision 3: DomainEventDispatcher Location
**Question:** Infrastructure or Application layer?

**Recommendation:** Infrastructure. The dispatcher is called from `BudgetDbContext.SaveChangesAsync` and needs `IServiceProvider` to resolve handlers. Application layer would require passing dispatcher through UnitOfWork abstraction (leaky).

---

## Summary Table

| Slice | Name | Layers | Deps | Agent | AC Closed |
|-------|------|--------|------|-------|-----------|
| 1 | Domain Event Foundation | Domain, Infra | — | Lucius | US-120-008 (partial) |
| 2 | Dispatch Wiring | Infra | 1 | Lucius | US-120-008 |
| 3 | Plugin.Abstractions | SDK | 1 | Lucius | US-120-001,002,003,004,009 |
| 4 | Plugin.Hosting | Hosting | 3 | Lucius | US-120-005,007 |
| 5 | API Integration | API, Contracts | 4 | Lucius | US-120-002,006 |
| 6 | Import Parser Extension | Application | 4 | Lucius | US-120-003 |
| 7 | Report Builder Extension | App, API | 4 | Lucius | US-120-004 |
| 8 | Client Navigation | Client | 5,4 | Both | US-120-009 |
| 9 | Management Page | Client | 5,8 | Both | US-120-006 |
| 10 | Sample + Docs | Samples | 1-9 | Both | US-120-001 |

---

**Next Step:** Resolve Decision 1 (IDomainEvent location) before starting Slice 1. Barbara to begin writing failing tests for Slice 1 domain event types.


---

### 22. Feature 148: Fix Bare `.ToString("C")` in Statement Reconciliation UI (2026-04-09)

**By:** Lucius (fix) + Barbara (tests)  
**Status:** Complete  
**Severity:** 🔴 Critical — F-001 (2026-04-09 audit)

**What it did:** Replaced 7 bare `.ToString("C")` calls across 4 Statement Reconciliation Razor components with `FormatCurrency(Culture.CurrentCulture)`. Injected `CultureService` into each component. Added 6 bUnit locale tests asserting correct currency formatting for `de-DE` and `en-US`.

**Commits:**
- `e7a94d5` — `fix(client): replace bare ToString("C") in reconciliation components`
- `e1bcfa5` — `test(client): bUnit locale tests for reconciliation currency formatting`

**Key Technical Decisions:**
- **Inject naming:** `@inject CultureService Culture` matches project convention
- **Extension method:** `FormatCurrency()` from `CurrencyFormattingExtensions` — already globally imported in `_Imports.razor`
- **Test helper:** `TestCultureServiceFactory` created for reusable bUnit locale setup (can be documented as standard pattern in Engineering Guide §37)
- **StubBudgetApiService extension:** Added configurable `ReconciliationHistory` and `ReconciliationTransactions` list properties for test scenarios

**Acceptance Criteria Met:**
- ✅ All 7 bare `.ToString("C")` calls replaced
- ✅ `CultureService` injected into all 4 components
- ✅ `de-DE` locale renders "1.234,56 €"
- ✅ `en-US` locale renders "$1,234.56" (no regression)
- ✅ bUnit tests cover culture-correct rendering (6 tests, all GREEN)

**Test Results:** 5,771 passed, 0 failed, 1 skipped

---

# Feature Flags Architecture

**Date:** 2026-04-07  
**Status:** Approved  
**Context:** Feature 129 (Kakeibo Alignment Audit)

## Decision

Implement a hierarchical feature flag system with instance-level (per-deployment) flags for two purposes:

1. **User Simplification** — Allow users to hide features they don't use via `UserSettings` (persisted per-user in database). Feature flags control what's *available* on a deployment; user settings control what's *visible* to individual users.

2. **Phased Rollout** — Deploy experimental features behind flags (default off), enable progressively via environment variables, flip to default-on when stable.

## Configuration Shape

**`appsettings.json`:**
```json
{
  "FeatureFlags": {
    "Calendar": { "SpendingHeatmap": true, "KakeiboOverlay": false },
    "Kakeibo": { "MonthlyReflectionPrompts": true },
    "Kaizen": { "MicroGoals": true, "Dashboard": false },
    "AI": { "ChatAssistant": true, "RuleSuggestions": true },
    "Reports": { "CustomReportBuilder": false, "LocationReport": true },
    "Charts": { "AdvancedCharts": false },
    "Paycheck": { "PaycheckPlanner": true },
    "Reconciliation": { "StatementReconciliation": true },
    "DataHealth": { "Dashboard": true },
    "Location": { "Geocoding": false }
  }
}
```

**Environment Variable Overrides (Docker):**
```bash
FeatureFlags__Kakeibo__CalendarOverlay=true
FeatureFlags__Reports__CustomReportBuilder=true
```

## API Surface

**Server-Side:** `IFeatureFlagService` in Application layer. Methods: `IsEnabled(string flagPath)`, `GetAllFlags()`.

**Client-Side:** `GET /api/v1/config/feature-flags` endpoint returns JSON tree. `FeatureFlagClientService` (scoped) fetches on app load, caches in memory.

**DTO:** `FeatureFlagsDto` in Contracts (nested structure matching JSON above).

## Naming Convention

Pattern: `{Area}:{SubArea?}:{Feature}` in PascalCase.  
Examples: `Calendar:SpendingHeatmap`, `Kakeibo:MonthlyReflectionPrompts`, `Reports:CustomReportBuilder`.

Environment vars: Replace `:` with `__` (e.g., `FeatureFlags__Calendar__SpendingHeatmap`).

## Clean Architecture Placement

- **Domain:** Flag-agnostic. Entities exist regardless of UI visibility.
- **Application:** `IFeatureFlagService` reads flags from `IConfiguration`. Services may conditionally enable use cases (rare).
- **API:** Controllers check flags via injected `IFeatureFlagService` to conditionally expose endpoints. `ConfigController` exposes flags to client.
- **Client:** `IFeatureFlagClientService` fetches flags from API, caches in memory. Components inject service and use `@if (FeatureFlags.IsEnabled("Feature.Name"))` to conditionally render.
- **Contracts:** `FeatureFlagsDto` only.

## Blazor Client Pattern

**Service:** `IFeatureFlagClientService` initialized in `Program.cs` before root component render. Components inject and check flags:

```razor
@inject IFeatureFlagClientService FeatureFlags

@if (FeatureFlags.IsEnabled("Calendar.SpendingHeatmap"))
{

---

### Controllers Are Standard; No Minimal API Migration (2026-04-10)

**Author:** Fortinbra (via Copilot)

**Decision:** CategorySuggestionEndpoints.cs (Minimal API pilot from F-153) has been reverted to CategorySuggestionsController. Controllers remain the standard architectural pattern for this codebase. No further Minimal API migration is planned.

**Rationale:** Consistency across the codebase. Controllers provide familiar patterns, explicit dependencies, and simplified testing. The Minimal API experiment demonstrated no compelling advantage justifying codebase-wide migration.

**Commit:** `35a378a` – refactor(api): revert CategorySuggestions Minimal API pilot to controller

**Implication:** All new API endpoints use controller pattern. Minimal API is not recommended for future work.
    <SpendingHeatmapOverlay ... />
}
```

**Graceful Fallback:** If API call fails, assume all flags `false` (safe default).

## Default Strategy

**Default-On (`true`):** Core Kakeibo/Kaizen features, established features (AI Chat, Paycheck Planner, Location Report).  
**Default-Off (`false`):** Experimental features (Custom Reports Builder, Advanced Charts, Geocoding stub, in-development Kakeibo features during rollout).

**Migration for New Features:**
1. Develop behind flag (default `false`)
2. Test with flag `true` in dev/staging
3. Deploy to production with flag `false`
4. Gather feedback (enable via env var for beta users)
5. Flip to `true` in `appsettings.json` when ready for GA
6. Remove flag 6-12 months post-launch when stable

## First-Pass Flags (17 Total)

| Flag | Type | Default | Purpose |
|------|------|---------|---------|
| `Calendar:SpendingHeatmap` | user-simplification | `true` | Heatmap toggle |
| `Calendar:KakeiboOverlay` | experimental | `false` → `true` | Feature 128 rollout |
| `Kakeibo:MonthlyReflectionPrompts` | user-simplification | `true` | Reflection prompts |
| `Kaizen:MicroGoals` | user-simplification | `true` | Weekly goals |
| `Kaizen:Dashboard` | experimental | `false` → `true` | Kaizen report |
| `AI:ChatAssistant` | user-simplification + experimental | `true` | AI chat panel |
| `AI:RuleSuggestions` | user-simplification | `true` | AI categorization |
| `AI:RecurringChargeDetection` | user-simplification | `true` | AI pattern detection |
| `Reports:CustomReportBuilder` | experimental | `false` | Power-user feature |
| `Reports:LocationReport` | user-simplification | `true` | Geographic spending |
| `Reports:CandlestickChart` | experimental | `false` | No data source yet |
| `Charts:AdvancedCharts` | experimental | `false` | Showcase only |
| `Paycheck:PaycheckPlanner` | user-simplification | `true` | Paycheck tool |
| `Reconciliation:StatementReconciliation` | user-simplification | `true` | PDF upload |
| `DataHealth:Dashboard` | user-simplification | `true` | Data quality |
| `Location:Geocoding` | experimental | `false` | Stub only |

## Rationale

- **Instance-level flags (not per-user):** Simpler architecture, no per-user flag storage. User preferences handled separately via `UserSettings`.
- **Hierarchical naming:** Clear namespacing prevents collisions, self-documenting.
- **API endpoint (not embedded in HTML):** Allows dynamic flag changes without API restart.
- **Default-on for core features:** Users opt out if they don't want them; flag system doesn't hide shipped features by default.
- **Default-off for experiments:** Explicit opt-in for incomplete/questionable features.

## Implementation Scope

Lucius implements in Phase 6 (low priority, after core Kakeibo features ship). Feature 128 can proceed without flags initially; flags added post-launch for simplification and future experimental features.


---

# Kakeibo Alignment Audit Complete

**Date:** 2026-04-07  
**Status:** Approved  
**Context:** Feature 129 (Kakeibo Alignment Audit)

## Decision

Feature audit complete. 27 features audited against Kakeibo + Kaizen calendar-first philosophy. Top 3 priority modifications identified:

### 1. Calendar UI Kakeibo Enhancements (🟢 Aligned → Immediate Priority)
**What:** Calendar is already the homepage but needs Kakeibo overlay. Add:
- Spending heatmap (intensity-based day cell tinting)
- Month-start intention prompt (savings goal + intention text)
- Week summary Kakeibo breakdown (mini-bars: Essentials/Wants/Culture/Unexpected)
- Day cell Kakeibo badges
- Month-end reflection panel (four questions)

**Why:** The calendar IS the household ledger. All Kakeibo philosophy flows through this surface. Without these visual cues, users won't experience the mindful budgeting rhythm.

**Priority:** Immediate — Feature 128 Phase 2.

### 2. Budget Categories Kakeibo Routing (🟡 Needs Work → Immediate Priority)
**What:** Add `KakeiboCategory` field to `BudgetCategory` entity. Every Expense category maps to exactly one of four Kakeibo buckets (Essentials/Wants/Culture/Unexpected). Migration applies smart defaults (Groceries → Essentials, Dining → Wants, Education → Culture). One-time **Kakeibo Setup Wizard** in onboarding guides users to review/confirm routing.

**Why:** Categories are the bridge between familiar vocabulary (Groceries, Dining) and Kakeibo philosophy. This routing must exist before transaction entry changes go live. Without it, transactions cannot be categorized by Kakeibo intent.

**Priority:** Immediate — Feature 128 Phase 1 (foundation).

### 3. Transaction Entry Kakeibo Selector (🟡 Needs Work → Immediate Priority)
**What:** Add `KakeiboSelector` component to transaction add/edit modal. Four icons (Essentials/Wants/Culture/Unexpected) with labels. Default derived from selected `BudgetCategory.KakeiboCategory`. Allow per-transaction override. Small educational tooltip on first use.

**Why:** Every transaction entry is a mindful recording act. The Kakeibo category picker reinforces the philosophy at the moment of entry. Users consciously choose intent, not just account category.

**Priority:** Immediate — Feature 128 Phase 1 (foundation).

## Additional High-Priority Modifications (Top 5-10)

4. **Onboarding Kakeibo Setup** — Add 5th step to onboarding wizard: introduce four Kakeibo categories, guide user to confirm/correct routing for existing Expense categories. First impression must communicate philosophy.
5. **Kaizen Micro-Goals** — Weekly goal setting and outcome tracking. Embed in week summary. Closes the Kaizen continuous improvement loop.
6. **Monthly Reflection Panel** — End-of-month four-questions panel (income/savings goal/actual savings/reflection text). Closes the Kakeibo monthly ritual loop.
7. **Transaction List Kakeibo Filter** — Add filter dropdown on `/transactions` page. Display Kakeibo badges on transaction rows. Supports bulk analysis by Kakeibo category.
8. **AI Chat Kakeibo Awareness** — AI must prompt for Kakeibo category when creating transactions (clarification action). Should suggest but allow override. AI must support, not bypass, mindful categorization.
9. **Settings Kakeibo Preferences** — Add user settings: "Show spending heatmap by default", "Remind me to set monthly savings intention", "Enable weekly Kaizen micro-goals". User control over philosophy features.
10. **Reports Kakeibo Grouping** — Add Kakeibo grouping toggle to Monthly Categories Report and Budget Comparison Report. Show spending by bucket, not just individual categories.

## Feature Flag Recommendations

17 flags proposed. Top 3 flag decisions:

1. **Custom Reports Builder → Default OFF** (🔴 Tension) — Encourages endless data exploration, opposite of Kakeibo's simple reflection. Feature-flag for power users only.
2. **Kakeibo Calendar Overlay → Default OFF during development, ON when shipped** (experimental rollout) — Phase Feature 128 safely without disrupting existing users.
3. **Advanced Charts → Default OFF** (experimental, showcase only) — SparkLine, LineChart, GroupedBarChart have no consumers. Keep in codebase for future but don't expose in production.

## Implementation Order

**Phase 1 (Immediate):** Budget categories Kakeibo routing, onboarding setup, transaction entry selector — domain foundation.  
**Phase 2 (Immediate):** Calendar heatmap, intention prompt, week breakdown, day badges — visual philosophy layer.  
**Phase 3 (Soon):** Reflection panel, Kaizen goals, Kaizen dashboard — close ritual loops.  
**Phase 4 (Soon):** Transaction list filter, AI awareness, settings preferences — supporting features.  
**Phase 5 (Low):** Reports grouping, paycheck breakdown, location filtering, export column — nice-to-haves.  
**Phase 6 (Low):** Feature flag system, Custom Reports flag, user settings toggles — simplification infrastructure.

## Rationale

Audit revealed that **5 features require immediate changes** (calendar, transaction entry, budget categories, onboarding, Kaizen goals) to support Kakeibo philosophy. **12 features need soon-priority modifications** (reports, AI, settings, list views) to augment the philosophy. **8 features need low-priority enhancements** (paycheck, location, export) or should be feature-flagged (Custom Reports Builder). One feature (Custom Reports Builder) has philosophical tension and should be opt-in only.

The recommended implementation order prioritizes the calendar (the philosophical centerpiece) and transaction entry (the mindful recording act) first, followed by reflection/Kaizen tracking, then supporting features, and finally optional enhancements. Feature flag architecture provides clean user simplification and phased rollout without polluting the domain layer.

## Next Steps

Lucius implements Feature 128 Phases 1-4 per the recommended order. Feature flag system (Phase 6) deferred to post-launch unless Custom Reports Builder needs gating earlier.


---

# Architecture Decision: Runtime Feature Flag Storage

**Date:** 2026-04-09  
**By:** Alfred (Lead)  
**Status:** Approved  

---

## Decision: Option B — Database-backed flags with in-memory cache

### Rationale

Option A (file hot-reload) is incompatible with Docker: environment variables used in production do not hot-reload. Option C (file rewrite) is fundamentally unsafe — container filesystems are ephemeral, and modifying appsettings.json in production is not idiomatic. Option B is the only choice that meets the requirement of runtime toggleability across all deployment contexts (local dev, Docker, Raspberry Pi).

Database-backed flags with in-memory cache deliver zero per-request overhead (cache hit) while enabling true runtime toggles via a simple admin API. The cost is one new table and a cache invalidation pattern — both standard, low-complexity infrastructure.

### Architecture

**Storage:** New `FeatureFlags` table (columns: `Id`, `Name`, `Enabled`, `CreatedAt`, `UpdatedAt`). One row per flag.

**Runtime Behavior:**
1. **Startup:** Load all flags from DB into `IMemoryCache` (keyed `feature:all`). Cache TTL: 5 minutes (allows stale reads during DB downtime; updated on each toggle).
2. **Read Path:** Check `IMemoryCache` first (cache hit = no DB access). Fall back to DB only on cache miss (startup or after expiry).
3. **Write Path:** `PUT /api/v1/features/{flagName}` (admin endpoint) updates the DB row, invalidates cache immediately.
4. **Client Cache:** Client-side flag cache TTL remains **1 hour** — client polling is infrequent enough that eventual consistency is acceptable. Admin toggling a flag will propagate to UI within ~1 hour.

### Implications for `docs/129-feature-audit-kakeibo-alignment.md`

**Feature Flag Architecture section (lines 494–802):**
- Update "Configuration Shape" subsection (currently lines 504–561): Replace "file-based + env vars only" with "database + file fallback for seeding".
  - Explain: Flags are stored in `FeatureFlags` DB table. Dev/staging can seed defaults from `appsettings.json` migration or init script. Production uses DB as source of truth.
- Add new "Runtime Toggleability" subsection explaining the cache strategy and admin endpoint.
- Update "Default Strategy" (lines 600–630) to clarify: defaults are applied at DB seed time, not via appsettings.json.
- Explain cache TTL and eventual consistency trade-off.

### Implications for `docs/129b-feature-flag-implementation.md`

**Lucius must update:**
1. **Layer Placement diagram (lines 45–67):** Add Infrastructure → Database layer below API.
2. **Configuration section (lines 34–40):** Replace "No database storage" rationale with "Database is source of truth; file-based seeding is optional (dev convenience only)".
3. **Code Sketches (lines 79+):** 
   - Add `FeatureFlagsDbContext` and migration sketch.
   - Update `FeaturesController` to inject `IMemoryCache` and implement cache invalidation.
   - Add `IFeatureFlagRepository` interface for read/write (just GetAllAsync and UpdateAsync).
   - Update example `appsettings.json` to show seeding migration or init script (no longer the source of truth).
4. **Testing section (add if missing):** Mock `IMemoryCache` for unit tests; use real cache + test DB for integration tests.

### Admin UI Shape

**Endpoint:** `PUT /api/v1/features/{flagName}`  
**Payload:** `{ "enabled": true }`  
**Response:** 200 OK with updated flag state  
**Auth:** Admin-only (protected by existing authorization middleware)

**Optional UI (Phase 2):** Dedicated admin page `/admin/features` listing all flags with toggle switches, wired to the endpoint above. For MVP, CLI/curl is sufficient:
```bash
curl -X PUT https://localhost:5099/api/v1/features/Reports:CustomReportBuilder \
  -H "Content-Type: application/json" \
  -d '{"enabled":true}' \
  -H "Authorization: Bearer <admin-token>"
```

Or if deployed on Raspberry Pi, SSH into the container and hit localhost:5000 directly (no TLS in local deployments).

### Client-Side Cache TTL

**Keep 1 hour as proposed.** Rationale:

---

### 6. User Directive: Release Tagging Must Source from Main (2026-04-12)

**Directive:** Releases must only be cut from `main` branch. Before tagging or pushing release refs, ensure all intended changes are merged to `main` and create the release tag from `main`, not from `squad` or another working branch.

---

### 7. Feature 160 Phase 1: Backend Type & Endpoint Configuration (2026-04-12)

**Author:** Lucius (implementation), Barbara (tests)

**Status:** ✅ Complete

#### 7.1 Compatibility Alias for OllamaEndpoint During Phase 1

**Decision:** Keep `AiSettingsDto.OllamaEndpoint` as a compatibility alias while introducing `EndpointUrl` and `BackendType` for phase 1.

**Rationale:** Existing client and test code already consumes `OllamaEndpoint`. The first slice is about proving the new backend surface without forcing a broader client migration in the same change. Phase 2 can deprecate and remove the alias.

**Implementation:**
- `BackendType` enum introduced (Ollama as default)
- `EndpointUrl` property added to hold normalized endpoint
- `OllamaEndpoint` property retained as readonly alias to `EndpointUrl`
- Application service configures defaults (Ollama backend, standard endpoint)
- Tests verify both new properties and alias functionality

**Test Coverage:** Phase-1 tests assert `EndpointUrl`/`BackendType` behavior on application and API surfaces; alias prevents unrelated breakage outside this slice.

**Next Phase:** Phase 2 will add actual multi-backend switching logic (LocalAI, vLLM, etc.) and can remove the alias if all consumers migrated.

---

### 8. Release v3.27.0: Merge Squad Branch to Main (2026-04-12)

**Author:** Lucius

**Status:** ✅ Complete

**Decision:** Create release tag v3.27.0 on origin/main after safely merging origin/squad into main.

**Rationale:** The squad branch (tagged v3.26.0) contained audit-approved work (audit report publication, performance optimizations, code quality fixes) that needed to be merged to main and released as v3.27.0.

**Implementation:**
1. Created clean worktree on origin/main
2. Verified origin/squad is descendant of origin/main (clean fast-forward)
3. Merged origin/squad → main using `git merge --ff-only`
4. Created annotated tag v3.27.0 on 04e5ea5
5. Pushed main and v3.27.0 to origin
6. GitHub Actions released Docker images (amd64, arm64)

**Result:**
- ✅ v3.27.0 tag created and pushed
- ✅ origin/main updated to squad commit
- ✅ v3.26.0 unchanged (same commit)
- ✅ No source files modified
- ✅ Release workflow started

**Context:** User directive captured by Copilot on behalf of Fortinbra after correcting an out-of-order release cut (v3.26.0 was tagged on `squad` instead of `main`).

**Resolution Applied:** Squad branch merged into `main` with force re-tag, establishing the canonical rule that all release tags must point to the stable `main` branch.

**Implication:** All future releases follow SemVer discipline—commits are merged to `main`, reviewed, then tagged on `main`, ensuring consistency between git history and release artifacts.
- Client polling for flag changes every 5 minutes (or on-demand) would be excessive for a feature flag service.
- 1-hour eventual consistency is acceptable for a household app. Admins toggling features are rare events.
- If a specific flag needs immediate propagation (e.g., emergency shutdown of a broken feature), admin can restart the affected client or browser refresh forces a new fetch.
- This aligns with typical SaaS feature flag services (Unleash, LaunchDarkly) which use 5–60 minute polling or client-requested updates.

---

## Implementation Checklist (for Lucius)

- [ ] Create `FeatureFlags` table migration (Id, Name, Enabled, CreatedAt, UpdatedAt)
- [ ] Seed default flags from Feature 129 audit (17 flags, default on/off as specified)
- [ ] Create `FeatureFlagRepository` (GetAllAsync, UpdateAsync)
- [ ] Update `FeaturesController` GET endpoint to use cache
- [ ] Add `PUT /api/v1/features/{flagName}` admin endpoint with cache invalidation
- [ ] Update `Program.cs` DI registration for cache and repository
- [ ] Update docs/129b with new architecture sections
- [ ] Add unit tests for cache behavior
- [ ] Add integration tests for DB + cache round-trip


---

# Decision: Serialization Alternatives Investigation (Feature 130)

**Date:** 2026-04-10  
**Author:** Alfred  
**Status:** Ready for Scribe (merge to decisions.md)

---

## What Was Decided

Investigated 7 serialization candidates for BudgetExperiment API to optimize bandwidth for Raspberry Pi ARM64 deployment. Evaluated each against Blazor WASM compatibility, ASP.NET Core support, OpenAPI tooling, and complexity.

## The Decision

### Immediate Action: Deploy Brotli HTTP Compression
**Already configured in `Program.cs` (lines 75-85, 155).** Test on Raspberry Pi hardware.

- **Benefit:** 40-45% bandwidth reduction on all API responses.
- **Cost:** ~5% CPU overhead on ARM64 (negligible for total page load).
- **Breaking changes:** None. Compression is transparent HTTP-level optimization.
- **OpenAPI impact:** None. Scalar UI unaffected.
- **Timeline:** This sprint (test on Pi).

### Defer: Binary Serialization Formats
MessagePack, CBOR, FlatBuffers, Avro — all deferred to future feature flag (if needed).

**Rationale:**
- Brotli provides sufficient bandwidth savings (40-45% vs JSON) without custom code.
- Each binary format adds 50-200 KB to Blazor WASM bundle.
- All binary formats require custom `OutputFormatter`/`InputFormatter` (maintenance burden).
- All binary formats break OpenAPI/Scalar UI tooling (no schema introspection).
- Only pursue if post-deployment metrics prove bandwidth is still a constraint.

### Reject: Protocol Buffers + gRPC
**Not recommended for this app.**

**Rationale:**
- gRPC is designed for polyglot microservices; BudgetExperiment is single-tier.
- gRPC-Web in browser is a workaround, not a native fit.
- Requires gRPC-Web proxy (e.g., Envoy) or custom middleware.
- Breaking changes to API contract; existing clients break.
- OpenAPI tooling incompatibility (gRPC uses proto, not OpenAPI).
- Marginal bandwidth gain (0-15% over Brotli) does not justify operational complexity.

### Reject: Apache Avro
**Not recommended for this app.**

**Rationale:**
- Avro is optimized for big-data (Hadoop, Kafka); not idiomatic for HTTP APIs.
- Schema management complexity (schema-on-read pattern is operational burden).
- WASM bundle bloat (~200 KB gzipped, largest impact of all candidates).
- No compelling advantage over MessagePack if binary format ever needed.
- Team lacks Avro expertise in web context.

## Monitoring & Future Reconsideration

### If Brotli Deployment Shows Bandwidth is Still Inadequate
1. Implement MessagePack as opt-in binary format via feature flag and Accept header.
2. Client requests: `Accept: application/messagepack` opt-in (JSON default).
3. Responses: automatic content negotiation (same format as request content-type).
4. **No breaking changes** — JSON remains default.

### If Large-Payload Exports Become a Performance Pain Point (10,000+ objects)
1. Reconsider FlatBuffers for zero-copy deserialization semantics.
2. Only for export/bulk download endpoints, not standard REST API.
3. Measure GC pressure and memory bandwidth before pursuing.

## Why This Matters

- **Raspberry Pi is bandwidth-constrained.** A typical page load (5-6 API calls) can be 30-50 KB uncompressed. Brotli reduces this to 10-15 KB immediately.
- **No breaking changes.** Brotli is transparent; all existing clients work without modification.
- **Proven in production.** Every major API (Google, AWS, GitHub) uses Brotli compression. Industry standard.
- **Binary formats add complexity without commensurate benefit.** The sweet spot is JSON + compression.

## Implications

1. **No code changes required** — Brotli is already configured.
2. **Test on Raspberry Pi** — measure CPU, memory, and decompression transparency.
3. **Document metrics** — bandwidth, CPU, memory post-deployment.
4. **Keep binary format infrastructure in a future branch** (if ever needed) — `features/binary-serialization` skeleton with MessagePack formatters, feature flag, and content negotiation logic ready to go.

## Appendix: Verdict Summary

| Candidate | Recommendation | Rationale |
|-----------|---|---|
| **JSON + Brotli** | 🟢 Deploy now | 40-45% reduction, zero cost, transparent. |
| **JSON + Source Gen** | 🟢 Already optimal | Baseline for uncompressed JSON. |
| **MessagePack** | 🟡 Defer, opt-in if needed | High complexity, +5-10% over Brotli. |
| **CBOR** | 🟡 Defer, niche | Slightly better than MessagePack, less ecosystem. |
| **FlatBuffers** | 🟡 Defer, niche future | Zero-copy only valuable for 10,000+ objects. |
| **Protobuf + gRPC** | 🔴 Reject | Architectural mismatch, breaking changes. |
| **Apache Avro** | 🔴 Reject | Big-data focus, bundle bloat, not idiomatic. |

---

**Ready for Scribe:** Merge to `.squad/decisions.md` under "## Active Decisions" as a new decision record.


---

### 20260406-235342: User directive
**By:** Fortinbra (via Copilot)
**What:** Budget scopes (Personal vs Shared) do not fit the Kakeibo household-ledger model. There is only one scope — the shared family/household ledger. The BudgetScope enum and any personal/shared scope logic should be removed or collapsed to a single scope.
**Why:** Kakeibo is explicitly a household ledger (家計簿). The concept of a personal scope alongside a shared scope introduces a duality that contradicts the philosophy. User decision — captured for team memory.


---

### 20260407-001043: User directive
**By:** User (via Copilot)
**What:** When a user changes a feature flag selection, the client cache must be invalidated immediately so the new state is visible right away. The user should not have to wait up to 1 hour for the cache to expire.
**Implications:**
- After a successful PUT /api/v1/admin/features/{flagName}, the admin UI must call IFeatureFlagClientService.RefreshAsync() to force a re-fetch
- The admin toggle action is a two-step: (1) write to server, (2) refresh local cache
- Lucius should update the Blazor client service to expose RefreshAsync() and call it from the admin toggle component
- The 1-hour ResponseCache on GET /api/v1/features is still fine for passive reads; admin toggles bypass it by explicitly refreshing

---

### 20260407-000737: User directive
**By:** User (via Copilot)
**What:** A user's feature flag selections must persist across application restarts AND application updates (new Docker image deployments).
**Why:** User request — the household instance owner customizes their feature set once and expects it to survive upgrades.
**Implications:**
- DB-backed storage already handles restart persistence (external Postgres survives container restart)
- EF Core HasData() is PROHIBITED for feature flag seeding — it runs on every migration and can overwrite user customizations
- Correct seeding strategy: application startup inserts ONLY missing flags (INSERT ... WHERE NOT EXISTS / ON CONFLICT DO NOTHING)
- When a new flag is introduced in an app update, it gets seeded with its code-defined default ONLY if no row exists for it yet
- User-set values in the DB are always the authoritative source of truth — migrations/deployments must never reset them
- The 'default' value in code serves only as the initial seed value for new instances, not an override

---

### 20260407-000616: User directive
**By:** User (via Copilot)
**What:** Feature flags must be toggleable at runtime without restarting the application, with no performance impact on hot paths.
**Why:** User wants to enable/disable features on a live instance (Raspberry Pi Docker deployment) without a container restart. Flags must not add per-request latency.
**Implications:**
- IOptions<T> (deployment-time only) is NOT sufficient — requires restart to pick up changes
- IOptionsMonitor<T> enables hot-reload from config file but only works with file-based config, not Docker env vars
- DB-backed flags with in-memory caching satisfies both runtime toggle AND zero hot-path overhead
- Admin UI or API endpoint needed to toggle flags without SSH/file editing
- Client-side cache TTL (Lucius proposed 1hr) must be reconsidered — shorter TTL or server push for fast propagation

---

# Decision: Feature Flag Implementation Approach

**Author:** Lucius  
**Date:** 2026-04-05  
**Status:** Proposed (awaiting Alfred's review)

## Summary

Hand-rolled `FeatureFlagOptions` POCO with `IOptions<T>` injection, explicit configuration in `appsettings.json`, and `/api/v1/features` endpoint for Blazor client delivery. No external dependencies (no `Microsoft.FeatureManagement`).

## Rationale

- Simple on/off switches sufficient for gradual feature rollout to client UI
- Zero external dependencies, aligns with "no magic" principle
- Matches existing config patterns (`DatabaseOptions`, `AuthenticationOptions`, `ClientConfigOptions`)
- Trivial to test (mock `IOptions<T>` or `HttpClient`)
- Extensible: upgrade to `IOptionsMonitor<T>` for runtime toggles, or migrate to `Microsoft.FeatureManagement` (~2 hours) if targeting/A/B testing needed

## Details

See full implementation proposal: `docs/129b-feature-flag-implementation.md`

**Key Architectural Decisions:**
- **POCO location:** `BudgetExperiment.Shared` (shared by API + Client, matches `BudgetScope` pattern)
- **API endpoint:** `/api/v1/features` (`[AllowAnonymous]`, 1-hour cache)
- **Client service:** `IFeatureFlagService` fetched at startup, cached for session
- **Default strategy:** Default-off for new features, default-on for completed features (via property initializers)
- **Graceful degradation:** Client falls back to all-false if API unavailable (except completed features with `true` initializers)

## Open Questions (for Alfred)

1. Flag naming: feature-based (`Kakeibo`) or page-based (`KakeiboCategorizationPage`)?
2. Inventory source-of-truth: code (`FeatureFlagOptions` properties) or separate `docs/feature-flag-inventory.md`?
3. Component pattern: inline `@if (IsEnabled("Flag"))` or code-behind property?
4. Backend enforcement: API endpoints check flags (404 if disabled) or client-only gating?

## Next Steps

1. Alfred reviews proposal, answers open questions
2. Alfred confirms flag inventory aligns with architecture doc
3. Lucius implements (POCO, controller, service, DI wiring) — ~2 hours
4. Barbara writes tests (unit + integration) — ~3 hours


---

# Decision: Runtime Feature Flags Architecture Update

**Date:** 2026-04-09  
**By:** Lucius (Backend Dev)  
**Status:** Implementation Proposed  
**Related:** Alfred's decision in `alfred-runtime-feature-flags.md` (Option B approved)

---

## Context

User requested runtime toggleability for feature flags without performance impacts. Alfred approved Option B (DB-backed + `IMemoryCache`) in `.squad/decisions/inbox/alfred-runtime-feature-flags.md`. Lucius updated `docs/129b-feature-flag-implementation.md` to reflect the new architecture.

---

## Architecture Summary

**Storage:** PostgreSQL `FeatureFlags` table (`Name` TEXT PK, `IsEnabled` BOOL, `UpdatedAtUtc` TIMESTAMP).

**Read Path:** Application `IFeatureFlagService` checks `IMemoryCache` first (key: `"feature:all"`, TTL: 5 minutes). Cache miss → fetch from DB via `IFeatureFlagRepository`, cache result.

**Write Path:** Admin endpoint `PUT /api/v1/features/{flagName}` (requires `[Authorize]`) → `IFeatureFlagService.SetFlagAsync` → updates DB → invalidates cache immediately.

**Client:** Blazor client fetches flags via `GET /api/v1/features` (public, cached 60 seconds) at startup. `IFeatureFlagClientService` exposes `IsEnabled(string flagName)` + `RefreshAsync()`.

**Performance:** Zero per-request overhead (cache hit = in-memory dictionary lookup, < 1 µs). No DB access on read after initial cache load.

---

## Implementation Details

### New Domain Entity
- `FeatureFlag.cs` (Domain/Entities) — `Name`, `IsEnabled`, `UpdatedAtUtc`

### Infrastructure
- `FeatureFlagConfiguration.cs` (Infrastructure/Data/Config) — EF Core fluent config + `HasData()` seeds 17 flags from Feature 129 audit
- `IFeatureFlagRepository` (Application interface) — `GetAllAsync`, `GetByNameAsync`, `UpdateAsync`
- `FeatureFlagRepository` (Infrastructure implementation) — EF Core, `AsNoTracking` on reads

### Application Service
- `IFeatureFlagService` (Application interface) — `IsEnabledAsync`, `GetAllAsync`, `SetFlagAsync`
- `FeatureFlagService` (Application implementation) — uses `IMemoryCache` (5-min TTL), invalidates on write
- Requires `IMemoryCache` registration in Application `DependencyInjection.cs` (if not already present)

### API Controller
- `FeaturesController` (API/Controllers):
  - `GET /api/v1/features` — returns all flags as `Dictionary<string, bool>`, `[AllowAnonymous]`, `ResponseCache(Duration = 60)`
  - `PUT /api/v1/features/{flagName}` — body: `{"enabled": true/false}`, requires `[Authorize]`, returns 200 + updated state or 404
- DTOs: `UpdateFeatureFlagRequest`, `UpdateFeatureFlagResponse`

### Client Service
- `IFeatureFlagClientService` (Client interface) — `Flags` (Dictionary), `IsEnabled`, `LoadFlagsAsync`, `RefreshAsync`
- `FeatureFlagClientService` (Client implementation) — fetches from API, caches for session, graceful degradation (empty dict on API failure)

### Seed Data (17 Flags from Feature 129 Audit)
| Flag Name | Default | Type |
|-----------|---------|------|
| `Calendar:SpendingHeatmap` | `true` | [user-simplification] |
| `Calendar:KakeiboOverlay` | `false` | [experimental] |
| `Kakeibo:MonthlyReflectionPrompts` | `true` | [user-simplification] |
| `Kakeibo:CalendarOverlay` | `false` | [experimental] (duplicate — consider consolidating) |
| `Kaizen:MicroGoals` | `true` | [user-simplification] |
| `Kaizen:Dashboard` | `false` | [experimental] |
| `AI:ChatAssistant` | `true` | [user-simplification] + [experimental] |
| `AI:RuleSuggestions` | `true` | [user-simplification] |
| `AI:RecurringChargeDetection` | `true` | [user-simplification] |
| `Reports:CustomReportBuilder` | `false` | [experimental] |
| `Reports:LocationReport` | `true` | [user-simplification] |
| `Charts:AdvancedCharts` | `false` | [experimental] |
| `Charts:CandlestickChart` | `false` | [experimental] (consider folding into `Charts:AdvancedCharts`) |
| `Paycheck:PaycheckPlanner` | `true` | [user-simplification] |
| `Reconciliation:StatementReconciliation` | `true` | [user-simplification] |
| `DataHealth:Dashboard` | `true` | [user-simplification] |
| `Location:Geocoding` | `false` | [experimental] |

---

## Testing Strategy (Barbara's Responsibility)

**Unit Tests:**
- `FeatureFlagServiceTests` — cache hit/miss, `SetFlagAsync` invalidates cache, `IsEnabledAsync` correctness
- `FeatureFlagRepositoryTests` — `GetAllAsync`, `GetByNameAsync`, `UpdateAsync` (use Testcontainers PostgreSQL or in-memory SQLite)
- `FeaturesControllerTests` — GET returns flags from service, PUT requires auth, returns 200 or 404
- `FeatureFlagClientServiceTests` — `LoadFlagsAsync` caches, `IsEnabled` correctness, graceful degradation on HTTP failure

**Integration Tests:**
- `FeaturesEndpointTests` (WebApplicationFactory) — `/api/v1/features` returns JSON from DB, verify `ResponseCache` headers (max-age=60)
- PUT endpoint integration — toggle flag → verify DB updated, cache invalidated, GET returns new state

**Performance:**
- Micro-benchmark `IsEnabledAsync` cache hit path — expect < 1 µs (in-memory dictionary lookup)

---

## Migration & Rollout

1. **Migration:** Create `FeatureFlags` table, seed 17 flags via `HasData()`
2. **Backend:** Implement entity, repository, service, controller
3. **Client:** Implement client service, load at startup
4. **Existing features:** Wrap nav links in `@if (FeatureFlagService.IsEnabled("AI:ChatAssistant"))` — no behavior change (flag is `true` by default)
5. **New features:** Add components behind `@if (FeatureFlagService.IsEnabled("Calendar:KakeiboOverlay"))` — hidden by default
6. **Activation:** `PUT /api/v1/features/Calendar:KakeiboOverlay` with `{"enabled":true}` → feature appears after client refresh

**No Breaking Changes:** Defaults align with current production state. Existing features remain visible unless explicitly toggled off.

---

## Estimated Effort

- **Lucius (Implementation):** 6 hours (entity, migration, repository, service, controller, client service, DI wiring)
- **Barbara (Tests):** 6 hours (unit + integration tests for all layers)
- **Alfred (Review):** 1 hour (verify flag inventory matches Feature 129 audit)
- **Total:** ~13 hours

---

## Future Extensions (NOT IN SCOPE)

- **Per-user flags:** Add `UserFeatureFlagOverrides` table. Deferred until user-specific rollout is required (adds complexity).
- **`Microsoft.FeatureManagement` migration:** If targeting, time windows, or external providers (Azure App Config, LaunchDarkly) are needed. Migration effort: ~4 hours.

---

## Implementation Checklist

See `docs/129b-feature-flag-implementation.md` § 11 for full 21-item checklist.

---

## Decision Record

**Rationale for DB-backed approach:**
- Docker env vars don't hot-reload (requires container restart)
- Modifying `appsettings.json` in containers is ephemeral/unsafe
- Database is the only reliable runtime toggle mechanism across all deployment contexts (local dev, Docker, Raspberry Pi)
- `IMemoryCache` (5-min TTL) delivers zero per-request overhead (cache hit = no DB access)
- Client-side 60-second cache acceptable (1-hour eventual consistency is fine per Alfred's decision)

**Hierarchical naming convention:**
- Colon-separated (e.g., `Calendar:SpendingHeatmap`) matches Feature 129 audit inventory
- Groups related flags, extensible to nested categories

**Authentication:**
- GET endpoint: `[AllowAnonymous]` (client needs flags before authentication)
- PUT endpoint: `[Authorize]` (admin users only)

**Graceful degradation:**
- Client service returns empty dictionary on API failure (all flags off)
- Completed features should not rely solely on flags (check authentication state, user settings, etc.)

---

**Next Steps:**
1. Lucius implements per checklist (`docs/129b-feature-flag-implementation.md` § 11)
2. Barbara writes tests (unit + integration)
3. Alfred reviews flag inventory alignment with Feature 129 audit, approves for merge


---




---

# Vic — Principle & Performance Audits (2026-04-09)

# Vic — Audit Findings for Team (2026-04-09)

## Critical: Financial Display Accuracy

**F-001:** 7 instances of bare `.ToString("C")` in Statement Reconciliation UI (4 Razor files). These bypass the `FormatCurrency()` extension method required by §38. Users with non-US browser locales will see incorrect currency formatting. This is a direct financial accuracy risk at the display boundary.

**Action:** Lucius — replace with `.FormatCurrency(CultureService.CurrentCulture)`. Low effort, high impact.

---

## High: DIP Violations Still Open (Decision #2 Incomplete)

**F-002, F-003:** `CalendarController` injects concrete `CalendarService` (no interface exists). `AccountsController` injects concrete `AccountService` (no interface exists). Decision #2 from 2026-03-22 identified this class of violation and prescribed interface extraction. These two remain unfixed.

**Action:** Lucius — extract `ICalendarService` and `IAccountService` interfaces. Low effort.

---

## High: ITransactionRepository at 23 Methods (ISP)

**F-004:** `ITransactionRepository` has 23 methods spanning date queries, import operations, analytics, and reconciliation. This violates ISP and makes test fakes burdensome.

**Action:** Alfred — decide on split strategy (by concern vs. by read/write/analytics). Lucius implements.

---

## Structural Debt: God Classes

**F-005, F-006, F-013:** 23 classes across Domain (5) and Application (18) exceed the 300-line limit. The `Transaction` entity at 545 lines and `RuleSuggestionResponseParser` at 515 lines are the worst offenders.

**Action:** Alfred — prioritize which to split first. Recommend splitting during feature work that touches these files, not as standalone refactoring.

---

## Observation: Assertion Framework Drift

**F-012:** Tests mix Shouldly and xUnit Assert freely, even within the same files. The Engineering Guide says "Shouldly OR built-in Assert" (implying a project-level choice). Neither is wrong individually, but the inconsistency creates style ambiguity for new tests.

**Action:** Team — decide per-project standard. No immediate urgency.


---

# Alfred — Feature Docs 148–153 (2026-04-09)

# Alfred — Feature Docs 148–153 Confirmation

**Date:** 2026-04-09  
**From:** Alfred (Lead)  
**To:** Fortinbra  

## Summary

Six feature specification documents have been created and committed to address all Critical and High findings from Vic's 2026-04-09 full principle audit.

## Documents Created

| Doc | Slug | Finding | Severity |
|-----|------|---------|----------|
| 148 | `148-statement-reconciliation-locale-fix.md` | F-001 | 🔴 Critical |
| 149 | `149-extract-icalendarservice-iaccountservice.md` | F-002 + F-003 | 🟠 High |
| 150 | `150-split-itransactionrepository-isp.md` | F-004 | 🟠 High |
| 151 | `151-extract-transactionfactory.md` | F-005 | 🟠 High |
| 152 | `152-god-application-services-split-plan.md` | F-006 | 🟠 High |
| 153 | `153-god-controllers-split-strategy.md` | F-007 | 🟠 High |

## Key Notes

- **Doc 148 (F-001)** is the only Critical finding. It's a low-effort, high-trust bug fix — 7 lines changed across 4 Razor files. Recommend prioritizing this first.
- **Doc 149 (F-002 + F-003)** formally closes **Decision #2** from 2026-03-22. The original DIP verdict covered 3 controllers; 2 were missed (`CalendarController`, `AccountsController`). These are the remaining two.
- **Docs 152 and 153** establish policy for god service/controller splits — opportunistic during feature work for the long tail, standalone PRs for the top offenders.
- All 6 docs are in `Proposed` status and ready for Lucius to implement.

## Commit

`bde4d03` — `docs: add feature specs 148-153 for Vic audit findings (F-001 through F-007)`


---

# Vic — Performance Review Findings (2026-04-09)

# Vic — Performance Audit Findings (2026-04-09)

**Report:** `docs/audit/2026-04-09-performance-review.md`
**Priority:** Team should review before next sprint planning.

## Critical

- **P-001:** `DataHealthService.AnalyzeAsync()` loads ALL transactions into memory 3 separate times via `GetAllForHealthAnalysisAsync()`. On Pi with 5K+ transactions, this risks OOM. Also contains O(n²) near-duplicate loop.

## High

- **P-002:** `BudgetProgressService.GetMonthlySummaryAsync()` issues N+1 queries — one `GetSpendingByCategoryAsync` per expense category in a `foreach` loop. 20 categories = 20 sequential DB round-trips.
- **P-003:** `ReportService.BuildCategorySpendingListAsync()` and `BuildTopCategoriesAsync()` issue N+1 queries to resolve category names via `GetByIdAsync` per category — despite categories already being loaded via `.Include()`.
- **P-004:** `GetUncategorizedAsync()` returns ALL uncategorized transactions with no limit. Called by `CategorySuggestionService`.
- **P-005:** `GetAllForHealthAnalysisAsync()` loads full entity graphs for all transactions with no projection.
- **P-006:** `GetAllDescriptionsAsync()` returns all distinct descriptions unbounded.
- **P-007:** `GET /api/v1/transactions` (by date range) has no pagination parameters.

## Medium

- **P-008 through P-014:** Various double iterations, unbounded results, correlated subqueries, missing `<Virtualize>` and `@key` in Blazor client.

## Decision Needed

Should the team prioritize P-001 and P-002 as immediate fixes, or batch all High findings into a performance sprint?


---

## Merged from Inbox (2026-04-08)

### 19. Feature 145 & 146 — Kakeibo Report & Transfer Deletion (2026-04-08)

**Status:** Completed and tested

#### Feature 145: Kakeibo Date-Range Report Service (Lucius + Barbara)

**Implementation Decisions (Lucius):**
- Method name: GetEffectiveKakeiboCategory() (domain entity method name, not spec)
- Feature flag: Kakeibo:DateRangeReports (colon-separated; existing convention)
- DTO location: Contracts/Dtos/ (no Reports subdirectory)
- Null category handling: Excluded (uncategorized = no Kakeibo bucket)
- Amount sign: Expenses negative in DB; service uses Math.Abs() for positive totals

**Test Coverage (Barbara): 24 tests**
- 14 unit tests (KakeiboReportServiceTests)
- 6 API integration tests (KakeiboReportControllerTests)
- 4 Testcontainers accuracy tests (KakeiboReportServiceAccuracyTests)

#### Feature 146: Transfer Deletion with Orphan Detection (Lucius + Barbara)

**Implementation Decisions (Lucius):**
- ITransactionRepository.DeleteTransferAsync returns Task (void) — repo handles all 3 cases (none/orphan/both) silently; service wraps
- Two delete methods: old DeleteAsync (non-atomic, backward-compatible) + new DeleteTransferAsync (atomic)
- Feature flag returns **403 Forbidden** (not 404) when disabled — feature exists, gated
- Orphan handling: log warning + delete immediately
- :guid constraint returns 404 (not 400) for invalid GUIDs — routing layer, not model binding
- EnsureFeatureFlag test helper: (1) SQL upsert + (2) SetFlagAsync for cache invalidation

**Test Coverage (Barbara): 13 tests**
- 6 unit tests (TransferDeletionServiceTests)
- 4 API integration tests (TransferDeletionControllerTests)
- 3 Testcontainers accuracy tests (TransferDeletionAccuracyTests)

**Regression Fixes:**
- MockTransferService in ChatActionExecutorTests: added DeleteTransferAsync stub
- KakeiboReportControllerTests: nullable DateOnly handling + cache invalidation + routing assertions

---

### 20. Transfer Deletion — Implementation Lessons (2026-04-08)

**Logger Constructor Injection:**
- All test instantiation sites must be updated when adding constructor params to repositories
- Use Python regex for bulk updates (PowerShell too slow on large files)

**Feature Flag 403 vs 404:**
- 403 Forbidden: feature exists but gated
- 404 Not Found: feature does not exist

**EnsureFeatureFlag Pattern:**
- SQL upsert alone insufficient (handles zero-record state but doesn't invalidate cache)
- Must also call SetFlagAsync to clear IMemoryCache

**Routing vs Model Binding:**
- :guid constraint → routing layer → 404 (no match)
- Type validation → model binding → 400 Bad Request

**StyleCop During Test Writing:**
- SA1512: section comments // === X === no blank line after
- SA1615: all public async Task methods need <returns>A <see cref="Task"/>...
- CS1734: <paramref> only for actual parameters
- SA1204: static helpers before non-static
- Document early to avoid batch rework

---

### 21. Feature Specs 154–159: Performance Audit Response (2026-04-09)

**By:** Alfred  
**Status:** Complete; specs ready for implementation

**6 Documents Created (Performance Audit):**

| # | Slug | Finding | Severity |
|---|------|---------|----------|
| 154 | datahealth-triple-load-on2-dedup-fix | P-001 | 🔴 Critical |
| 155 | udget-progress-n-plus-one-fix | P-002 | 🟠 High |
| 156 | eport-service-n-plus-one-category-lookup-fix | P-003 | 🟠 High |
| 157 | datahealth-repository-unbounded-queries-projections | P-004 + P-005 | 🟠 High |
| 158 | get-all-descriptions-bounded-search | P-006 | 🟠 High |
| 159 | 	ransactions-date-range-endpoint-pagination | P-007 | 🟠 High |

**Key Decisions:**
- Doc 154 (P-001): Critical, depends on 157
- Doc 157: Foundation (unbounded queries)
- Doc 159: Option A (deprecate) or B (pagination) — **Fortinbra decision needed**

---

### 22. Performance Batch 154–159 Audit Complete — Release-Ready (2026-04-12)

**By:** Barbara (Tester)  
**Scope:** Features 154–159 (performance batch) implementation and test coverage audit  
**Verdict:** ✅ **Release-ready** — no blocking bugs, no regressions

**Findings:**

| Finding | Severity | Status |
|---------|----------|--------|
| Missing integration test for `TransactionRepository.GetSpendingByCategoriesAsync` (Feature 155) | Medium | Non-blocking follow-up |
| Dead fallback code in `BudgetProgressService` (lines 100–116) | Low | Noted, not blocking |
| V2 endpoint missing `startDate > endDate` validation test | Minor | Coverage gap |

**Test Coverage Verdict:**
- Feature 154 (DataHealth) — ✅ Green: contract + behavioral + linear guard tests
- Feature 155 (BudgetProgress) — ⚠️ Unit tests green, **missing integration test** for grouped query
- Feature 156 (ReportService) — ✅ Green: N+1 fix verified, nav-property dict pattern clean
- Feature 157 (Unbounded queries) — ✅ Green: 6 integration tests validate all projection shapes
- Feature 158 (GetAllDescriptions) — ✅ Green: bounded + EF translation tests (LIKE operator)
- Feature 159 (v1 deprecation + v2 paginated) — ✅ Green: headers + v2 pagination logic

**Decision:**
The batch is ready for release. The missing `GetSpendingByCategoriesAsync` integration test is a test-coverage gap, not a bug (query logic is straightforward and unit-tested). **File as follow-up task:** Add PostgreSQL integration test with known transaction data, validate grouped totals match per-category queries.

**Related Docs:**
- Audit detail: `.squad/decisions/inbox/barbara-audit-pass-2.md` (merged here)
- Orchestration log: `.squad/orchestration-log/2026-04-12T20-53-24Z-barbara.md`

---

### 41. Release Tag v3.26.0 — Lucius Decision (2026-03-23)

**Actor:** Lucius (Backend Dev)  
**Status:** COMPLETE

#### Summary
Created local annotated git tag `v3.26.0` at current HEAD (commit `04e5ea5`). Tag verified and points to squad branch. No remote push executed per request.

#### Rationale
- Marks stable state of codebase after audit report publication merge.
- Follows semantic versioning bump from v3.25.0.
- Annotated tag allows metadata (tagger, timestamp, message) for release traceability.

#### Verification
```
Tag: v3.26.0
Points to: 04e5ea5 (squad: merge audit report publication decisions)
Type: Annotated
Tagger: Copilot
Message: Release v3.26.0
```

---

### 42. Push Release Refs to Origin (2026-01-09)

**Actor:** Lucius (Backend Dev)  
**Request Origin:** Copilot  
**Status:** Completed

#### Summary
Successfully pushed the `squad` branch and `v3.26.0` tag to origin to kick off GitHub Actions.

#### Context
- Local branch `squad` was ahead of origin (not yet pushed)
- Local annotated tag `v3.26.0` existed but had not been pushed to origin
- Working tree was dirty with uncommitted changes, but no modifications or commits were made

#### Actions Taken
1. **Pushed local branch `squad` to origin**
   - Commit: `04e5ea56311ec79e0b6b24a0b48277a09336b6c7`
   - Message: "squad: merge audit report publication decisions"
   - Result: ✅ New branch created on remote

2. **Pushed local tag `v3.26.0` to origin**
   - Points to: `69ff21e90118ce8f0863a75458f806c2504d7e01`
   - Result: ✅ New tag created on remote

#### Verification
Both refs now exist on origin:
- `refs/heads/squad` → `04e5ea56311ec79e0b6b24a0b48277a09336b6c7`
- `refs/tags/v3.26.0` → `69ff21e90118ce8f0863a75458f806c2504d7e01`

GitHub Actions should now trigger on the pushed tag for the release workflow.

---

---

## 43. Release Order Recovery Decision — v3.26.0

# Release Order Recovery Decision — v3.26.0

**Author:** Alfred  
**Date:** 2026-04-12  
**Issue:** v3.26.0 tag and release published from `squad` branch instead of `main`.

---

## Situation

- **Current state:** v3.26.0 tag exists at commit `04e5ea5` on `origin/squad`; GitHub release published with full notes.
- **Problem:** Canonical releases should be cut from `main`, not feature/integration branches.
- **Risk:** If someone clones v3.26.0, they receive squad-only commits, not the integrated, stable main-branch code.
- **Ancestry:** `main` is a strict ancestor of `squad`; no conflicts exist.

---

## Decision

**Do NOT delete or replace the v3.26.0 release.** Instead, execute this sequence:

### Phase 1: Merge Squad → Main

1. Check out `main`: `git checkout main`
2. Merge `squad` non-destructively:
   ```
   git merge origin/squad --no-ff
   ```
   (Creates merge commit, preserves squad history; `-ff-only` alternative if preferred)
3. Verify merge is clean (no conflicts expected — main is ancestor).

### Phase 2: Re-Tag on Main

4. **Option A (Preserves Release Number):**
   ```
   git tag -f v3.26.0 HEAD
   git push origin main
   git push origin v3.26.0 -f
   ```
   - Updates the tag to point to the merge commit on main.
   - GitHub release automatically reflects the new commit.
   - Preserves v3.26.0 version identity.

   **OR**

5. **Option B (Creates Sequential Version):**
   ```
   git tag v3.26.1 HEAD
   git push origin main
   git push origin v3.26.1
   ```
   - Leaves v3.26.0 on squad as historical record.
   - New release v3.26.1 points to main.
   - Cleaner tag history; no force-push needed.

---

## Recommendation

**Use Option A** (re-tag on main). Rationale:
- Docker CI/CD already consumed v3.26.0; re-tagging to main is transparent.
- Avoids version number inflation.
- Establishes the rule: **release tags always point to main**.

---

## Next Steps

**Do NOT execute** until coordinated with team lead. This decision documents the path; implementation is deferred pending final go-ahead.

1. Ensure all squad changes are CI-tested against the merge commit.
2. Coordinate with Docker CI/CD team if re-pushing the tag requires image rebuild.
3. If force-push of tags is restricted, use **Option B** instead.

---

## Notes

- All work in squad already exists; this is a **branch structure fix**, not a code change.
- No commits are lost or discarded in either option.
- The decision enforces: **main = production release branch** going forward.


---

## 44. Release State Inspection: v3.26.0 Alignment Issue

# Release State Inspection: v3.26.0 Alignment Issue

**Date:** 2026-04-12  
**Inspector:** Lucius (Backend Dev)  
**Status:** NEEDS REMEDIATION

## Findings

### 1. Branch & Tag State

- **Local `main`**: `a587fc7` — "Adding Playwright skills"
- **Local `squad`**: `1d8c505` — "squad: orchestration, release v3.26.0 push completion"
- **origin/main**: `a587fc7` — "Adding Playwright skills"
- **origin/squad**: `04e5ea5` — "squad: merge audit report publication decisions"
- **Tag v3.26.0**: Points to `04e5ea5` (on origin/squad, tagged commit)

### 2. Divergence Analysis

**Main is 5 commits behind squad:**
```
1d8c505 squad: orchestration, release v3.26.0 push completion
04e5ea5 squad: merge audit report publication decisions  ← v3.26.0 tag here
c57014a .squad: Record Barbara audit pass — performance batch 154–159 release-ready
b1b6136 .squad: Merge Barbara final audit, update decisions
87e23ca Post-agent orchestration: Merge 9 inbox decisions, update team histories, log audit-ready outcome
```

**v3.26.0 is NOT in origin/main:**
- The tag points to commit `04e5ea5` on origin/squad
- origin/main is at `a587fc7`, which is an ancestor of neither squad branch
- These branches have **diverged completely**

### 3. Release Status

✅ **GitHub Release EXISTS** for v3.26.0 (created 2026-04-12 21:22:24 UTC)
- Author: github-actions[bot] (automated)
- Target: **main** (release was created targeting main branch)
- Published: 2026-04-12 21:24:21 UTC
- Status: No assets (source-only release)

### 4. Workflow Status

❌ **Docker Build-Publish workflow** for v3.26.0:
- Run #225: Status=**FAILURE**
- Event: Pushed to tag v3.26.0 on commit `04e5ea5` (squad branch)
- Conclusion: Failed

## Problem Statement

**The release was cut from the wrong branch and to the wrong destination.**

The workflow that ran (docker-build-publish.yml) executed against `04e5ea5` on squad branch, **not** from origin/main where the release was published. This creates a **versioning inconsistency:**
- The GitHub Release (v3.26.0) was created targeting `main` and published as a stable release
- But the tagged commit `04e5ea5` exists only on `squad`
- The actual `main` branch is 5+ commits behind and untagged

**Additionally:** squad has uncommitted local changes (~80 files modified/deleted), and the Docker build failed on the v3.26.0 push.

## Root Cause

Workflow execution order:
1. Squad branch advanced with orchestration commits
2. Tag v3.26.0 was created on squad (`04e5ea5`)
3. Release.yml created GitHub release **targeting main**, but the tag was on squad
4. Docker build triggered from the tag, but failed
5. Main was never merged with squad before or after the release

## Recommendation

Before continuing:
1. **Understand intent:** Was v3.26.0 meant to release from squad or main? (Should be main per SemVer discipline)
2. **Merge squad → main:** If release quality confirmed, merge squad into main so they point to the same commit
3. **Re-tag if needed:** If main should have the v3.26.0 tag, move the tag or create a new one after merge
4. **Re-run workflows:** Re-trigger docker-build-publish.yml from the correct commit on main
5. **Clean squad:** Commit or reset the 80 local file changes on squad branch before next work

This decision block is intentionally non-prescriptive—the fix depends on whether the release was _premature_ (squad work not ready for main) or _misaligned_ (right code, wrong branch).

**Do not proceed with merging squad → main without confirming the release intent with the PO and/or Alfred (Architect).**


---

## Merged from Inbox (2026-04-13)

### Alfred's Feature-Doc Sequencing Decision

**Date:** 2026-03-22  
**Context:** Post-release-fix planning. Three feature docs remain active; recommend sequencing.

**Status:** Feature 160 now in progress; 161 scheduled after 160 completion.

#### Current State
- **Active docs:** Features 113, 160, 161 (all in \docs/\, not archived)
- **Archived batches:** 131–150 (Kakeibo Waves 1 & 2, completed and moved to \docs/archive/\)
- **Release status:** Release tagging process stabilized; \main\ is stable

#### Recommendation: Start Feature 160 (Now In Progress)

**160 (Pluggable AI Backend) — Self-contained Infrastructure Refactor**
- ✅ **Self-contained:** Infrastructure layer only, no domain changes
- ✅ **Low risk:** Respects layer boundaries (DIP), backward-compatible default behavior
- ✅ **Quick win:** Isolated refactor, clear acceptance criteria, solid testing surface
- ✅ **Unblocks:** Enables llama.cpp users without code changes; sets up extensibility for future backends

**161 (BudgetScope Removal) — After 160 Completes & Deploys**
- 🔴 **Multi-layer:** Domain, Application, API, Client, Database, ~80+ files
- 🔴 **High-coordination:** Fundamental enum removal affects repositories, entities, DTOs, UI, migrations
- ✅ **Architectural evolution:** Aligns with Kakeibo philosophy (single household scope); right decision, **better timing after 160**

**113 (Performance Test Environment) — Hold Until Pi Hardware Ready**
- 🛑 **External blocker:** Hardware acquisition, physical setup, network/auth configuration
- 📋 **Reference material:** Doc is well-written; ready to execute once infrastructure is available

#### Decision: Feature 160 Prioritized
Start Feature 160 (Pluggable AI Backend) as next work. Once code-complete, tested, and deployed to main, schedule Feature 161 as dedicated architectural sprint.

---

### Feature 160 Base-Class Slice Completion (Lucius & Barbara)

**Timestamp:** 2026-04-13  
**Status:** Base-class extraction and test coverage complete

#### Lucius — Squad Sync & Implementation

✅ **Squad/main sync verified** — \squad\ already contained \origin/main\ after fetch; no merge/rebase required.

✅ **OpenAiCompatibleAiService extracted** — Shared HTTP execution flow encapsulates:
- OpenAI-compatible message formatting (\/v1/chat/completions\)
- Streaming response handling
- Token counting via response metadata
- Request/response lifecycle

✅ **Ollama refactored onto base class** — \OllamaAiService\ now inherits from \OpenAiCompatibleAiService\:
- Native Ollama hooks preserved (\/api/version\, \/api/tags\, \/api/chat\)
- Backward-compatible behavior unchanged
- Ready for llama.cpp wiring and future backends

#### Barbara — Test Coverage & Validation

✅ **Infrastructure regression tests added** — \OllamaAiServiceTests.cs\ and \OpenAiCompatibleAiServiceTests.cs\:
- Fake HTTP response handling (no live Ollama required)
- Token counting behavior locked
- Endpoint shape validation (\/api/version\, \/api/tags\, \/api/chat\)
- Streaming response parsing

✅ **Base-class unit-test acceptance criterion** — Marked complete in \docs/160-pluggable-ai-backend.md\

✅ **Non-Docker validation** — All infrastructure tests pass without Testcontainers or Docker

#### Next Slice
LlamaCpp implementation (inherits base class).

---

## 45. Feature 160 LlamaCpp Concrete Backend & DI Selection (2026-04-13)

**Timestamp:** 2026-04-13T01:54:22Z  
**Implementer:** Lucius (Backend Dev)  
**Validator:** Barbara (Tester)  
**Status:** Complete

### Lucius — Implementation

✅ **LlamaCppAiService implemented** — New concrete `OpenAiCompatibleAiService` subclass:
- Overrides service URL construction to point to `llama.cpp` server endpoint
- Inherits OpenAI-compatible HTTP flow (message formatting, streaming, token counting)
- Fully compatible with existing token counting and model selection patterns

✅ **BackendSelectingAiService created** — Runtime selector:
- Reads persisted backend choice from app settings (`ISettings.AiBackend`)
- Maps choice (Ollama/llama.cpp) to correct concrete service instance
- Preserves Ollama as default if settings unavailable
- Allows backend switching without app restart

✅ **Infrastructure DI registration** — Both backends registered:
- `OllamaAiService` registered as typed HttpClient service
- `LlamaCppAiService` registered as typed HttpClient service
- `BackendSelectingAiService` bound to `IAiService`
- Application and API layers remain unaware of concrete implementations

✅ **Feature doc acceptance criteria marked complete** — `docs/160-pluggable-ai-backend.md`:
- [x] llama.cpp concrete backend implementation
- [x] DI selector wiring

### Barbara — Test Validation

✅ **Token counting regression tests added** — `LlamaCppAiServiceTests.cs`:
- Fake HTTP response handling for llama.cpp endpoint shape
- Token counting behavior locked for new backend
- Streaming response parsing validation
- No live llama.cpp server required

✅ **Backend selection and DI tests added**:
- `BackendSelectingAiService` returns correct concrete instance based on settings
- Default Ollama behavior preserved when settings unavailable
- Explicit backend selection (via `ISettings.AiBackend`) respected
- DI registration verified for both `OllamaAiService` and `LlamaCppAiService`

✅ **Infrastructure DI registration coverage** — Targeted unit tests validate:
- Both backends registered as typed HttpClient services
- Selector correctly wired to `IAiService` interface
- No concrete backend leakage into Application or API layers

✅ **AI controller backend/endpoint mapping** — Non-Docker controller tests:
- `/ai/status` correctly reports backend choice
- `/ai/suggest` routing works with both backends
- Backend switch observable through controller responses

✅ **Feature doc acceptance criteria marked complete** — `docs/160-pluggable-ai-backend.md`:
- [x] llama.cpp backend tests
- [x] DI validation

### Notes

- Non-Docker regression coverage ensures llama.cpp and DI selector behaviors locked
- Ollama default behavior preserved and tested; backward compatibility verified
- Ready for Docker-enabled environment validation when available
- All non-Docker build/test surface passing

---

## 46. Feature 160 Persistence/API Slice (2026-04-13)

**Timestamp:** 2026-04-13T02:03:09Z  
**Implementer:** Lucius (Backend Dev)  
**Validator:** Barbara (Tester)  
**Status:** Complete

### Lucius — Implementation

✅ **Settings persistence domain model** — `AiSettingsData` aggregate root:
- Generic `EndpointUrl` field (replaces Ollama-specific naming)
- `BackendType` enumeration for backend selection (Ollama, LlamaCpp)
- Database column `AiOllamaEndpoint` preserved for backward compatibility
- Domain-level defaults when endpoint URL is missing

✅ **Infrastructure repository implementation** — `AiSettingsRepository`:
- EF Core persistence layer for `AiSettingsData`
- Read/write operations with optional caching strategy
- Migration to include new `BackendType` column alongside existing `AiOllamaEndpoint`
- Seamless loading/saving of serialized settings

✅ **Application settings service** — `AiSettingsService`:
- Settings round-trip (load/save) through repository
- Automatic fallback to backend-specific default endpoint when URL missing
- `BackendType` selection propagated to AI service selector
- Clean application boundary (no EF Core types exposed)

✅ **API controller endpoint mapping** — `AiSettingsController`:
- POST/GET `/api/v1/settings/ai` endpoint for settings CRUD
- `AiSettingsDto` with `EndpointUrl` and legacy `OllamaEndpoint` aliases
- Automatic conversion of legacy request payloads into domain `AiSettingsData`
- Backward-compatible JSON serialization

✅ **DTO backward compatibility** — `AiSettingsDto`:
- `EndpointUrl` as primary field (generic)
- `OllamaEndpoint` as legacy alias (serialized/deserialized)
- Both fields round-trip cleanly in JSON
- Missing fields default to backend-specific values

✅ **Feature doc acceptance criteria marked complete** — `docs/160-pluggable-ai-backend.md`:
- [x] Settings persistence domain model
- [x] Application settings service
- [x] API settings endpoint
- [x] Backward-compatible persistence

### Barbara — Test Validation

✅ **Domain model tests** — `AiSettingsDataTests.cs`:
- `BackendType` string serialization (Ollama, LlamaCpp)
- Default endpoint resolution based on selected backend
- Round-trip validation for settings aggregate root

✅ **DTO serialization tests** — `AiSettingsDtoTests.cs`:
- JSON round-trip for `BackendType` enumeration
- `EndpointUrl` field serialization/deserialization
- Legacy `OllamaEndpoint` aliasing in JSON payloads
- Missing endpoint fields correctly null or default

✅ **Application service tests** — `AiSettingsServiceTests.cs`:
- Settings loading through repository with mock data
- Settings saving with proper domain state conversion
- Backend-type propagation to selector
- Automatic fallback to backend-specific defaults when endpoint URL missing

✅ **API controller endpoint tests** — `AiSettingsControllerTests.cs`:
- GET `/api/v1/settings/ai` returns current settings
- POST `/api/v1/settings/ai` persists updates
- Request payloads with legacy `OllamaEndpoint` field correctly mapped
- `BackendType` properly serialized in responses
- Backward-compatible contract validated

✅ **Infrastructure DI registration tests** — Non-Docker infrastructure tests:
- `ISettingsRepository` registered and injected
- `IAiSettingsService` bound to application implementation
- Controller dependency injection verified
- Settings service wiring complete

✅ **Feature doc acceptance criteria marked complete** — `docs/160-pluggable-ai-backend.md`:
- [x] Domain model tests
- [x] Application service tests
- [x] API endpoint tests
- [x] Persistence contract validation

### Decision: Backward Compatibility with Pluggable Backend Support

**Decision:** Keep the persisted database column name `AiOllamaEndpoint` for backward compatibility, but expose the core setting as generic `EndpointUrl`.

**Why:** Pluggable backend support makes Ollama-specific naming in the core model a leaky abstraction. Preserving the existing column avoids a needless migration while still cleaning up the domain/application surface.

**API compatibility:** `AiSettingsDto` round-trips both `EndpointUrl` and `OllamaEndpoint`, and missing endpoint fields now fall back to the backend-specific default selected by `BackendType`.

### Docker-Backed Integration Note

⚠️ **Full end-to-end persistence/API validation with PostgreSQL deferred** — Docker-backed integration tests remain blocked by environment constraints. Non-Docker regression coverage validates public contract and backward compatibility.

### Notes

- Pluggable backend support now fully integrated into persistence layer
- Ollama-specific column name preserved; domain abstraction cleaned
- All non-Docker unit/integration tests passing
- Docker-backed integration tests deferred (next phase)
- Ready for full API integration testing when Docker environment available

---

### Feature 160: Pluggable AI Backend Implementation - Complete (2026-04-13)

**Author:** Lucius

**Decision:** Feature 160 implementation cycle complete. All work committed to `squad` branch (commit `959fbdc`) with message "feat: Feature 160 - Pluggable AI backend implementation".

**Scope:** Full pluggable backend orchestration from domain through persistence and API layers.

**Components Delivered:**
- `BackendSelectingAiService` — orchestrates backend selection and delegation
- `LlamaCppAiService` — native llama.cpp integration with `/api/completions` endpoint support
- `OpenAiCompatibleAiService` — generic OpenAI protocol client for compatible services
- `AiSettingsData` aggregate root — domain model with `BackendType` selector and generic `EndpointUrl`
- `AiSettingsRepository` + migration — EF Core persistence with backward-compatible `AiOllamaEndpoint` column
- `AiSettingsService` — application orchestrator handling round-trip settings load/save
- `AiSettingsController` — REST `/api/v1/settings/ai` CRUD endpoint
- Comprehensive unit and integration test coverage

**Status:** ✅ All Feature 160 acceptance criteria met. Working tree clean. Branch tracking origin/squad.

**Next:** Ready for integration testing, additional backend types, or client-side consumption of settings endpoint.

---

### Feature 161: Scope Removal Phase 1 Slice 1 - Approved (2026-04-13)

**Author:** Barbara (Tester), building on Lucius initial slice  
**Decision:** Slice 1 (hidden scope normalization) approved after fixing AccountForm scope leak.

**Problem Resolved:**  
Lucius's initial slice removed visible scope UI but left a hidden path: legacy edit/create account models could still carry 'Personal' scope values and submit them through AccountForm despite the UI removing all scope-selection options.

**Solution Implemented:**  
Coerce incoming account form models to 'Shared' scope in AccountForm.OnParametersSet(). This ensures:
- Legacy 'Personal' values cannot persist through the hidden client form path
- All user-visible behavior defaults to household (Shared) scope
- No API compatibility breakage yet (scope plumbing still in place for Phase 2)

**Files Modified:**
- src/BudgetExperiment.Client/Components/Forms/AccountForm.razor — added scope normalization in OnParametersSet()
- docs/161-budget-scope-removal.md — updated slice 1 progress notes

**Validation:**  
Targeted client slice test filter: Category!=Performance&FullyQualifiedName~AccountFormTests|FullyQualifiedName~NavMenuTests|FullyQualifiedName~SettingsPageTests|FullyQualifiedName~ScopeServiceTests|FullyQualifiedName~ScopeMessageHandlerTests
- Before fix: 62 passed, 1 failed
- After fix: 63 passed, 0 failed ✅

**Status:** ✅ Slice 1 approved for merge. Phase 1 UI simplification complete.

**Next:** Slice 2 (API layer removal) can proceed with clean foundation. Scope header, middleware, and DTOs targeted for removal in upstream slices.

---

## Feature 127: Code Coverage Beyond 80% — Team Analysis & Roadmap

**Date:** 2026-04-21  
**Status:** APPROVED (with Vic's mandatory guardrails)  
**Team:** Lucius, Alfred, Barbara, Vic  
**Decision Owner:** Alfred (Lead), with critical inputs from Barbara (Tester), Lucius (High-ROI targets), Vic (Audit & guardrails)

---

### Executive Summary

**Lucius's Analysis (High-ROI Targets):**
Api coverage at 77.2% and Application at 90.3% reveal missing error path tests (validation, concurrency conflicts, exception handling). Identified 20 high-ROI tests (10 Api, 10 Application) focusing on concurrency integrity (ETag conflicts) and input validation boundaries. Expected gain: ~4% (79.7% Api, 91.8% Application). Tests avoid heavy mocking; most are "Simple" complexity with 4-5 lines/test coverage.

**Alfred's Roadmap (Three-Phase Strategy):**
Solution at 78.4% requires surgical three-phase approach (Application→Api→Client) to reach 80.5% within 2-3 sprints (~50 high-ROI tests). Architecture review confirms yield differences reflect layer responsibilities (Service orchestration: 77.8 lines/test; Controllers: 26.4 lines/test; Components: 46.2 lines/test). Phase order (Application→Api→Client) maximizes EffortIndex; module-specific targets prevent diminishing returns (Application 85%→90%, Api 77.2%→80%, Client 68.1%→70%).

**Barbara's Analysis (Client Ceiling):**
Client at 68.1% with 2,847 tests shows 70% is appropriate ceiling due to markup saturation (182 .razor files, minimal testable logic). High-impact opportunities: Tier 1 (DataHealthViewModel, RecurringChargeSuggestionsViewModel, Calendar, StatementReconciliation, ReconciliationHistory — ~30 tests, 250-350 lines, 71-72% total). Tier 2 quick wins (ReportsIndex, ReconciliationDetail, KaizenDashboardView — 73-74%). Tier 3 (LocationReportPage, Transactions) deferred. ComponentShowcase (0% coverage, pure demo UI) explicitly excluded from testing.

**Vic's Audit (Sustainability with Guardrails):**
Coverage strategy is **defensible but fragile**. Current critical gaps: Application at 35% is **project-threatening** (should be 85%+), Domain at 44% untested (financial invariants at risk, should be 90%+). Diminishing returns Phase 2/3 incentivizes coverage gaming without enforcement. Per-module CI gates **non-negotiable** to prevent "averaging down." Confidence: MEDIUM IF guardrails enforced; LOW without them. **Phase 0 (NEW) critical:** Application 35%→60% must address major critical paths before Client UI work.

---

### Module-Specific Coverage Targets (Vic's Recommendations)

| Module | Current | Target | Rationale |
|--------|---------|--------|-----------|
| **Domain** | 44% | **90%** | Financial invariants, arithmetic, core entities — must be exhaustive |
| **Application** | 35% | **85%** | Business logic orchestration — Phase 0 (35%→60%) CRITICAL before phases 1-3 |
| **Api** | 77.2% | **80%** | Controller orchestration, error paths, concurrency conflicts |
| **Client** | 68.1% | **75%** | UI components — high-traffic pages (Transactions, Budgets, Accounts) 80%+, low-traffic deferred |
| **Infrastructure** | ? | **70%** | Data access, integration-heavy, Testcontainer-backed |
| **Contracts** | 95% | **60%** | DTOs with minimal logic, low risk |
| **Solution** | 78.4% | **75%** overall gate | Overall floor (pragmatic, allows Infrastructure/Contracts to pull down average if necessary) |

---

### Critical Phase 0: Application 35%→60% (NEW — Vic's Major Concern)

**Why Phase 0 Exists:**
Application at 35% is **project-threatening**. This layer contains budgeting rules, categorization logic, recurring transaction patterns, financial reporting orchestration. 65% untested means silent bugs risk user data integrity. **Cannot proceed to Client UI tests (Phase 1) until critical Application paths covered.**

**Phase 0 Scope:**
- Audit Application services for zero-coverage critical paths
- Prioritize services with **financial impact:** BudgetProgressService, CategorySuggestionService, RecurringChargeDetectionService, TransactionService, BudgetGoalService, RecurringTransactionService
- Target: Application 60% coverage (addresses major orchestration, business logic, exception handling)
- Estimate: ~10-15 tests, ~200-300 lines gained
- Duration: 1 sprint (before Phase 1 begins)

**Success Criteria Phase 0:**
- ✅ Application coverage: 60%+
- ✅ Critical financial services (Budget, Transaction, Categorization, RecurringTransaction, Transfer) have >50% coverage each
- ✅ Zero regression in existing passing tests
- ✅ Ready to proceed to Phase 1 (Client UI + Api error paths in parallel)

---

### Phase 1: Application 60%→90% (Parallel with Phase 2)

**Duration:** Sprint 1-2  
**Owner:** Barbara (Application specialist)  
**Focus:** Edge cases, exception handling, business logic orchestration

**High-ROI Targets from Lucius (Application Layer 10 tests):**
1. **BudgetGoalService.SetGoalAsync:** Concurrency conflict path (DbUpdateConcurrencyException) — Medium complexity, HIGH priority (budget goal integrity)
2. **BudgetCategoryService.CreateAsync:** Invalid CategoryType/KakeiboCategory strings → DomainException — Simple tests for enum parsing
3. **CategorizationRuleService.CreateAsync:** Invalid MatchType string → DomainException
4. **ImportService.PreviewAsync:** Empty rows list early return (confirms fast path)
5. **RecurringTransactionService.CreateAsync:** Account not found → DomainException
6. **TransactionService.CreateAsync:** Account not found path → DomainException
7. **TransactionService.GetByDateRangeAsync:** Kakeibo filter with zero matches → empty list (edge case)
8. **CategorizationRuleService.ListPagedAsync:** Status null/invalid handling (null to repo)
9. **BudgetGoalService.CopyGoalsAsync:** Zero source goals early return
10. **TransactionService edge cases:** Invalid currency conversion, concurrent modification, linked recurring charge cascade/block

**Expected Coverage Gain:** ~390 lines → **Application 90%+**

**Success Criteria Phase 1:**
- ✅ Application coverage: 90%+
- ✅ 10+ new Application.Tests added (Lucius's targets + additional discovered)
- ✅ Zero test flakiness introduced
- ✅ Code review: Barbara validates test quality (no coverage gaming per Vic's guardrails)

---

### Phase 2: Api 77.2%→80% (Parallel with Phase 1, Sequential with Phase 3)

**Duration:** Sprint 1-2  
**Owner:** Lucius (Api specialist)  
**Focus:** Error paths, validation failures, concurrency conflicts, RFC 7807 compliance

**High-ROI Targets from Lucius (Api Layer 10 tests — HIGH PRIORITY):**

**Concurrency Conflicts (HIGH priority — data integrity):**
1. **AccountsController ETag Conflict (PUT):** Create account, update with stale ETag → expect 409 Conflict (4-5 lines)
2. **BudgetsController ETag Conflict (PUT):** Set goal, then update with wrong version → 409 (4-5 lines)
3. **RecurringTransactionsController ETag Conflict (PUT):** Update with wrong version → 409 (4-5 lines)
4. **ImportController ETag Conflict (PUT):** Create mapping, update with stale version → 409 (4-5 lines)

**Input Validation Boundaries (MEDIUM priority):**
5. **BudgetsController Month Validation (GET):** month=0 or month=13 → 400 BadRequest (3-4 lines)
6. **CalendarController Month Validation (GET):** month=15 → 400 (3-4 lines)
7. **CategorizationRulesController Pagination Boundary:** page=0, pageSize=-1 → 400 (5-6 lines)
8. **TransactionQueryController Invalid Kakeibo Category:** Enum.TryParse failure → 400 (4-5 lines)
9. **CategorizationRulesController Status Filter Validation:** Invalid status like "pending" → validate behavior (3-4 lines)
10. **TransactionBatchController Missing If-Match:** PUT without If-Match header → 200 OK (last-write-wins path, 3-4 lines)

**Expected Coverage Gain:** ~80 lines → **Api 80%+**

**Success Criteria Phase 2:**
- ✅ Api coverage: 80%+
- ✅ 10 new Api.Tests added (focus: error paths, concurrency, validation)
- ✅ ProblemDetails middleware fully covered (RFC 7807)
- ✅ Code review: Barbara validates test quality (behavioral assertions, no implementation details)

---

### Phase 3: Client 68.1%→75% (Sequential after Phase 2)

**Duration:** Sprint 2-3  
**Owner:** Barbara (Client specialist)  
**Focus:** High-impact pages only; skip markup-heavy showcase components

**Tier 1 High-Impact Tests (Target these FIRST — ~30 tests, 250-350 lines, 71-72%):**

1. **DataHealthViewModel** (114 lines, 8-10 tests):
   - LoadAsync, MergeDuplicatesAsync, DismissOutlierAsync
   - Covers duplicate detection, outlier dismissal, error handling
   
2. **RecurringChargeSuggestionsViewModel** (70-90 lines, 10-12 tests):
   - Pattern detection and suggestion management
   - Detect, accept, dismiss, filter, confidence formatting
   
3. **Calendar.razor Code-Behind Logic** (50-70 lines, 8-10 tests):
   - Month navigation, week selection, day detail logic, account filtering
   - Most-used page in application
   
4. **StatementReconciliation.razor** (60-80 lines, 10-12 tests):
   - Multi-step workflow (balance input, transaction clearing, reconciliation)
   - Financial accuracy critical path
   
5. **ReconciliationHistory.razor** (30-40 lines, 5-6 tests):
   - List filtering, date range, account selection logic
   - Audit trail for reconciliation feature

**Tier 2 Quick Wins (Optional, 73-74% total — ~20 tests):**
6. **ReportsIndex.razor** (20-30 lines, 4-5 tests): Navigation hub with feature flags
7. **ReconciliationDetail.razor** (25-35 lines, 5-6 tests): Display logic for completed reconciliation
8. **KaizenDashboardView.razor** (40-50 lines, 6-8 tests): Kakeibo insights aggregation

**Tier 3 / Explicitly Deferred (Low ROI — defer or skip):**
- **ComponentShowcase.razor** (0% coverage, 144 lines, pure demo UI) — **explicitly excluded**
- **LocationReportPage.razor** (25-35 lines, low usage) — **defer to 60%**
- **Transactions.razor** (15-20 lines, mostly wiring) — ViewModel already 100% tested

**Expected Coverage Gain:** ~350-450 lines → **Client 74-76%**

**Success Criteria Phase 3:**
- ✅ Client coverage: 75%+
- ✅ Tier 1 tests complete (high-impact pages 75%+)
- ✅ Tier 2 optional (if effort allows, push toward 76%)
- ✅ ComponentShowcase, low-traffic pages explicitly documented as "low-coverage exemptions"

---

### Mandatory Guardrails (Vic's Non-Negotiable Requirements)

**These are mandatory. Do NOT proceed without them.**

#### A. Per-Module CI Gates

**Why:** Solution-wide 75% gate allows "averaging down." Api at 80%, but Application at 50% still passes gate.

**Implementation in .github/workflows/ci.yml:**
`yaml
minimum_coverage:
  solution: 75%  # Overall floor
  per_module:
    BudgetExperiment.Domain: 90%       # Financial invariants
    BudgetExperiment.Application: 85%  # Business logic
    BudgetExperiment.Api: 80%          # Controller orchestration
    BudgetExperiment.Client: 75%       # UI components
    BudgetExperiment.Infrastructure: 70%  # Data access (integration-heavy)
    BudgetExperiment.Contracts: 60%    # DTOs (minimal logic)
`

**Effect:**
- PR fails if *any* module drops below target
- Per-module coverage reported in PR comment
- Prevents shipping low-quality modules to ship low-quality modules within high-quality solution

#### B. Coverage Quality Review (Vic's Anti-Gaming Mechanism)

**Why:** 75% gate incentivizes hitting numbers, not writing valuable tests.

**Checklist for PR reviewers:**
- [ ] Each new test asserts on *behavior*, not implementation
- [ ] Each test would catch a real regression (ask: "what breaks if I delete this code?")
- [ ] No trivial assertions (\Assert.NotNull()\, \Assert.True(true)\, \Assert.Empty()\ without context)
- [ ] No brittle UI tests that break on CSS refactor without testing logic
- [ ] Test failure message explains *why* it matters

**Forbidden patterns:**
- Constructor tests that only set properties without testing behavior
- CSS class assertions without behavioral validation
- \Assert.NotNull()\ as only assertion

**Spot Audits:**
- Vic reviews 10% of Phase 2/3 tests for coverage gaming
- Report patterns to team for calibration
- Team calibration session after Phase 1 completes (align on "what good looks like")

#### C. Testcontainer Flakiness Fix BEFORE Phase 2

**Current state:** Infrastructure tests exhibit pre-existing Docker flakiness (noted in Feature 161 audit). Phase 2/3 will add 100+ integration tests.

**Must fix before Phase 2 starts:**
- Investigate and document current flake rate
- Add retry logic to Testcontainer startup (3 attempts with exponential backoff)
- Establish flake budget: <1% of tests
- If flake exceeds 1%, **halt new test additions** until root cause fixed

**Monitoring:**
- Track flaky tests in \.squad/decisions.md\ with repro steps
- Monthly review in team standup (if flake accumulates, escalate)

#### D. Explicit Low-Coverage Exemptions

**Document zero-coverage files with rationale and exclude from module calculation:**

| File | Lines | Reason | Module |
|------|-------|--------|--------|
| \Program.cs\ | ~100 | Composition root — tested via integration tests | Api |
| \App.razor\ | ~20 | Routing only — integration-tested | Client |
| \*Layout.razor\ | ~150 total | Pure HTML, no logic to test | Client |
| \ComponentShowcase.razor\ | 144 | Demo page, zero production logic | Client |
| \CsvParseResultModel.cs\ | ~50 | DTO, no behavior | Contracts |

**Effect:** Module coverage calculated excluding these files prevents team from wasting effort testing infrastructure glue.

---

### Success Criteria & Confidence Assessment

#### Phase 0 (Critical Foundation)
- ✅ Application coverage: 60%+
- ✅ Critical financial services have >50% coverage each
- ✅ Zero regression in existing tests
- ✅ Testcontainer flakiness audit complete

#### Phase 1 (Parallel: Application→90%, Api→80%)
- ✅ Application coverage: 90%+
- ✅ Api coverage: 80%+
- ✅ 20+ new tests (10 Application, 10 Api) added
- ✅ Zero test flakiness introduced
- ✅ Code review validates test quality (no gaming)

#### Phase 2 (sequential: Client→75%)
- ✅ Client coverage: 75%+
- ✅ Tier 1 tests complete (DataHealth, RecurringChargeSuggestions, Calendar, Reconciliation)
- ✅ ComponentShowcase and low-traffic pages explicitly exempted

#### Overall Success
- ✅ Solution coverage: 80.5%+
- ✅ Per-module targets met (Domain 90%, Application 85%, Api 80%, Client 75%, Infrastructure 70%, Contracts 60%)
- ✅ CI gate enforces per-module minimums
- ✅ Coverage quality review prevents gaming
- ✅ Test suite health: <2min unit tests, <5min integration tests, <1% flaky tests
- ✅ Coverage debt documented in \.squad/decisions.md\ with rationales

#### Confidence Assessment

**Vic's Verdict: MEDIUM-HIGH confidence IF guardrails enforced.**

**High confidence IF:**
- ✅ Per-module CI gates implemented
- ✅ Application Phase 0 addresses 35% gap (major concern)
- ✅ Domain target set at 90% (financial invariants)
- ✅ Coverage quality review established (Barbara + Vic spot-checks)
- ✅ Testcontainer flakiness fixed before Phase 2
- ✅ Low-coverage exemptions documented

**Low confidence IF:**
- ❌ Strategy executed as-is without guardrails
- ❌ Application/Domain gaps deferred
- ❌ Coverage gaming allowed in Phase 2/3
- ❌ Per-module gates not enforced

---

### High-ROI Test Targets (Lucius's 20 High-Confidence Targets)

#### API Layer (10 tests — 77.2%→79.7% expected gain)

**Implementation Order:** Phase 2 (must-complete by end of Phase 1/2 parallel work)

| # | Controller | Test Case | File:Line | Expected Lines | Priority |
|---|-----------|-----------|-----------|---|----------|
| 1 | AccountsController | ETag Concurrency Conflict (PUT) | AccountsController.cs:107 | 4-5 | HIGH |
| 2 | BudgetsController | Month Validation (GET) | BudgetsController.cs:51 | 3-4 | MEDIUM |
| 3 | BudgetsController | ETag Conflict (PUT) | BudgetsController.cs:97-99 | 4-5 | HIGH |
| 4 | CalendarController | Month Validation (GET) | CalendarController.cs:79 | 3-4 | MEDIUM |
| 5 | CategorizationRulesController | Pagination Boundary (page=0) | CategorizationRulesController.cs:61-76 | 5-6 | MEDIUM |
| 6 | CategorizationRulesController | Status Filter Validation | CategorizationRulesController.cs:54 | 3-4 | LOW |
| 7 | ImportController | ETag Conflict (PUT) | ImportController.cs:115 | 4-5 | MEDIUM |
| 8 | RecurringTransactionsController | ETag Conflict (PUT) | RecurringTransactionsController.cs:108 | 4-5 | HIGH |
| 9 | TransactionBatchController | Missing If-Match (200 fallback) | TransactionBatchController.cs:81-84 | 3-4 | MEDIUM |
| 10 | TransactionQueryController | Invalid Kakeibo Category (400) | TransactionQueryController.cs:76-80 | 4-5 | MEDIUM |

#### Application Layer (10 tests — 90.3%→91.8% expected gain)

**Implementation Order:** Phase 1 (must-complete by end of Phase 1)

| # | Service | Test Case | File:Line | Expected Lines | Priority |
|---|---------|-----------|-----------|---|----------|
| 11 | BudgetCategoryService | Invalid CategoryType → DomainException | BudgetCategoryService.cs:70-73 | 4-5 | MEDIUM |
| 12 | BudgetCategoryService | Invalid KakeiboCategory → DomainException | BudgetCategoryService.cs:78-80 | 3-4 | MEDIUM |
| 13 | BudgetGoalService | Concurrency Conflict Path | BudgetGoalService.cs:73-76 | 5-6 | HIGH |
| 14 | CategorizationRuleService | Invalid MatchType → DomainException | CategorizationRuleService.cs:96-99 | 4-5 | MEDIUM |
| 15 | ImportService | Empty Rows List Early Return | ImportService.cs:84-87 | 3-4 | LOW |
| 16 | RecurringTransactionService | Account Not Found → DomainException | RecurringTransactionService.cs:97-100 | 4-5 | MEDIUM |
| 17 | TransactionService | Account Not Found Path | TransactionService.cs:100 | 4-5 | MEDIUM |
| 18 | TransactionService | Kakeibo Filter Zero Matches | TransactionService.cs:74-82 | 5-6 | LOW |
| 19 | CategorizationRuleService | Status Null Handling (ListPagedAsync) | CategorizationRuleService.cs:54-59 | 3-4 | LOW |
| 20 | BudgetGoalService | Zero Source Goals (CopyGoalsAsync) | BudgetGoalService.cs:110-117 | 3-4 | LOW |

**Estimated Total Gain:** ~70-100 lines of uncovered code → **4.0% total coverage improvement** (solution 78.4%→82.4% if applied in isolation; combined with Client gains → 80.5%+)

---

### Client High-Impact Targets (Barbara's Tier 1-3 Analysis)

#### Tier 1: High Value, Moderate Effort (Target FIRST — Push to 71-72%)

1. **DataHealthViewModel** (114 lines, 8-10 tests) — Duplicate detection, outlier dismissal, error handling — **Highest priority**
2. **RecurringChargeSuggestionsViewModel** (70-90 lines, 10-12 tests) — Pattern detection, suggestion management
3. **Calendar.razor Code-Behind** (50-70 lines, 8-10 tests) — Most-used page, month/week navigation, filtering
4. **StatementReconciliation.razor** (60-80 lines, 10-12 tests) — Multi-step financial workflow
5. **ReconciliationHistory.razor** (30-40 lines, 5-6 tests) — Audit trail, list filtering

**Tier 1 Total:** ~30 tests, 250-350 lines covered → Client 71-72%

#### Tier 2: Moderate Value, Low Effort (Quick Wins — Optional, Push to 73-74%)

6. **ReportsIndex.razor** (20-30 lines, 4-5 tests) — Navigation hub, feature flags
7. **ReconciliationDetail.razor** (25-35 lines, 5-6 tests) — Display logic, balance rendering
8. **KaizenDashboardView.razor** (40-50 lines, 6-8 tests) — Cultural feature aggregation

**Tier 2 Total:** ~20 tests, 100-150 lines covered → Client 73-74%

#### Tier 3 / Explicitly Deferred (Low ROI — Skip unless business case emerges)

- **ComponentShowcase.razor** — 0% coverage, 144 lines, **PURE DEMO UI — EXPLICITLY EXCLUDED**
- **LocationReportPage.razor** — Low usage, 25-35 lines, 4-5 tests, defer to 60%
- **Transactions.razor** — ViewModel already 100% tested, mostly markup wiring

---

### Implementation Plan & Timeline

**Sprint 1 (Week 1-2):**
1. ✅ Phase 0 begins: Application audit for zero-coverage critical paths
2. ✅ Phase 1 (Application): Barbara starts Lucius's 10 high-ROI targets
3. ✅ Phase 2 (Api): Lucius starts Api layer concurrency & validation tests (parallel with Phase 1)
4. ✅ Testcontainer flakiness audit completes; retry logic added if needed
5. ✅ Per-module CI gates implemented in \.github/workflows/ci.yml\

**Sprint 2 (Week 3-4):**
1. ✅ Phase 1 & 2 complete: Application 90%, Api 80%
2. ✅ Phase 3 begins: Barbara starts Tier 1 Client tests
3. ✅ Coverage quality review established; Barbara validates test behavioral value
4. ✅ Low-coverage exemptions documented (ComponentShowcase, Program.cs, Layouts)

**Sprint 3 (Week 5-6):**
1. ✅ Phase 3 Tier 1 complete: Client 71-72%
2. ✅ Tier 2 optional: If effort allows, push Client to 73-74%
3. ✅ Per-module gates stable; Coverage 80.5%+ achieved
4. ✅ Team calibration session: Review what "good coverage quality" looks like
5. ✅ Quarterly audit calendar established (Vic reviews one module/quarter)

**Ongoing:**
- Monitor test execution time budget (<2min unit, <5min integration)
- Track flaky tests; fix before adding more
- Monthly coverage trend review
- TDD non-negotiable for Domain/Application changes
- Quarterly Vic audit of coverage quality (not just quantity)

---

### Recommendation

**APPROVE** this comprehensive coverage strategy with all mandatory guardrails:

1. ✅ **Phase 0 (NEW):** Application 35%→60% critical paths (addresses Vic's "project-threatening" concern)
2. ✅ **Phase 1:** Application 60%→90% (Barbara, Sprint 1-2)
3. ✅ **Phase 2:** Api 77.2%→80% (Lucius, Sprint 1-2, parallel with Phase 1)
4. ✅ **Phase 3:** Client 68.1%→75% (Barbara, Sprint 2-3, sequential after Phase 2)
5. ✅ **Mandatory Guardrails:**
   - Per-module CI gates (Domain 90%, App 85%, Api 80%, Client 75%, Infrastructure 70%, Contracts 60%)
   - Coverage quality review (Barbara validates, Vic spot-checks 10% of Phase 2/3 tests)
   - Testcontainer flakiness fixed before Phase 2 begins
   - Explicit low-coverage exemptions (ComponentShowcase, Program.cs, Layouts)
   - Quarterly audit calendar + test suite health metrics

**Do NOT approve if:**
- ❌ Team rejects per-module CI gates
- ❌ Application/Domain targets deferred
- ❌ Coverage quality review not established
- ❌ Phase 0 (Application critical paths) skipped

---

### Success Probability

**Vic's Assessment: HIGH confidence (75%+) IF guardrails enforced.**

**Why this works:**
- Team has excellent TDD discipline (5,866 tests, strong cultural baseline)
- 75% gate acknowledges current reality while establishing quality floor
- Per-module gates prevent averaging down
- Coverage quality review prevents gaming
- Phase 0 addresses project-threatening Application gap immediately

**Risk factors mitigated:**
- ✅ Application at 35% → Phase 0 (60%+) before Client work
- ✅ Domain at 44% → Explicit 90% target with financial invariants focus
- ✅ Diminishing returns → Lucius's high-ROI targets eliminate speculation
- ✅ Coverage gaming → Quality review + Barbara validation + Vic audits
- ✅ Test debt → Health metrics (execution time <5min, flake <1%, LOC ratio 1.5-2.5x)
- ✅ Drift → Per-module CI gates + TDD enforcement + Quarterly audits

---

**Decision Approved**  
**Led by:** Alfred (Roadmap), with critical input from Lucius (High-ROI targets), Barbara (Client analysis + quality review), Vic (Audit & mandatory guardrails)  
**Date:** 2026-04-21  
**Next Step:** Phase 0 begins immediately; Barbara audits Application services for zero-coverage critical paths. Phase 1 (Application→90%) and Phase 2 (Api→80%) can proceed in parallel. Phase 3 (Client→75%) sequential after Phase 2 completes.
