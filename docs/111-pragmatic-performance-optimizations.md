# Feature 111: Pragmatic Performance Optimizations
> **Status:** Done

## Overview

A senior developer knows when to follow the rules — and when to break them. This project follows Clean Architecture, SOLID, and full abstraction layering. Those principles have kept the codebase organized, testable, and maintainable. But dogmatic adherence to abstractions has a cost: unnecessary allocations, extra virtual dispatch, redundant database round trips, and change-tracked read-only queries.

This feature identifies concrete areas where the abstractions provide no realistic future benefit and where removing or relaxing them yields measurable performance gains — without sacrificing the qualities that made the architecture worth following in the first place.

## Problem Statement

### Current State

The architecture is clean but carries overhead in areas where the "just in case" flexibility will never be exercised:

- **19 repository interfaces**, each with exactly one EF Core implementation — will never be swapped for Dapper, MongoDB, or anything else. The interface exists to satisfy layering rules, not to enable real polymorphism.
- **Zero `AsNoTracking()` usage** across all repositories. Every read-only query (GET endpoints, calendar grids, balance calculations) pays the cost of EF Core change tracking: identity resolution, snapshot creation, and increased GC pressure — all for entities that are immediately mapped to DTOs and never saved.
- **9–10 sequential database round trips** in hot-path endpoints like `CalendarGridService.GetCalendarGridAsync` and `TransactionListService.GetAccountTransactionListAsync`. Each `await` waits for the previous to complete before issuing the next query, even when the queries are independent.
- **73 scoped service registrations** in the Application DI container. Every HTTP request allocates all resolved services, even when the endpoint only uses 2–3 of them. Duplicate backward-compatibility registrations inflate this further.
- **`DayDetailService.GetDayDetailAsync` calls `GetAllAsync()` for accounts** — loading every account in the system just to resolve a few account names for display.
- **`AccountRepository.GetByIdWithTransactionsAsync` eagerly loads ALL transactions** via `.Include(a => a.Transactions)` — unbounded and dangerous as data grows.

### Target State

A codebase that retains its clean layering where it matters (multi-repo orchestration, domain logic isolation, external service swappability) but sheds unnecessary overhead in commodity CRUD paths and read-only queries. Concrete gains:

- Read-only queries skip change tracking entirely.
- Hot-path endpoints issue concurrent or batched queries instead of sequential awaits.
- Repository interfaces that will never be polymorphic are removed or consolidated.
- DI registrations are lean — no duplicates, lazy resolution where beneficial.
- No unbounded eager loading of child collections.

---

## Philosophy: When Abstractions Earn Their Keep

Not every abstraction is worth removing. The decision framework:

| Keep the abstraction | Remove / simplify |
|---|---|
| `IAiService` — will realistically be swapped (Ollama → OpenAI → Anthropic) | `IAppSettingsRepository` — will always be EF Core against the single settings row |
| `IExportFormatter` — Strategy pattern with real extension (CSV today, PDF/Excel later) | `IChatMessageRepository` / `IChatSessionRepository` — simple CRUD, no alternative data stores |
| Services coordinating 3–4 repositories (`CalendarGridService`, `AutoRealizeService`) — genuine orchestration value | Repository interfaces for aggregate roots with one implementation and no business rules beyond CRUD |
| `IUserContext` — decouples auth mechanism from business logic, essential for testing | Duplicate `TransactionService` + `ITransactionService` registrations for backward compatibility |

**The principle:** If the abstraction enables real substitution (external services, export formats, auth providers) or genuine orchestration (multi-repo coordination with business rules), keep it. If it exists solely to satisfy a layering rule and the implementation will never change, it's ceremony — remove it.

---

## Optimization Areas

### Area 1: AsNoTracking for Read-Only Queries (High Impact, Zero Risk)

**Current cost:** Every `GetAllAsync`, `GetByIdAsync`, `GetByDateRangeAsync`, etc. creates change-tracking snapshots for entities that are immediately mapped to DTOs and discarded. For a calendar grid with 60–90 days of transactions, this means hundreds of tracked entities per request with zero benefit.

**Fix:** Add `.AsNoTracking()` to all repository methods that serve read-only endpoints. Alternatively, set `QueryTrackingBehavior.NoTrackingWithIdentityResolution` as the default and opt-in to tracking only for write methods.

