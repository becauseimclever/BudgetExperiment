# Performance Code Review — 2026-04-09

**Scope:** Code-level performance analysis (no benchmarks run)
**Auditor:** Vic
**Requested by:** Fortinbra
**Hardware target:** Raspberry Pi (single-board ARM64, limited RAM/CPU)

## Executive Summary

The codebase demonstrates solid awareness of EF Core performance in many areas — `AsNoTracking()` and `AsNoTrackingWithIdentityResolution()` are used consistently on read-only queries, paginated endpoints exist for the primary transaction listing, and the architecture uses `Task.WhenAll` for parallel data loading in the calendar and transaction list services. UTC discipline and layer separation remain clean.

However, several structural performance issues exist that will compound on Raspberry Pi hardware. The most consequential are: (1) the `DataHealthService` loads **all transactions into memory** up to three times per `AnalyzeAsync()` call, (2) the `BudgetProgressService` issues N+1 database queries when computing monthly budget summaries (one `GetSpendingByCategoryAsync` per expense category), (3) the `ReportService` issues N+1 queries to resolve category names inside a loop, (4) multiple "Get All" repository methods return unbounded result sets with no pagination guard, and (5) the Blazor WebAssembly client uses zero `<Virtualize>` components despite rendering potentially large transaction lists.

## Findings

### P-001 [Critical] — DataHealthService loads all transactions into memory multiple times

**Location:** `src/BudgetExperiment.Application/DataHealth/DataHealthService.cs:49-89`
**Pattern:** `AnalyzeAsync()` calls `FindDuplicatesAsync()`, `FindOutliersAsync()`, and `FindDateGapsAsync()` sequentially. Each calls `GetAllForHealthAnalysisAsync()`, which loads **every transaction with Category includes** into memory (line 528-544 of `TransactionRepository`). For N transactions, this means 3×N entities materialized, tracked temporarily, and GC'd. Additionally, `GetUncategorizedSummaryAsync()` loads all uncategorized transactions unbounded.
**Impact:** On a Raspberry Pi with 1-4 GB RAM and a modest 5,000-transaction database, this materializes ~15,000+ entity objects per health analysis call. At 10,000+ transactions, this risks OOM or severe GC pressure on ARM64. The O(n²) near-duplicate comparison in `FindNearDuplicateClusters` (nested loop, lines 189-228) makes this exponentially worse.
**Recommendation:** (1) Refactor `AnalyzeAsync` to fetch the transaction set once and pass it to each sub-method. (2) Add pagination or streaming to the duplicate detection algorithm. (3) Push the grouping/aggregation for outliers and date gaps into SQL queries.

---

### P-002 [High] — BudgetProgressService N+1 query pattern in GetMonthlySummaryAsync

**Location:** `src/BudgetExperiment.Application/Budgeting/BudgetProgressService.cs:88-120`
**Pattern:** The `foreach (var category in allExpenseCategories.Where(c => c.IsActive))` loop calls `_transactionRepository.GetSpendingByCategoryAsync(category.Id, year, month)` once per category. With 20 expense categories, this is 20 sequential database round-trips, each executing a filtered SUM query.
**Impact:** On a Raspberry Pi's SD card I/O and single-core-dominated PostgreSQL, 20 sequential queries add significant latency (50-200ms each = 1-4 seconds total). This endpoint backs the Budget page, which is loaded every time the user navigates to it.
**Recommendation:** Replace with a single query that groups spending by `CategoryId` for the given month, returning all category totals in one round-trip. The `GetDailyTotalsAsync` pattern in `TransactionRepository` already demonstrates the correct GROUP BY approach.

---

### P-003 [High] — ReportService N+1 query for category names in BuildCategorySpendingListAsync

**Location:** `src/BudgetExperiment.Application/Reports/ReportService.cs:267-289` and `317-330`
**Pattern:** `BuildCategorySpendingListAsync` loops over category groups and calls `ResolveCategoryDetailsAsync` (which hits `_categoryRepository.GetByIdAsync`) for each category. Similarly, `BuildTopCategoriesAsync` (line 200-224) calls `ResolveCategoryNameAsync` per category group.
**Impact:** If a user has 15 categories in a month's report, this produces 15 individual DB queries to resolve names/colors that were already available on the transaction's `.Category` navigation property (which is `.Include()`d by `GetByDateRangeAsync`). Wasteful and slow.
**Recommendation:** Use the already-loaded `Category` navigation property on each transaction, or pre-fetch all categories once with `GetAllAsync()` and build a lookup dictionary. The `GetByDateRangeAsync` already includes `.Include(t => t.Category)`.

