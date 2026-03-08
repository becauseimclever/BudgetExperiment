# Feature 104: ViewModel Extraction — Onboarding Page

> **Status:** Planning
> **Priority:** Low
> **Dependencies:** Feature 097 (Done — established pattern)

## Overview

Extract the handler logic from the Onboarding page `@code` block into a testable `OnboardingViewModel` class, following the ViewModel/Presenter pattern established in Feature 097 (Categories). The Onboarding page is the smallest candidate with a wizard-style multi-step flow, currency selection, and first-day-of-week preference.

## Problem Statement

### Current State

The Onboarding page ([Onboarding.razor](../src/BudgetExperiment.Client/Pages/Onboarding.razor)) contains **228 lines** with the `@code` block starting at line 159. All handler logic lives inline in the Razor file.

**Structural issues:**
- **6 handler methods** embedded in `@code` block: `NextStep`, `PreviousStep`, `SelectCurrency`, `GetCurrencyDisplay`, `SkipOnboarding`, `CompleteOnboarding`
- **7 state fields** managing step navigation, currency selection, and saving state
- **1 computed property** (`FilteredCurrencies` — filters currency list by search term)
- **2 injected services**: `IBudgetApiService`, `NavigationManager`
- Does **not** implement `IDisposable` (no event subscriptions)

**Coverage instrumentation gap:** Same async state-machine issue as Categories — `@code` block handlers report near-0% coverage despite being exercised by bUnit tests.

### Target State

- `OnboardingViewModel` is a plain C# class in `BudgetExperiment.Client/ViewModels/`
- All handler methods, state fields, and computed properties live in the ViewModel
- `Onboarding.razor` is a thin binding layer
- ViewModel is directly testable with xUnit + Shouldly
- Coverage reports accurately for all handler methods
- Existing 14 `OnboardingPageTests` continue to pass

---

## User Stories

### US-104-001: Extract OnboardingViewModel

**As a** developer
**I want to** extract handler logic from Onboarding.razor into an OnboardingViewModel class
**So that** page business logic is testable with plain xUnit and coverage reports accurately.

**Acceptance Criteria:**
- [ ] `OnboardingViewModel` class exists in `src/BudgetExperiment.Client/ViewModels/OnboardingViewModel.cs`
- [ ] All 6 handler methods are moved to the ViewModel
- [ ] All 7 state fields are moved to the ViewModel
- [ ] 1 computed property (`FilteredCurrencies`) is moved to the ViewModel
- [ ] ViewModel exposes `Action? OnStateChanged` callback for re-rendering
- [ ] `Onboarding.razor` delegates all logic to the injected ViewModel
- [ ] ViewModel receives services via constructor injection
- [ ] All existing 14 `OnboardingPageTests` pass without modification (or with minimal binding changes)

### US-104-002: Add ViewModel Unit Tests

**As a** developer
**I want to** test OnboardingViewModel directly with xUnit
**So that** all handler logic has comprehensive and accurately reported coverage.

**Acceptance Criteria:**
- [ ] `OnboardingViewModelTests.cs` exists in `tests/BudgetExperiment.Client.Tests/ViewModels/`
- [ ] Tests cover all 6 handler methods
- [ ] Tests verify step navigation (next, previous, boundaries)
- [ ] Tests verify currency selection and filtering
- [ ] Tests verify skip and complete onboarding workflows
- [ ] Tests verify error handling (API failures)
- [ ] Coverage for OnboardingViewModel ≥ 90%

---

## Technical Design

### Architecture

```
Before:
  Onboarding.razor
    └── @code { 6 handlers, 7 fields, 1 computed property, 2 services }

After:
  Onboarding.razor
    └── @inject OnboardingViewModel ViewModel
    └── binds UI to ViewModel.Property / ViewModel.MethodAsync()

  OnboardingViewModel (plain C# class)
    └── constructor(IBudgetApiService, NavigationManager)
    └── 6 public methods
    └── 7 public properties (state)
    └── 1 computed property (FilteredCurrencies)
    └── Action? OnStateChanged (for Razor re-render callback)
```

### OnboardingViewModel Design

