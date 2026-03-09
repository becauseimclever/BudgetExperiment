# Feature 103: ViewModel Extraction — Transfers Page

> **Status:** Done
> **Priority:** Medium
> **Dependencies:** Feature 097 (Done — established pattern)

## Overview

Extract the handler logic from the Transfers page `@code` block into a testable `TransfersViewModel` class, following the ViewModel/Presenter pattern established in Feature 097 (Categories). The Transfers page manages inter-account transfer listing with filtering, CRUD, and date-range queries.

## Problem Statement

### Current State

The Transfers page ([Transfers.razor](../src/BudgetExperiment.Client/Pages/Transfers.razor)) contains **316 lines** with the `@code` block starting at line 126. All handler logic lives inline in the Razor file.

**Structural issues:**
- **14 handler methods** embedded in `@code` block: `OnInitializedAsync`, `OnScopeChanged`, `Dispose`, `LoadDataAsync`, `RetryLoad`, `DismissError`, `ApplyFilters`, `ClearFilters`, `ShowCreateTransfer`, `ShowEditTransfer`, `HideTransferDialog`, `CreateTransfer`, `UpdateTransfer`, `DeleteTransfer`
- **13 state fields** managing loading, filtering, forms, and editing state
- **0 computed properties**
- **4 injected services**: `IBudgetApiService`, `NavigationManager`, `ScopeService`, `IChatContextService`
- Note: `NavigationManager` is injected but unused

**Coverage instrumentation gap:** Same async state-machine issue as Categories — `@code` block handlers report near-0% coverage despite being exercised by bUnit tests.

### Target State

- `TransfersViewModel` is a plain C# class in `BudgetExperiment.Client/ViewModels/`
- All handler methods and state fields live in the ViewModel
- `Transfers.razor` is a thin binding layer
- ViewModel is directly testable with xUnit + Shouldly
- Coverage reports accurately for all handler methods
- Existing 19 `TransfersPageTests` continue to pass

---

## User Stories

### US-103-001: Extract TransfersViewModel

**As a** developer
**I want to** extract handler logic from Transfers.razor into a TransfersViewModel class
**So that** page business logic is testable with plain xUnit and coverage reports accurately.

**Acceptance Criteria:**
- [x] `TransfersViewModel` class exists in `src/BudgetExperiment.Client/ViewModels/TransfersViewModel.cs`
- [x] All 14 handler methods are moved to the ViewModel
- [x] All 13 state fields are moved to the ViewModel
- [x] ViewModel exposes `Action? OnStateChanged` callback for re-rendering
- [x] `Transfers.razor` delegates all logic to the injected ViewModel
- [x] ViewModel receives services via constructor injection
- [x] All existing 19 `TransfersPageTests` pass without modification (or with minimal binding changes)

### US-103-002: Add ViewModel Unit Tests

**As a** developer
**I want to** test TransfersViewModel directly with xUnit
**So that** all handler logic has comprehensive and accurately reported coverage.

**Acceptance Criteria:**
- [x] `TransfersViewModelTests.cs` exists in `tests/BudgetExperiment.Client.Tests/ViewModels/`
- [x] Tests cover all 14 handler methods
- [x] Tests verify state transitions (loading, error, success)
- [x] Tests verify CRUD operations (create, update, delete)
- [x] Tests verify filtering (apply filters, clear filters)
- [x] Tests verify error handling (API failures)
- [x] Tests verify scope change triggers reload and dispose cleans up
- [x] Coverage for TransfersViewModel ≥ 90%

---

## Technical Design

### Architecture

```
Before:
  Transfers.razor
    └── @code { 14 handlers, 13 fields, 4 services }

After:
  Transfers.razor
    └── @inject TransfersViewModel ViewModel
    └── binds UI to ViewModel.Property / ViewModel.MethodAsync()

  TransfersViewModel (plain C# class)
    └── constructor(IBudgetApiService, NavigationManager, ScopeService, IChatContextService)
    └── 14 public methods
    └── 13 public properties (state)
    └── Action? OnStateChanged (for Razor re-render callback)
```

### TransfersViewModel Design

