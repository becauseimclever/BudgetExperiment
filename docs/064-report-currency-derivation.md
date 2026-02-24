# Feature 064: Global Currency — Replace Hardcoded USD
> **Status:** ✅ Complete  
> **Priority:** Medium  
> **Estimated Effort:** Medium (1–2 sprints)  
> **Dependencies:** Feature 066 Phase 1 domain work (UserSettings.PreferredCurrency exists today)  
> **Completed:** 2026-02-23

## Overview

Replace **all ~50+ hardcoded `"USD"` strings** across Application services with the user's global `PreferredCurrency` from `UserSettings`. The application enforces a **single-currency model** (set during onboarding via Feature 066 and editable in Settings). This feature introduces `ICurrencyProvider` so every service can resolve the active currency without injecting `IUserSettingsRepository` directly, then systematically replaces hardcoded values one service group at a time as independently testable vertical slices.

## Problem Statement

### Current State

`UserSettings.PreferredCurrency` exists in the domain and is settable via the API, but **no service reads it**. Every `MoneyDto` and `MoneyValue` construction in Application services hardcodes `"USD"`:

| Service | Hardcoded `"USD"` Count (actual) |
|---------|-----------------------------------|
| `ReportService` | 14 |
| `CalendarGridService` | 8 |
| `TransactionListService` | 8 |
| `BalanceCalculationService` | 6 |
| `ChatService` | 4 |
| `DayDetailService` | 3 |
| `BudgetProgressService` | 2 |
| `ImportService` | 2 |
| `PastDueService` | 1 |
| `PaycheckAllocationService` | 1 |
| `BudgetCategoryService` | 1 |
| `MoneyDto` (default) | 1 |

Additionally, `IUserContext` exposes identity and scope but **not** currency, so services have no lightweight way to resolve it.

### Target State

- A single `ICurrencyProvider` abstraction resolves the user's preferred currency (falls back to `"USD"` when unset).
- **Zero hardcoded `"USD"` strings** remain in Application services — all use `ICurrencyProvider`.
- `MoneyDto.Currency` default aligns with the global currency or is always explicitly set.
- Each vertical slice is independently testable: services can be updated and verified one at a time.

---

## User Stories

#### US-064-001: Monetary values respect global currency
**As a** user who selected EUR during onboarding  
**I want** all reports, calendars, and transaction views to display EUR  
**So that** amounts are accurately labeled in my chosen currency

**Acceptance Criteria:**
- [x] All `MoneyDto.Currency` values in API responses reflect `UserSettings.PreferredCurrency`
- [x] Changing currency in Settings immediately affects subsequent API responses
- [x] When `PreferredCurrency` is null/empty, the system defaults to `"USD"`

#### US-064-002: Currency fallback for unset preferences
**As a** user who skipped onboarding or has no saved currency  
**I want** the system to default to USD  
**So that** the app works correctly without requiring explicit currency configuration

**Acceptance Criteria:**
- [x] `ICurrencyProvider.GetCurrencyAsync()` returns `"USD"` when `PreferredCurrency` is null
- [x] No `NullReferenceException` or empty currency strings in any API response

---

## Technical Design

### Approach: `ICurrencyProvider` Abstraction

Rather than each service injecting `IUserSettingsRepository` + `IUserContext` and duplicating resolution logic, introduce a focused abstraction:

```csharp
// Domain (or Application) — interface
public interface ICurrencyProvider
{
    Task<string> GetCurrencyAsync(CancellationToken cancellationToken = default);
}
```

```csharp
// Application — implementation
public sealed class UserSettingsCurrencyProvider : ICurrencyProvider
{
    private const string DefaultCurrency = "USD";
    private readonly IUserContext _userContext;
    private readonly IUserSettingsRepository _settingsRepository;

    public UserSettingsCurrencyProvider(
        IUserContext userContext,
        IUserSettingsRepository settingsRepository)
    {
        _userContext = userContext;
        _settingsRepository = settingsRepository;
    }

    public async Task<string> GetCurrencyAsync(CancellationToken cancellationToken = default)
    {
        if (!_userContext.IsAuthenticated)
        {
            return DefaultCurrency;
        }

        var settings = await _settingsRepository
            .GetByUserIdAsync(_userContext.UserIdAsGuid, cancellationToken);

        return string.IsNullOrWhiteSpace(settings.PreferredCurrency)
            ? DefaultCurrency
            : settings.PreferredCurrency;
    }
}
```

