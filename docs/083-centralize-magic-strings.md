# Feature 083: Centralize Magic Strings and Constants

> **Status:** In Progress (Slice 1 complete)
> **Priority:** Medium (code quality / maintainability)
> **Dependencies:** None

## Overview

The coding standard (§8) requires "Centralize constants; avoid magic strings/numbers." A codebase audit found **~36+ magic string instances** across 15+ files, including duplicated claim names, reconciliation status strings, hardcoded URLs, OIDC scopes, default currency values, and export column names. Several values are duplicated across files, making them error-prone to update.

### Existing Constants Classes (already centralized)

| Class | Project | Purpose |
|-------|---------|---------|
| `AuthModeConstants` | Api | `"None"`, `"OIDC"` |
| `AuthProviderConstants` | Api | `"Authentik"`, `"Google"`, `"Microsoft"`, `"OIDC"` |
| `ImportValidationConstants` | Application | Max import limits |

### What Is NOT in Scope

- **Sort field strings** — The codebase already uses LINQ property access (`t => t.Date`) rather than string-based sorting. No magic strings to centralize.
- **Ollama URL in `AppSettings.cs`** — The domain entity already defines the default correctly. Only the EF configuration / test duplications need fixing.

---

## Vertical Slices

Each slice is independently deliverable: create the constants class, replace all usages, add tests, verify build. Slices are ordered by risk and duplication severity.

---

### Slice 1: Claim Name Constants ✅

**Goal:** Eliminate ~15 hardcoded OIDC claim type strings across auth files.

**Completed:** `ClaimConstants` created in `BudgetExperiment.Contracts/Constants/ClaimConstants.cs` (shared by Api and Client). 15 magic string replacements across 6 files. 5 regression guard tests added in `ClaimConstantsTests.cs`.

#### Current State

| String | Files Using It |
|--------|---------------|
| `"sub"` | `UserContext.cs`, `NoAuthHandler.cs`, `NoAuthAuthenticationStateProvider.cs` |
| `"preferred_username"` | `UserContext.cs`, `NoAuthHandler.cs`, `GoogleClaimMapper.cs`, `MicrosoftClaimMapper.cs`, `GenericOidcClaimMapper.cs` |
| `"email"` | `UserContext.cs`, `GoogleClaimMapper.cs`, `MicrosoftClaimMapper.cs`, `GenericOidcClaimMapper.cs` |
| `"name"` | `UserContext.cs`, `NoAuthHandler.cs` |
| `"picture"` | `UserContext.cs` |

#### Tasks

1. **RED** — Write unit tests in `BudgetExperiment.Api.Tests` verifying `ClaimConstants` fields have expected values (regression guard).
2. **GREEN** — ~~Create `ClaimConstants` static class in `src/BudgetExperiment.Api/Authentication/ClaimConstants.cs`.~~ Created in `src/BudgetExperiment.Contracts/Constants/ClaimConstants.cs` (shared by Api + Client).
3. **REFACTOR** — Replace all hardcoded claim strings in:
   - `UserContext.cs` (5 usages) ✅
   - `NoAuthHandler.cs` (3 usages) ✅
   - `GoogleClaimMapper.cs` (3 usages) ✅
   - `MicrosoftClaimMapper.cs` (3 usages) ✅
   - `GenericOidcClaimMapper.cs` (3 usages) ✅
   - `NoAuthAuthenticationStateProvider.cs` (1 usage) ✅ — Client references Contracts, so shared constant works directly.
4. **VERIFY** — Build succeeds (0 warnings, 0 errors), 70 auth tests + 645 client tests pass. ✅

#### Design

```csharp
// src/BudgetExperiment.Contracts/Constants/ClaimConstants.cs
public static class ClaimConstants
{
    public const string Subject = "sub";
    public const string PreferredUsername = "preferred_username";
    public const string Email = "email";
    public const string Name = "name";
    public const string Picture = "picture";
}
```

#### Commit
```
refactor: centralize OIDC claim name strings into ClaimConstants

Replace ~15 hardcoded claim type strings across UserContext,
NoAuthHandler, and all claim mapper classes with ClaimConstants.

Refs: #083
```

---

