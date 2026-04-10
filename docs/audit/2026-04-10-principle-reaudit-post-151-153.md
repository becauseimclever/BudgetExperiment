# Principle Re-Audit — 2026-04-10

**Scope:** Verification of prior audit findings (F-001 through F-007) and review of changes introduced by Features 151-153
**Auditor:** Vic
**Requested by:** Fortinbra
**Trigger:** Feature 153 (god controllers split) marked Done

---

## Executive Summary

The refactoring effort across Features 151-153 has been largely successful in addressing the Critical and High-severity findings from the April 9th audit. **F-001 (Critical financial display issue) is fully resolved** — all 7 bare `.ToString("C")` calls in Statement Reconciliation have been replaced with `.FormatCurrency(Culture.CurrentCulture)`. **The DIP violations (F-002, F-003) are resolved** — both `CalendarController` and `AccountsController` now inject interfaces. **The ISP violation (F-004) is resolved** — `ITransactionRepository` has been decomposed into focused `ITransactionQueryRepository`, `ITransactionImportRepository`, and `ITransactionAnalyticsRepository` interfaces. The controller splits (F-007) successfully reduced `TransactionsController` (401→ 198+235 lines) and `RecurringTransactionsController`/`RecurringTransfersController` both dropped below 200 lines. However, **significant god class debt remains** (F-005, F-006 partially resolved): 17 Application services still exceed 300 lines, and the `Transaction` entity remains at 532 lines despite factory extraction. The Minimal API pilot (`CategorySuggestionEndpoints`) is well-structured and under the 300-line threshold. One new concern: 4 controllers now sit just above 300 lines (302-323), a sign of creeping growth.

---

## Section 1: Prior Findings Review

### F-001 ✅ Resolved — Bare `.ToString("C")` in Statement Reconciliation UI

**Evidence:** All 7 instances identified in the original audit have been replaced with `.FormatCurrency(Culture.CurrentCulture)`:
- `ReconciliationBalanceBar.razor` now uses `FormatCurrency` at lines 7, 11, 23
- `ClearableTransactionRow.razor` now uses `FormatCurrency` at line 22
- `ReconciliationHistory.razor` now uses `FormatCurrency` at lines 68, 69
- `ReconciliationDetail.razor` now uses `FormatCurrency` at line 55

Zero `.ToString("C")` calls remain anywhere in the Client project.

---

### F-002 ✅ Resolved — DIP Violation: CalendarController

**Evidence:** `CalendarController.cs` line 22 now declares `private readonly ICalendarService _calendarService`. Constructor at line 43 accepts `ICalendarService calendarService`. The interface injection is complete.

---

### F-003 ✅ Resolved — DIP Violation: AccountsController

**Evidence:** `AccountsController.cs` line 22 now declares `private readonly IAccountService _service`. Constructor at line 28 accepts `IAccountService service`. The interface injection is complete.

---

### F-004 ✅ Resolved — ISP Violation: ITransactionRepository (23 methods)

**Evidence:** The repository interface has been split into focused interfaces:
- `ITransactionQueryRepository` — date-range queries, paged queries, search (150 lines)
- `ITransactionImportRepository` — duplicate detection, batch queries (47 lines)
- `ITransactionAnalyticsRepository` — health analysis, spending by category

`ITransactionRepository` now composes these interfaces plus base `IReadRepository<Transaction>` and `IWriteRepository<Transaction>`, with only one additional method (`DeleteTransferAsync`). The interface is now 31 lines total.

---

### F-005 ⚠️ Partially Resolved — God Class: Transaction Entity

**Status:** 545 → 532 lines (marginal reduction)

**Evidence:** `TransactionFactory.cs` was extracted (155 lines) containing all factory methods (`Create`, `CreateFromRecurring`, `CreateTransfer`, `CreateFromRecurringTransfer`). The `Transaction` entity now delegates to `CreateRaw` for instantiation.