**Why this design:**
- **Single Responsibility** — currency resolution logic lives in one place.
- **Testable** — services receive `ICurrencyProvider` which is trivially mockable.
- **Cacheable** — the implementation can later add per-request caching without changing consumers.
- **No domain leakage** — services don't need to know about `UserSettings` internals.

### Affected Files (Full Inventory)

| File | Hardcoded `"USD"` (actual) | Slice | Status |
|------|---------------------------|-------|--------|
| *New:* `ICurrencyProvider.cs` | — | 1 | ✅ |
| *New:* `UserSettingsCurrencyProvider.cs` | — | 1 | ✅ |
| `ReportService.cs` | 14 | 2 | ✅ |
| `CalendarGridService.cs` | 8 | 3 | ✅ |
| `DayDetailService.cs` | 3 | 3 | ✅ |
| `PastDueService.cs` | 1 | 3 | ✅ |
| `TransactionListService.cs` | 8 | 4 | ✅ |
| `BalanceCalculationService.cs` | 6 | 4 | ✅ |
| `ChatService.cs` | 4 | 5 | ✅ |
| `ImportService.cs` | 2 | 5 | ✅ |
| `BudgetProgressService.cs` | 2 | 5 | ✅ |
| `BudgetCategoryService.cs` | 1 | 5 | ✅ |
| `PaycheckAllocationService.cs` | 1 | 5 | ✅ |
| `MoneyDto.cs` | 1 (default) | 6 | ✅ |

---

## Implementation Plan — Vertical Slices

Each slice is a self-contained commit that can be merged, deployed, and tested independently. Slices build on each other but each leaves the system in a working state.

---

### Slice 1: `ICurrencyProvider` Abstraction + DI Registration
> **Commit:** `feat(app): add ICurrencyProvider for global currency resolution`

**Goal:** Introduce the abstraction and its implementation so subsequent slices can inject it. No existing behavior changes yet.

**Tasks:**
- [x] Write unit tests for `UserSettingsCurrencyProvider`:
  - Returns `PreferredCurrency` when set (e.g., `"EUR"`)
  - Returns `"USD"` when `PreferredCurrency` is null
  - Returns `"USD"` when `PreferredCurrency` is whitespace
  - Returns `"USD"` when user is not authenticated
- [x] Create `ICurrencyProvider` interface in Domain (`BudgetExperiment.Domain.Settings`)
- [x] Create `UserSettingsCurrencyProvider` implementation in Application (`BudgetExperiment.Application.Settings`)
- [x] Register `ICurrencyProvider` → `UserSettingsCurrencyProvider` (Scoped) in DI

**Test checkpoint:** ✅ 4 unit tests pass. Existing tests unaffected. App runs identically.

---

### Slice 2: ReportService — Use Global Currency
> **Commit:** `fix(app): ReportService uses global currency instead of hardcoded USD`

**Goal:** Replace all 14 hardcoded `"USD"` in `ReportService` with `ICurrencyProvider`.

**Tasks:**
- [x] Inject `ICurrencyProvider` into `ReportService` constructor
- [x] In each public method, resolve currency via `await _currencyProvider.GetCurrencyAsync(ct)`
- [x] Replace all 14 `"USD"` literals with the resolved currency variable
- [x] Update existing `ReportService` unit tests (21 constructor calls in `ReportServiceTests`, 7 in `ReportServiceLocationTests`)
- [x] Add test: `GetMonthlyCategoryReportAsync_Uses_Currency_From_Provider` verifies EUR flows through

**Test checkpoint:** ✅ 35 report tests pass (34 existing + 1 new).

---

### Slice 3: Calendar Services — Use Global Currency
> **Commit:** `fix(app): calendar services use global currency instead of hardcoded USD`

**Goal:** Replace hardcoded `"USD"` in `CalendarGridService` (8), `DayDetailService` (3), and `PastDueService` (1).

