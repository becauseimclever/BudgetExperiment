# Feature 066: Initial User Onboarding — Currency & First Day of Week
> **Status:** 🗒️ Planning  
> **Priority:** High  
> **Estimated Effort:** Small–Medium (1 sprint)  
> **Dependencies:** None (builds on existing `UserSettings` entity and `UserController` API)

## Overview

Introduce a first-run onboarding experience that greets new users and captures two essential application-wide preferences before they begin using the app: **preferred currency** and **first day of the week**. The onboarding wizard appears once — the first time an authenticated user accesses the application and does not yet have persisted settings. After completing (or skipping) the wizard, the user proceeds to the normal application and never sees it again.

The application enforces a **single currency** model: all monetary values (transactions, budgets, reports) use the user's chosen currency. Currency can be changed later in the Settings page, but only one currency is active at any time.

## Problem Statement

### Current State

- `UserSettings` exists in the domain with `PreferredCurrency` (nullable string), but it is **never surfaced in the client UI**.
- No `FirstDayOfWeek` property exists anywhere — calendar views default to a hard-coded start day.
- The `api/v1/user/settings` endpoints (GET/PUT) exist but the **Blazor client has no service calls to them** and no UI to display or edit user-specific settings.
- New users land directly on the dashboard with system defaults and no guidance on how to configure the app.

### Target State

- On first login, users see a clean, friendly onboarding wizard (2–3 steps) that collects:
  1. **Currency** — single ISO 4217 currency code (e.g., USD, EUR, GBP) used app-wide.
  2. **First day of the week** — Sunday or Monday (the two most common worldwide conventions).
- After completing or skipping onboarding, the flag `IsOnboarded` is set to `true` in `UserSettings` and the wizard never reappears.
- The Settings page is updated to allow editing currency and first-day-of-week after onboarding.
- Calendar components and any day-of-week–dependent logic respect the user's `FirstDayOfWeek` preference.
- All monetary display and entry defaults to the user's chosen currency (single currency model).

---

## User Stories

### Onboarding Wizard

#### US-066-001: First-run onboarding prompt
**As a** new user  
**I want to** be guided through initial setup when I first log in  
**So that** the application is configured to my locale preferences from the start

**Acceptance Criteria:**
- [ ] Onboarding wizard appears on first authenticated visit when `IsOnboarded` is `false`
- [ ] Wizard does not block unauthenticated routes (login page, public pages)
- [ ] Wizard offers a "Skip" option that sets reasonable defaults (USD, Sunday) and marks onboarding complete
- [ ] After completion or skip, user is redirected to the dashboard
- [ ] Wizard never reappears once `IsOnboarded` is `true`

#### US-066-002: Select preferred currency
**As a** user going through onboarding  
**I want to** choose my currency  
**So that** all monetary values in the app display in my local currency

**Acceptance Criteria:**
- [ ] Currency step shows a searchable dropdown of common ISO 4217 currency codes with display names (e.g., "USD — US Dollar")
- [ ] Default pre-selection is USD
- [ ] Only one currency can be selected at a time
- [ ] Selected currency is persisted to `UserSettings.PreferredCurrency`

#### US-066-003: Select first day of the week
**As a** user going through onboarding  
**I want to** choose whether my week starts on Sunday or Monday  
**So that** calendar views and weekly reports align with my locale convention

**Acceptance Criteria:**
- [ ] First-day-of-week step offers two clear choices: Sunday or Monday
- [ ] Default pre-selection is Sunday
- [ ] Selected value is persisted to `UserSettings.FirstDayOfWeek`
- [ ] Calendar components use this setting to render the correct start day

#### US-066-004: Edit preferences after onboarding
**As a** user  
**I want to** change my currency and first-day-of-week in the Settings page  
**So that** I can update my preferences without re-running onboarding

**Acceptance Criteria:**
- [ ] Settings page has a "User Preferences" section with currency and first-day-of-week fields
- [ ] Changes are persisted via `PUT api/v1/user/settings`
- [ ] Changing currency updates display across the app without page reload (or after minimal refresh)
- [ ] Changing first-day-of-week updates calendar rendering

