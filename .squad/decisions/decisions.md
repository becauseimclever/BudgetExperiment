# Squad Decisions — Code Quality Review (2026-03-22)

## Executive Summary
Full-solution code quality review by Alfred (Lead), Lucius (Backend), and Barbara (Tester). Three finding documents analyzed, consolidated, and prioritized below.

---

## Architecture Review Decisions (Alfred)

### DECISION 1: Controllers Should Depend on Interfaces (DIP Enforcement)
**Severity:** WARNING | **Effort:** 1-2 hours | **Status:** Pending

**Issue:** Controllers inject concrete service types instead of interfaces:
- `TransactionsController` → `TransactionService` (violates DIP)
- `AccountsController` → `AccountService` (needs interface)
- `RecurringTransactionsController` → `RecurringTransactionService` (has interface but not used)

**Action Required:**
- Ensure `TransactionService` has `ITransactionService` interface (likely already exists)
- Create `IAccountService` interface
- Update controller constructors to accept interfaces
- Verify DI registrations use interfaces

**Owner:** Architecture Lead (Alfred)  
**Validation:** Controllers compile and resolve correctly, tests pass

---

### DECISION 2: Enforce Consistent Field Access Style
**Severity:** WARNING | **Effort:** 30 minutes | **Status:** Pending

**Issue:** Mixed usage of `this._fieldName` and `_fieldName` across Application layer
- `CalendarGridService` — uses `_field`
- `UnifiedTransactionService` — uses `this._field`
- `BudgetCategoryService` — uses `this._field`

**Decision:** Standardize to `this._fieldName` (current majority pattern in codebase)

**Action Required:**
- Update `.editorconfig` or StyleCop rule to enforce (if not already)
- Fix `CalendarGridService` to use `this._` prefix
- Lint to verify consistency

**Owner:** Code Quality Lead  
**Validation:** Solution lints with zero warnings

---

### DECISION 3: DateTime.Now in Client Should Use CultureService
**Severity:** WARNING | **Effort:** 30 minutes | **Status:** Pending

**Issue:** `Reconciliation.razor` uses `DateTime.Now` instead of consistent time source
- Lines 52, 273-274 directly reference `DateTime.Now`
- Inconsistent with UTC-everywhere policy

**Action Required:**
- Inject `CultureService` in Reconciliation component
- Replace `DateTime.Now` with `DateTime.UtcNow` or time sourced from `CultureService`

**Owner:** Client Lead  
**Validation:** No breaking changes to UI, existing tests pass

---

### DECISION 4: ImportService Constructor Size (14 Dependencies)
**Severity:** INFO | **Status:** Accepted

**Finding:** `ImportService` has 14 constructor dependencies.

**Assessment:** Design is acceptable because:
- Service properly delegates to focused sub-services
- Large constructor reflects orchestration role, not direct responsibility violation
- No action required at this time

**Monitoring:** Flag for review if dependencies exceed 20

---

## Backend Code Quality Decisions (Lucius)

### DECISION 5: Refactor Six Critically Nested Methods
**Severity:** CRITICAL | **Effort:** 4-6 hours | **Status:** Pending

**Issue:** Six methods violate 20-line guideline with 3+ nesting levels:

1. **TransactionListService.AddRecurringTransactionInstancesAsync** (42 lines, 3 levels)
   - Fix: Extract `TryAddRecurringInstance()` helper

2. **TransactionListService.AddRecurringTransferInstancesAsync** (42 lines, 3 levels)
   - Fix: Extract recurring transfer validation logic

3. **ImportExecuteRequestValidator.Validate** (35 lines, 3 levels)
   - Fix: Split into `ValidateHeaderRows()`, `ValidateDateFormat()`, `ValidateAmountFormat()`

4. **RuleSuggestionResponseParser.ExtractJson** (26 lines, 3 levels)
   - Fix: Replace nested ifs with guard clauses

5. **ImportRowProcessor.DetermineCategory** (25 lines, 3 levels)
   - Fix: Extract each priority level to separate method

6. **LocationParserService** property initializer (40 lines, 3 levels)
   - Fix: Move to static constructor or factory method

**Action Required:**
- Extract methods following guard clause pattern
- Maintain existing behavior (no refactoring side effects)
- Add/update unit tests for extracted helpers

