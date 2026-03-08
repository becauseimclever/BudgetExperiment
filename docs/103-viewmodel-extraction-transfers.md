# Feature 103: ViewModel Extraction — Transfers Page

> **Status:** Planning
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
- [ ] `TransfersViewModel` class exists in `src/BudgetExperiment.Client/ViewModels/TransfersViewModel.cs`
- [ ] All 14 handler methods are moved to the ViewModel
- [ ] All 13 state fields are moved to the ViewModel
- [ ] ViewModel exposes `Action? OnStateChanged` callback for re-rendering
- [ ] `Transfers.razor` delegates all logic to the injected ViewModel
- [ ] ViewModel receives services via constructor injection
- [ ] All existing 19 `TransfersPageTests` pass without modification (or with minimal binding changes)

### US-103-002: Add ViewModel Unit Tests

**As a** developer
**I want to** test TransfersViewModel directly with xUnit
**So that** all handler logic has comprehensive and accurately reported coverage.

**Acceptance Criteria:**
- [ ] `TransfersViewModelTests.cs` exists in `tests/BudgetExperiment.Client.Tests/ViewModels/`
- [ ] Tests cover all 14 handler methods
- [ ] Tests verify state transitions (loading, error, success)
- [ ] Tests verify CRUD operations (create, update, delete)
- [ ] Tests verify filtering (apply filters, clear filters)
- [ ] Tests verify error handling (API failures)
- [ ] Tests verify scope change triggers reload and dispose cleans up
- [ ] Coverage for TransfersViewModel ≥ 90%

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
- [ ] Create `src/BudgetExperiment.Client/ViewModels/TransfersViewModel.cs`
- [ ] Create `tests/BudgetExperiment.Client.Tests/ViewModels/TransfersViewModelTests.cs`
- [ ] Write tests for `InitializeAsync` — loads accounts and transfers, subscribes to scope changes, sets chat context
- [ ] Write tests for `LoadDataAsync` — success, failure, loading state transitions
- [ ] Write tests for `RetryLoadAsync` — sets retrying flag, reloads
- [ ] Write tests for `DismissError` — clears error message
- [ ] Write tests for filtering: `ApplyFilters` (by account, date range), `ClearFilters`
- [ ] Write tests for CRUD: `CreateTransfer`, `UpdateTransfer`, `DeleteTransfer` — success, failure
- [ ] Write tests for dialog toggles: `ShowCreateTransfer`/`ShowEditTransfer`/`HideTransferDialog`
- [ ] Write tests for `Dispose` — unsubscribes from scope change events, clears chat context
- [ ] Write tests for `OnStateChanged` callback — invoked after state mutations
- [ ] Implement ViewModel to pass all tests
- [ ] Verify ≥ 90% coverage on TransfersViewModel

### Phase 2: Refactor Transfers.razor to Use ViewModel

**Tasks:**
- [ ] Register `TransfersViewModel` as transient in DI
- [ ] Update `Transfers.razor` to inject `TransfersViewModel`
- [ ] Replace all state fields with `ViewModel.PropertyName` bindings
- [ ] Replace all event handlers with `ViewModel.MethodAsync` calls
- [ ] Wire `OnStateChanged` callback in `OnInitializedAsync`
- [ ] Remove `@code` block (except thin `OnInitializedAsync` / `Dispose` wiring)
- [ ] Verify all 19 existing `TransfersPageTests` pass
- [ ] Verify application runs correctly (manual smoke test)

### Phase 3: Verify Coverage & Finalize

**Tasks:**
- [ ] Run coverage report for TransfersViewModel — verify ≥ 90%
- [ ] Update this document status to Done

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