---

## Technical Design

### Architecture Changes

No new projects are introduced. Changes span all existing layers:

| Layer | Changes |
|-------|---------|
| **Domain** | Add `FirstDayOfWeek` and `IsOnboarded` properties to `UserSettings` |
| **Contracts** | Add `FirstDayOfWeek` and `IsOnboarded` to `UserSettingsDto` |
| **Infrastructure** | EF migration for new columns; update `UserSettingsConfiguration` |
| **Application** | Update `UserSettingsService` to handle new fields |
| **API** | Update `UserController` mapping (fields already flow through existing endpoints) |
| **Client** | New onboarding wizard component; update Settings page; wire `api/v1/user/settings` |

### Domain Model

#### `UserSettings` — new properties

```csharp
// Added to existing UserSettings entity
public DayOfWeek FirstDayOfWeek { get; private set; } = DayOfWeek.Sunday;
public bool IsOnboarded { get; private set; }

public void UpdateFirstDayOfWeek(DayOfWeek firstDayOfWeek)
{
    // Only Sunday and Monday are valid choices
    if (firstDayOfWeek != DayOfWeek.Sunday && firstDayOfWeek != DayOfWeek.Monday)
    {
        throw new DomainException("First day of the week must be Sunday or Monday.", ExceptionType.Validation);
    }

    this.FirstDayOfWeek = firstDayOfWeek;
    this.UpdatedAtUtc = DateTime.UtcNow;
}

public void CompleteOnboarding()
{
    this.IsOnboarded = true;
    this.UpdatedAtUtc = DateTime.UtcNow;
}
```

#### `CreateDefault` factory update

```csharp
public static UserSettings CreateDefault(Guid userId)
{
    return new UserSettings
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        DefaultScope = BudgetScope.Shared,
        AutoRealizePastDueItems = true,
        PastDueLookbackDays = 30,
        PreferredCurrency = "USD",
        FirstDayOfWeek = DayOfWeek.Sunday,
        IsOnboarded = false,       // <-- new
        CreatedAtUtc = DateTime.UtcNow,
        UpdatedAtUtc = DateTime.UtcNow,
    };
}
```

### Contracts / DTOs

#### `UserSettingsDto` — new fields

```csharp
public DayOfWeek FirstDayOfWeek { get; set; } = DayOfWeek.Sunday;
public bool IsOnboarded { get; set; }
```

### API Endpoints

No new endpoints required. The existing `UserController` endpoints handle the new fields:

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `api/v1/user/settings` | Returns `UserSettingsDto` including `FirstDayOfWeek`, `IsOnboarded` |
| PUT | `api/v1/user/settings` | Updates user settings including new fields |

A dedicated convenience endpoint for completing onboarding may optionally be added:

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `api/v1/user/settings/complete-onboarding` | Sets `IsOnboarded = true` (called at wizard finish) |

### Database Changes

New columns on the `UserSettings` table:

| Column | Type | Default | Nullable |
|--------|------|---------|----------|
| `FirstDayOfWeek` | `integer` (maps to `DayOfWeek` enum) | `0` (Sunday) | No |
| `IsOnboarded` | `boolean` | `false` | No |

