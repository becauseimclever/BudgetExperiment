# Feature 097: ViewModel Extraction — Categories Page (Prototype)

> **Status:** Planning
> **Priority:** Medium
> **Dependencies:** Feature 095 (Done)

## Overview

Extract the handler logic from the Categories page `@code` block into a testable `CategoriesViewModel` class. This serves as the prototype for the ViewModel/Presenter pattern that can be applied across all Blazor pages. The goal is to move core business logic out of Razor files into plain C# classes that are directly testable with standard xUnit — eliminating the async state machine coverage instrumentation gap discovered in Feature 095.

## Problem Statement

### Current State

The Categories page ([Categories.razor](../src/BudgetExperiment.Client/Pages/Categories.razor)) contains **378 lines** with the `@code` block starting at line 104. All handler logic — CRUD operations, error handling, state management — lives inline in the Razor file.

**Structural issues:**
- **17 handler methods** embedded in `@code` block: `OnInitializedAsync`, `OnScopeChanged`, `Dispose`, `LoadCategories`, `RetryLoad`, `DismissError`, `ShowAddCategory`, `HideAddCategory`, `CreateCategory`, `ShowEditCategory`, `HideEditCategory`, `UpdateCategory`, `ConfirmDeleteCategory`, `CancelDelete`, `DeleteCategory`, `ActivateCategory`, `DeactivateCategory`
- **16 state fields** managing loading, forms, editing, and deletion state
- **3 computed properties** (`ExpenseCategories`, `IncomeCategories`, `TransferCategories`) with LINQ filtering/ordering
- **4 injected services**: `IBudgetApiService`, `IToastService`, `ScopeService`, `IChatContextService`

**Coverage instrumentation gap (from Feature 095 investigation):**
Each `async Task` handler compiles into a compiler-generated state machine class (e.g., `<CreateCategory>d__XX`). Coverlet tracks these as separate sub-classes of the page. When bUnit invokes handlers through Blazor's `EventCallback` pipeline, async continuations execute outside coverlet's instrumented path — resulting in 0% reported coverage for exercised handlers.

Code-behind (`.razor.cs`) components already in the project show 85-100% coverage, confirming the issue is specific to `@code` block compilation.

### Target State

- `CategoriesViewModel` is a plain C# class in `BudgetExperiment.Client/ViewModels/`
- All handler methods, state fields, and computed properties live in the ViewModel
- `Categories.razor` is a thin binding layer: injects the ViewModel, binds UI elements to ViewModel properties/methods
- ViewModel is directly testable with xUnit + Shouldly — no bUnit required for logic tests
- Coverage reports accurately for all handler methods
- Existing bUnit page tests (`CategoriesPageTests.cs`, 19 tests) continue to pass

---

## User Stories

### US-097-001: Extract CategoriesViewModel
**As a** developer
**I want to** extract handler logic from Categories.razor into a CategoriesViewModel class
**So that** page business logic is testable with plain xUnit and coverage reports accurately.

**Acceptance Criteria:**
- [ ] `CategoriesViewModel` class exists in `src/BudgetExperiment.Client/ViewModels/CategoriesViewModel.cs`
- [ ] All 17 handler methods are moved to the ViewModel
- [ ] All 16 state fields are moved to the ViewModel
- [ ] All 3 computed properties are moved to the ViewModel
- [ ] ViewModel exposes a `StateHasChanged` action/event for the Razor page to subscribe to for re-rendering
- [ ] `Categories.razor` delegates all logic to the injected ViewModel
- [ ] ViewModel receives services via constructor injection
- [ ] All existing 19 `CategoriesPageTests` pass without modification (or with minimal binding changes)

### US-097-002: Add ViewModel Unit Tests
**As a** developer
**I want to** test CategoriesViewModel directly with xUnit
**So that** all handler logic has comprehensive and accurately reported coverage.

**Acceptance Criteria:**
- [ ] `CategoriesViewModelTests.cs` exists in `tests/BudgetExperiment.Client.Tests/ViewModels/`
- [ ] Tests cover all 17 handler methods
- [ ] Tests verify state transitions (loading, error, success)
- [ ] Tests verify CRUD operations call correct API service methods
- [ ] Tests verify computed properties filter/sort correctly
- [ ] Tests verify error handling (API failures, conflict detection on update)
- [ ] Tests verify scope change triggers reload and dispose cleans up
- [ ] Coverage for CategoriesViewModel ≥ 90%

---

## Technical Design

### Architecture

