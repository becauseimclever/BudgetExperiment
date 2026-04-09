# Full Principle Audit — 2026-04-09

**Scope:** Full codebase review against Engineering Guide (copilot-instructions.md)
**Auditor:** Vic
**Requested by:** Fortinbra
**Commit:** HEAD at time of audit
**Codebase:** All 7 `src/` projects, all `tests/` projects

---

## Executive Summary

The codebase is architecturally sound at the macro level: clean layer separation is maintained, EF Core does not leak into Domain or Application, monetary values use `decimal` throughout, and `DateTime.UtcNow` is used consistently. The financial invariant framework (ACCURACY-FRAMEWORK.md) is well-designed and mostly covered by tests. However, two categories of drift demand attention. **First, a Critical financial display issue:** 7 instances of bare `.ToString("C")` in the Statement Reconciliation UI will render monetary values incorrectly for any user whose browser locale is not en-US — directly undermining the accuracy promise of a financial application. **Second, structural debt is accumulating:** 2 controllers still inject concrete service types (a known violation from Decision #2 that remains unfixed), the `ITransactionRepository` interface has grown to 23 methods violating ISP, 18 Application services exceed 300 lines, and the `Transaction` entity has ballooned to 545 lines. No Critical financial *calculation* issues were found — the money math is honest. The display layer is where trust breaks down.

---

## Findings

### F-001 Critical — Bare `.ToString("C")` in Statement Reconciliation UI

**Location:** 4 Razor files, 7 instances:
- `src/BudgetExperiment.Client/Shared/StatementReconciliation/ReconciliationBalanceBar.razor` (lines 5, 9, 21)
- `src/BudgetExperiment.Client/Shared/StatementReconciliation/ClearableTransactionRow.razor` (line 21)
- `src/BudgetExperiment.Client/Pages/StatementReconciliation/ReconciliationHistory.razor` (lines 67, 68)
- `src/BudgetExperiment.Client/Pages/StatementReconciliation/ReconciliationDetail.razor` (line 54)

**Principle:** Engineering Guide §38 — "All client-side currency formatting MUST use the `FormatCurrency()` extension method with `CultureService.CurrentCulture`. Never use bare `ToString("C")` without an explicit `IFormatProvider`."

**Observation:** These files call `.ToString("C")` on `decimal` values without passing an `IFormatProvider`. The `CurrencyFormattingExtensions.FormatCurrency()` method exists and is used correctly elsewhere in the codebase, but the Statement Reconciliation feature bypasses it entirely.

**Impact:** Users with non-US browser locales will see monetary values formatted with the wrong currency symbol, decimal separator, or grouping. In a financial application, displaying "$1,234.56" as "1.234,56 €" or "¤1,234.56" erodes user trust in the accuracy of their financial data. This violates the spirit of INV-1 through INV-5 (balance identity) at the display boundary.

**Recommendation:** Replace all 7 instances with `.FormatCurrency(CultureService.CurrentCulture)` or equivalent. Inject `CultureService` into the affected components. Add bUnit tests asserting culture-correct formatting.

---

### F-002 High — DIP Violation: CalendarController Injects Concrete CalendarService

**Location:** `src/BudgetExperiment.Api/Controllers/CalendarController.cs` (lines 22, 43)

**Principle:** Engineering Guide §7 (DIP) — "Higher layers depend on abstractions." Decision #2 — "Use interface… controller incorrectly uses concrete type."

**Observation:** `CalendarController` declares `private readonly CalendarService _calendarService` and accepts `CalendarService calendarService` in its constructor. No `ICalendarService` interface exists. Other services in the same controller (`ICalendarGridService`, `IDayDetailService`, `IKakeiboCalendarService`) correctly use interfaces.

**Impact:** This is a known violation from Decision #2 (2026-03-22) that remains unfixed. It prevents mocking `CalendarService` in API tests, makes the controller tightly coupled to one implementation, and sets a precedent that concrete injection is acceptable.

**Recommendation:** Extract `ICalendarService` interface from `CalendarService`, update the controller to depend on it, and register it in DI.

---

### F-003 High — DIP Violation: AccountsController Injects Concrete AccountService

**Location:** `src/BudgetExperiment.Api/Controllers/AccountsController.cs` (lines 22, 28)

**Principle:** Engineering Guide §7 (DIP), Decision #2.

**Observation:** `AccountsController` declares `private readonly AccountService _service` and accepts `AccountService service` in its constructor. No `IAccountService` interface exists.

**Impact:** Same as F-002. This is the same class of violation identified in Decision #2 but for a different controller. `AccountService` cannot be mocked in API tests.

**Recommendation:** Extract `IAccountService` interface, update controller and DI registration.

---

### F-004 High — ISP Violation: ITransactionRepository Has 23 Methods

**Location:** `src/BudgetExperiment.Domain/Repositories/ITransactionRepository.cs` (254 lines)

**Principle:** Engineering Guide §7 (ISP) — "Lean interfaces (split broad repository behaviors as needed — e.g., `IReadRepository<T>`, `IWriteRepository<T>`)."

**Observation:** `ITransactionRepository` inherits from `IReadRepository<Transaction>` (3 methods) and `IWriteRepository<Transaction>` (2 methods), then declares 18 additional methods of its own, totaling 23 methods. Methods range from date-range queries to duplicate detection to paged unified queries to health analysis — at least 4 distinct concerns.

**Impact:** Any new implementation or test fake must satisfy all 23 methods. This discourages alternative implementations and makes test setup verbose. The interface conflates read queries, write operations, and analytical queries.

**Recommendation:** Split into focused interfaces: `ITransactionQueryRepository` (date range, daily totals), `ITransactionImportRepository` (duplicate detection, batch queries), `ITransactionAnalyticsRepository` (health analysis, spending by category), keeping `ITransactionRepository` as a composition root if backward compatibility is needed.

---

### F-005 High — God Class: Transaction Entity (545 lines)

**Location:** `src/BudgetExperiment.Domain/Accounts/Transaction.cs`

**Principle:** Engineering Guide §8 — "God services (>~300 lines or too many responsibilities)" are forbidden. §12 — "Entities: Identity + behavior."

**Observation:** The `Transaction` entity is 545 lines and contains factory methods for at least 5 creation scenarios (`Create`, `CreateFromRecurringTransaction`, `CreateFromRecurringTransfer`, `CreateTransferPair`, etc.), update logic, import-batch assignment, reconciliation linking, and location management.

**Impact:** Violates SRP — a single entity shouldn't be the factory, the updater, and the state manager for reconciliation. Makes the class difficult to reason about and test in isolation.

**Recommendation:** Extract factory methods to a `TransactionFactory` domain service. Move reconciliation-specific behavior to a dedicated domain service or pattern.

---

### F-006 High — 18 Application Services Exceed 300-Line Limit

**Location:** (see list below)

**Principle:** Engineering Guide §8/§24 — "God services (>~300 lines or too many responsibilities)" are forbidden.

**Observation:** 18 services in the Application layer exceed the 300-line threshold:

| Service | Lines |
|---------|-------|
| `RuleSuggestionResponseParser` | 515 |
| `ImportRowProcessor` | 508 |
| `ChatService` | 487 |
| `ChatActionParser` | 482 |
| `CategorySuggestionService` | 453 |
| `TransactionListService` | 430 |
| `CategorizationRuleService` | 411 |
| `RuleSuggestionService` | 394 |
| `CalendarGridService` | 388 |
| `MerchantKnowledgeBase` | 369 |
| `DataHealthService` | 358 |
| `ImportMappingService` | 339 |
| `RecurringTransactionService` | 332 |
| `ReportService` | 331 |
| `RecurringTransferService` | 325 |
| `AiPrompts` | 324 |
| `TransferService` | 314 |
| `DayDetailService` | 309 |

**Impact:** Large services accumulate responsibilities over time. `ChatService` at 487 lines handles session management, message handling, date parsing, category resolution, and action execution — clearly multiple concerns.

**Recommendation:** Prioritize splitting the top 5 by line count. `ImportRowProcessor` can decompose into field extraction, parsing, and validation services. `ChatService` can split into conversation management and action dispatch.

---

### F-007 High — 4 API Controllers Exceed 300-Line Limit

**Location:**
- `TransactionsController.cs` — 401 lines
- `RecurringTransactionsController.cs` — 390 lines
- `RecurringTransfersController.cs` — 388 lines
- `CategorySuggestionsController.cs` — 309 lines

**Principle:** Engineering Guide §24 — "God services (>~300 lines or too many responsibilities)."

**Observation:** These controllers have grown beyond the 300-line threshold. `TransactionsController` at 401 lines handles CRUD, batch operations, paging, filtering, and ETag concurrency — at least 3 distinct endpoint groups.

**Impact:** Large controllers are harder to review, test, and extend. Each new feature added to `TransactionsController` pushes it further from the guideline.

**Recommendation:** Split into focused controllers (e.g., `TransactionBatchController`, `TransactionQueryController`) or migrate to Minimal API endpoint groups.

---

### F-008 Medium — 24+ Long Methods in Domain Layer (>20 lines)

**Location:** Key offenders:
- `RecurringTransaction.Create()` — 53 lines (line 171)
- `Transaction.Create()` — 38 lines (line 251)
- `CategorizationRule.Create()` — 37 lines (line 136)
- `RuleSuggestion.CreateNewRuleSuggestion()` — 35 lines (line 190)
- `RecurringTransaction.Update()` — 35 lines (line 233)
- `RecurringTransfer.GetOccurrencesBetween()` — 33 lines (line 317)

**Principle:** Engineering Guide §8 — "Short methods (<~20 lines target; justify exceptions)."

**Observation:** 24 methods across the Domain layer exceed the 20-line guideline. Most are entity factory/create methods with extensive guard clauses and property assignments.

**Impact:** Long factory methods are harder to maintain and extend. Each new property added to an entity makes its `Create` method longer.

**Recommendation:** Extract guard-clause validation into private `Validate*` helper methods. Consider Builder pattern for entities with many construction parameters.

---

### F-009 Medium — Missing Pagination on List Endpoints

**Location:**
- `TransactionsController.GetByDateRangeAsync` — no pagination for potentially unbounded results
- `RecurringTransactionsController.GetAllAsync` — no pagination
- `RecurringTransfersController.GetAllAsync` — no pagination
- `CategoriesController` — list endpoints lack pagination
- `AccountsController` — list endpoints lack pagination

**Principle:** Engineering Guide §9 — "Pagination: Use `?page=1&pageSize=20` (document defaults & max)."

**Observation:** Several list endpoints return all matching records without pagination support. `GetByDateRangeAsync` on transactions could return thousands of records for a year-long date range.

**Impact:** Performance degradation for users with large datasets, especially on the target Raspberry Pi hardware. Could cause out-of-memory issues or browser rendering slowdowns.

**Recommendation:** Add pagination to all list endpoints. `GetByDateRangeAsync` is highest priority given transaction volume.

---

### F-010 Medium — Magic Strings and Numbers in API Controllers

**Location:** Distributed across multiple controllers (50+ instances):
- Validation messages: `"Month must be between 1 and 12"`, `"Year must be between 2000 and 2100"` (repeated in CalendarController, BudgetsController, ExportController, ReconciliationController)
- Magic numbers: `24` (max months), `2000`/`2100` (year range), `50` (default pageSize), `100` (max batch), `20` (default take)
- Header strings: `"X-Pagination-TotalCount"` repeated in multiple files
- ETag quote trimming: `.Trim('"')` repeated ~10 times across controllers

**Principle:** Engineering Guide §8 — "Centralize constants; avoid magic strings/numbers."

**Observation:** The same validation messages and numeric bounds are hardcoded across multiple controllers. The `"X-Pagination-TotalCount"` header name appears in at least 2 controllers without a shared constant.

**Impact:** Inconsistency risk: if the year validation range changes, every controller must be updated individually. Typos in header names would cause silent pagination failures.

**Recommendation:** Create a `ValidationConstants` or `ApiConstants` class with shared validation messages, bound values, and header names. Extract ETag parsing to a shared helper method.

---

### F-011 Medium — Two Controllers Not Sealed

**Location:**
- `src/BudgetExperiment.Api/Controllers/MerchantMappingsController.cs` — `public class MerchantMappingsController`
- `src/BudgetExperiment.Api/Controllers/VersionController.cs` — `public class VersionController`

**Principle:** Engineering Guide §7 (OCP) — "sealed where appropriate for safety." All 32 other controllers use `public sealed class`.

**Observation:** These two controllers are the only ones not marked `sealed`, inconsistent with the rest of the codebase.

**Impact:** Minor inconsistency. Unsealed controllers could be inadvertently subclassed, though this is unlikely in practice.

**Recommendation:** Add `sealed` modifier to both controllers for consistency.

---

### F-012 Medium — Assertion Framework Inconsistency in Tests

**Location:** All test projects

**Principle:** Engineering Guide §15 — "xUnit (unit tests) + Shouldly OR built-in Assert."

**Observation:** The codebase uses both Shouldly (`.ShouldBe()`, `.ShouldNotBeNull()`) and xUnit Assert (`Assert.Equal()`, `Assert.NotNull()`) across test files, often within the same project. Approximately 86 files import `using Shouldly`, while the majority of test files rely on `Assert.*`. Many files use both side by side.

**Impact:** While both are technically permitted by the Engineering Guide, the inconsistency makes the test suite feel unfinished. New contributors must guess which style to follow. The "OR" in the guide implies a project-level choice, not a per-file mix.

**Recommendation:** Pick one style per test project and standardize. Shouldly produces better failure messages; if adopting it, do so consistently per project.

---

### F-013 Medium — God Domain Classes (4 additional)

**Location:**
- `src/BudgetExperiment.Domain/Categorization/RuleSuggestion.cs` — 434 lines
- `src/BudgetExperiment.Domain/Recurring/RecurringTransfer.cs` — 350 lines
- `src/BudgetExperiment.Domain/Recurring/RecurringTransaction.cs` — 342 lines
- `src/BudgetExperiment.Domain/Categorization/CategorizationRule.cs` — 332 lines

**Principle:** Engineering Guide §24 — "God services (>~300 lines)."

**Observation:** In addition to `Transaction` (F-005), 4 more domain entities exceed 300 lines. `RuleSuggestion` at 434 lines has 5 distinct factory methods for different suggestion types.

**Impact:** Same pattern as F-005 — entities accumulating factory logic that could live in separate domain services.

**Recommendation:** Extract factory methods to dedicated factory classes (e.g., `RuleSuggestionFactory`).

---

### F-014 Medium — Infrastructure: TransactionRepository God Class (495 lines)

**Location:** `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs`

**Principle:** Engineering Guide §24.

**Observation:** 495 lines with `GetUnifiedPagedAsync` at 111 lines and `GetUncategorizedPagedAsync` at 78 lines. The repository mirrors the bloated `ITransactionRepository` interface.

**Impact:** Direct consequence of F-004 (ISP violation). The implementation inherits the interface's excessive breadth.

**Recommendation:** Splitting the interface (F-004) naturally splits this implementation.

---

### F-015 Medium — Magic Strings in Infrastructure Sort Logic

**Location:** `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs` (lines 280-291, 492-515)

**Principle:** Engineering Guide §8.

**Observation:** Hard-coded sort field names (`"AMOUNT"`, `"DESCRIPTION"`, `"CATEGORY"`, `"ACCOUNT"`) appear in switch statements in two separate methods without shared constants.

**Recommendation:** Extract to an enum or constants class shared between Application and Infrastructure layers.

---

### F-016 Medium — Client God Components

**Location:**
- `src/BudgetExperiment.Client/Components/Charts/LineChart.razor.cs` — 517 lines
- `src/BudgetExperiment.Client/Components/Charts/GroupedBarChart.razor.cs` — 305 lines

**Principle:** Engineering Guide §24.

**Observation:** Chart code-behind files exceed the 300-line limit. `LineChart` at 517 lines handles data processing, SVG path building, axis rendering, and smooth curve interpolation.

**Impact:** Reduced maintainability. Note: Decision #8 proposes migrating to Blazor-ApexCharts, which would obsolete these custom chart components.

**Recommendation:** If the ApexCharts migration (Decision #8) proceeds, deprioritize refactoring. Otherwise, extract SVG path builders and axis renderers into utility classes.

---

### F-017 Low — DTO Naming Inconsistency in Contracts

**Location:** `src/BudgetExperiment.Contracts/Dtos/` — 11 files

**Principle:** Engineering Guide §5 — naming conventions.

**Observation:** 11 DTO types in the Contracts project don't follow a consistent suffix pattern. Examples: `BatchRealizeFailure`, `BulkMatchActionResult`, `CopyBudgetGoalsResult`, `ImportResult` — these lack a `Dto` or `Response`/`Request` suffix that would clarify their role.

**Impact:** Minor readability concern. The types are in a `Dtos` folder which provides some context, but naming alone doesn't signal their purpose.

**Recommendation:** Low priority. If a naming convention is adopted, apply it during the next feature that touches these files.

---

### F-018 Low — 6 Repository Interfaces Exceed 10 Methods

**Location:**
| Interface | Own Methods | With Inherited |
|-----------|------------|----------------|
| `ITransactionRepository` | 18 | 23 |
| `IRuleSuggestionRepository` | 12 | 17 |
| `IRecurringTransferRepository` | 8 | 13 |
| `IRecurringTransactionRepository` | 6 | 11 |
| `ICategorizationRuleRepository` | 5 | 10 |
| `ICategorySuggestionRepository` | 5 | 10 |

**Principle:** Engineering Guide §7 (ISP).

**Observation:** While the top offender (`ITransactionRepository`) warrants splitting (F-004), the remaining interfaces are borderline at 10-17 methods. These are functionally cohesive repository interfaces where the methods belong to a single aggregate's read/write operations.

**Recommendation:** Monitor for growth. `IRuleSuggestionRepository` at 17 methods is approaching the threshold where splitting would add clarity.

---

## Strengths

1. **Financial arithmetic is provably correct.** `MoneyValue` is a sealed immutable record using `decimal` with 2-decimal-place rounding (`MidpointRounding.AwayFromZero`). Mixed-currency operations throw `DomainException`. No `float` or `double` appears anywhere in monetary calculation paths. This is the most important thing in a financial application, and it's done right.

2. **Clean layer separation is enforced.** Zero EF Core references in Domain or Application. All repository access goes through interfaces. No `IQueryable` leaks. The onion architecture is real, not aspirational.

3. **UTC discipline is excellent.** No `DateTime.Now` found anywhere in the production codebase. All timestamps use `DateTime.UtcNow` or `DateOnly` as appropriate. This eliminates an entire class of timezone bugs.

4. **Accuracy test framework is rigorous.** The ACCURACY-FRAMEWORK.md defines 10 financial invariants with mathematical precision. 7 are fully tested, 3 have documented gaps with prioritized remediation plans. The framework itself is a model of how financial software should be validated.

5. **No banned libraries.** Zero occurrences of FluentAssertions or AutoFixture across all test projects. The team has maintained discipline on this constraint.

6. **Value objects are well-designed.** `MoneyValue`, `RecurrencePatternValue`, `GeoCoordinateValue`, `ImportPatternValue`, `TransactionLocationValue` all follow the `*Value` suffix convention, are immutable, and encapsulate domain logic properly.

7. **File-per-type rule is maintained.** Across 126+ domain files and all other layers, each file contains one top-level type with a matching filename. This is a surprisingly consistent achievement at this codebase size.

8. **Naming conventions are followed with high consistency.** Private fields use `_camelCase`, async methods end with `Async`, controllers use `sealed`, REST routes are pluralized and versioned (`api/v{version}/{resource}`). The exceptions (F-002, F-003, F-011) are notable precisely because the baseline compliance is so high.

9. **Problem Details (RFC 7807) is properly implemented.** `ExceptionHandlingMiddleware` maps domain exceptions to standard problem details with appropriate HTTP status codes. Error handling is centralized, not scattered.

10. **Test coverage is comprehensive.** ~5,400+ tests across 5 projects with meaningful assertions. The team has invested heavily in test infrastructure (Testcontainers, `WebApplicationFactory`, bUnit, performance baselines).

---

## Priority Matrix

| ID | Severity | Area | Effort to Fix |
|----|----------|------|---------------|
| F-001 | Critical | Client — Financial Display | Low |
| F-002 | High | API — DIP | Low |
| F-003 | High | API — DIP | Low |
| F-004 | High | Domain — ISP | Medium |
| F-005 | High | Domain — SRP/God Class | Medium |
| F-006 | High | Application — SRP/God Classes | High |
| F-007 | High | API — God Controllers | Medium |
| F-008 | Medium | Domain — Clean Code | Medium |
| F-009 | Medium | API — REST Design | Medium |
| F-010 | Medium | API — Clean Code | Low |
| F-011 | Medium | API — Consistency | Low |
| F-012 | Medium | Tests — Consistency | High |
| F-013 | Medium | Domain — God Classes | Medium |
| F-014 | Medium | Infrastructure — God Class | Medium |
| F-015 | Medium | Infrastructure — Clean Code | Low |
| F-016 | Medium | Client — God Components | Low* |
| F-017 | Low | Contracts — Naming | Low |
| F-018 | Low | Domain — ISP | Low |

\* Low effort if ApexCharts migration (Decision #8) proceeds; Medium otherwise.

---

## Remediation Priority

**Immediate (this sprint):**
- F-001: Fix bare `.ToString("C")` — 7 edits, protects financial display integrity
- F-002, F-003: Extract interfaces for `CalendarService` and `AccountService` — low effort, closes Decision #2
- F-011: Add `sealed` to 2 controllers — trivial

**Next sprint:**
- F-004: Split `ITransactionRepository` into focused interfaces
- F-010: Centralize magic strings/constants in API layer
- F-015: Centralize sort field constants

**Ongoing:**
- F-005 through F-008, F-013, F-014: Decompose god classes and long methods as features touch them. Don't refactor for refactoring's sake — do it when a file needs modification.
- F-009: Add pagination when endpoints are next modified
- F-012: Standardize assertion framework per project when writing new tests
