# Feature 100: ViewModel Extraction — Budget Page

> **Status:** Planning
> **Priority:** Medium
> **Dependencies:** Feature 097 (Done — established pattern)

## Overview

Extract the handler logic from the Budget page `@code` block into a testable `BudgetViewModel` class, following the ViewModel/Presenter pattern established in Feature 097 (Categories). The Budget page includes month navigation, budget goal editing, and status computation logic.

## Problem Statement

### Current State

The Budget page ([Budget.razor](../src/BudgetExperiment.Client/Pages/Budget.razor)) contains **369 lines** with the `@code` block starting at line 161. All handler logic lives inline in the Razor file.

**Structural issues:**
- **16 handler methods** embedded in `@code` block: `OnInitializedAsync`, `OnScopeChanged`, `Dispose`, `LoadBudget`, `RetryLoad`, `DismissError`, `PreviousMonth`, `NextMonth`, `GetOverallStatus`, `ShowEditGoal`, `HideEditGoal`, `GetModalTitle`, `IsCreatingNewGoal`, `SaveGoal`, `DeleteGoal`, `NavigateToCategories`
- **9 state fields** managing loading, date navigation, goal editing, and summary data
- **3 computed methods** (`GetOverallStatus`, `GetModalTitle`, `IsCreatingNewGoal`) that behave as computed properties
- **3 injected services**: `IBudgetApiService`, `NavigationManager`, `ScopeService`

**Coverage instrumentation gap:** Same async state-machine issue as Categories — `@code` block handlers report near-0% coverage despite being exercised by bUnit tests.

### Target State

- `BudgetViewModel` is a plain C# class in `BudgetExperiment.Client/ViewModels/`
- All handler methods, state fields, and computed methods live in the ViewModel
- `Budget.razor` is a thin binding layer
- ViewModel is directly testable with xUnit + Shouldly
- Coverage reports accurately for all handler methods
- Existing 21 `BudgetPageTests` continue to pass

---

## User Stories

### US-100-001: Extract BudgetViewModel

**As a** developer
**I want to** extract handler logic from Budget.razor into a BudgetViewModel class
**So that** page business logic is testable with plain xUnit and coverage reports accurately.

**Acceptance Criteria:**
- [ ] `BudgetViewModel` class exists in `src/BudgetExperiment.Client/ViewModels/BudgetViewModel.cs`
- [ ] All 16 handler methods are moved to the ViewModel
- [ ] All 9 state fields are moved to the ViewModel
- [ ] All 3 computed methods are moved to the ViewModel
- [ ] ViewModel exposes `Action? OnStateChanged` callback for re-rendering
- [ ] `Budget.razor` delegates all logic to the injected ViewModel
- [ ] ViewModel receives services via constructor injection
- [ ] All existing 21 `BudgetPageTests` pass without modification (or with minimal binding changes)

### US-100-002: Add ViewModel Unit Tests

**As a** developer
**I want to** test BudgetViewModel directly with xUnit
**So that** all handler logic has comprehensive and accurately reported coverage.

**Acceptance Criteria:**
- [ ] `BudgetViewModelTests.cs` exists in `tests/BudgetExperiment.Client.Tests/ViewModels/`
- [ ] Tests cover all 16 handler methods
- [ ] Tests verify state transitions (loading, error, success)
- [ ] Tests verify month navigation (PreviousMonth, NextMonth reload data)
- [ ] Tests verify budget goal CRUD (save, delete)
- [ ] Tests verify computed methods (GetOverallStatus, GetModalTitle, IsCreatingNewGoal)
- [ ] Tests verify error handling (API failures)
- [ ] Tests verify scope change triggers reload and dispose cleans up
- [ ] Coverage for BudgetViewModel ≥ 90%

---

## Technical Design

### Architecture

```
Before:
  Budget.razor
    └── @code { 16 handlers, 9 fields, 3 computed methods, 3 services }

After:
  Budget.razor
    └── @inject BudgetViewModel ViewModel
    └── binds UI to ViewModel.Property / ViewModel.MethodAsync()

  BudgetViewModel (plain C# class)
    └── constructor(IBudgetApiService, NavigationManager, ScopeService)
    └── 16 public methods
    └── 9 public properties (state)
    └── 3 computed properties/methods
    └── Action? OnStateChanged (for Razor re-render callback)
```

### BudgetViewModel Design