```
Before:
  Categories.razor
    └── @code { 17 handlers, 16 fields, 3 computed props, 4 services }

After:
  Categories.razor
    └── @inject CategoriesViewModel ViewModel
    └── binds UI to ViewModel.Property / ViewModel.MethodAsync()

  CategoriesViewModel (plain C# class)
    └── constructor(IBudgetApiService, IToastService, ScopeService, IChatContextService)
    └── 17 public methods
    └── 16 public properties (state)
    └── 3 computed properties
    └── Action? OnStateChanged (for Razor re-render callback)
```

### CategoriesViewModel Design

```csharp
public class CategoriesViewModel : IDisposable
{
    private readonly IBudgetApiService _apiService;
    private readonly IToastService _toastService;
    private readonly ScopeService _scopeService;
    private readonly IChatContextService _chatContextService;

    // State properties (public for binding)
    public bool IsLoading { get; private set; }
    public bool IsRetrying { get; private set; }
    public bool IsSubmitting { get; private set; }
    public bool IsDeleting { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<BudgetCategoryDto> Categories { get; private set; } = new();

    // Form state
    public bool ShowAddCategory { get; private set; }
    public BudgetCategoryCreateDto NewCategory { get; set; } = new();
    public bool ShowEditCategory { get; private set; }
    public BudgetCategoryCreateDto EditCategory { get; set; } = new();
    public Guid? EditingCategoryId { get; private set; }
    public string? EditingVersion { get; private set; }
    public int EditSortOrder { get; private set; }
    public bool ShowDeleteConfirm { get; private set; }
    public BudgetCategoryDto? DeletingCategory { get; private set; }

    // Computed
    public IEnumerable<BudgetCategoryDto> ExpenseCategories => ...;
    public IEnumerable<BudgetCategoryDto> IncomeCategories => ...;
    public IEnumerable<BudgetCategoryDto> TransferCategories => ...;

    // Re-render callback for Razor page
    public Action? OnStateChanged { get; set; }

    // Handler methods (public async Task)
    public async Task InitializeAsync() { ... }
    public async Task LoadCategoriesAsync() { ... }
    public async Task RetryLoadAsync() { ... }
    public void DismissError() { ... }
    public void OpenAddCategory() { ... }
    public void CloseAddCategory() { ... }
    public async Task CreateCategoryAsync() { ... }
    public void OpenEditCategory(BudgetCategoryDto category) { ... }
    public void CloseEditCategory() { ... }
    public async Task UpdateCategoryAsync() { ... }
    public void ConfirmDeleteCategory(BudgetCategoryDto category) { ... }
    public void CancelDelete() { ... }
    public async Task DeleteCategoryAsync() { ... }
    public async Task ActivateCategoryAsync(BudgetCategoryDto category) { ... }
    public async Task DeactivateCategoryAsync(BudgetCategoryDto category) { ... }
    public void Dispose() { ... }
}
```

### DI Registration

Register as `Transient` (one instance per page render) in the Client's service configuration:

```csharp
builder.Services.AddTransient<CategoriesViewModel>();
```

### Razor Binding Pattern

```razor
@page "/categories"
@inject CategoriesViewModel ViewModel

<!-- UI binds to ViewModel properties -->
@if (ViewModel.IsLoading) { <LoadingSpinner /> }
@if (ViewModel.ErrorMessage != null) { <ErrorAlert Message="@ViewModel.ErrorMessage" ... /> }

<!-- Event handlers delegate to ViewModel -->
<button @onclick="ViewModel.OpenAddCategory">Add Category</button>

@code {
    protected override async Task OnInitializedAsync()
    {
        ViewModel.OnStateChanged = () => InvokeAsync(StateHasChanged);
        await ViewModel.InitializeAsync();
    }

    public void Dispose()
    {
        ViewModel.OnStateChanged = null;
        ViewModel.Dispose();
    }
}
```

---

## Implementation Plan

### Phase 1: Create CategoriesViewModel with Tests (TDD)

**Objective:** Build the ViewModel class test-first with all handlers and state management.