### Slice 2: Reconciliation Status Constants

**Goal:** Eliminate 4 hardcoded reconciliation status strings and prevent status typo bugs.

#### Current State

| String | File | Context |
|--------|------|---------|
| `"Matched"` | `ReconciliationStatusBuilder.cs` | Assignment + switch case |
| `"Pending"` | `ReconciliationStatusBuilder.cs` | Assignment + switch case |
| `"Missing"` | `ReconciliationStatusBuilder.cs` | Assignment |
| `"Missing"` | `RecurringInstanceStatusDto.cs` | Default property value |

#### Tasks

1. **RED** — Write unit tests verifying `ReconciliationStatus` constants have expected string values.
2. **GREEN** — Create `ReconciliationStatus` constants class. Place in `BudgetExperiment.Contracts` (both Application and Contracts already use these values).
3. **REFACTOR** — Replace all hardcoded status strings in:
   - `ReconciliationStatusBuilder.cs` (~4 usages: assignments and switch cases)
   - `RecurringInstanceStatusDto.cs` (1 usage: default property value)
4. **VERIFY** — Build succeeds, reconciliation tests pass, status display unchanged.

#### Design

```csharp
// src/BudgetExperiment.Contracts/Constants/ReconciliationStatus.cs
public static class ReconciliationStatus
{
    public const string Matched = "Matched";
    public const string Pending = "Pending";
    public const string Missing = "Missing";
}
```

#### Commit
```
refactor: centralize reconciliation status strings into ReconciliationStatus

Replace hardcoded "Matched"/"Pending"/"Missing" in ReconciliationStatusBuilder
and RecurringInstanceStatusDto with shared constants.

Refs: #083
```

---

### Slice 3: Default Currency Constant

**Goal:** Eliminate 5+ hardcoded `"USD"` default currency strings.

#### Current State

| File | Context |
|------|---------|
| `UserSettingsCurrencyProvider.cs` | `private const string DefaultCurrency = "USD"` — already a local constant, but not shared |
| `AccountDto.cs` | Default property value |
| `AccountCreateDto.cs` | Default property value |
| `TransactionRepository.cs` | Fallback currency |
| `DatabaseSeeder.cs` | Seed data |

#### Tasks

1. **RED** — Write unit test verifying `CurrencyDefaults.DefaultCurrency` equals `"USD"`.
2. **GREEN** — Create `CurrencyDefaults` in `BudgetExperiment.Domain` (domain concept, referenced by all layers).
3. **REFACTOR** — Replace all hardcoded `"USD"` defaults:
   - `UserSettingsCurrencyProvider.cs` — replace local constant with `CurrencyDefaults.DefaultCurrency`
   - `AccountDto.cs`, `AccountCreateDto.cs` — reference shared constant
   - `TransactionRepository.cs` — reference shared constant
   - `DatabaseSeeder.cs` — reference shared constant
4. **VERIFY** — Build succeeds, account/transaction tests pass.

#### Design

```csharp
// src/BudgetExperiment.Domain/Constants/CurrencyDefaults.cs
public static class CurrencyDefaults
{
    public const string DefaultCurrency = "USD";
}
```

#### Commit
```
refactor: centralize default currency ("USD") into CurrencyDefaults

Replace 5 hardcoded "USD" strings across DTOs, repository, seeder,
and currency provider with a single domain constant.

Refs: #083
```

---

### Slice 4: OIDC Scope Constants

**Goal:** Eliminate 4 duplicated OIDC scope arrays across Api, Contracts, and Client.

#### Current State

| File | Context |
|------|---------|
| `GenericOidcProviderOptions.cs` | `Scopes = ["openid", "profile", "email"]` |
| `ClientConfigOptions.cs` | `Scopes = ["openid", "profile", "email"]` |
| `OidcConfigDto.cs` | `Scopes = ["openid", "profile", "email"]` |
| `Client/Program.cs` | Individual `options.ProviderOptions.DefaultScopes.Add(...)` calls |

#### Tasks