```csharp
public sealed class TransfersViewModel : IDisposable
{
    // Constructor injection
    // IBudgetApiService, NavigationManager, ScopeService, IChatContextService

    // State properties (13)
    public bool IsLoading { get; private set; }
    public bool IsRetrying { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<AccountDto> Accounts { get; private set; }
    public List<TransferListItemResponse> Transfers { get; private set; }
    public List<TransferListItemResponse> FilteredTransfers { get; private set; }
    public string SelectedAccountId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool ShowTransferDialog { get; private set; }
    public CreateTransferRequest NewTransfer { get; set; }
    public UpdateTransferRequest EditTransfer { get; set; }
    public TransferListItemResponse? EditingTransfer { get; private set; }

    // Re-render callback
    public Action? OnStateChanged { get; set; }

    // All 14 handler methods (public)
}
```

### DI Registration

```csharp
builder.Services.AddTransient<TransfersViewModel>();
```

---

## Implementation Plan

### Phase 1: Create TransfersViewModel with Tests (TDD)

**Tasks:**
- [x] Create `src/BudgetExperiment.Client/ViewModels/TransfersViewModel.cs`
- [x] Create `tests/BudgetExperiment.Client.Tests/ViewModels/TransfersViewModelTests.cs`
- [x] Write tests for `InitializeAsync` — loads accounts and transfers, subscribes to scope changes, sets chat context
- [x] Write tests for `LoadDataAsync` — success, failure, loading state transitions
- [x] Write tests for `RetryLoadAsync` — sets retrying flag, reloads
- [x] Write tests for `DismissError` — clears error message
- [x] Write tests for filtering: `ApplyFilters` (by account, date range), `ClearFilters`
- [x] Write tests for CRUD: `CreateTransfer`, `UpdateTransfer`, `DeleteTransfer` — success, failure
- [x] Write tests for dialog toggles: `ShowCreateTransfer`/`ShowEditTransfer`/`HideTransferDialog`
- [x] Write tests for `Dispose` — unsubscribes from scope change events, clears chat context
- [x] Write tests for `OnStateChanged` callback — invoked after state mutations
- [x] Implement ViewModel to pass all tests
- [x] Verify ≥ 90% coverage on TransfersViewModel

### Phase 2: Refactor Transfers.razor to Use ViewModel

**Tasks:**
- [x] Register `TransfersViewModel` as transient in DI
- [x] Update `Transfers.razor` to inject `TransfersViewModel`
- [x] Replace all state fields with `ViewModel.PropertyName` bindings
- [x] Replace all event handlers with `ViewModel.MethodAsync` calls
- [x] Wire `OnStateChanged` callback in `OnInitializedAsync`
- [x] Remove `@code` block (except thin `OnInitializedAsync` / `Dispose` wiring)
- [x] Verify all 19 existing `TransfersPageTests` pass
- [x] Verify application runs correctly (manual smoke test)

### Phase 3: Verify Coverage & Finalize

**Tasks:**
- [x] Run coverage report for TransfersViewModel — verify ≥ 90%
- [x] Update this document status to Done

---

## Testing Strategy

### Unit Tests (TransfersViewModelTests)

Tests use `StubBudgetApiService` (already exists) and stub/mock `NavigationManager`, `ScopeService`, and `IChatContextService`.

**Test categories:**
- **Initialization:** Loads accounts and transfers, sets chat context, handles API failure
- **CRUD:** Create/Update/Delete with success and failure paths
- **Filtering:** Apply filters by account ID and date range, clear filters resets state
- **State transitions:** Loading → loaded, error → dismissed
- **Scope change:** Reloads data when scope changes
- **Dispose:** Cleans up scope change subscription, clears chat context
- **StateHasChanged:** Callback invoked after each state mutation

### Integration Tests (Existing bUnit)

The 19 existing `TransfersPageTests` serve as integration tests verifying the Razor → ViewModel → DOM pipeline.

---

## References

- [Feature 097: ViewModel Extraction — Categories (Prototype)](./097-viewmodel-extraction-categories.md) — Established pattern
- [Component Standards — Section 11: ViewModel Pattern](./COMPONENT-STANDARDS.md) — Pattern documentation
