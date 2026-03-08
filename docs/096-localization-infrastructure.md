# Feature 096: Localization Infrastructure (i18n Preparation)
> **Status:** Planning

## Overview

Prepare the application for multi-language and multi-culture support by establishing the localization infrastructure, middleware, and patterns. This feature does **not** add translations or enable additional languages — it lays the groundwork so that a follow-up feature can add locale-specific resources with minimal friction.

## Problem Statement

### Current State

- **No localization framework** — `IStringLocalizer`, `.resx` files, and `RequestLocalizationOptions` are entirely absent.
- **50+ hardcoded English strings** scattered across Razor pages and components (button labels, headings, error messages, empty-state text, navigation items).
- **Currency formatting is ad-hoc** — most components use `ToString("C")` without an explicit `IFormatProvider`, relying on `CultureInfo.CurrentCulture` (which varies by OS/CI environment and already caused a CI failure).
- **No browser locale/timezone detection** — the `UserSettings.TimeZoneId` domain model exists but nothing on the client detects or persists the browser's timezone or preferred language.
- **No localization middleware** registered in either the API or Client `Program.cs`.

### Target State

- Localization middleware configured in both API and Client.
- `.resx` resource file structure in place with English (`en-US`) as the default/fallback culture.
- `IStringLocalizer<T>` pattern established and demonstrated on a representative set of components (not full extraction — just the pattern).
- Browser locale and timezone detection via JS interop, wired to `UserSettings.TimeZoneId`.
- Currency and date formatting centralized through a `CultureService` (or similar) that provides a consistent `CultureInfo` for formatting, seeded from browser detection.
- All existing `ToString("C")` calls updated to use an explicit `IFormatProvider`.
- CI tests remain green with explicit culture in test setup (section 37 of copilot-instructions).

---

## User Stories

### Locale Detection

#### US-096-001: Browser Locale Detection
**As a** user  
**I want to** have the app detect my browser's language and timezone automatically  
**So that** formatting (currency, dates) matches my preferences without manual configuration

**Acceptance Criteria:**
- [ ] Browser timezone detected via `Intl.DateTimeFormat().resolvedOptions().timeZone` JS interop
- [ ] Browser language detected via `navigator.language` JS interop
- [ ] Detected values stored in a `CultureService` (scoped, client-side)
- [ ] `UserSettings.TimeZoneId` wired to detected timezone on first load
- [ ] Fallback to `en-US` if detection fails

### Formatting Consistency

#### US-096-002: Consistent Currency Formatting
**As a** developer  
**I want** all currency formatting to use an explicit culture provider  
**So that** the output is deterministic regardless of host OS locale

**Acceptance Criteria:**
- [ ] All `ToString("C")` calls in Razor components replaced with explicit `IFormatProvider`
- [ ] Chart codebehind formatting uses the same `CultureService`
- [ ] API export formatting continues to use `InvariantCulture` (no change)
- [ ] No CI test failures due to culture-dependent formatting

### Localization Plumbing

#### US-096-003: Resource File Infrastructure
**As a** developer  
**I want** the localization framework wired up with resource files  
**So that** adding new languages later is a matter of adding `.resx` files

**Acceptance Criteria:**
- [ ] `Microsoft.Extensions.Localization` configured in API and Client
- [ ] `RequestLocalizationOptions` middleware registered with `en-US` as default and only supported culture
- [ ] Shared resource file (`SharedResources.resx`) created with English strings as the baseline
- [ ] `IStringLocalizer<SharedResources>` injectable and functional
- [ ] At least one page/component converted to use `IStringLocalizer` as a reference pattern
- [ ] Documentation added on how to add a new language (add `.resx`, register culture)

---

## Technical Design

### Architecture Changes

#### New Services

- **`CultureService`** (Client) — Scoped service providing `CultureInfo` for formatting. Initialized from browser JS interop on startup. Exposes `CurrentCulture`, `CurrentTimeZone`.
- **`culture.js`** (Client wwwroot) — JS interop module for detecting `navigator.language` and `Intl.DateTimeFormat().resolvedOptions().timeZone`.

#### New Resources

- `src/BudgetExperiment.Client/Resources/SharedResources.resx` — Default (en-US) string resources.
- `src/BudgetExperiment.Client/Resources/SharedResources.cs` — Marker class for `IStringLocalizer<SharedResources>`.

#### Middleware Changes

- **API `Program.cs`** — Add `builder.Services.AddLocalization()` and `app.UseRequestLocalization()` with `en-US` default.
- **Client `Program.cs`** — Add `builder.Services.AddLocalization()` and register `CultureService`.