**Owner:** Backend Lead (Lucius)  
**Validation:** All tests pass, no behavior change, max 20 lines per method

---

### DECISION 6: Fix Exception Handling String Matching
**Severity:** HIGH | **Effort:** 30 minutes | **Status:** Pending

**Issue:** `ExceptionHandlingMiddleware.cs:69` uses brittle string matching:
```csharp
else if (ex is DomainException de && de.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
```

**Fix:** Use existing `ExceptionType` enum on `DomainException`:
```csharp
else if (ex is DomainException de)
{
    (status, title) = de.ExceptionType switch
    {
        ExceptionType.NotFound => (404, "Not Found"),
        ExceptionType.Validation => (400, "Validation Error"),
        ExceptionType.Conflict => (409, "Conflict"),
        _ => (400, "Domain Error")
    };
}
```

**Owner:** Backend Lead (Lucius)  
**Validation:** API tests pass, status codes correct for all exception types

---

### DECISION 7: Remove Redundant Concrete Service Registrations
**Severity:** MEDIUM | **Effort:** 15 minutes | **Status:** Pending

**Issue:** Three services registered as both interface and concrete:
- `ITransactionService` + `TransactionService`
- (likely others, documented as "for backward compatibility")

**Action Required:**
1. Search codebase for direct concrete type resolution (e.g., `ActivatorUtilities.GetServiceOrCreateInstance<TransactionService>()`)
2. If no direct consumers found, remove concrete registrations
3. If consumers found, document in DI comments

**Owner:** Infrastructure Lead  
**Validation:** Solution builds, all DI resolution works

---

### DECISION 8: Long Methods (21-40 Lines) Refactoring Plan
**Severity:** LOW | **Effort:** 3-4 hours (next sprint) | **Status:** Deferred

**Issue:** 26 methods in 21-40 line range, acceptable but candidates for extraction.

**Top candidates:**
- `ImportRowProcessor.ProcessRow` (52 lines) → Extract: `ExtractValues()`, `ValidateRow()`, `BuildPreview()`
- `ChatActionParser.ParseResponse` (68 lines) → Extract action-specific parsers
- `MerchantMappingService.LearnFromCategorizationAsync` (28 lines) → Extract pattern normalization

**Decision:** Defer to next sprint after critical 3+ nesting methods completed.

**Owner:** Backend Lead (Lucius)

---

### DECISION 9: EF Configuration Method Length (19 Files, 60-100 Lines)
**Severity:** LOW | **Effort:** 2-3 hours (optional) | **Status:** Deferred

**Finding:** EF Core fluent API configuration files are long due to inherent verbosity (acceptable).

**Recommendation:** Optional refactoring pattern if clarity needed:
```csharp
private void Configure(EntityTypeBuilder<T> builder)
{
    ConfigureTable(builder);
    ConfigureProperties(builder);
    ConfigureRelationships(builder);
    ConfigureIndexes(builder);
}
```

**Status:** Optional, not blocking

---

## Test Coverage Decisions (Barbara)

### DECISION 10: Migrate to Testcontainers (PostgreSQL)
**Severity:** CRITICAL | **Effort:** 4-6 hours | **Status:** Pending | **Blocks:** New feature work

**Issue:** Tests use in-memory databases (SQLite, EF InMemoryDatabase) instead of Testcontainers
- **Current:** `Infrastructure.Tests` use SQLite in-memory (`InMemoryDbFixture`)
- **Current:** `Api.Tests` use EF Core InMemoryDatabase (`CustomWebApplicationFactory`)
- **Risk:** Missing PostgreSQL-specific bugs (JSON columns, array types, sequence behavior, concurrency)

**Action Required:**
1. Add `Testcontainers.PostgreSql` NuGet package
2. Replace `InMemoryDbFixture` with PostgreSQL Testcontainer fixture
3. Replace `CustomWebApplicationFactory` InMemoryDatabase with Testcontainer PostgreSQL
4. Verify migration: All Infrastructure and API tests pass

**Owner:** Test Lead (Barbara)  
**Validation:** All tests pass with real PostgreSQL, no test flakiness

**Approval:** Alfred (Lead) required before implementation

---

### DECISION 11: Add API Controller Tests (2 Untested Controllers)
**Severity:** CRITICAL | **Effort:** 3-4 hours | **Status:** Pending

**Issue:** Two controllers with 6 total endpoints have no test coverage:

1. **RecurringChargeSuggestionsController** (4 endpoints)
   - `DetectAsync` — Detect recurring charges
   - `GetSuggestionsAsync` — Get recurring suggestions
   - `AcceptAsync` — Accept a suggestion
   - `DismissAsync` — Dismiss a suggestion

2. **RecurringController** (2 endpoints)
   - `GetPastDueAsync` — Get past-due recurring charges
   - `RealizeBatchAsync` — Realize batch of charges

**Test Requirements:**
- Create `RecurringChargeSuggestionsControllerTests.cs`
- Create `RecurringControllerTests.cs`
- Test happy path + error cases (400, 404, 409)
- Use Testcontainers (after Decision 10)

**Owner:** Test Lead (Barbara)  
**Validation:** All endpoint tests green, coverage > 90%

---

### DECISION 12: Add Repository Tests (4 Untested Repositories)
**Severity:** CRITICAL | **Effort:** 3-4 hours | **Status:** Pending

**Issue:** Four repositories have no test coverage:
- `AppSettingsRepository` (singleton settings auto-creation logic)
- `CustomReportLayoutRepository` (custom reports CRUD)
- `RecurringChargeSuggestionRepository` (recurring suggestion persistence)
- `UserSettingsRepository` (user preference persistence, auto-creation)

**Test Coverage Required:**
- `AppSettingsRepositoryTests.cs` (CRUD + auto-creation)
- `CustomReportLayoutRepositoryTests.cs` (CRUD + layout validation)
- `RecurringChargeSuggestionRepositoryTests.cs` (CRUD + filtering)
- `UserSettingsRepositoryTests.cs` (CRUD + auto-creation + user context)

**Owner:** Test Lead (Barbara)  
**Validation:** All repository tests pass with Testcontainers

---

### DECISION 13: Behavioral Test Gaps in Services
**Severity:** MEDIUM | **Effort:** 2-3 hours (next sprint) | **Status:** Deferred

**Issue:** Some tested services have incomplete behavioral coverage:

**TransactionService:**
- Missing: `UpdateAsync()`
- Missing: `ClearLocationAsync()`
- Missing: `ClearAllLocationDataAsync()`
- Missing: `GetByDateRangeAsync()`

**AccountService:**
- Missing: `GetAllAsync()`

**Domain Entities (behavioral coverage gaps):**
- `BudgetCategory` — possibly incomplete
- `BudgetGoal` — possibly incomplete
- `BudgetProgress` — possibly incomplete

**Action Required:** After controller/repository tests, audit and add missing tests.

**Owner:** Test Lead (Barbara)  
**Deferred:** Next sprint (after critical tests completed)

---

### DECISION 14: Remove or Justify Vanity Enum Tests
**Severity:** MEDIUM | **Effort:** 30 minutes (next sprint) | **Status:** Deferred