1. **RED** — Write unit test verifying `OidcScopeDefaults` contains expected scope values.
2. **GREEN** — Create `OidcScopeDefaults` in `BudgetExperiment.Contracts` (shared between Api and Client).
3. **REFACTOR** — Replace all hardcoded scope arrays:
   - `GenericOidcProviderOptions.cs` — use constants
   - `ClientConfigOptions.cs` — use constants
   - `OidcConfigDto.cs` — use constants
   - `Client/Program.cs` — use constants
4. **VERIFY** — Build succeeds, OIDC auth flow unchanged.

#### Design

```csharp
// src/BudgetExperiment.Contracts/Constants/OidcScopeDefaults.cs
public static class OidcScopeDefaults
{
    public const string OpenId = "openid";
    public const string Profile = "profile";
    public const string Email = "email";

    public static readonly string[] DefaultScopes = [OpenId, Profile, Email];
}
```

#### Commit
```
refactor: centralize OIDC scope strings into OidcScopeDefaults

Replace 4 duplicated scope arrays across provider options, DTOs,
and Client setup with shared constants.

Refs: #083
```

---

### Slice 5: Export Column Name Constants

**Goal:** Centralize ~17 hardcoded export column header strings in `ExportController.cs`.

#### Current State

| Method | Columns |
|--------|---------|
| `BuildCategoryTable` | `"Category"`, `"Amount"`, `"Currency"`, `"Percentage"`, `"Transactions"` |
| `BuildTrendsTable` | `"Month"`, `"Income"`, `"Spending"`, `"Net"`, `"Transactions"` |
| `BuildBudgetComparisonTable` | `"Category"`, `"Budgeted"`, `"Spent"`, `"Remaining"`, `"PercentUsed"`, `"Status"`, `"Transactions"` |

These are only used in one file, so risk of typo-induced bugs is lower. However, centralizing them makes export format changes easier and enables test assertions against column names.

#### Tasks

1. **RED** — Write unit test verifying `ExportColumns` constants have expected values.
2. **GREEN** — Create `ExportColumns` static class in `BudgetExperiment.Application/Export/` (or alongside ExportController if preferred).
3. **REFACTOR** — Replace all hardcoded column name strings in `ExportController.cs`:
   - `BuildCategoryTable` (5 columns)
   - `BuildTrendsTable` (5 columns)
   - `BuildBudgetComparisonTable` (7 columns)
4. **VERIFY** — Build succeeds, export tests pass, export output unchanged.

#### Design

```csharp
// src/BudgetExperiment.Application/Export/ExportColumns.cs
public static class ExportColumns
{
    // Shared
    public const string Category = "Category";
    public const string Transactions = "Transactions";

    // Category table
    public const string Amount = "Amount";
    public const string Currency = "Currency";
    public const string Percentage = "Percentage";

    // Trends table
    public const string Month = "Month";
    public const string Income = "Income";
    public const string Spending = "Spending";
    public const string Net = "Net";

    // Budget comparison table
    public const string Budgeted = "Budgeted";
    public const string Spent = "Spent";
    public const string Remaining = "Remaining";
    public const string PercentUsed = "PercentUsed";
    public const string Status = "Status";
}
```

#### Commit
```
refactor: centralize export column names into ExportColumns

Replace ~17 hardcoded column header strings across 3 export table
builder methods with shared constants.

Refs: #083
```

---

### Slice 6: Ollama URL Default Consolidation

**Goal:** Remove duplicated default Ollama URL from EF configuration; reference the domain default.

#### Current State

| File | Context |
|------|---------|
| `AppSettings.cs` (Domain) | Default value `"http://localhost:11434"` — **this is the source of truth** |
| `AppSettingsConfiguration.cs` (Infrastructure) | Duplicated in EF Fluent API `.HasDefaultValue(...)` |
| `OllamaAiServiceTests.cs` (Tests) | Duplicated in test setup |

#### Tasks

1. **RED** — Write unit test verifying `AiDefaults.DefaultOllamaUrl` equals `"http://localhost:11434"`.
2. **GREEN** — Create `AiDefaults` in `BudgetExperiment.Domain/Settings/AiDefaults.cs`.
3. **REFACTOR** — Replace duplicated URLs:
   - `AppSettings.cs` — reference `AiDefaults.DefaultOllamaUrl`
   - `AppSettingsConfiguration.cs` — reference `AiDefaults.DefaultOllamaUrl`
   - `OllamaAiServiceTests.cs` — reference `AiDefaults.DefaultOllamaUrl`