Migration:
```bash
dotnet ef migrations add AddUserOnboardingFields --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

### UI Components

#### Onboarding Wizard (`Pages/Onboarding.razor`)

- **Route:** `/onboarding`
- **Layout:** Minimal (no sidebar/nav), centered card layout
- **Steps:**
  1. **Welcome** — brief greeting, explain what's next
  2. **Currency** — searchable dropdown, pre-selected USD
  3. **First Day of Week** — two-option toggle (Sunday / Monday), pre-selected Sunday
  4. **Confirmation** — summary of choices, "Get Started" button
- **Skip:** Available on every step; sets defaults and completes onboarding

#### Onboarding Guard (redirect logic)

- In `App.razor` or a layout component, after authentication resolves:
  - Call `GET api/v1/user/settings`
  - If `IsOnboarded == false`, redirect to `/onboarding`
  - If `IsOnboarded == true`, proceed normally
- Cache the `IsOnboarded` flag client-side after first check to avoid re-fetching on every navigation

#### Settings Page Update

- Add a "User Preferences" section (or tab) to the existing Settings page with:
  - Currency dropdown (same as onboarding)
  - First-day-of-week toggle (same as onboarding)
  - Save button → `PUT api/v1/user/settings`

#### Client Service

- Add methods to `BudgetApiService` (or a new `UserSettingsService`):
  - `GetUserSettingsAsync()` → `GET api/v1/user/settings`
  - `UpdateUserSettingsAsync(UserSettingsDto)` → `PUT api/v1/user/settings`
  - `CompleteOnboardingAsync()` → `POST api/v1/user/settings/complete-onboarding`

#### Currency List

A static list of common currencies (subset of ISO 4217) stored client-side:

```csharp
public static readonly IReadOnlyList<CurrencyOption> Currencies = new[]
{
    new CurrencyOption("USD", "US Dollar", "$"),
    new CurrencyOption("EUR", "Euro", "€"),
    new CurrencyOption("GBP", "British Pound", "£"),
    new CurrencyOption("CAD", "Canadian Dollar", "CA$"),
    new CurrencyOption("AUD", "Australian Dollar", "A$"),
    new CurrencyOption("JPY", "Japanese Yen", "¥"),
    new CurrencyOption("CHF", "Swiss Franc", "CHF"),
    new CurrencyOption("SEK", "Swedish Krona", "kr"),
    new CurrencyOption("NOK", "Norwegian Krone", "kr"),
    new CurrencyOption("DKK", "Danish Krone", "kr"),
    new CurrencyOption("NZD", "New Zealand Dollar", "NZ$"),
    new CurrencyOption("MXN", "Mexican Peso", "MX$"),
    new CurrencyOption("BRL", "Brazilian Real", "R$"),
    new CurrencyOption("INR", "Indian Rupee", "₹"),
    new CurrencyOption("ZAR", "South African Rand", "R"),
    // ... extend as needed
};

public record CurrencyOption(string Code, string Name, string Symbol);
```

---

## Implementation Plan

### Phase 1: Domain — Add `FirstDayOfWeek` and `IsOnboarded` to `UserSettings`

**Objective:** Extend the domain entity with new properties, validation, and mutation methods.

**Tasks:**
- [ ] Write unit tests for `UpdateFirstDayOfWeek` (valid: Sunday, Monday; invalid: other days → `DomainException`)
- [ ] Write unit tests for `CompleteOnboarding` (sets `IsOnboarded = true`, updates timestamp)
- [ ] Write unit test verifying `CreateDefault` initializes `FirstDayOfWeek = Sunday` and `IsOnboarded = false`
- [ ] Add `FirstDayOfWeek` property and `UpdateFirstDayOfWeek()` method to `UserSettings`
- [ ] Add `IsOnboarded` property and `CompleteOnboarding()` method to `UserSettings`
- [ ] Update `CreateDefault()` factory

**Commit:**
```bash
git add .
git commit -m "feat(domain): add FirstDayOfWeek and IsOnboarded to UserSettings

- DayOfWeek property restricted to Sunday/Monday
- IsOnboarded flag for first-run onboarding tracking
- Domain validation with DomainException for invalid days
- Unit tests for new behavior

Refs: #066"
```

---

### Phase 2: Contracts & Infrastructure — DTO and migration

**Objective:** Add DTO fields and create the EF Core migration for new columns.

**Tasks:**
- [ ] Add `FirstDayOfWeek` and `IsOnboarded` to `UserSettingsDto`
- [ ] Update `UserSettingsConfiguration` (EF Fluent API) if needed for column mapping
- [ ] Create EF migration `AddUserOnboardingFields`
- [ ] Update mapping logic in `UserSettingsService` / `UserController` for new fields
- [ ] Write integration tests verifying round-trip persistence of new fields

**Commit:**
```bash
git add .
git commit -m "feat(infra): add EF migration for UserSettings onboarding fields