**Issue:** ~20 test files test enum integer values (C# compiler behavior, not domain logic):
- `BudgetScopeTests` — `Assert.Equal(0, (int)BudgetScope.Shared)`
- `DescriptionMatchModeTests` — Same pattern
- `ImportBatchStatusTests` — Same pattern
- `RecurrenceFrequencyTests` — Same pattern
- `TransferDirectionTests` — Same pattern

**Assessment:** Tests inflate coverage numbers but would never catch real regressions.

**Options:**
1. Delete immediately (preferred)
2. Document rationale if tests serve other purpose (e.g., API contract)

**Decision:** Team to decide (Alfred) — delete or document.

**Owner:** Test Lead (Barbara)  
**Deferred:** Next cleanup sprint

---

## Summary Table

| # | Decision | Severity | Effort | Owner | Status |
|---|----------|----------|--------|-------|--------|
| 1 | DIP: Controllers use interfaces | WARNING | 1-2h | Alfred | Pending |
| 2 | Consistent field style (this._) | WARNING | 30m | Code Lead | Pending |
| 3 | DateTime.Now → CultureService | WARNING | 30m | Client Lead | Pending |
| 4 | ImportService size (14 deps) | INFO | — | — | Accepted |
| 5 | Refactor 6 nested methods | CRITICAL | 4-6h | Lucius | Pending |
| 6 | Fix exception string matching | HIGH | 30m | Lucius | Pending |
| 7 | Remove redundant DI registrations | MEDIUM | 15m | Infrastructure | Pending |
| 8 | Long methods (21-40 lines) refactor | LOW | 3-4h | Lucius | Deferred |
| 9 | EF config method refactor | LOW | 2-3h | Lucius | Optional |
| 10 | Migrate to Testcontainers | CRITICAL | 4-6h | Barbara | Pending |
| 11 | Add untested controller tests | CRITICAL | 3-4h | Barbara | Pending |
| 12 | Add untested repository tests | CRITICAL | 3-4h | Barbara | Pending |
| 13 | Fill service behavioral gaps | MEDIUM | 2-3h | Barbara | Deferred |
| 14 | Remove/justify vanity tests | MEDIUM | 30m | Barbara | Deferred |

---

## Timeline

**This Sprint (Immediate):**
1. Decision 5 (Refactor 6 nested methods) — 4-6h
2. Decision 6 (Fix exception handling) — 30m
3. Decision 10 (Testcontainers migration) — 4-6h
4. Decision 1, 2, 3 (Architecture/field style/DateTime fixes) — 2h

**Next Sprint:**
5. Decision 11 (Controller tests) — 3-4h
6. Decision 12 (Repository tests) — 3-4h
7. Decision 8 (Long methods) — 3-4h
8. Decision 13 (Service behavioral gaps) — 2-3h

**Future/Optional:**
9. Decision 14 (Vanity tests cleanup)
10. Decision 9 (EF config refactor)

---

## Next Review
Estimated: 1 week (after critical decisions 5, 6, 10 completed)

---

## Implementation Status Update (2026-03-22T10-04-29)

**Major Progress:**

### COMPLETED

**Decision 10: Migrate to Testcontainers (PostgreSQL)** — ✅ DONE
- Infrastructure tests migrated from SQLite to Testcontainers PostgreSQL
- PostgreSqlFixture created with TRUNCATE isolation strategy
- All 16 repository test classes updated; InMemoryDbFixture deleted
- Test result: 183/183 passing, no SQLite-specific logic found
- Docker required for test suite (validate Docker endpoint at build time)
- **Note:** API tests (CustomWebApplicationFactory) still use EF InMemoryDatabase — separate task

**Decision 6: Fix Exception Handling String Matching** — ✅ DONE
- Created `DomainExceptionType` enum (Validation = 0, NotFound = 1)
- Updated `DomainException` to accept optional DomainExceptionType (defaults to Validation for backward compatibility)
- Switched `ExceptionHandlingMiddleware` to `switch (domainEx.ExceptionType)` pattern
- Updated all 17 "not found" throw sites across Domain/Application to pass DomainExceptionType.NotFound
- Middleware unit test updated
- No string matching — clean, exhaustive, type-safe

**Decision 3: DateTime.Now in Client** — ✅ DONE
- Replaced all 3 occurrences of `DateTime.Now` in Reconciliation.razor with `DateTime.UtcNow`
- Field initializers and year-range loop updated
- Aligns with UTC-everywhere policy

**Decision 7: Remove/Clarify Redundant Concrete Service Registrations** — ✅ DONE
- Investigated all three "backward compatibility" registrations
- Found all three are **legitimately required**:
  - `TransactionService` ← consumed by `TransactionsController`
  - `RecurringTransactionService` ← consumed by `RecurringTransactionsController`
  - `RecurringTransferService` ← consumed by `RecurringTransfersController`
- Updated DependencyInjection.cs comments to name actual consumers
- No registrations removed; clarity improved

### PENDING

**Decision 1: Controllers Should Depend on Interfaces (DIP Enforcement)** — Pending
- Assigned to Alfred (Architecture Lead)
- Three controllers still inject concrete types; requires assessment per Fortinbra pragmatic directive

**Decision 2: Enforce Consistent Field Access Style** — Pending
- Mixed `_field` vs `this._field` usage
- Recommend standardize to `this._fieldName` (majority pattern)
- Requires `.editorconfig` update or StyleCop rule enforcement

**Decision 5: Refactor Six Critically Nested Methods** — Pending
- Six methods with 3+ nesting levels identified
- TransactionListService, ImportExecuteRequestValidator, RuleSuggestionResponseParser, etc.
- Requires method extraction following guard clause pattern

**Decision 8: Long Methods (21-40 Lines) Refactoring Plan** — Deferred
- 26 candidates identified; deferring to next sprint after critical methods completed

**Decision 11: Add API Controller Tests (2 Untested Controllers)** — Pending
- RecurringChargeSuggestionsController (4 endpoints)
- RecurringController (2 endpoints)
- Depends on Decision 10 (Testcontainers migration for API tests)

**Decision 12: Add Repository Tests (4 Untested Repositories)** — Pending
- AppSettingsRepository, CustomReportLayoutRepository, RecurringChargeSuggestionRepository, UserSettingsRepository
- Depends on Decision 10 (completed for Infrastructure)

**Decision 13: Behavioral Test Gaps in Services** — Deferred
- TransactionService missing tests for UpdateAsync, ClearLocationAsync, ClearAllLocationDataAsync, GetByDateRangeAsync
- AccountService missing GetAllAsync tests
- Domain entity coverage gaps (BudgetCategory, BudgetGoal, BudgetProgress)

**Decision 14: Remove or Justify Vanity Enum Tests** — Deferred
- ~20 enum integer-value tests identified (compile-time behavior, not domain logic)
- Requires team decision (Alfred): delete or document rationale

---

---

## 2026-03-24: Multi-Vendor Bank Connector Support (Alfred, Copilot)

**Decision:** Promote multi-vendor bank connector support from future consideration to first-class feature in Feature 126.

**Key Architectural Changes:**
- **ConnectorRegistry pattern:** `IBankConnectorRegistry` manages available connectors (Plaid, Nordigen, etc.), their regions, and enabled/configured state.
- **`BankConnection` entity gains `ConnectorType` field:** Each linked account stores which vendor adapter it uses.
- **User-selectable connectors at link time:** UI filters available connectors from the registry by region.
- **Configuration via `appsettings.json`:** Each connector configured with `Enabled`, `DisplayName`, `SupportedRegions`.
- **Seamless sync routing:** System resolves correct `IBankConnector` implementation via registry during background sync.

**Rationale:**
- User request: "Support multiple vendors; let users decide who they use, especially US vs EU."
- Original `IBankConnector` abstraction already supports this pattern.
- Non-Goal NG3 ("one active connector per deployment") was overly conservative.
- Promotes architecture clarity: ConnectorRegistry is straightforward; new vendors require Infrastructure layer changes only.

**Implementation Plan:**
- Phase 1: Plaid adapter (existing plan, Slice 3–6)
- Phase 2: Nordigen adapter (new Infrastructure project)
- Phase 3+: Additional vendors (same pattern)

**Feature Doc 126 Updates:**
- Removed Non-Goal NG3
- Added Goal G8 (multi-vendor with user selection)
- Architecture section specifies ConnectorRegistry with region filtering
- 10 new acceptance criteria (AC-126-36 through AC-126-45)
- New Connector Configuration section showing `appsettings.json` structure

**Decisions Made:**
- `ConnectorType` is required on `BankConnection`
- Region filtering at API level
- Each connector independently configured (enable/disable at deployment time)
- Backward compatible: CSV import unchanged

**Status:** Specification complete; implementation can proceed.

---

## Approval

**Completed (2026-03-22):**
- ✅ Testcontainers migration (Infrastructure)
- ✅ Exception handling enum routing
- ✅ DateTime.Now → DateTime.UtcNow

**Completed (2026-03-24):**
- ✅ Multi-Vendor Bank Connector Support (Feature 126 scope update)
- ✅ DI registration clarification

**Pending:**
- Alfred review of remaining DIP assessments (Decision 1)
- Alfred decision on vanity enum tests (Decision 14)
- Lucius refactoring plan for 6 critically nested methods (Decision 5)

---

## Reconciliation System Investigation & Proposals (2026-03-24)

**Author:** Alfred (Architect)  
**Requested by:** Fortinbra  
**Status:** Proposals & Findings Documented

### Executive Summary

Audited the existing reconciliation system (Features 028, 038, 039). It covers ~30% of transactions (recurring-only) and has 6 functional gaps preventing full account reconciliation against bank statements.

### Current System Strengths

| Layer | Status |
|-------|--------|
| **Domain** | `ReconciliationMatch` with confidence scoring (High/Medium/Low), status lifecycle (Suggested → Accepted/Rejected/AutoMatched), manual linking/unlinking, configurable `TransactionMatcher`. |
| **Application** | `ReconciliationService` (orchestrator), `ReconciliationStatusBuilder`, `ReconciliationMatchActionHandler`, `LinkableInstanceFinder` — solid service layer. |
| **API** | 10 well-designed endpoints at `/api/v1/reconciliation/` covering status, match review, linking, and bulk operations. |
| **UI** | `/reconciliation` page with period/account filtering, match review cards, tolerance settings, full workflows. |
| **Tests** | Domain, Application, API, Client test coverage is solid. |

### Identified Gaps (6 Critical Issues)

| Gap | Severity | Impact | Fix Proposal |
|-----|----------|--------|--------------|
| 1. No statement balance comparison | CRITICAL | Users cannot input bank ending balance or see app-to-bank discrepancy. | Proposal 2: Add statement reconciliation workflow |
| 2. No "cleared" status on transactions | CRITICAL | No way to mark individual transactions as verified against bank statement. | Proposal 1: Add `IsCleared` + `ClearedAtUtc` to Transaction |
| 3. No running cleared balance | CRITICAL | Cannot see sum of cleared-only transactions (standard reconciliation metric). | Derived from Proposal 1 (cleared balance display) |
| 4. Non-recurring transactions invisible to reconciliation | HIGH | 70% of transactions (one-offs, manual entries) outside reconciliation workflow. | Proposal 3: Extend reconciliation view to all transactions, not just recurring instances |
| 5. No line-by-line checkoff workflow | HIGH | UI is "suggestion-oriented" (accept/reject AI matches), not "user-verification-oriented" (mark cleared per line). | Proposal 2 includes checkoff mode |
| 6. No reconciliation history or completion | HIGH | Cannot mark "reconciled through June 30" or audit past reconciliations. | Proposal 2: Add `ReconciliationSession` concept with locked records & history |

### Ranked Proposals for Resolution

#### Proposal 1: Transaction Cleared Status + Cleared Balance Display ⭐ **RECOMMENDED FIRST**

**Problem:** User cannot mark transactions as cleared or see cleared balance.

**Solution:**
- Add `IsCleared` (bool, default false) + `ClearedAtUtc` (DateTime?, null) to `Transaction` entity
- API endpoints for mark-cleared, mark-uncleared, bulk-clear
- Cleared balance display in account header
- Checkbox column in transaction table UI
- Database migration with index on (AccountId, IsCleared)

**Complexity:** Medium (domain change, migration, service, API, UI)  
**Value:** Foundation — Proposals 2, 3, 5 depend on this  
**Risk:** Low — optional boolean, backward-compatible

**Why First:** Immediate user relief. Even without full reconciliation workflow, users can manually checkoff against bank statements and see running cleared balance.

---

#### Proposal 2: Statement Balance Reconciliation Workflow

**Problem:** User cannot input bank's ending balance and identify the discrepancy.

**Solution:**
- New domain concept: `ReconciliationSession` (account, statement date, statement balance, reconciliation period)
- Workflow: User enters statement balance → system shows cleared balance + difference → user checks off uncleared transactions until difference = $0.00 → "Complete Reconciliation" locks session
- Stores historical `ReconciliationRecord` per account per period for audit trail
- New endpoints and UI page for guided reconciliation experience

**Complexity:** Large (new aggregate root, new workflow, new UI)  
**Value:** Transforms reconciliation from ad-hoc to systematic  
**Depends On:** Proposal 1 (cleared status)

---

#### Proposal 3: All-Transaction Reconciliation View (Not Just Recurring)

**Problem:** Non-recurring/one-off transactions invisible to reconciliation system.

**Solution:**
- Extend Reconciliation page to show **all** transactions for the period, not just recurring instances
- Each row shows state: matched-to-recurring, cleared, or unverified
- Filter for "unverified only" to quickly find gaps
- Reuse existing cleared status from Proposal 1

**Complexity:** Medium (mostly UI/query changes)  
**Value:** Closes visibility gap for 70% of transactions  
**Depends On:** Proposal 1 (cleared status)

---

#### Proposal 4: Quick-Match from Transaction List

**Problem:** Current workflow requires navigating to `/reconciliation`. Friction is high.

**Solution:**
- Add "Reconcile" action button per transaction row in Transactions page
- Opens existing `LinkableInstancesDialog` inline (reuse, don't rebuild)
- Also allow direct "mark cleared" from transaction row

**Complexity:** Small (reuses existing components & APIs)  
**Value:** Accessibility improvement  
**PR-Level:** Yes — could ship without feature doc

---

#### Proposal 5: Reconciliation Summary on Account Dashboard

**Problem:** User must navigate to `/reconciliation` to see if anything needs attention. No at-a-glance indicator.

**Solution:**
- Add reconciliation health badge per account (e.g., "45 of 120 transactions cleared" or "Last reconciled: June 15")
- Clicking navigates to reconciliation for that account
- Extend account endpoint with reconciliation summary or new dedicated endpoint

**Complexity:** Small-to-Medium (new component, new endpoint/summary)  
**Value:** Discoverability improvement  
**Depends On:** Proposal 1 (cleared status)

---

### Recommendation

**Start with Proposal 1 (Transaction Cleared Status)** because:
1. **Foundation** — Proposals 2, 3, 5 depend on it
2. **Immediate value** — Users can manually reconcile without full workflow
3. **Moderate scope** — Clean vertical slice (domain → migration → service → API → UI)
4. **Low risk** — Optional boolean, backward-compatible, no existing tests break

**Follow-up:** Proposal 2 (Statement Balance Workflow) after Proposal 1 ships.

---

### Feature Documentation Required

- **Proposal 1:** Yes — full feature doc (125a-like vertical slices)
- **Proposal 2:** Yes — significant new workflow (125b-like)
- **Proposal 3:** Likely — section within Proposal 2 doc
- **Proposal 4:** No — small PR-level enhancement
- **Proposal 5:** No — incremental enhancement after Proposal 1

---

## Performance Baseline Decision (2026-03-23)

**Author:** Lucius  
**Status:** Implemented ✅

### Decision

**Extended baseline.json to cover all four load test scenarios** (`get_accounts`, `get_budgets`, `get_calendar`, `get_transactions`) instead of only `get_transactions`. Fixed CI pipeline (`performance.yml`) to independently compare each scenario's CSV file against baseline.

### Rationale

Previously, only `get_transactions` had a baseline entry; the other three appeared "New" in every CI run, disabling regression detection for 75% of load tests. Additionally, `performance.yml` used `tail -1` to pick one CSV, missing the other three entirely.

### Metrics (dev hardware, in-memory DB)

| Scenario         | p95 (ms) | p99 (ms) | RPS  |
|------------------|----------|----------|------|
| get_accounts     | 0.66     | 0.84     | 9.14 |
| get_budgets      | 0.76     | 1.54     | 9.14 |
| get_calendar     | 12.02    | 15.53    | 9.14 |
| get_transactions | 11.61    | 30.43    | 9.14 |

### Future Work

When running against real PostgreSQL (`PERF_USE_REAL_DB=true`), re-run load tests and regenerate `baseline.json` with real database metrics.

---

## Release v3.25.0 Coordination Checkpoint (2026-03-23)

**Status:** lucius/history.md committed. Waiting for alfred-changelog to finish CHANGELOG before version tag.

**Coordination:** Both documents (history.md, CHANGELOG) completed; tag imminent (awaiting final merge/approval).

---

## Feature 127: Hybrid Chart Implementation Strategy (2026-03-24)

**Author:** Alfred (via user directive)
**Status:** Implemented ✅

### Decision

**Hybrid approach is the PRIMARY strategy** for Feature 127 (Enhanced Charts & Visualizations). Self-implement Tier 1 & 2 chart types (7 of 9) using existing SVG + Blazor pattern (zero new JS dependencies). Use `Blazor-ApexCharts` **only** for Tier 3 (Treemap, Radar) where algorithms are error-prone to maintain in-house.

### Tier Classification

| Tier | Charts | Approach | JS Dependency |
|------|--------|----------|---------------|
| 1 | Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick | Self-implement (SVG + Blazor) | None |
| 2 | Waterfall, Box Plot | Self-implement (extra statistical testing) | None |
| 3 | Treemap, Radar | `Blazor-ApexCharts` v6.1.0 | ~80 KB gzipped |

### Rationale

1. Preserves zero-JS-dependency advantage for 7 of 9 chart types.
2. Squarified rectangle packing (Treemap) and trigonometric polygon rendering (Radar) are error-prone in-house; library handles this robustly.
3. Unified visual language via CSS custom property theming across both render paths.
4. Bundle validated in Slice 1 (~80 KB gzipped, within 200 KB budget).

### Implementation Slices

- **Slice 1:** ApexCharts spike + ChartThemeService + ChartColorProvider ✅ Done
- **Slice 2:** Chart data service foundation (models + interface + implementation) ✅ Done
- **Slice 3:** Tier 1 — Heatmap, Scatter
- **Slice 4:** Tier 1 — Stacked Area, Radial Bar, Candlestick
- **Slice 5:** Tier 2 — Waterfall, Box Plot
- **Slice 6:** Tier 3 — Treemap, Radar (ApexCharts)

### Feature Doc

`docs/127-enhanced-charts-and-visualizations.md` updated to reflect hybrid as primary recommendation.

---

## Feature 127 Slice 1: ApexCharts Integration Decisions (2026-04-04)

**Author:** Alfred
**Status:** Implemented ✅

### Decisions

1. **Package `Blazor-ApexCharts` v6.1.0** — added via `dotnet add package`. Targets `net10.0` natively. No manual `.csproj` edits.
2. **No manual `<script>` tag** — v6.x uses ES module lazy loading via `IJSRuntime`; static web assets served automatically.
3. **`AddApexCharts()` registered in `Program.cs`** — adds scoped `IApexChartService` for global options; present for future Tier 3 components.
4. **`ChartThemeService` injects concrete `ThemeService`** — `IThemeService` does not exist; consistent with existing DI registration pattern.
5. **Dark themes are `dark` and `vscode-dark` only** — `system` defaults to light (C# cannot read browser OS preference without async JS interop).
6. **Colours hardcoded in C#** — CSS custom properties cannot be read from .NET. Values from `tokens.css` / `dark.css` embedded. Must sync on design token changes.
7. **`GetCategoryColor` deterministic via hash** — `Math.Abs(name.GetHashCode() % Palette.Length)` — stable per session, not cross-runtime (acceptable for visual assignment).

### Test Results

- 11 new tests (6 `ChartThemeServiceTests`, 5 `ChartColorProviderTests`) — all passed
- Full Client suite: 2729 passed, 0 failed, 1 pre-existing skip
- Build: 0 warnings, 0 errors

---

## Feature 127 Slice 2: ChartDataService Behavioral Contracts (2026-04-04)

**Author:** Barbara (contracts), Lucius (implementation)
**Status:** Implemented ✅

### Contracts (Barbara — RED phase)

20 behavioral tests established across 4 methods. Key contracts:

- **`BuildSpendingHeatmap`:** 7-row invariant (Mon=0…Sun=6); absolute amounts; same-day aggregation; multi-week separation.
- **`BuildBudgetWaterfall`:** Spending sorted by absolute amount descending; cumulative running totals from income; net segment last (`IsTotal=true`); net can be negative; `CategorySpendingDto.Amount.Amount` for decimal.
- **`BuildBalanceCandlesticks`:** Open = first balance; Close = last; `IsBullish = Close >= Open`; multi-month ascending order.
- **`BuildCategoryDistributions`:** Tukey's hinges; IQR × 1.5 outlier detection; `monthsBack` relative to dataset max date; one `BoxPlotSummary` per category.

### Implementation (Lucius — GREEN phase)

- `ChartDataService` sealed class — all 20 tests passed.
- DayIndex mapping: `dow == DayOfWeek.Sunday ? 6 : (int)dow - 1`.
- WeekIndex: 0-based offset from earliest transaction's Monday.
- `monthsBack` reference: `list.Max(t => t.Date)` — deterministic regardless of wall clock.
- Waterfall sort: ascending by `Amount.Amount` (all-negative = descending absolute).
- Tukey's hinges: shared `ComputeMedian` helper for both half-arrays.

### Test Results

- ChartDataServiceTests: 20/20 passed
- Full Client suite: 2718 passed, 0 failed, 1 pre-existing skip
- Build: 0 warnings, 0 errors

---

## Process Directive: Archive Consolidation (2026-03-24)

**Author:** User (via Copilot)
**Status:** Standing policy ✅

All archived feature docs must be consolidated into 10-feature range files (e.g., `111-120-*.md`). No individual standalone feature files are allowed in `docs/archive/`. When archiving a feature, always merge it into the appropriate range file, creating the range file if it does not yet exist.
