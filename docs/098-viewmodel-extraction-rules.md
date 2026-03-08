# Feature 098: ViewModel Extraction — Rules Page

> **Status:** Planning
> **Priority:** High
> **Dependencies:** Feature 097 (Done — established pattern)

## Overview

Extract the handler logic from the Rules page `@code` block into a testable `RulesViewModel` class, following the ViewModel/Presenter pattern established in Feature 097 (Categories). The Rules page is the largest candidate with the most handlers and lowest coverage, making it the highest-priority extraction target.

## Problem Statement

### Current State

The Rules page ([Rules.razor](../src/BudgetExperiment.Client/Pages/Rules.razor)) contains **434 lines** with the `@code` block starting at line 99. All handler logic lives inline in the Razor file.

**Structural issues:**
- **23 handler methods** embedded in `@code` block: `NavigateToAiSuggestions`, `OnInitializedAsync`, `Dispose`, `OnScopeChanged`, `LoadDataAsync`, `RetryLoad`, `DismissError`, `ShowAddRule`, `HideAddRule`, `CreateRule`, `ShowEditRule`, `HideEditRule`, `UpdateRule`, `ConfirmDeleteRule`, `CancelDelete`, `DeleteRule`, `ActivateRule`, `DeactivateRule`, `TestPattern`, `ShowApplyRules`, `HideApplyRules`, `OnRulesApplied`, `CreateRuleFromTest`
- **17 state fields** managing loading, forms, editing, testing, and deletion state
- **0 computed properties**
- **4 injected services**: `IBudgetApiService`, `IToastService`, `NavigationManager`, `ScopeService`

**Coverage instrumentation gap:** Same async state-machine issue as Categories — `@code` block handlers report near-0% coverage despite being exercised by bUnit tests.

### Target State

- `RulesViewModel` is a plain C# class in `BudgetExperiment.Client/ViewModels/`
- All handler methods, state fields live in the ViewModel
- `Rules.razor` is a thin binding layer
- ViewModel is directly testable with xUnit + Shouldly
- Coverage reports accurately for all handler methods
- Existing 20 `RulesPageTests` continue to pass

---

## User Stories

### US-098-001: Extract RulesViewModel

**As a** developer
**I want to** extract handler logic from Rules.razor into a RulesViewModel class
**So that** page business logic is testable with plain xUnit and coverage reports accurately.

**Acceptance Criteria:**
- [ ] `RulesViewModel` class exists in `src/BudgetExperiment.Client/ViewModels/RulesViewModel.cs`
- [ ] All 23 handler methods are moved to the ViewModel
- [ ] All 17 state fields are moved to the ViewModel
- [ ] ViewModel exposes `Action? OnStateChanged` callback for re-rendering
- [ ] `Rules.razor` delegates all logic to the injected ViewModel
- [ ] ViewModel receives services via constructor injection
- [ ] All existing 20 `RulesPageTests` pass without modification (or with minimal binding changes)

### US-098-002: Add ViewModel Unit Tests

**As a** developer
**I want to** test RulesViewModel directly with xUnit
**So that** all handler logic has comprehensive and accurately reported coverage.

**Acceptance Criteria:**
- [ ] `RulesViewModelTests.cs` exists in `tests/BudgetExperiment.Client.Tests/ViewModels/`
- [ ] Tests cover all 23 handler methods
- [ ] Tests verify state transitions (loading, error, success)
- [ ] Tests verify CRUD operations call correct API service methods
- [ ] Tests verify error handling (API failures, conflict detection on update)
- [ ] Tests verify pattern testing functionality
- [ ] Tests verify apply rules workflow
- [ ] Tests verify scope change triggers reload and dispose cleans up
- [ ] Coverage for RulesViewModel ≥ 90%

---

## Technical Design

### Architecture

```
Before:
  Rules.razor
    └── @code { 23 handlers, 17 fields, 4 services }

After:
  Rules.razor
    └── @inject RulesViewModel ViewModel
    └── binds UI to ViewModel.Property / ViewModel.MethodAsync()

  RulesViewModel (plain C# class)
    └── constructor(IBudgetApiService, IToastService, NavigationManager, ScopeService)
    └── 23 public methods
    └── 17 public properties (state)
    └── Action? OnStateChanged (for Razor re-render callback)
```

### RulesViewModel Design