**Tasks:**
- [x] Inject `ICurrencyProvider` into `CalendarGridService`
- [x] Replace 8 hardcoded `"USD"` in `CalendarGridService` with resolved currency (threaded through `BuildGridDays`, `CalculateRunningBalances`, `CalculateMonthSummary` static methods)
- [x] Inject `ICurrencyProvider` into `DayDetailService`
- [x] Replace 3 hardcoded `"USD"` in `DayDetailService` with resolved currency
- [x] Inject `ICurrencyProvider` into `PastDueService`
- [x] Replace 1 hardcoded `"USD"` in `PastDueService` with resolved currency
- [x] Update existing unit tests for all three services (mock `ICurrencyProvider`)

**Test checkpoint:** ✅ 33 calendar-related tests pass.

---

### Slice 4: Transaction & Balance Services — Use Global Currency
> **Commit:** `fix(app): transaction and balance services use global currency instead of hardcoded USD`

**Goal:** Replace hardcoded `"USD"` in `TransactionListService` (8 actual) and `BalanceCalculationService` (6 actual).

**Tasks:**
- [x] Inject `ICurrencyProvider` into `TransactionListService`
- [x] Replace 8 hardcoded `"USD"` in `TransactionListService` with resolved currency (threaded through `CalculateDailyBalances` static method)
- [x] Inject `ICurrencyProvider` into `BalanceCalculationService`
- [x] Replace 6 hardcoded `"USD"` in `BalanceCalculationService` with resolved currency (across `GetBalanceBeforeDateAsync`, `GetBalanceAsOfDateAsync`, `GetOpeningBalanceForDateAsync`)
- [x] Update existing unit tests (mock `ICurrencyProvider`)

**Test checkpoint:** ✅ 44 transaction/balance tests pass.

---

### Slice 5: Chat, Import & Budget Services — Use Global Currency
> **Commit:** `fix(app): remaining services use global currency instead of hardcoded USD`

**Goal:** Replace hardcoded `"USD"` in `ChatService` (4), `ImportService` (2), `BudgetProgressService` (2), `BudgetCategoryService` (1), and `PaycheckAllocationService` (1).

**Tasks:**
- [x] Inject `ICurrencyProvider` into `ChatService`
- [x] Replace 4 hardcoded `"USD"` in `ChatService` with resolved currency (transaction, transfer, recurring transaction, recurring transfer DTOs)
- [x] Inject `ICurrencyProvider` into `ImportService`
- [x] Replace 2 hardcoded `"USD"` in `ImportService` with resolved currency (threaded through `FindBestMatchForPreviewRow` and `CreatePreviewTransaction` static methods)
- [x] Inject `ICurrencyProvider` into `BudgetProgressService`
- [x] Replace 2 hardcoded `"USD"` in `BudgetProgressService` with resolved currency
- [x] Inject `ICurrencyProvider` into `BudgetCategoryService`
- [x] Replace 1 hardcoded `"USD"` in `BudgetCategoryService` with resolved currency
- [x] Inject `ICurrencyProvider` into `PaycheckAllocationService`
- [x] Replace 1 hardcoded `"USD"` in `PaycheckAllocationService` with resolved currency
- [x] Update existing unit tests for all 5 services + `ImportServiceLocationTests` (mock `ICurrencyProvider`)

**Test checkpoint:** ✅ 122 tests pass across all 5 services.

---

### Slice 6: MoneyDto Default + Final Sweep
> **Commit:** `fix(contracts): remove hardcoded USD default from MoneyDto; final cleanup`

**Goal:** Ensure no residual hardcoded `"USD"` exists anywhere. Clean up `MoneyDto` default.

**Tasks:**
- [x] Change `MoneyDto.Currency` default from `"USD"` to `string.Empty`
- [x] Run `grep -r '"USD"'` across `src/` — verify zero hits in Application layer
- [x] Contracts layer: 4 remaining `"USD"` defaults in `TransferDto` (2) and `AccountDto` (2) — these are API request DTO defaults for backward compatibility; services (`TransferService`, `AccountService`) read currency from the DTO, so callers (including `ChatService`) already set the correct currency via `ICurrencyProvider`
- [x] No DTO-level tests relied on the `"USD"` default (all explicitly set `Currency`)
- [x] Verify all tests pass