- FirstDayOfWeek (integer, default Sunday) column
- IsOnboarded (boolean, default false) column
- UserSettingsDto updated with new fields
- Mapping updated in service/controller layer

Refs: #066"
```

---

### Phase 3: API — Optional complete-onboarding endpoint

**Objective:** Add a convenience endpoint to mark onboarding as complete.

**Tasks:**
- [ ] Add `POST api/v1/user/settings/complete-onboarding` action to `UserController`
- [ ] Write API integration test (WebApplicationFactory) for the new endpoint
- [ ] Verify existing `PUT api/v1/user/settings` correctly persists `FirstDayOfWeek` and `IsOnboarded`

**Commit:**
```bash
git add .
git commit -m "feat(api): add complete-onboarding endpoint

- POST api/v1/user/settings/complete-onboarding
- Sets IsOnboarded = true for current user
- Integration tests for endpoint

Refs: #066"
```

---

### Phase 4: Client — Onboarding wizard UI

**Objective:** Build the onboarding wizard and redirect logic.

**Tasks:**
- [ ] Add `GetUserSettingsAsync` and `UpdateUserSettingsAsync` methods to `BudgetApiService`
- [ ] Add `CompleteOnboardingAsync` method to `BudgetApiService`
- [ ] Create `CurrencyOption` record and static currency list (e.g., `CurrencyList.cs`)
- [ ] Create `Onboarding.razor` page with step-based wizard (Welcome → Currency → First Day of Week → Confirm)
- [ ] Implement onboarding guard (redirect to `/onboarding` when `IsOnboarded == false`)
- [ ] Cache `IsOnboarded` flag client-side after first check
- [ ] Style wizard for clean, minimal, centered layout

**Commit:**
```bash
git add .
git commit -m "feat(client): add first-run onboarding wizard

- Step-based wizard: currency, first day of week
- Redirect guard when IsOnboarded is false
- Currency list with common ISO 4217 codes
- Skip option with sensible defaults

Refs: #066"
```

---

### Phase 5: Client — Update Settings page

**Objective:** Expose currency and first-day-of-week in the existing Settings page for post-onboarding editing.

**Tasks:**
- [ ] Add "User Preferences" section/tab to `Settings.razor`
- [ ] Currency dropdown (reuse `CurrencyList`)
- [ ] First-day-of-week toggle (Sunday / Monday)
- [ ] Save button calls `PUT api/v1/user/settings`
- [ ] Toast/notification on successful save

**Commit:**
```bash
git add .
git commit -m "feat(client): add user preferences to Settings page

- Currency and first-day-of-week editing
- Reuses currency list from onboarding
- Persists via PUT api/v1/user/settings

Refs: #066"
```

---

### Phase 6: Client — Wire FirstDayOfWeek into calendar components

**Objective:** Calendar views respect the user's first-day-of-week preference.

**Tasks:**
- [ ] Identify all calendar rendering logic that assumes a fixed start day
- [ ] Inject user's `FirstDayOfWeek` from cached settings into calendar components
- [ ] Update day-header labels (Sun–Sat vs Mon–Sun)
- [ ] Test both Sunday-start and Monday-start rendering manually

**Commit:**
```bash
git add .
git commit -m "feat(client): calendar respects FirstDayOfWeek user setting

- Calendar grid starts on user's preferred day
- Day-of-week headers update accordingly

Refs: #066"
```

---

### Phase 7: Documentation & Cleanup

**Objective:** Final polish, documentation updates, and cleanup.

**Tasks:**
- [ ] Update API documentation / OpenAPI specs (new fields auto-documented)
- [ ] Add/update XML comments for public APIs (`UserSettings`, `UserController` actions)
- [ ] Remove any TODO comments introduced during development
- [ ] Final code review

**Commit:**
```bash
git add .
git commit -m "docs(onboarding): documentation for feature 066

