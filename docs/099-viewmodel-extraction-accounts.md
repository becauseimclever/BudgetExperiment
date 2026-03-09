# Feature 099: ViewModel Extraction — Accounts Page

> **Status:** Done
> **Priority:** High
> **Dependencies:** Feature 097 (Done — established pattern)

## Overview

Extract the handler logic from the Accounts page `@code` block into a testable `AccountsViewModel` class, following the ViewModel/Presenter pattern established in Feature 097 (Categories). The Accounts page has 20 handlers and significant state complexity including transfer dialogs, making it a high-priority extraction target.

## Problem Statement

### Current State

The Accounts page ([Accounts.razor](../src/BudgetExperiment.Client/Pages/Accounts.razor)) contains **348 lines** with the `@code` block starting at line 107. All handler logic lives inline in the Razor file.

**Structural issues:**
- **20 handler methods** embedded in `@code` block: `OnInitializedAsync`, `OnScopeChanged`, `Dispose`, `LoadAccounts`, `RetryLoad`, `DismissError`, `ShowAddAccount`, `HideAddAccount`, `ShowEditAccount`, `HideEditAccount`, `UpdateAccount`, `ShowTransfer`, `ShowTransferFrom`, `HideTransfer`, `CreateTransfer`, `CreateAccount`, `ViewAccount`, `DeleteAccount`, `ConfirmDelete`, `CancelDelete`
- **17 state fields** managing loading, forms, editing, transfer, and deletion state
- **0 computed properties**
- **4 injected services**: `IBudgetApiService`, `IToastService`, `NavigationManager`, `ScopeService`

**Coverage instrumentation gap:** Same async state-machine issue as Categories — `@code` block handlers report near-0% coverage despite being exercised by bUnit tests.

### Target State

- `AccountsViewModel` is a plain C# class in `BudgetExperiment.Client/ViewModels/`
- All handler methods and state fields live in the ViewModel
- `Accounts.razor` is a thin binding layer
- ViewModel is directly testable with xUnit + Shouldly
- Coverage reports accurately for all handler methods
- Existing 20 `AccountsPageTests` continue to pass

---

## User Stories

### US-099-001: Extract AccountsViewModel

**As a** developer
**I want to** extract handler logic from Accounts.razor into an AccountsViewModel class
**So that** page business logic is testable with plain xUnit and coverage reports accurately.

**Acceptance Criteria:**
- [x] `AccountsViewModel` class exists in `src/BudgetExperiment.Client/ViewModels/AccountsViewModel.cs`
- [x] All 20 handler methods are moved to the ViewModel
- [x] All 17 state fields are moved to the ViewModel
- [x] ViewModel exposes `Action? OnStateChanged` callback for re-rendering
- [x] `Accounts.razor` delegates all logic to the injected ViewModel
- [x] ViewModel receives services via constructor injection
- [x] All existing 20 `AccountsPageTests` pass without modification (or with minimal binding changes)

### US-099-002: Add ViewModel Unit Tests

**As a** developer
**I want to** test AccountsViewModel directly with xUnit
**So that** all handler logic has comprehensive and accurately reported coverage.

**Acceptance Criteria:**
- [x] `AccountsViewModelTests.cs` exists in `tests/BudgetExperiment.Client.Tests/ViewModels/`
- [x] Tests cover all 20 handler methods
- [x] Tests verify state transitions (loading, error, success)
- [x] Tests verify CRUD operations call correct API service methods
- [x] Tests verify transfer creation workflow (show, create, hide)
- [x] Tests verify error handling (API failures, conflict detection on update)
- [x] Tests verify navigation to account details
- [x] Tests verify scope change triggers reload and dispose cleans up
- [x] Coverage for AccountsViewModel ≥ 90%

---

## Technical Design

### Architecture

```
Before:
  Accounts.razor
    └── @code { 20 handlers, 17 fields, 4 services }

After:
  Accounts.razor
    └── @inject AccountsViewModel ViewModel
    └── binds UI to ViewModel.Property / ViewModel.MethodAsync()

  AccountsViewModel (plain C# class)
    └── constructor(IBudgetApiService, IToastService, NavigationManager, ScopeService)
    └── 20 public methods
    └── 17 public properties (state)
    └── Action? OnStateChanged (for Razor re-render callback)
```

### AccountsViewModel Design

