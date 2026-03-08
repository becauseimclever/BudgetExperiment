# Feature 101: ViewModel Extraction — Recurring Transactions Page

> **Status:** Planning
> **Priority:** Medium
> **Dependencies:** Feature 097 (Done — established pattern)

## Overview

Extract the handler logic from the Recurring Transactions page `@code` block into a testable `RecurringViewModel` class, following the ViewModel/Presenter pattern established in Feature 097 (Categories). The Recurring page has 23 handlers, 18 state fields, and manages complex recurring transaction workflows including skip, pause, resume, and import patterns.

## Problem Statement

### Current State

The Recurring page ([Recurring.razor](../src/BudgetExperiment.Client/Pages/Recurring.razor)) contains **441 lines** with the `@code` block starting at line 150. All handler logic lives inline in the Razor file.

**Structural issues:**
- **23 handler methods** embedded in `@code` block: `OnInitializedAsync`, `OnScopeChanged`, `Dispose`, `LoadCategories`, `LoadRecurringTransactions`, `RetryLoad`, `DismissError`, `ShowAddForm`, `ShowEditForm`, `HideForm`, `CreateRecurring`, `UpdateRecurring`, `SkipNext`, `Pause`, `Resume`, `ShowDeleteConfirm`, `ConfirmDelete`, `CancelDelete`, `ShowImportPatterns`, `HideImportPatterns`, `HandleImportPatternsSaved`, `FormatFrequency` (static), `FormatMoney` (static)
- **18 state fields** managing loading, forms, editing, deletion, and import patterns state
- **0 computed properties**
- **5 injected services**: `IBudgetApiService`, `IToastService`, `NavigationManager`, `ScopeService`, `IChatContextService`

**Coverage instrumentation gap:** Same async state-machine issue as Categories — `@code` block handlers report near-0% coverage despite being exercised by bUnit tests.

### Target State

- `RecurringViewModel` is a plain C# class in `BudgetExperiment.Client/ViewModels/`
- All handler methods and state fields live in the ViewModel
- `Recurring.razor` is a thin binding layer
- ViewModel is directly testable with xUnit + Shouldly
- Coverage reports accurately for all handler methods
- Existing 20 `RecurringPageTests` continue to pass

---

## User Stories

### US-101-001: Extract RecurringViewModel

**As a** developer
**I want to** extract handler logic from Recurring.razor into a RecurringViewModel class
**So that** page business logic is testable with plain xUnit and coverage reports accurately.

**Acceptance Criteria:**
- [ ] `RecurringViewModel` class exists in `src/BudgetExperiment.Client/ViewModels/RecurringViewModel.cs`
- [ ] All 23 handler methods are moved to the ViewModel (static helpers may stay or move)
- [ ] All 18 state fields are moved to the ViewModel
- [ ] ViewModel exposes `Action? OnStateChanged` callback for re-rendering
- [ ] `Recurring.razor` delegates all logic to the injected ViewModel
- [ ] ViewModel receives services via constructor injection
- [ ] All existing 20 `RecurringPageTests` pass without modification (or with minimal binding changes)

### US-101-002: Add ViewModel Unit Tests

**As a** developer
**I want to** test RecurringViewModel directly with xUnit
**So that** all handler logic has comprehensive and accurately reported coverage.

**Acceptance Criteria:**
- [ ] `RecurringViewModelTests.cs` exists in `tests/BudgetExperiment.Client.Tests/ViewModels/`
- [ ] Tests cover all handler methods
- [ ] Tests verify state transitions (loading, error, success)
- [ ] Tests verify CRUD operations (create, update, delete)
- [ ] Tests verify lifecycle actions (skip next, pause, resume)
- [ ] Tests verify import patterns workflow
- [ ] Tests verify error handling (API failures, conflict detection)
- [ ] Tests verify scope change triggers reload and dispose cleans up
- [ ] Coverage for RecurringViewModel ≥ 90%

---

## Technical Design

### Architecture

```
Before:
  Recurring.razor
    └── @code { 23 handlers, 18 fields, 5 services }

After:
  Recurring.razor
    └── @inject RecurringViewModel ViewModel
    └── binds UI to ViewModel.Property / ViewModel.MethodAsync()

  RecurringViewModel (plain C# class)
    └── constructor(IBudgetApiService, IToastService, NavigationManager, ScopeService, IChatContextService)
    └── 23 public methods
    └── 18 public properties (state)
    └── Action? OnStateChanged (for Razor re-render callback)
```

### RecurringViewModel Design