---

### P-004 [High] — GetUncategorizedAsync returns unbounded result set

**Location:** `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs:214-222`
**Pattern:** `GetUncategorizedAsync()` returns **all** uncategorized transactions with no limit. This is called by `CategorySuggestionService.AnalyzeTransactionsAsync()` (line 63) and `GetSuggestedRulesAsync()` (line 216). Both load the full set into memory, then filter in-memory.
**Impact:** If a user imports a large CSV with 2,000 uncategorized transactions, every call to analyze suggestions loads all 2,000 entities. On the Pi, this creates significant memory pressure and GC pauses.
**Recommendation:** Add a `Take()` limit or paginate. For the suggestion service, consider querying only distinct descriptions (like `GetAllDescriptionsAsync` does) rather than full entities.

---

### P-005 [High] — GetAllForHealthAnalysisAsync returns unbounded full entities

**Location:** `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs:528-544`
**Pattern:** This method loads ALL transactions (scoped) with `.Include(t => t.Category)` and no `.Take()`. Used by `DataHealthService` for duplicates, outliers, and date gap analysis.
**Impact:** This is the query backing P-001. As the transaction count grows, this becomes the single largest memory consumer in the application. A 10,000 transaction database returns ~10,000 full entity graphs per call.
**Recommendation:** For date gaps, only `AccountId` and `Date` columns are needed — use a projection query. For duplicate detection, project to `(Id, AccountId, Date, Amount, Description)`. For outliers, project to `(Id, Description, Amount)`. Each sub-analysis needs a fraction of the full entity.

---

### P-006 [High] — GetAllDescriptionsAsync returns unbounded distinct descriptions

**Location:** `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs:314-322`
**Pattern:** Selects all distinct descriptions across all scoped transactions with no limit. While it projects to strings (not full entities), the result set grows linearly with transaction history.
**Impact:** With years of transaction history, this could return 5,000+ strings. Used for autocomplete or matching — loading them all defeats the purpose.
**Recommendation:** Add a search prefix parameter and use `StartsWith` or `Contains` in the query with a `Take(100)` limit, or implement server-side search.

---

### P-007 [High] — TransactionsController GET endpoint has no pagination

**Location:** `src/BudgetExperiment.Api/Controllers/TransactionsController.cs:57-85`
**Pattern:** The `GetByDateRangeAsync` endpoint accepts `startDate` and `endDate` but has no `page`/`pageSize` parameters. A query spanning an entire year could return thousands of transactions in a single response.
**Impact:** Large JSON serialization, large HTTP response payload, and large client-side deserialization on WASM — all of which tax the Pi server and the client browser.
**Recommendation:** Either deprecate this endpoint in favor of the paginated `GetPagedAsync` endpoint, or add pagination parameters with a reasonable default (50) and maximum (100).

---

### P-008 [Medium] — CategorySuggestionService double-iteration over uncategorized transactions

**Location:** `src/BudgetExperiment.Application/Categorization/CategorySuggestionService.cs:220-228`
**Pattern:** In `GetSuggestedRulesAsync`, for each merchant pattern, the code iterates the full uncategorized list twice — once to get matching descriptions (`.Where(...).Take(5)`) and once to count matches (`.Count(...)`). With P patterns and N transactions, this is O(P×2N).
**Impact:** Moderate — typically the pattern count is low. But combined with P-004 (unbounded uncategorized list), this amplifies the impact.
**Recommendation:** Combine into a single pass: iterate once, collect both sample descriptions and count simultaneously.

---

### P-009 [Medium] — GetAllWithLocationAsync returns unbounded result set

**Location:** `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs:352-358`
**Pattern:** Returns ALL transactions with a non-null `Location` property. No pagination, no date range filter.
**Impact:** As users add location data to transactions over time, this result set grows unboundedly. Used by `LocationReportBuilder` which processes the full set in memory.
**Recommendation:** Add date range parameters to scope the query. Location reports should be period-bounded.

---

### P-010 [Medium] — GetByDateRangeAsync loads full entities for calendar/report use