**Tasks:**
- [ ] Create `src/BudgetExperiment.Client/ViewModels/CategoriesViewModel.cs`
- [ ] Create `tests/BudgetExperiment.Client.Tests/ViewModels/CategoriesViewModelTests.cs`
- [ ] Write tests for `InitializeAsync` — loads categories, subscribes to scope changes
- [ ] Write tests for `LoadCategoriesAsync` — success, failure, loading state transitions
- [ ] Write tests for `RetryLoadAsync` — sets retrying flag, reloads
- [ ] Write tests for `DismissError` — clears error message
- [ ] Write tests for computed properties — `ExpenseCategories`, `IncomeCategories`, `TransferCategories` filter and sort correctly
- [ ] Write tests for `CreateCategoryAsync` — success (adds to list, shows toast, hides form), failure (sets error)
- [ ] Write tests for `UpdateCategoryAsync` — success, conflict detection (409), failure
- [ ] Write tests for `DeleteCategoryAsync` — success (removes from list, shows toast), failure
- [ ] Write tests for `ActivateCategoryAsync` / `DeactivateCategoryAsync` — success refreshes item, failure sets error
- [ ] Write tests for `OpenAddCategory` / `CloseAddCategory`, `OpenEditCategory` / `CloseEditCategory`, `ConfirmDeleteCategory` / `CancelDelete` — state flag toggles
- [ ] Write tests for `Dispose` — unsubscribes from scope change events
- [ ] Write tests for `OnStateChanged` callback — invoked after state mutations
- [ ] Implement ViewModel to pass all tests
- [ ] Verify ≥ 90% coverage on CategoriesViewModel

**Commit:** `feat(client): add CategoriesViewModel with comprehensive unit tests`

### Phase 2: Refactor Categories.razor to Use ViewModel

**Objective:** Replace inline `@code` logic with ViewModel delegation.

**Tasks:**
- [ ] Register `CategoriesViewModel` as transient in DI
- [ ] Update `Categories.razor` to inject `CategoriesViewModel`
- [ ] Replace all state fields with `ViewModel.PropertyName` bindings
- [ ] Replace all `@onclick` / event handlers with `ViewModel.MethodAsync` calls
- [ ] Wire `OnStateChanged` callback in `OnInitializedAsync`
- [ ] Remove `@code` block (except the thin `OnInitializedAsync` / `Dispose` wiring)
- [ ] Verify all 19 existing `CategoriesPageTests` pass
- [ ] Verify application runs correctly (manual smoke test)

**Commit:** `refactor(client): wire Categories.razor to CategoriesViewModel`

### Phase 3: Verify Coverage & Document Pattern

**Objective:** Confirm the pattern delivers accurate coverage and document it for other pages.

**Tasks:**
- [ ] Run coverage report for CategoriesViewModel — verify ≥ 90%
- [ ] Compare coverage before/after for Categories page
- [ ] Document the ViewModel extraction pattern in a brief guide (update copilot-instructions.md or add to COMPONENT-STANDARDS.md)
- [ ] Identify next candidate pages for extraction (prioritized by size and coverage gap)

**Commit:** `docs(client): document ViewModel extraction pattern`

---

## Testing Strategy

### Unit Tests (CategoriesViewModelTests)

Tests use a stub `IBudgetApiService` (already exists: `StubBudgetApiService`) and mock/stub `IToastService`, `ScopeService`, and `IChatContextService`.

**Test categories:**
- **Initialization:** Loads categories on init, handles API failure gracefully
- **CRUD:** Create/Update/Delete with success and failure paths
- **State transitions:** Loading → loaded, submitting → done, error → dismissed
- **Computed properties:** Correct filtering by type, correct sort order
- **Concurrency:** Update conflict (409) detection
- **Scope change:** Reloads categories when scope changes
- **Dispose:** Cleans up scope change subscription
- **StateHasChanged:** Callback invoked after each state mutation

### Integration Tests (Existing bUnit)

The 19 existing `CategoriesPageTests` serve as integration tests — they verify the full Razor → ViewModel → Stub → DOM pipeline continues to work after extraction.

---

## Candidate Pages for Future ViewModel Extraction

After Categories proves the pattern, apply to other pages prioritized by size and coverage gap:

| Page | Lines | Coverage (Post-095) | Handler Count | Priority |
|------|-------|---------------------|---------------|----------|
| Rules | 442 | ~17.6% | ~12 | High |
| Accounts | 349 | ~25% | ~8 | High |
| Budget | 374 | ~20% | ~6 | Medium |
| Recurring | 423 | ~22% | ~8 | Medium |
| RecurringTransfers | 378 | ~21% | ~8 | Medium |
| Transfers | 322 | ~24% | ~5 | Medium |
| Onboarding | 241 | ~30% | ~4 | Low |

Larger pages (Import, Calendar, AccountTransactions) would also benefit but require more careful extraction due to their complexity (700-1100 lines, 15-25+ handlers each). Extract smaller pages first to validate the pattern.

---

## References

- [Feature 095: Client Test Coverage Phase 2](./095-client-test-coverage-phase2.md) — Coverage investigation and architectural findings
- [Feature 091: Client Test Coverage Phase 1](./archive/081-090-versioning-concurrency-hygiene-test-coverage.md) — Initial coverage work
- [Component Standards](./COMPONENT-STANDARDS.md) — Existing component patterns