```csharp
public sealed class OnboardingViewModel
{
    // Constructor injection
    // IBudgetApiService, NavigationManager

    // State properties (7)
    public int CurrentStep { get; private set; }
    public string SelectedCurrency { get; set; }
    public DayOfWeek SelectedFirstDay { get; set; }
    public string CurrencySearch { get; set; }
    public bool ShowCurrencyDropdown { get; set; }
    public bool IsSaving { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Computed
    public IEnumerable<CurrencyOption> FilteredCurrencies => ...;

    // Re-render callback
    public Action? OnStateChanged { get; set; }

    // All 6 handler methods (public)
    public void NextStep() { ... }
    public void PreviousStep() { ... }
    public void SelectCurrency(CurrencyOption currency) { ... }
    public string GetCurrencyDisplay() { ... }
    public async Task SkipOnboardingAsync() { ... }
    public async Task CompleteOnboardingAsync() { ... }
}
```

### DI Registration

```csharp
builder.Services.AddTransient<OnboardingViewModel>();
```

---

## Implementation Plan

### Phase 1: Create OnboardingViewModel with Tests (TDD)

**Tasks:**
- [ ] Create `src/BudgetExperiment.Client/ViewModels/OnboardingViewModel.cs`
- [ ] Create `tests/BudgetExperiment.Client.Tests/ViewModels/OnboardingViewModelTests.cs`
- [ ] Write tests for `NextStep` — increments step, respects upper bound
- [ ] Write tests for `PreviousStep` — decrements step, respects lower bound
- [ ] Write tests for `SelectCurrency` — sets currency, closes dropdown
- [ ] Write tests for `GetCurrencyDisplay` — returns formatted currency string
- [ ] Write tests for `FilteredCurrencies` — filters by code/name, case-insensitive
- [ ] Write tests for `SkipOnboardingAsync` — calls API, navigates away
- [ ] Write tests for `CompleteOnboardingAsync` — saves preferences, navigates, handles failure
- [ ] Write tests for `OnStateChanged` callback — invoked after state mutations
- [ ] Implement ViewModel to pass all tests
- [ ] Verify ≥ 90% coverage on OnboardingViewModel

### Phase 2: Refactor Onboarding.razor to Use ViewModel

**Tasks:**
- [ ] Register `OnboardingViewModel` as transient in DI
- [ ] Update `Onboarding.razor` to inject `OnboardingViewModel`
- [ ] Replace all state fields with `ViewModel.PropertyName` bindings
- [ ] Replace all event handlers with `ViewModel.MethodAsync` calls
- [ ] Wire `OnStateChanged` callback in `OnInitializedAsync`
- [ ] Remove `@code` block (except thin `OnInitializedAsync` wiring)
- [ ] Verify all 14 existing `OnboardingPageTests` pass
- [ ] Verify application runs correctly (manual smoke test)

### Phase 3: Verify Coverage & Finalize

**Tasks:**
- [ ] Run coverage report for OnboardingViewModel — verify ≥ 90%
- [ ] Update this document status to Done

---

## Testing Strategy

### Unit Tests (OnboardingViewModelTests)

Tests use `StubBudgetApiService` (already exists) and a stub/mock `NavigationManager`.

**Test categories:**
- **Step navigation:** Next/previous with boundary clamping
- **Currency selection:** Select currency, search/filter, display formatting
- **Skip onboarding:** Calls API and navigates to home
- **Complete onboarding:** Saves preferences, handles API failure, navigates on success
- **StateHasChanged:** Callback invoked after each state mutation

### Integration Tests (Existing bUnit)

The 14 existing `OnboardingPageTests` serve as integration tests verifying the Razor → ViewModel → DOM pipeline.

---

## Notes

- This is the simplest extraction candidate — only 6 handlers and no IDisposable needed.
- No `ScopeService` dependency — simpler than other pages.
- Good candidate for a developer new to the ViewModel pattern to implement.

---

## References

- [Feature 097: ViewModel Extraction — Categories (Prototype)](./097-viewmodel-extraction-categories.md) — Established pattern
- [Component Standards — Section 11: ViewModel Pattern](./COMPONENT-STANDARDS.md) — Pattern documentation