4. **VERIFY** — Build succeeds, AI settings tests pass.

#### Design

```csharp
// src/BudgetExperiment.Domain/Settings/AiDefaults.cs
public static class AiDefaults
{
    public const string DefaultOllamaUrl = "http://localhost:11434";
}
```

#### Commit
```
refactor: consolidate default Ollama URL into AiDefaults

Replace 3 duplicated "http://localhost:11434" strings with a single
domain constant.

Refs: #083
```

---

### Slice 7: Family User Context Deduplication (Client ↔ Api)

**Goal:** Eliminate duplicated family user identity values between Api and Client.

#### Current State

| Value | Api (`FamilyUserContext.cs`) | Client (`NoAuthAuthenticationStateProvider.cs`) |
|-------|-----|--------|
| User ID | `new Guid("00000000-0000-0000-0000-000000000001")` | `"00000000-0000-0000-0000-000000000001"` (string) |
| User Name | `"Family"` | `"Family"` |
| User Email | `"family@localhost"` | `"family@localhost"` |

These must stay in sync. If someone changes the Api value, the Client silently breaks.

#### Tasks

1. **RED** — Write unit test verifying shared family user constants match expected values.
2. **GREEN** — Create `FamilyUserDefaults` in `BudgetExperiment.Contracts` (shared between Api and Client).
3. **REFACTOR** — Update both:
   - `FamilyUserContext.cs` (Api) — reference shared constants
   - `NoAuthAuthenticationStateProvider.cs` (Client) — reference shared constants
4. **VERIFY** — Build succeeds, NoAuth login flow unchanged.

#### Design

```csharp
// src/BudgetExperiment.Contracts/Constants/FamilyUserDefaults.cs
public static class FamilyUserDefaults
{
    public const string UserId = "00000000-0000-0000-0000-000000000001";
    public const string UserName = "Family";
    public const string UserEmail = "family@localhost";
}
```

#### Commit
```
refactor: deduplicate family user identity values into FamilyUserDefaults

Move shared family user ID/name/email to Contracts so Api and Client
reference the same source of truth.

Refs: #083
```

---

## Slice Summary & Recommended Order

| # | Slice | Instances | Risk | Priority |
|---|-------|-----------|------|----------|
| 1 | Claim Name Constants | ~15 | Low | P1 — most duplicated |
| 2 | Reconciliation Status Constants | 4 | Low | P1 — bug-prone strings |
| 3 | Default Currency Constant | 5+ | Low | P2 |
| 4 | OIDC Scope Constants | 4 files | Low | P2 |
| 5 | Export Column Name Constants | ~17 | Very Low | P3 — single file |
| 6 | Ollama URL Consolidation | 3 | Very Low | P3 — partially done |
| 7 | Family User Context Dedup | 3 values | Low | P3 — cross-project sync |

All slices are pure refactoring with no behavior change. Each can be delivered and merged independently.

## Testing Strategy

Each slice follows the same pattern:

1. **Constants regression test** — Verify constant values match expected strings (guards against accidental edits).
2. **Existing test suite** — All existing tests must continue to pass with zero changes (no behavior change).
3. **Build verification** — `dotnet build` succeeds with zero warnings.

No new integration or E2E tests needed — these are compile-time refactorings.

## Risk Assessment

- **Overall risk: Low** — Pure refactoring, replacing string literals with constant references. No runtime behavior change.
- **Layer placement decisions:**
  - Claims → Api (or Contracts if Client needs them)
  - Reconciliation status → Contracts (shared between Application and Contracts DTOs)
  - Currency defaults → Domain (fundamental domain concept)
  - OIDC scopes → Contracts (shared between Api and Client)
  - Export columns → Application (used only by export feature)
  - AI defaults → Domain (settings are domain entities)
  - Family user → Contracts (shared between Api and Client)

## References

- Coding standard §8: "Centralize constants; avoid magic strings/numbers."
- Existing patterns: `AuthModeConstants`, `AuthProviderConstants`, `ImportValidationConstants`

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