**Location:** `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs:61-81`
**Pattern:** `GetByDateRangeAsync` returns full `Transaction` entities with `.Include(t => t.Category)`. This is used by `BudgetProgressService.BuildKakeiboGroupedSummaryAsync`, `ReportService.BuildCategoryReportAsync`, `ChatService.TryHandleKakeiboQueryAsync`, and `RecurringChargeDetectionService.DetectAsync` — all of which only need a subset of fields.
**Impact:** Over-fetching columns wastes bandwidth on the DB connection and memory for entity tracking. On Pi hardware, every unnecessary byte matters.
**Recommendation:** For aggregate queries (sums, counts by category), push computation to SQL with `GroupBy`/`Sum` projections. For the chat Kakeibo query, project to `(Amount, IsTransfer, KakeiboOverride, CategoryKakeiboCategory)`.

---

### P-011 [Medium] — ReconciliationRecordRepository.GetByAccountAsync is unbounded

**Location:** `src/BudgetExperiment.Infrastructure/Persistence/Repositories/ReconciliationRecordRepository.cs:70-77`
**Pattern:** Returns ALL reconciliation records for an account with no pagination.
**Impact:** Grows linearly over years of reconciliation history. Typically low volume (monthly), but the pattern is inconsistent with the project's pagination discipline.
**Recommendation:** Add `Take()` or ensure callers use the paginated `ListAsync` overload.

---

### P-012 [Medium] — Account sort-by subquery in GetUnifiedPagedAsync

**Location:** `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs:504-511`
**Pattern:** When `sortBy` is `"ACCOUNT"`, the ordering uses a correlated subquery: `.OrderBy(t => _context.Accounts.Where(a => a.Id == t.AccountId).Select(a => a.Name).FirstOrDefault())`. This generates a subquery per row in the SQL execution plan.
**Impact:** With 1,000+ transactions, this subquery executes once per row in the sort phase. On PostgreSQL/Pi, this creates measurable latency.
**Recommendation:** Use a `Join` with the Accounts table instead of a correlated subquery, or materialize account names in the transaction DTO at the service layer.

---

### P-013 [Medium] — No Virtualize component usage in Blazor client

**Location:** Entire `src/BudgetExperiment.Client/Pages/` directory (grep found zero `Virtualize` usages)
**Pattern:** All list renderings use `@foreach` loops. Transaction lists, category lists, budget progress cards, recurring items — all render every item to the DOM simultaneously.
**Impact:** For paginated views (Transactions page uses 50-item pages), this is tolerable. But pages like `Budget.razor` (line 99, rendering all category progress cards) and `Calendar.razor` could benefit from virtualization when data sets are large. On WASM in a browser with limited resources, unnecessary DOM nodes cause render lag.
**Recommendation:** Add `<Virtualize>` to the Transactions page table body and any other list that could exceed ~30 items. This is a Blazor built-in component requiring minimal code changes.

---

### P-014 [Medium] — Transaction list @foreach loop missing @key directive

**Location:** `src/BudgetExperiment.Client/Pages/Transactions.razor:169`
**Pattern:** `@foreach (var txn in ViewModel.PageData.Items)` renders `<tr>` elements without `@key="txn.Id"`. When the list updates (pagination, sort change), Blazor's diff algorithm cannot efficiently match old and new DOM elements.
**Impact:** Every page change forces Blazor to rebuild the entire table body rather than patching only changed rows. On WASM with 50 rows, this causes a noticeable re-render delay.
**Recommendation:** Add `@key="txn.Id"` to the `<tr>` element: `<tr @key="txn.Id" class="...">`.

---

### P-015 [Low] — Task.Result usage after Task.WhenAll in ViewModels

**Location:** `src/BudgetExperiment.Client/ViewModels/TransactionsViewModel.cs:952-954`, `TransfersViewModel.cs:168-169`
**Pattern:** After `await Task.WhenAll(...)`, the code accesses `.Result` on already-completed tasks. While technically safe (the tasks are completed), `.Result` wraps exceptions in `AggregateException` rather than preserving the original exception type. The idiomatic pattern is `await taskVariable`.
**Impact:** Low — functionally correct since `Task.WhenAll` has already thrown if any task failed. Slight risk of obscured exception details in error handling paths.
**Recommendation:** Replace `accountsTask.Result` with `await accountsTask` for consistency and better error propagation.

---

### P-016 [Low] — ChatService fetches all accounts and categories per message