### Currency Formatting Centralization

Replace ad-hoc `ToString("C")` calls with a helper approach:

```csharp
// Option A: Extension method using CultureService
public static string FormatCurrency(this decimal value, CultureService cultureService)
    => value.ToString("C", cultureService.CurrentCulture);

// Option B: Direct IFormatProvider injection in components
@inject CultureService Culture
...
@Amount.ToString("C", Culture.CurrentCulture)
```

### Browser Detection (JS Interop)

```javascript
// wwwroot/js/culture.js
export function detectCulture() {
    return {
        language: navigator.language || 'en-US',
        timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone || 'UTC'
    };
}
```

### Database Changes

None — `UserSettings.TimeZoneId` already exists in the domain model.

### UI Components

- No visual changes in this feature.
- One representative component (e.g., `MainLayout.razor` or `Accounts.razor`) converted to `IStringLocalizer` as a pattern demonstration.

---

## Implementation Plan

### Phase 1: Browser Locale & Timezone Detection

**Objective:** Detect browser language and timezone, expose via `CultureService`

**Tasks:**
- [ ] Create `wwwroot/js/culture.js` with `detectCulture()` function
- [ ] Create `CultureService` in Client (scoped, calls JS interop on init)
- [ ] Register `CultureService` in Client `Program.cs`
- [ ] Wire detected timezone to `UserSettings.TimeZoneId` persistence
- [ ] Write unit tests for `CultureService` (mock JS interop)

### Phase 2: Localization Middleware & Resource Files

**Objective:** Set up ASP.NET Core localization framework in both projects

**Tasks:**
- [ ] Add `Microsoft.Extensions.Localization` to Client project
- [ ] Create `Resources/SharedResources.resx` with initial en-US strings (representative sample, not full extraction)
- [ ] Create `SharedResources.cs` marker class
- [ ] Configure `AddLocalization()` and `UseRequestLocalization()` in API `Program.cs`
- [ ] Configure `AddLocalization()` in Client `Program.cs`
- [ ] Write integration test verifying localization resolves en-US strings

### Phase 3: Centralize Currency Formatting

**Objective:** Replace all ad-hoc `ToString("C")` with explicit culture-aware formatting

**Tasks:**
- [ ] Create currency formatting extension method or helper
- [ ] Update all `ToString("C")` calls in Razor components to use explicit `IFormatProvider` via `CultureService`
- [ ] Update chart codebehind formatting to use `CultureService`
- [ ] Verify API export formatting unchanged (`InvariantCulture`)
- [ ] Ensure all client test classes set `CultureInfo.CurrentCulture` per section 37 guidelines
- [ ] Run full test suite — no culture-related failures

### Phase 4: IStringLocalizer Pattern Demo

**Objective:** Convert one representative component to `IStringLocalizer` as a reference for future work

**Tasks:**
- [ ] Pick a component with moderate string usage (e.g., `MainLayout.razor` nav items)
- [ ] Extract hardcoded strings to `SharedResources.resx`
- [ ] Inject `IStringLocalizer<SharedResources>` and replace hardcoded strings
- [ ] Write test verifying localized strings render correctly
- [ ] Document the pattern in a brief section in `CONTRIBUTING.md` or inline comments

### Phase 5: Documentation & Cleanup

**Objective:** Document the localization approach and how to add languages in the future

**Tasks:**
- [ ] Update copilot-instructions.md with localization guidelines (new section)
- [ ] Add brief "Adding a New Language" guide to `docs/` or `CONTRIBUTING.md`
- [ ] Update this feature doc status to Done
- [ ] Remove any TODO comments

---

## Future Work (Out of Scope)

These items are explicitly deferred to a follow-up feature:

- **Full string extraction** — Converting all 50+ hardcoded strings to resource files
- **Additional language .resx files** — e.g., `SharedResources.es.resx`, `SharedResources.fr.resx`
- **Language picker UI** — Component for users to select their preferred language
- **Per-user culture persistence** — Storing preferred language in user profile/settings API
- **RTL layout support** — Right-to-left language layout adjustments
- **Pluralization rules** — Culture-specific plural forms

---

## Risks & Considerations

- **Blazor WASM localization** has some differences from server-side — resource satellite assemblies must be downloaded to the browser. Consider lazy loading for large translation sets in the follow-up feature.
- **Currency symbol vs. user preference** — Detecting browser locale gives a reasonable default, but users may want to override (e.g., expat using a foreign browser locale). The follow-up feature should address this via user settings.
- **Test culture discipline** — All test projects must continue to set explicit culture per section 37 to avoid CI regressions.