```csharp
public sealed class AccountsViewModel : IDisposable
{
    // Constructor injection
    // IBudgetApiService, IToastService, NavigationManager, ScopeService

    // State properties (17)
    public bool IsLoading { get; private set; }
    public bool IsRetrying { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool IsDeleting { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<AccountDto> Accounts { get; private set; }
    public bool ShowAddForm { get; private set; }
    public AccountCreateDto NewAccount { get; set; }
    public bool ShowEditForm { get; private set; }
    public AccountCreateDto EditAccount { get; set; }
    public Guid? EditingAccountId { get; private set; }
    public string? EditingVersion { get; private set; }
    public bool ShowTransferDialog { get; private set; }
    public CreateTransferRequest NewTransfer { get; set; }
    public Guid? PreSelectedSourceAccountId { get; private set; }
    public bool ShowDeleteConfirm { get; private set; }
    public AccountDto? DeletingAccount { get; private set; }

    // Re-render callback
    public Action? OnStateChanged { get; set; }

    // All 20 handler methods (public)
}
```

### DI Registration

```csharp
builder.Services.AddTransient<AccountsViewModel>();
```

---

## Implementation Plan

### Phase 1: Create AccountsViewModel with Tests (TDD)

**Tasks:**
- [x] Create `src/BudgetExperiment.Client/ViewModels/AccountsViewModel.cs`
- [x] Create `tests/BudgetExperiment.Client.Tests/ViewModels/AccountsViewModelTests.cs`
- [x] Write tests for `InitializeAsync` — loads accounts, subscribes to scope changes
- [x] Write tests for `LoadAccountsAsync` — success, failure, loading state transitions
- [x] Write tests for `RetryLoadAsync` — sets retrying flag, reloads
- [x] Write tests for `DismissError` — clears error message
- [x] Write tests for CRUD: `CreateAccount`, `UpdateAccount`, `ConfirmDelete` — success, failure, conflict
- [x] Write tests for transfer: `ShowTransfer`, `ShowTransferFrom`, `HideTransfer`, `CreateTransfer`
- [x] Write tests for navigation: `ViewAccount` — navigates to account details
- [x] Write tests for form toggles: `ShowAddAccount`/`HideAddAccount`, `ShowEditAccount`/`HideEditAccount`, `DeleteAccount`/`CancelDelete`
- [x] Write tests for `Dispose` — unsubscribes from scope change events
- [x] Write tests for `OnStateChanged` callback — invoked after state mutations
- [x] Implement ViewModel to pass all tests
- [x] Verify ≥ 90% coverage on AccountsViewModel

### Phase 2: Refactor Accounts.razor to Use ViewModel

**Tasks:**
- [x] Register `AccountsViewModel` as transient in DI
- [x] Update `Accounts.razor` to inject `AccountsViewModel`
- [x] Replace all state fields with `ViewModel.PropertyName` bindings
- [x] Replace all event handlers with `ViewModel.MethodAsync` calls
- [x] Wire `OnStateChanged` callback in `OnInitializedAsync`
- [x] Remove `@code` block (except thin `OnInitializedAsync` / `Dispose` wiring)
- [x] Verify all 20 existing `AccountsPageTests` pass
- [ ] Verify application runs correctly (manual smoke test)

### Phase 3: Verify Coverage & Finalize

**Tasks:**
- [x] Run coverage report for AccountsViewModel — verify ≥ 90%
- [x] Update this document status to Done

---

## Testing Strategy

### Unit Tests (AccountsViewModelTests)

Tests use `StubBudgetApiService` (already exists) and stub/mock `IToastService`, `ScopeService`, and `NavigationManager`.

**Test categories:**
- **Initialization:** Loads accounts on init, handles API failure gracefully
- **CRUD:** Create/Update/Delete with success, failure, and conflict paths
- **Transfers:** Show transfer dialog (with/without pre-selected source), create transfer, hide
- **Navigation:** View account navigates to detail page
- **State transitions:** Loading → loaded, submitting → done, error → dismissed
- **Scope change:** Reloads data when scope changes
- **Dispose:** Cleans up scope change subscription
- **StateHasChanged:** Callback invoked after each state mutation

### Integration Tests (Existing bUnit)

The 20 existing `AccountsPageTests` serve as integration tests verifying the Razor → ViewModel → DOM pipeline.

---

## References

- [Feature 097: ViewModel Extraction — Categories (Prototype)](./097-viewmodel-extraction-categories.md) — Established pattern
- [Component Standards — Section 11: ViewModel Pattern](./COMPONENT-STANDARDS.md) — Pattern documentation