```csharp
public sealed class BudgetViewModel : IDisposable
{
    // Constructor injection
    // IBudgetApiService, NavigationManager, ScopeService

    // State properties (9)
    public bool IsLoading { get; private set; }
    public bool IsRetrying { get; private set; }
    public bool IsSubmitting { get; private set; }
    public string? ErrorMessage { get; private set; }
    public BudgetSummaryDto? Summary { get; private set; }
    public DateTime CurrentDate { get; private set; }
    public bool ShowEditGoal { get; private set; }
    public BudgetProgressDto? EditingProgress { get; private set; }
    public decimal EditTargetAmount { get; set; }

    // Computed
    public string OverallStatus => ...;
    public string ModalTitle => ...;
    public bool IsCreatingNewGoal => ...;

    // Re-render callback
    public Action? OnStateChanged { get; set; }

    // All 16 handler methods (public)
}
```

### DI Registration

```csharp
builder.Services.AddTransient<BudgetViewModel>();
```

---

## Implementation Plan

### Phase 1: Create BudgetViewModel with Tests (TDD)

**Tasks:**
- [ ] Create `src/BudgetExperiment.Client/ViewModels/BudgetViewModel.cs`
- [ ] Create `tests/BudgetExperiment.Client.Tests/ViewModels/BudgetViewModelTests.cs`
- [ ] Write tests for `InitializeAsync` — loads budget, subscribes to scope changes
- [ ] Write tests for `LoadBudgetAsync` — success, failure, loading state transitions
- [ ] Write tests for `RetryLoadAsync` — sets retrying flag, reloads
- [ ] Write tests for `DismissError` — clears error message
- [ ] Write tests for `PreviousMonth` / `NextMonth` — updates date and reloads
- [ ] Write tests for computed: `OverallStatus`, `ModalTitle`, `IsCreatingNewGoal`
- [ ] Write tests for `SaveGoal` — create and update paths
- [ ] Write tests for `DeleteGoal` — success and failure
- [ ] Write tests for `ShowEditGoal` / `HideEditGoal` — state flag toggles
- [ ] Write tests for `NavigateToCategories` — delegates to NavigationManager
- [ ] Write tests for `Dispose` — unsubscribes from scope change events
- [ ] Write tests for `OnStateChanged` callback — invoked after state mutations
- [ ] Implement ViewModel to pass all tests
- [ ] Verify ≥ 90% coverage on BudgetViewModel

### Phase 2: Refactor Budget.razor to Use ViewModel

**Tasks:**
- [ ] Register `BudgetViewModel` as transient in DI
- [ ] Update `Budget.razor` to inject `BudgetViewModel`
- [ ] Replace all state fields with `ViewModel.PropertyName` bindings
- [ ] Replace all event handlers with `ViewModel.MethodAsync` calls
- [ ] Wire `OnStateChanged` callback in `OnInitializedAsync`
- [ ] Remove `@code` block (except thin `OnInitializedAsync` / `Dispose` wiring)
- [ ] Verify all 21 existing `BudgetPageTests` pass
- [ ] Verify application runs correctly (manual smoke test)

### Phase 3: Verify Coverage & Finalize

**Tasks:**
- [ ] Run coverage report for BudgetViewModel — verify ≥ 90%
- [ ] Update this document status to Done

---

## Testing Strategy

### Unit Tests (BudgetViewModelTests)

Tests use `StubBudgetApiService` (already exists) and stub/mock `NavigationManager` and `ScopeService`.

**Test categories:**
- **Initialization:** Loads budget on init, handles API failure gracefully
- **Month navigation:** Previous/next month updates date and reloads budget data
- **Goal management:** Save goal (create new / update existing), delete goal
- **Computed properties:** Overall status logic, modal title, new goal detection
- **State transitions:** Loading → loaded, submitting → done, error → dismissed
- **Scope change:** Reloads data when scope changes
- **Navigation:** Navigate to categories page
- **Dispose:** Cleans up scope change subscription
- **StateHasChanged:** Callback invoked after each state mutation

### Integration Tests (Existing bUnit)

The 21 existing `BudgetPageTests` serve as integration tests verifying the Razor → ViewModel → DOM pipeline.

---

## References

- [Feature 097: ViewModel Extraction — Categories (Prototype)](./097-viewmodel-extraction-categories.md) — Established pattern
- [Component Standards — Section 11: ViewModel Pattern](./COMPONENT-STANDARDS.md) — Pattern documentation