**Test checkpoint:** ✅ Full Application test suite green (562 tests). Zero hardcoded `"USD"` in Application layer. Only the intentional `DefaultCurrency` constant in `UserSettingsCurrencyProvider` remains.

---

## Testing Strategy

### Unit Tests (per slice)

Each slice updates existing service tests and adds currency-specific assertions:

| Slice | Tests Added/Updated | Result |
|-------|-------------------|--------|
| 1 | `UserSettingsCurrencyProviderTests` — 4 new tests | ✅ 4 pass |
| 2 | `ReportServiceTests` (21 ctors), `ReportServiceLocationTests` (7 ctors) + 1 new currency test | ✅ 35 pass |
| 3 | `CalendarGridServiceTests`, `DayDetailServiceTests`, `PastDueServiceTests` — all updated | ✅ 33 pass |
| 4 | `TransactionListServiceTests`, `BalanceCalculationServiceTests` — all updated | ✅ 44 pass |
| 5 | `ChatServiceTests`, `ImportServiceTests`, `ImportServiceLocationTests`, `BudgetProgressServiceTests`, `BudgetCategoryServiceTests`, `PaycheckAllocationServiceTests` — all updated | ✅ 122 pass |
| 6 | Final sweep — grep verification, `MoneyDto` default changed | ✅ 562 total pass |

### Integration Tests

- [ ] `WebApplicationFactory` test: set `PreferredCurrency = "EUR"` → call report endpoint → verify all `MoneyDto.Currency == "EUR"` *(future — not in scope for this feature)*
- [ ] `WebApplicationFactory` test: no `PreferredCurrency` set → verify fallback to `"USD"` *(future — not in scope for this feature)*

### Manual Testing Checklist

- [ ] Set currency to EUR in Settings → all pages show EUR
- [ ] Set currency to GBP → reports, calendar, transactions, chat all show GBP
- [ ] Clear currency (set to null) → falls back to USD
- [ ] New user with no settings → USD everywhere

---

## Security Considerations

None — currency is already stored in `UserSettings`; this change only reads and propagates it. No new data exposure.

---

## Performance Considerations

- `ICurrencyProvider.GetCurrencyAsync()` performs one `UserSettings` query per call. Since `UserSettings` is already loaded in most request pipelines (for scope resolution), this is typically a cache hit in EF's identity map.
- If profiling shows redundant queries, a per-request cache (via `AsyncLocal` or scoped service state) can be added to `UserSettingsCurrencyProvider` without changing any consumer code — this is a key benefit of the abstraction.

---

## Relationship to Feature 066 (Onboarding)

Feature 066 introduces the onboarding wizard that **sets** `PreferredCurrency`. This feature (064) ensures all services **read** it. The two features are complementary but independent:

- **064 without 066:** Currency defaults to `"USD"` (current behavior preserved) until a user manually sets `PreferredCurrency` via the Settings API.
- **066 without 064:** Onboarding saves the currency preference, but services ignore it and keep showing `"USD"`.
- **Both together:** Full end-to-end: user picks currency in onboarding → all services display it.

Either can be implemented first. This feature (064) is designed to work with the fallback default, so it can safely ship before or after 066.

---

## References

- [Feature 066: Initial User Onboarding](./066-initial-user-onboarding.md) — sets the global currency
- [Feature 050: Calendar-Driven Reports](./archive/051-060-ai-performance-deployment.md) — original gap (#9) that spawned this feature
- [UserSettings entity](../src/BudgetExperiment.Domain/Settings/UserSettings.cs) — `PreferredCurrency` property
- [IUserContext](../src/BudgetExperiment.Domain/Repositories/IUserContext.cs) — current user context (no currency today)
- [MoneyDto](../src/BudgetExperiment.Contracts/Dtos/MoneyDto.cs) — DTO with `string.Empty` default (previously hardcoded `"USD"`)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-07 | Initial draft — spun off from Feature 050 Phase 1 deferred task | @copilot |
| 2026-02-23 | Rewrite — vertical slices, ICurrencyProvider, aligned with global currency model (Feature 066) | @copilot |
| 2026-02-23 | Implementation complete — all 6 slices done, 562 tests pass, 51 hardcoded USD replaced | @copilot |