- XML comments for onboarding domain methods
- OpenAPI spec reflects new fields

Refs: #066"
```

---

## Testing Strategy

### Unit Tests

- [ ] `UserSettings.UpdateFirstDayOfWeek(DayOfWeek.Sunday)` succeeds
- [ ] `UserSettings.UpdateFirstDayOfWeek(DayOfWeek.Monday)` succeeds
- [ ] `UserSettings.UpdateFirstDayOfWeek(DayOfWeek.Wednesday)` throws `DomainException`
- [ ] `UserSettings.CompleteOnboarding()` sets `IsOnboarded = true` and updates timestamp
- [ ] `UserSettings.CreateDefault()` initializes `FirstDayOfWeek = Sunday`, `IsOnboarded = false`
- [ ] `UserSettings.UpdatePreferredCurrency("EUR")` succeeds (existing test, verify still passes)

### Integration Tests

- [ ] `UserSettings` round-trip: create → persist → retrieve with `FirstDayOfWeek` and `IsOnboarded`
- [ ] `GET api/v1/user/settings` returns new fields with correct defaults
- [ ] `PUT api/v1/user/settings` updates `FirstDayOfWeek` and `IsOnboarded`
- [ ] `POST api/v1/user/settings/complete-onboarding` sets flag correctly

### Manual Testing Checklist

- [ ] Fresh user login → onboarding wizard appears
- [ ] Complete wizard → redirected to dashboard, wizard does not reappear
- [ ] Skip wizard → defaults applied, wizard does not reappear
- [ ] Settings page → can change currency and first day of week
- [ ] Calendar → starts on selected first day of week
- [ ] All monetary displays show selected currency

---

## Migration Notes

### Database Migration

```bash
dotnet ef migrations add AddUserOnboardingFields --project src/BudgetExperiment.Infrastructure --startup-project src/BudgetExperiment.Api
```

Existing `UserSettings` rows will receive defaults (`FirstDayOfWeek = 0` (Sunday), `IsOnboarded = false`). Users who already use the app will see the onboarding wizard once — this is intentional so they can set their preferences. If this is undesirable, a data migration can set `IsOnboarded = true` for existing rows.

### Breaking Changes

None. New fields have defaults and existing API consumers will not break (additive DTO changes only).

---

## Security Considerations

- Onboarding wizard is only accessible to authenticated users — the redirect guard checks authentication first.
- Currency code is validated against a known list (prevent injection of arbitrary strings).
- `IsOnboarded` can only be set to `true` (no method to un-complete onboarding), preventing replay.

---

## Performance Considerations

- `IsOnboarded` check happens once on app startup and is cached client-side — negligible overhead.
- Currency list is a static client-side array — no API call needed.
- No additional database queries beyond the existing `GET api/v1/user/settings` call.

---

## Future Enhancements

- **Time zone selection** in onboarding (currently stored in `UserSettings.TimeZoneId` but not surfaced — could be added as a wizard step).
- **Default budget scope** selection (Personal vs Shared) as an onboarding step.
- **Multi-currency support** — if needed in the future, the single-currency model can be expanded; for now, simplicity is preferred.
- **Currency conversion** — not in scope; changing currency does not convert existing amounts.
- **Locale-aware formatting** — use `CultureInfo` derived from currency for number/date formatting.
- **More first-day-of-week options** — currently restricted to Sunday/Monday; could expand to Saturday for Middle Eastern locales.

---

## References

- [UserSettings entity](../src/BudgetExperiment.Domain/Settings/UserSettings.cs)
- [UserController API](../src/BudgetExperiment.Api/Controllers/UserController.cs)
- [MoneyValue domain object](../src/BudgetExperiment.Domain/Common/MoneyValue.cs)
- [Settings page (current)](../src/BudgetExperiment.Client/Pages/Settings.razor)
- [ISO 4217 Currency Codes](https://en.wikipedia.org/wiki/ISO_4217)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-08 | Initial draft | @copilot |