```csharp
public sealed class RecurringViewModel : IDisposable
{
    // Constructor injection
    // IBudgetApiService, IToastService, NavigationManager, ScopeService, IChatContextService

    // State properties (18)
    public bool IsLoading { get; private set; }
    public bool IsRetrying { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool IsDeleting { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<RecurringTransactionDto> RecurringTransactions { get; private set; }
    public List<BudgetCategoryDto> Categories { get; private set; }
    public bool ShowAddForm { get; private set; }
    public bool ShowEditForm { get; private set; }
    public bool ShowDeleteConfirm { get; private set; }
    public bool ShowImportPatterns { get; private set; }
    public RecurringTransactionCreateDto NewRecurring { get; set; }
    public RecurringTransactionUpdateDto EditModel { get; set; }
    public Guid EditingId { get; private set; }
    public string? EditingVersion { get; private set; }
    public RecurringTransactionDto? DeletingRecurring { get; private set; }
    public Guid ImportPatternsRecurringId { get; private set; }
    public string ImportPatternsDescription { get; private set; }

    // Re-render callback
    public Action? OnStateChanged { get; set; }

    // All 23 handler methods (public)
    // Static helpers: FormatFrequency, FormatMoney
}
```

### DI Registration

```csharp
builder.Services.AddTransient<RecurringViewModel>();
```

---

## Implementation Plan

### Phase 1: Create RecurringViewModel with Tests (TDD)

**Tasks:**
- [ ] Create `src/BudgetExperiment.Client/ViewModels/RecurringViewModel.cs`
- [ ] Create `tests/BudgetExperiment.Client.Tests/ViewModels/RecurringViewModelTests.cs`
- [ ] Write tests for `InitializeAsync` — loads categories and recurring transactions, subscribes to scope changes
- [ ] Write tests for `LoadRecurringTransactionsAsync` / `LoadCategoriesAsync` — success, failure, loading state
- [ ] Write tests for `RetryLoadAsync` — sets retrying flag, reloads
- [ ] Write tests for `DismissError` — clears error message
- [ ] Write tests for CRUD: `CreateRecurring`, `UpdateRecurring`, `ConfirmDelete` — success, failure, conflict
- [ ] Write tests for lifecycle: `SkipNext`, `Pause`, `Resume` — success, failure
- [ ] Write tests for import patterns: `ShowImportPatterns`, `HideImportPatterns`, `HandleImportPatternsSaved`
- [ ] Write tests for form toggles: `ShowAddForm`/`ShowEditForm`/`HideForm`, `ShowDeleteConfirm`/`CancelDelete`
- [ ] Write tests for static helpers: `FormatFrequency`, `FormatMoney`
- [ ] Write tests for `Dispose` — unsubscribes from scope change events, clears chat context
- [ ] Write tests for `OnStateChanged` callback — invoked after state mutations
- [ ] Implement ViewModel to pass all tests
- [ ] Verify ≥ 90% coverage on RecurringViewModel

### Phase 2: Refactor Recurring.razor to Use ViewModel

**Tasks:**
- [ ] Register `RecurringViewModel` as transient in DI
- [ ] Update `Recurring.razor` to inject `RecurringViewModel`
- [ ] Replace all state fields with `ViewModel.PropertyName` bindings
- [ ] Replace all event handlers with `ViewModel.MethodAsync` calls
- [ ] Wire `OnStateChanged` callback in `OnInitializedAsync`
- [ ] Remove `@code` block (except thin `OnInitializedAsync` / `Dispose` wiring)
- [ ] Verify all 20 existing `RecurringPageTests` pass
- [ ] Verify application runs correctly (manual smoke test)

### Phase 3: Verify Coverage & Finalize

**Tasks:**
- [ ] Run coverage report for RecurringViewModel — verify ≥ 90%
- [ ] Update this document status to Done

---

## Testing Strategy

### Unit Tests (RecurringViewModelTests)

Tests use `StubBudgetApiService` (already exists) and stub/mock `IToastService`, `ScopeService`, `NavigationManager`, and `IChatContextService`.

**Test categories:**
- **Initialization:** Loads categories and recurring transactions, handles API failure
- **CRUD:** Create/Update/Delete with success, failure, and conflict paths
- **Lifecycle actions:** Skip next, pause, resume with success and failure
- **Import patterns:** Show/hide import patterns, handle save
- **State transitions:** Loading → loaded, submitting → done, error → dismissed
- **Static helpers:** FormatFrequency, FormatMoney produce correct output
- **Scope change:** Reloads data when scope changes
- **Dispose:** Cleans up scope change subscription, clears chat context
- **StateHasChanged:** Callback invoked after each state mutation

### Integration Tests (Existing bUnit)

The 20 existing `RecurringPageTests` serve as integration tests verifying the Razor → ViewModel → DOM pipeline.

---

## References

- [Feature 097: ViewModel Extraction — Categories (Prototype)](./097-viewmodel-extraction-categories.md) — Established pattern
- [Component Standards — Section 11: ViewModel Pattern](./COMPONENT-STANDARDS.md) — Pattern documentation