**Why this doesn't break the rules:** This is an infrastructure-level change inside the existing repository implementations. No interface changes, no architectural compromise. It's the kind of optimization that should have been there from day one.

**Affected repositories (all 19):**
- `AccountRepository` — `GetAllAsync`, `GetByIdAsync` (read variant)
- `TransactionRepository` — `GetByDateRangeAsync`, `GetDailyTotalsAsync`, `GetByAccountIdAsync`
- `RecurringTransactionRepository` — `GetActiveAsync`, `GetByAccountIdAsync`
- `RecurringTransferRepository` — `GetActiveAsync`, `GetByAccountIdAsync`
- `BudgetCategoryRepository`, `BudgetGoalRepository`, `AppSettingsRepository`, `UserSettingsRepository` — all read methods
- `CategorizationRuleRepository`, `RuleSuggestionRepository`, `CategorySuggestionRepository` — all read methods
- `ImportMappingRepository`, `ImportBatchRepository` — all read methods
- `ChatSessionRepository`, `ChatMessageRepository` — all read methods
- `ReconciliationMatchRepository`, `LearnedMerchantMappingRepository`, `DismissedSuggestionPatternRepository`, `CustomReportLayoutRepository` — all read methods

**Estimated impact:** 20–40% reduction in memory allocations for read-heavy endpoints. EF Core change tracking is one of the most expensive per-entity operations.

---

### Area 2: Concurrent Database Queries in Hot Paths (High Impact, Medium Risk)

**Current cost:** `CalendarGridService.GetCalendarGridAsync` makes 9 sequential `await` calls. Each waits for the database round trip to complete before issuing the next query. On a Raspberry Pi with a local PostgreSQL instance, each round trip is 1–5ms. Nine sequential calls: 9–45ms of pure wait time.

