# Feature 102: ViewModel Extraction — Recurring Transfers Page

> **Status:** Planning
> **Priority:** Medium
> **Dependencies:** Feature 097 (Done — established pattern)

## Overview

Extract the handler logic from the Recurring Transfers page `@code` block into a testable `RecurringTransfersViewModel` class, following the ViewModel/Presenter pattern established in Feature 097 (Categories). The page manages recurring inter-account transfers with CRUD, skip/pause/resume lifecycle actions.

## Problem Statement

### Current State

The Recurring Transfers page ([RecurringTransfers.razor](../src/BudgetExperiment.Client/Pages/RecurringTransfers.razor)) contains **~338 lines** with the `@code` block starting at line 147. All handler logic lives inline in the Razor file.

**Structural issues:**
- **19 handler methods** embedded in `@code` block: `OnInitializedAsync`, `OnScopeChanged`, `Dispose`, `LoadRecurringTransfers`, `RetryLoad`, `DismissError`, `ShowAddForm`, `ShowEditForm`, `HideForm`, `CreateRecurring`, `UpdateRecurring`, `SkipNext`, `Pause`, `Resume`, `ShowDeleteConfirm`, `ConfirmDelete`, `CancelDelete`, `FormatFrequency` (static), `FormatMoney` (static)
- **14 state fields** managing loading, forms, editing, and deletion state
- **0 computed properties**
- **4 injected services**: `IBudgetApiService`, `IToastService`, `NavigationManager`, `ScopeService`

**Coverage instrumentation gap:** Same async state-machine issue as Categories — `@code` block handlers report near-0% coverage despite being exercised by bUnit tests.

### Target State

- `RecurringTransfersViewModel` is a plain C# class in `BudgetExperiment.Client/ViewModels/`
- All handler methods and state fields live in the ViewModel
- `RecurringTransfers.razor` is a thin binding layer
- ViewModel is directly testable with xUnit + Shouldly
- Coverage reports accurately for all handler methods
- Existing 21 `RecurringTransfersPageTests` continue to pass

---

## User Stories

### US-102-001: Extract RecurringTransfersViewModel

**As a** developer
**I want to** extract handler logic from RecurringTransfers.razor into a RecurringTransfersViewModel class
**So that** page business logic is testable with plain xUnit and coverage reports accurately.

**Acceptance Criteria:**
- [ ] `RecurringTransfersViewModel` class exists in `src/BudgetExperiment.Client/ViewModels/RecurringTransfersViewModel.cs`
- [ ] All 19 handler methods are moved to the ViewModel (static helpers may stay or move)
- [ ] All 14 state fields are moved to the ViewModel
- [ ] ViewModel exposes `Action? OnStateChanged` callback for re-rendering
- [ ] `RecurringTransfers.razor` delegates all logic to the injected ViewModel
- [ ] ViewModel receives services via constructor injection
- [ ] All existing 21 `RecurringTransfersPageTests` pass without modification (or with minimal binding changes)

### US-102-002: Add ViewModel Unit Tests

**As a** developer
**I want to** test RecurringTransfersViewModel directly with xUnit
**So that** all handler logic has comprehensive and accurately reported coverage.

**Acceptance Criteria:**
- [ ] `RecurringTransfersViewModelTests.cs` exists in `tests/BudgetExperiment.Client.Tests/ViewModels/`
- [ ] Tests cover all handler methods
- [ ] Tests verify state transitions (loading, error, success)
- [ ] Tests verify CRUD operations (create, update, delete)
- [ ] Tests verify lifecycle actions (skip next, pause, resume)
- [ ] Tests verify error handling (API failures, conflict detection)
- [ ] Tests verify scope change triggers reload and dispose cleans up
- [ ] Coverage for RecurringTransfersViewModel ≥ 90%

---

## Technical Design

### Architecture

```
Before:
  RecurringTransfers.razor
    └── @code { 19 handlers, 14 fields, 4 services }

After:
  RecurringTransfers.razor
    └── @inject RecurringTransfersViewModel ViewModel
    └── binds UI to ViewModel.Property / ViewModel.MethodAsync()

  RecurringTransfersViewModel (plain C# class)
    └── constructor(IBudgetApiService, IToastService, NavigationManager, ScopeService)
    └── 19 public methods
    └── 14 public properties (state)
    └── Action? OnStateChanged (for Razor re-render callback)
```

### RecurringTransfersViewModel Design