**Remaining issue:** The entity is still 532 lines — well above the 300-line threshold. It retains state management, update methods, reconciliation linking, location management, and import batch assignment. The factory extraction addressed one responsibility but others remain.

**Assessment:** Partial resolution. The factory extraction is correct and follows the recommendation. Further decomposition would require extracting reconciliation behavior or introducing aggregate root patterns.

---

### F-006 ⚠️ Partially Resolved — Application Services Exceeding 300-Line Limit

**Original count:** 18 services over 300 lines
**Current count:** 17 services over 300 lines

**Changes observed:**
- `ChatActionParser.cs`: 482 → 174 lines ✅ (Split into `TransactionActionParser`, `TransferActionParser`, `RecurringTransactionActionParser`, `RecurringTransferActionParser`, `ClarificationActionParser`, `ChatParserHelpers`)
- `ImportRowProcessor.cs`: 508 → 323 lines ⚠️ (Reduced but still over 300; `ImportFieldExtractor` and `ImportFieldParser` extracted)

**Still over 300 lines (17 services):**
| Service | Lines | Notes |
|---------|-------|-------|
| `ChatService` | 487 | Not addressed |
| `RuleSuggestionResponseParser` | 472 | Not addressed |
| `CategorySuggestionService` | 431 | Not addressed |
| `TransactionListService` | 431 | Not addressed |
| `CategorizationRuleService` | 411 | Not addressed |
| `RuleSuggestionService` | 394 | Not addressed |
| `CalendarGridService` | 389 | Not addressed |
| `MerchantKnowledgeBase` | 369 | Not addressed |
| `DataHealthService` | 358 | Not addressed |
| `ImportMappingService` | 339 | Not addressed |
| `RecurringTransactionService` | 332 | Not addressed |
| `ReportService` | 331 | Not addressed |
| `TransferService` | 328 | Not addressed |
| `RecurringTransferService` | 325 | Not addressed |
| `AiPrompts` | 324 | Not addressed |
| `ImportRowProcessor` | 323 | Reduced from 508 |
| `DayDetailService` | 309 | Not addressed |

**Assessment:** Partial resolution. The Chat and Import splits were executed well, but the scope only covered 2 of the top 5 offenders. The bulk of the debt remains.

---

### F-007 ✅ Resolved — API Controllers Exceeding 300-Line Limit

**Original offenders:**
- `TransactionsController.cs` — 401 lines → **Split into** `TransactionQueryController.cs` (198 lines) + `TransactionBatchController.cs` (235 lines) ✅
- `RecurringTransactionsController.cs` — 390 lines → 140 lines ✅
- `RecurringTransfersController.cs` — 388 lines → 184 lines ✅
- `CategorySuggestionsController.cs` — 309 lines → **Replaced with** `CategorySuggestionEndpoints.cs` (275 lines, Minimal API) ✅

**New borderline controllers (302-323 lines):**
| Controller | Lines |
|------------|-------|
| `RecurringTransactionInstanceController` | 323 |
| `CategorizationRulesController` | 321 |
| `ReportsController` | 306 |
| `CalendarController` | 302 |

**Assessment:** Fully resolved for the original findings. The splits are well-structured, each controller has a single cohesive purpose, and the Minimal API pilot is a good architectural direction. The new borderline controllers were not in scope for F-153 but warrant monitoring.

---

## Section 2: New Findings

### N-001 Medium — Domain Entities Remain God Classes

**Location:**
| Entity | Lines |
|--------|-------|
| `Transaction.cs` | 532 |
| `RuleSuggestion.cs` | 486 |
| `RecurringTransfer.cs` | 401 |
| `RecurringTransaction.cs` | 391 |
| `CategorizationRule.cs` | 383 |
| `ReconciliationMatch.cs` | 336 |
| `ImportMapping.cs` | 318 |
| `RecurringChargeSuggestion.cs` | 316 |
| `Account.cs` | 315 |

**Principle:** Engineering Guide §24 — "God services (>~300 lines)" are forbidden. While the guide specifically calls out "services," the principle of cohesion applies to entities.