**Fix:** Use `Task.WhenAll` for independent queries within the same endpoint. This requires separate `DbContext` instances (EF Core's `DbContext` is not thread-safe), which can be achieved via `IDbContextFactory<BudgetDbContext>`.

**Example — CalendarGridService today:**
```csharp
var dailyTotals = await _transactionRepository.GetDailyTotalsAsync(accountId, startDate, endDate, ct);
var recurringTransactions = await _recurringTransactionRepository.GetByAccountIdAsync(accountId, ct);
var recurringTransfers = await _recurringTransferRepository.GetByAccountIdAsync(accountId, ct);
var currency = await _userSettingsService.GetCurrencyAsync(ct);
```

**After (conceptual):**
```csharp
var dailyTotalsTask = _transactionRepository.GetDailyTotalsAsync(accountId, startDate, endDate, ct);
var recurringTransactionsTask = _recurringTransactionRepository.GetByAccountIdAsync(accountId, ct);
var recurringTransfersTask = _recurringTransferRepository.GetByAccountIdAsync(accountId, ct);
var currencyTask = _userSettingsService.GetCurrencyAsync(ct);

await Task.WhenAll(dailyTotalsTask, recurringTransactionsTask, recurringTransfersTask, currencyTask);
```

**Caution:** This requires either (a) `IDbContextFactory` to spawn separate DbContext instances per parallel query, or (b) consolidating multiple queries into a single raw SQL batch. Option (a) is the cleaner path and works well with EF Core.

**Affected hot paths:**
- `CalendarGridService.GetCalendarGridAsync` — 9 sequential DB calls
- `TransactionListService.GetAccountTransactionListAsync` — 10 sequential DB calls
- `DayDetailService.GetDayDetailAsync` — 7 sequential DB calls
- `PastDueService.GetPastDueItemsAsync` — multiple sequential calls

---

### Area 3: Eliminate Unbounded Eager Loading (High Impact, Zero Risk)

**Current issue:** `AccountRepository.GetByIdWithTransactionsAsync` uses `.Include(a => a.Transactions)` which loads ALL transactions for an account. An account with 2 years of history could have thousands of transactions — all loaded into memory just to display account details.

**Fix options:**
1. **Remove the method entirely** if no caller needs all transactions. Callers should use `TransactionRepository.GetByAccountIdAsync` with date filters.
2. **Add pagination or date-range parameters** to bound the loaded set.
3. **Replace with a projection** that loads only the fields needed (e.g., transaction count, most recent transaction date).

**Similarly:** `DayDetailService` calls `_accountRepository.GetAllAsync()` to resolve account names. This loads every account with full entity tracking just to build a name lookup dictionary. A lightweight `GetAccountNamesByIdsAsync(IEnumerable<Guid> ids)` method or a cached dictionary of `{id, name}` pairs would eliminate this.

---

### Area 4: Repository Interface Consolidation (Medium Impact, Low Risk)

**Current state:** 19 repository interfaces in Domain, each with exactly one implementation in Infrastructure. All are sealed EF Core implementations against PostgreSQL. None will ever be swapped for a different data store — the entire schema, migration history, and query patterns are PostgreSQL-specific.

**The honest assessment:** These interfaces exist for two reasons: (1) Clean Architecture says Domain shouldn't reference Infrastructure, and (2) tests can mock them. Reason 1 is valid in principle but provides no practical value here — this app will never run against MongoDB. Reason 2 is the real value, and it's significant.

**Pragmatic approach — don't remove all interfaces, but consolidate:**

1. **Keep interfaces for repositories used in multi-repo services** (the services that genuinely benefit from testability via mocking): `ITransactionRepository`, `IAccountRepository`, `IRecurringTransactionRepository`, `IRecurringTransferRepository`, `IBudgetCategoryRepository`, `IBudgetGoalRepository`.

2. **Consider removing interfaces for simple, single-use repositories** where the service that uses them is itself thin and could be tested via integration tests instead of unit tests with mocks: `IAppSettingsRepository`, `IChatSessionRepository`, `IChatMessageRepository`, `ICustomReportLayoutRepository`, `IDismissedSuggestionPatternRepository`, `ILearnedMerchantMappingRepository`.

3. **Alternative: Keep all interfaces but recognize the cost is near-zero.** The virtual dispatch overhead of an interface call is ~2 nanoseconds. The real cost is developer cognitive load (22 interface files + 19 implementation files = 41 files for data access). If the team is comfortable with this, the interfaces are harmless from a performance perspective.

**Recommendation:** This area is more about code maintainability than runtime performance. The actual nanoseconds saved from removing virtual dispatch are irrelevant. The value is in reducing the file count and cognitive overhead. Defer this unless the team finds the interface proliferation genuinely painful.

---

### Area 5: DI Registration Cleanup (Low-Medium Impact, Zero Risk)

**Current state:** 73 scoped service registrations. Some services are registered twice:

```csharp
// Interface + concrete both registered for backward compatibility
services.AddScoped<ITransactionService, TransactionService>();
services.AddScoped<TransactionService>();  // duplicate for controllers injecting concrete

services.AddScoped<IRecurringTransactionService, RecurringTransactionService>();
services.AddScoped<RecurringTransactionService>();  // duplicate
```

**Fix:**
1. **Remove duplicate registrations.** Pick one pattern (interface or concrete) and migrate all consumers. Since controllers inject concrete types (`AccountService`, `TransactionService`), the concrete registrations are the ones actually used. The interface registrations serve other services that depend on abstractions.
2. **Use `services.AddScoped<ITransactionService>(sp => sp.GetRequiredService<TransactionService>())` to unify** — register the concrete once, then alias the interface to the same instance.

**Allocation note:** Scoped services in ASP.NET Core are lazily resolved — they're only instantiated when first requested from the container during a request. The 73 registrations don't mean 73 allocations per request. Only the services actually injected into the resolved controller and its dependency chain are created. The real cost is the DI container's internal bookkeeping, which is minimal. This is a correctness cleanup, not a major performance win.

---

### Area 6: Mapping Allocation Optimization (Low Impact, Low Risk)

**Current state:** 8 mapper files with 30+ static `ToDto()` methods. Pattern:

```csharp
accounts.Select(AccountMapper.ToDto).ToList();
```

For list endpoints returning 50–200 items, this creates intermediate LINQ iterator objects plus a final `List<T>` allocation.

**Honest assessment:** This is clean, explicit, and easy to debug. The alternatives (source generators, implicit operators, records with deconstruction) save only a few microseconds per mapping. Not worth optimizing unless profiling reveals mapping as a bottleneck.

**One exception:** If `GetAllAsync` returns 1000+ items (e.g., transaction history), consider:
- Returning `IAsyncEnumerable<T>` from repositories to enable streaming serialization.
- Using `System.Text.Json` source generators for faster serialization of DTOs.

---

### Area 7: Targeted No-Abstraction Paths for Simple Reads (Medium Impact, Medium Risk)

**Current call chain for a simple account list:**
```
HTTP GET /api/v1/accounts
  → AccountsController.GetAllAsync()
    → AccountService.GetAllAsync()
      → IAccountRepository.GetAllAsync()
        → AccountRepository.GetAllAsync()
          → DbContext.Accounts.Where(...).ToListAsync()
      → AccountMapper.ToDto() × N
    → return DTOs
  → Ok(DTOs)
  → JSON serialization
```

Five layers of abstraction for what is conceptually `SELECT * FROM accounts WHERE ... → serialize to JSON`.

**Pragmatic option:** For truly simple read endpoints with no business logic, consider a "read model" path that bypasses the service layer:
- A lightweight `IReadModelQuery<TResult>` that runs a no-tracking, projection-only query directly from the controller.
- Uses `DbContext` directly (via a read-only wrapper) to project into DTOs without materializing domain entities.

**Example:**
```csharp
// Instead of: entity → track → snapshot → map → serialize
// Do: query → project → serialize
var accounts = await _dbContext.Accounts
    .AsNoTracking()
    .Where(a => a.Scope == BudgetScope.Shared || a.OwnerId == userId)
    .Select(a => new AccountDto { Id = a.Id, Name = a.Name, ... })
    .ToListAsync(ct);
```

**This skips:** Entity materialization, change tracking, domain object allocation, and separate DTO mapping. For a list of 50 accounts, this eliminates ~150 object allocations (50 entities + 50 change-tracking snapshots + 50 DTOs → 50 DTOs only).

**Where this is appropriate:** Read-only list endpoints with no domain validation, no concurrency, and no side effects. Specifically:
- `GET /api/v1/accounts` (account list)
- `GET /api/v1/categories` (budget category list)
- `GET /api/v1/rules` (categorization rule list)
- `GET /api/v1/settings` (app settings)

**Where this is NOT appropriate:** Any endpoint that involves writes, validation, multi-repo coordination, or domain events. The service layer is justified for all mutations.

---

## What NOT to Optimize

These areas earn their abstraction cost and should be left alone:

| Area | Why it stays |
|---|---|
| **Service layer for write operations** | Orchestrates domain factories, validates invariants, coordinates UnitOfWork. Every write service (`AccountService.CreateAsync`, `TransactionService.CreateAsync`) does real work. |
| **Multi-repo orchestration services** | `CalendarGridService`, `AutoRealizeService`, `BudgetProgressService`, `ReconciliationService` — these coordinate 3–4 repositories with genuine business logic. The service layer is where these queries belong. |
| **`IAiService` / `IGeocodingService` interfaces** | External services that will realistically be replaced (Ollama → OpenAI, Nominatim → Google Maps). The interface is justified. |
| **`IExportFormatter` strategy pattern** | Clean extension point. CSV exists today, PDF/Excel are plausible. Worth the 1 interface. |
| **Manual mappers** | Explicit, debuggable, testable. The alternative (AutoMapper, implicit conversions) trades clarity for marginal brevity. |
| **Domain value objects (`MoneyValue`, `CurrencyValue`)** | Prevent primitive obsession. The allocation cost of a struct-like value object is near zero. |
| **`IUnitOfWork` pattern** | Decouples save semantics from DbContext, used correctly in every write service. |

---

## Implementation Plan

### Phase 1: AsNoTracking for All Read-Only Repository Methods

**Objective:** Eliminate change-tracking overhead on every read query in the application.

**Tasks:**
- [ ] Audit each of the 19 repository implementations; tag every method as read-only or read-write
- [ ] Add `.AsNoTracking()` to all read-only methods
- [ ] Verify that no read-only method's result is later passed to `SaveChangesAsync` (would break)
- [ ] Run full test suite to confirm no regressions
- [ ] Benchmark before/after on a representative endpoint (e.g., `GET /api/v1/accounts/{id}/transactions`)

**Commit:**
```
perf(infrastructure): add AsNoTracking to all read-only repository queries

- Audit 19 repositories, apply AsNoTracking to read-only methods
- Reduces memory allocations and GC pressure on read-heavy endpoints
- No behavioral change for write paths

Refs: #111
```

---

### Phase 2: Fix Unbounded Eager Loading

**Objective:** Prevent loading unbounded child collections and unnecessary full-table scans.

**Tasks:**
- [ ] Remove or paginate `AccountRepository.GetByIdWithTransactionsAsync` — replace with bounded query or remove if unused
- [ ] Replace `DayDetailService`'s `GetAllAsync()` with `GetAccountNamesByIdsAsync()` or equivalent lightweight lookup
- [ ] Add integration tests validating the new query patterns return correct data
- [ ] Profile query plans for the new methods

**Commit:**
```
perf(infrastructure): eliminate unbounded eager loading

- Remove/paginate GetByIdWithTransactionsAsync
- Replace DayDetailService full account load with targeted name lookup
- Prevents memory issues as transaction history grows

Refs: #111
```

---

### Phase 3: Concurrent Queries in Calendar Hot Path

**Objective:** Reduce wall-clock latency of calendar/timeline endpoints by issuing independent queries in parallel.

**Tasks:**
- [ ] Register `IDbContextFactory<BudgetDbContext>` in Infrastructure DI
- [ ] Refactor `CalendarGridService.GetCalendarGridAsync` to identify independent query groups
- [ ] Use `Task.WhenAll` for independent queries, each using a factory-created DbContext
- [ ] Benchmark before/after latency on Raspberry Pi
- [ ] Repeat for `TransactionListService` and `DayDetailService` if gains are significant
- [ ] Add tests confirming concurrent execution produces identical results to sequential

**Commit:**
```
perf(application): parallelize independent queries in CalendarGridService

- Register IDbContextFactory for concurrent query support
- Group 9 sequential queries into 3-4 parallel batches
- Reduces endpoint latency proportional to query independence

Refs: #111
```

---

### Phase 4: DI Registration Cleanup

**Objective:** Remove duplicate service registrations and unify injection patterns.

**Tasks:**
- [ ] Audit all duplicate registrations (interface + concrete for same service)
- [ ] Unify to single registration with alias pattern: register concrete, alias interface
- [ ] Update any code injecting the now-removed registration type
- [ ] Verify full test suite passes

**Commit:**
```
refactor(application): unify duplicate DI registrations

- Remove backward-compat double registrations for TransactionService,
  RecurringTransactionService, RecurringTransferService
- Use alias pattern to serve both interface and concrete consumers

Refs: #111
```

---

### Phase 5: Read-Model Projection for Simple List Endpoints (Optional / Experimental)

**Objective:** Bypass entity materialization and service layer for trivially simple read-only list endpoints.

**Tasks:**
- [ ] Create a lightweight `IReadQuery<TResult>` abstraction in Application
- [ ] Implement DbContext projection queries for `GET /accounts` and `GET /categories` as proof of concept
- [ ] Benchmark against the existing service-layer path
- [ ] If gains are significant (>30% latency reduction), expand to other simple list endpoints
- [ ] If gains are marginal, revert and document findings
- [ ] Ensure the pattern does NOT proliferate to endpoints with business logic

**Commit:**
```
perf(api): add read-model projection for simple list endpoints

- Bypass entity materialization for GET /accounts and GET /categories
- Project directly from DbContext to DTOs with AsNoTracking
- Reduces allocations by ~66% for list endpoints

Refs: #111
```

---

## Success Metrics

| Metric | Baseline | Target |
|---|---|---|
| Memory allocations per `GET /accounts/{id}/transactions` | TBD (measure) | ≥30% reduction |
| Wall-clock latency for `GET /calendar/grid` | TBD (measure) | ≥40% reduction |
| DI registrations (unique) | 73 | ≤65 |
| Repository methods using `AsNoTracking` | 0 | 100% of read-only methods |
| Unbounded `.Include()` calls | 1 | 0 |

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| `AsNoTracking` applied to a method whose result is later saved | Data loss (changes silently not persisted) | Audit every call site before applying; test suite covers write paths |
| Concurrent queries expose race conditions | Incorrect data aggregation | Each parallel task uses its own DbContext; no shared state |
| Read-model bypasses evolve into a parallel architecture | Maintenance burden, two paths for everything | Strict rule: read-model ONLY for list endpoints with zero business logic; all writes and complex reads stay in service layer |
| Removing repository interfaces breaks test mocking | Tests require rework | Only remove interfaces where integration tests replace unit-with-mocks adequately; keep interfaces for heavily-mocked repositories |