```csharp
public sealed class RecurringTransfersViewModel : IDisposable
{
    // Constructor injection
    // IBudgetApiService, IToastService, NavigationManager, ScopeService

    // State properties (14)
    public bool IsLoading { get; private set; }
    public bool IsRetrying { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool IsDeleting { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<RecurringTransferDto> RecurringTransfers { get; private set; }
    public bool ShowAddForm { get; private set; }
    public bool ShowEditForm { get; private set; }
    public bool ShowDeleteConfirm { get; private set; }
    public RecurringTransferCreateDto NewRecurring { get; set; }
    public RecurringTransferUpdateDto EditModel { get; set; }
    public Guid EditingId { get; private set; }
    public string? EditingVersion { get; private set; }
    public RecurringTransferDto? DeletingTransfer { get; private set; }

    // Re-render callback
    public Action? OnStateChanged { get; set; }

    // All 19 handler methods (public)
    // Static helpers: FormatFrequency, FormatMoney
}
```

### DI Registration

```csharp
builder.Services.AddTransient<RecurringTransfersViewModel>();
```

---

## Implementation Plan

### Phase 1: Create RecurringTransfersViewModel with Tests (TDD)

**Tasks:**
- [ ] Create `src/BudgetExperiment.Client/ViewModels/RecurringTransfersViewModel.cs`
- [ ] Create `tests/BudgetExperiment.Client.Tests/ViewModels/RecurringTransfersViewModelTests.cs`
- [ ] Write tests for `InitializeAsync` — loads recurring transfers, subscribes to scope changes
- [ ] Write tests for `LoadRecurringTransfersAsync` — success, failure, loading state transitions
- [ ] Write tests for `RetryLoadAsync` — sets retrying flag, reloads
- [ ] Write tests for `DismissError` — clears error message
- [ ] Write tests for CRUD: `CreateRecurring`, `UpdateRecurring`, `ConfirmDelete` — success, failure, conflict
- [ ] Write tests for lifecycle: `SkipNext`, `Pause`, `Resume` — success, failure
- [ ] Write tests for form toggles: `ShowAddForm`/`ShowEditForm`/`HideForm`, `ShowDeleteConfirm`/`CancelDelete`
- [ ] Write tests for static helpers: `FormatFrequency`, `FormatMoney`
- [ ] Write tests for `Dispose` — unsubscribes from scope change events
- [ ] Write tests for `OnStateChanged` callback — invoked after state mutations
- [ ] Implement ViewModel to pass all tests
- [ ] Verify ≥ 90% coverage on RecurringTransfersViewModel

### Phase 2: Refactor RecurringTransfers.razor to Use ViewModel

**Tasks:**
- [ ] Register `RecurringTransfersViewModel` as transient in DI
- [ ] Update `RecurringTransfers.razor` to inject `RecurringTransfersViewModel`
- [ ] Replace all state fields with `ViewModel.PropertyName` bindings
- [ ] Replace all event handlers with `ViewModel.MethodAsync` calls
- [ ] Wire `OnStateChanged` callback in `OnInitializedAsync`
- [ ] Remove `@code` block (except thin `OnInitializedAsync` / `Dispose` wiring)
- [ ] Verify all 21 existing `RecurringTransfersPageTests` pass
- [ ] Verify application runs correctly (manual smoke test)

### Phase 3: Verify Coverage & Finalize

**Tasks:**
- [ ] Run coverage report for RecurringTransfersViewModel — verify ≥ 90%
- [ ] Update this document status to Done

---

## Testing Strategy

### Unit Tests (RecurringTransfersViewModelTests)

Tests use `StubBudgetApiService` (already exists) and stub/mock `IToastService`, `ScopeService`, and `NavigationManager`.

**Test categories:**
- **Initialization:** Loads recurring transfers, handles API failure
- **CRUD:** Create/Update/Delete with success, failure, and conflict paths
- **Lifecycle actions:** Skip next, pause, resume with success and failure
- **State transitions:** Loading → loaded, submitting → done, error → dismissed
- **Static helpers:** FormatFrequency, FormatMoney produce correct output
- **Scope change:** Reloads data when scope changes
- **Dispose:** Cleans up scope change subscription
- **StateHasChanged:** Callback invoked after each state mutation

### Integration Tests (Existing bUnit)

The 21 existing `RecurringTransfersPageTests` serve as integration tests verifying the Razor → ViewModel → DOM pipeline.

---

## References

- [Feature 097: ViewModel Extraction — Categories (Prototype)](./097-viewmodel-extraction-categories.md) — Established pattern
- [Feature 101: ViewModel Extraction — Recurring Transactions](./101-viewmodel-extraction-recurring.md) — Similar page (transactions vs transfers)
- [Component Standards — Section 11: ViewModel Pattern](./COMPONENT-STANDARDS.md) — Pattern documentation
