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

## Approval

**Completed (2026-03-22):**
- ✅ Testcontainers migration (Infrastructure)
- ✅ Exception handling enum routing
- ✅ DateTime.Now → DateTime.UtcNow
- ✅ DI registration clarification

**Pending:**
- Alfred review of remaining DIP assessments (Decision 1)
- Alfred decision on vanity enum tests (Decision 14)
- Lucius refactoring plan for 6 critically nested methods (Decision 5)