```csharp
public sealed class RulesViewModel : IDisposable
{
    // Constructor injection
    // IBudgetApiService, IToastService, NavigationManager, ScopeService

    // State properties (17)
    public bool IsLoading { get; private set; }
    public bool IsRetrying { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool IsDeleting { get; private set; }
    public bool IsTesting { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<CategorizationRuleDto> Rules { get; private set; }
    public List<BudgetCategoryDto> Categories { get; private set; }
    public bool ShowAddForm { get; private set; }
    public CategorizationRuleCreateDto NewRule { get; set; }
    public TestPatternResponse? TestResult { get; private set; }
    public bool ShowEditForm { get; private set; }
    public CategorizationRuleCreateDto EditRule { get; set; }
    public Guid? EditingRuleId { get; private set; }
    public string? EditingVersion { get; private set; }
    public bool ShowDeleteConfirm { get; private set; }
    public CategorizationRuleDto? DeletingRule { get; private set; }
    public bool ShowApplyRules { get; private set; }

    // Re-render callback
    public Action? OnStateChanged { get; set; }

    // All 23 handler methods (public)
}
```

### DI Registration

```csharp
builder.Services.AddTransient<RulesViewModel>();
```

---

## Implementation Plan

### Phase 1: Create RulesViewModel with Tests (TDD)

**Tasks:**
- [ ] Create `src/BudgetExperiment.Client/ViewModels/RulesViewModel.cs`
- [ ] Create `tests/BudgetExperiment.Client.Tests/ViewModels/RulesViewModelTests.cs`
- [ ] Write tests for `InitializeAsync` — loads rules and categories, subscribes to scope changes
- [ ] Write tests for `LoadDataAsync` — success, failure, loading state transitions
- [ ] Write tests for `RetryLoadAsync` — sets retrying flag, reloads
- [ ] Write tests for `DismissError` — clears error message
- [ ] Write tests for CRUD: `CreateRule`, `UpdateRule`, `DeleteRule` — success, failure, conflict
- [ ] Write tests for `ActivateRule` / `DeactivateRule` — success, failure
- [ ] Write tests for `TestPattern` — success, failure, testing state
- [ ] Write tests for `ShowApplyRules` / `HideApplyRules` / `OnRulesApplied`
- [ ] Write tests for `CreateRuleFromTest` — creates rule from test pattern results
- [ ] Write tests for `NavigateToAiSuggestions` — delegates to NavigationManager
- [ ] Write tests for form toggles: `ShowAddRule`/`HideAddRule`, `ShowEditRule`/`HideEditRule`, `ConfirmDeleteRule`/`CancelDelete`
- [ ] Write tests for `Dispose` — unsubscribes from scope change events
- [ ] Write tests for `OnStateChanged` callback — invoked after state mutations
- [ ] Implement ViewModel to pass all tests
- [ ] Verify ≥ 90% coverage on RulesViewModel

### Phase 2: Refactor Rules.razor to Use ViewModel

**Tasks:**
- [ ] Register `RulesViewModel` as transient in DI
- [ ] Update `Rules.razor` to inject `RulesViewModel`
- [ ] Replace all state fields with `ViewModel.PropertyName` bindings
- [ ] Replace all event handlers with `ViewModel.MethodAsync` calls
- [ ] Wire `OnStateChanged` callback in `OnInitializedAsync`
- [ ] Remove `@code` block (except thin `OnInitializedAsync` / `Dispose` wiring)
- [ ] Verify all 20 existing `RulesPageTests` pass
- [ ] Verify application runs correctly (manual smoke test)

### Phase 3: Verify Coverage & Finalize

**Tasks:**
- [ ] Run coverage report for RulesViewModel — verify ≥ 90%
- [ ] Update this document status to Done

---

## Testing Strategy

### Unit Tests (RulesViewModelTests)

Tests use `StubBudgetApiService` (already exists) and stub/mock `IToastService`, `ScopeService`, and `NavigationManager`.

**Test categories:**
- **Initialization:** Loads rules and categories on init, handles API failure gracefully
- **CRUD:** Create/Update/Delete with success, failure, and conflict paths
- **Pattern testing:** Test pattern with success and failure
- **Apply rules:** Show/hide apply rules dialog, handle response
- **State transitions:** Loading → loaded, submitting → done, error → dismissed
- **Scope change:** Reloads data when scope changes
- **Navigation:** Navigate to AI suggestions
- **Dispose:** Cleans up scope change subscription
- **StateHasChanged:** Callback invoked after each state mutation

### Integration Tests (Existing bUnit)

The 20 existing `RulesPageTests` serve as integration tests verifying the Razor → ViewModel → DOM pipeline.

---

## References

- [Feature 097: ViewModel Extraction — Categories (Prototype)](./097-viewmodel-extraction-categories.md) — Established pattern
- [Component Standards — Section 11: ViewModel Pattern](./COMPONENT-STANDARDS.md) — Pattern documentation