**Observation:** 9 domain entities exceed 300 lines. These entities combine identity, behavior, factory methods, and state transitions. The `Transaction` factory extraction was a good start but more decomposition is needed.

**Recommendation:** For each entity, consider:
1. Extracting factory methods to dedicated `*Factory` classes (as done with `TransactionFactory`)
2. Moving complex state transitions to domain services
3. Using the Entity Base Class pattern to separate identity/auditing from business logic

---

### N-002 Low — Minimal API Endpoint Has Inline DTO Mapping

**Location:** `src/BudgetExperiment.Api/Endpoints/CategorySuggestionEndpoints.cs` (line 257)

**Observation:** The `MapToDto` method is defined as a private static method inside the endpoint class. This is acceptable but creates a precedent question: as more controllers migrate to Minimal API, should mapping logic live in endpoints or in dedicated mappers (per existing `*Mapper` classes in Application layer)?

**Recommendation:** Document in team decisions whether Minimal API endpoints should use inline mappers (for self-containment) or Application-layer mappers (for consistency with Controller pattern). Either is valid; consistency matters.

---

### N-003 Low — Four Controllers at 300-Line Boundary

**Location:**
- `RecurringTransactionInstanceController.cs` — 323 lines
- `CategorizationRulesController.cs` — 321 lines
- `ReportsController.cs` — 306 lines
- `CalendarController.cs` — 302 lines

**Observation:** These controllers are just above the 300-line threshold. They weren't in scope for F-153, but they're now the next candidates for attention.

**Recommendation:** Track these. If any grows by 30+ lines in future features, schedule a split. `CategorizationRulesController` (rule management, conflict detection, batch creation) has clear split potential.

---

## Section 3: Strengths

1. **F-001 financial display fix is complete.** Every currency display in the Client now uses the `FormatCurrency()` extension with explicit culture. This is the most important fix — it directly impacts user trust in a financial application.

2. **Repository interface split is textbook ISP.** The decomposition of `ITransactionRepository` into Query, Import, and Analytics interfaces is well-designed. The focused interfaces have clear responsibilities and are easily testable in isolation.

3. **Controller splits follow single responsibility.** `TransactionQueryController` handles only GET operations; `TransactionBatchController` handles mutations. The separation is logical and makes each controller easier to understand and test.

4. **ChatActionParser refactoring is excellent.** The extraction of per-action-type parsers (`TransactionActionParser`, `TransferActionParser`, etc.) follows the Single Responsibility Principle and makes extending the chat feature straightforward.

5. **Minimal API pilot is well-structured.** The `CategorySuggestionEndpoints` class demonstrates a clean Minimal API pattern: grouped endpoints, explicit route registration, and proper OpenAPI metadata. This is a good foundation if the team decides to migrate more controllers.

6. **TransactionFactory extraction is a model to follow.** The factory encapsulates all creation logic with proper guard clauses while the entity retains state management. This pattern should be replicated for `RuleSuggestion`, `RecurringTransfer`, etc.

7. **Import service decomposition reduces cognitive load.** Splitting `ImportFieldExtractor` and `ImportFieldParser` from `ImportRowProcessor` makes the import pipeline understandable: extract → parse → process.

---

## Section 4: Executive Summary

Features 151-153 have resolved all Critical and High-priority findings that were technically addressable within their scope. The financial display issue (F-001) is eliminated. The DIP violations (F-002, F-003) and ISP violation (F-004) are fixed. The controller size issues (F-007) are resolved through well-executed splits. However, substantial god class debt remains: 17 Application services and 9 Domain entities still exceed 300 lines. The refactoring approach demonstrated in these features — extracting factories, decomposing parsers, splitting interfaces — is sound and should continue. The Minimal API pilot introduces a new architectural pattern that warrants team consensus before broader adoption. Overall, the codebase is healthier than before April 9th, but the work is not complete.

---

**Next audit trigger:** When god class reduction features (targeting F-005/F-006 remaining items) are marked Done, or upon manual request.