**Location:** `src/BudgetExperiment.Application/Chat/ChatService.cs:471-481`
**Pattern:** Every `SendMessageAsync` call to the non-Kakeibo path invokes `GetAccountInfoAsync` and `GetCategoryInfoAsync`, each of which calls the respective repository's `GetAllAsync` / `GetActiveAsync`. These are small, stable datasets — but there is no caching.
**Impact:** Low for a single-user Pi deployment. Would compound if multiple chat messages are sent in quick succession.
**Recommendation:** Consider injecting `IMemoryCache` and caching accounts/categories for a short TTL (30-60 seconds).

---

### P-017 [Low] — CategorizationRuleRepository.ReorderPrioritiesAsync uses O(n×m) lookup

**Location:** `src/BudgetExperiment.Infrastructure/Persistence/Repositories/CategorizationRuleRepository.cs:103-117`
**Pattern:** For each rule, `priorityList.First(p => p.RuleId == rule.Id)` performs a linear scan. With 100 rules being reordered, this is O(n²).
**Impact:** Low — rule count is typically small (< 50), and this operation is infrequent (manual reorder).
**Recommendation:** Convert `priorityList` to a `Dictionary<Guid, int>` for O(1) lookup.

---

## Strengths

The codebase demonstrates several intentional performance practices:

1. **Consistent `AsNoTracking()`**: Read-only repository methods consistently apply `AsNoTracking()` or `AsNoTrackingWithIdentityResolution()`, avoiding EF Core change tracker overhead.

2. **Parallel data loading**: Both `CalendarGridService` and `TransactionListService` use `Task.WhenAll` with separate DI scopes to execute independent database queries in parallel — a sophisticated pattern.

3. **Server-side pagination on primary endpoints**: The unified transaction listing (`GetUnifiedPagedAsync`) and uncategorized transactions (`GetUncategorizedPagedAsync`) both implement server-side filtering, sorting, and pagination with count queries.

4. **Projection queries where appropriate**: `GetDailyTotalsAsync` uses `GroupBy`/`Sum`/`Select` projection to compute aggregates in SQL. `GetAccountNamesByIdsAsync` projects to `{Id, Name}` instead of loading full entities. `GetClearedBalanceSumAsync` uses `GroupBy`/`Sum` to compute balance in SQL.

5. **Bounded date-range filtering**: The account transaction view wisely defaults to a 90-day lookback (`DefaultTransactionLookbackDays`), preventing unbounded history loads.

6. **No lazy loading**: The codebase uses explicit `.Include()` for navigation properties and avoids EF Core lazy loading proxies, eliminating surprise N+1 patterns from property access.

7. **PageSize caps**: Controllers enforce a maximum page size (100), preventing clients from requesting unbounded results through pagination parameters.

## Priority Matrix

| ID | Severity | Area | Effort | Description |
|------|----------|------------------|--------|----------------------------------------------|
| P-001 | Critical | Application/Infra | Medium | DataHealthService loads all txns 3× per call |
| P-002 | High | Application | Low | BudgetProgress N+1 query per category |
| P-003 | High | Application | Low | ReportService N+1 for category names |
| P-004 | High | Infrastructure | Low | GetUncategorizedAsync unbounded |
| P-005 | High | Infrastructure | Medium | GetAllForHealthAnalysisAsync unbounded + over-fetching |
| P-006 | High | Infrastructure | Low | GetAllDescriptionsAsync unbounded |
| P-007 | High | API | Low | GET /transactions has no pagination |
| P-008 | Medium | Application | Low | Double iteration in GetSuggestedRulesAsync |
| P-009 | Medium | Infrastructure | Low | GetAllWithLocationAsync unbounded |
| P-010 | Medium | Infrastructure | Medium | Full entity fetch where projections suffice |
| P-011 | Medium | Infrastructure | Low | GetByAccountAsync (reconciliation) unbounded |
| P-012 | Medium | Infrastructure | Medium | Correlated subquery for account name sorting |
| P-013 | Medium | Client | Low | No Virtualize component usage |
| P-014 | Medium | Client | Low | Missing @key on transaction list loop |
| P-015 | Low | Client | Low | Task.Result after WhenAll |
| P-016 | Low | Application | Low | No caching for chat accounts/categories |
| P-017 | Low | Infrastructure | Low | O(n²) lookup in ReorderPrioritiesAsync |
