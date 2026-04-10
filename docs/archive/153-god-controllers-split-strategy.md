# Feature 153: God API Controllers — Split Strategy

> **Status:** Done  
> **Severity:** 🟠 High — F-007  
> **Audit Source:** `docs/audit/2026-04-09-full-principle-audit.md`

---

## Overview

Four API controllers exceed the 300-line Engineering Guide threshold. `TransactionsController` (401 lines) handles CRUD, batch operations, paging, filtering, and ETag concurrency — at least three distinct endpoint groups. The recommended strategy: split the two largest controllers at natural seams (`TransactionsController`, `RecurringTransactionsController`) and migrate `CategorySuggestionsController` to Minimal API endpoint groups as a pilot for future Minimal API adoption.

**Guiding principle:** New endpoints are written as Minimal API endpoint groups. Existing controllers are split when a feature touches them or when the split clearly reduces review complexity.

---

## Problem Statement

### Current State

| Controller | Lines | Concerns |
|------------|-------|----------|
| `TransactionsController` | 401 | CRUD, batch operations, paged query, ETag concurrency |
| `RecurringTransactionsController` | 390 | CRUD, scheduling, instance management, skip/resume |
| `RecurringTransfersController` | 388 | CRUD, scheduling, transfer pairs, skip/resume |
| `CategorySuggestionsController` | 309 | Suggestion retrieval, review, batch accept/reject, AI trigger |

### Target State

- `TransactionsController` split into `TransactionQueryController` (GET operations) and `TransactionBatchController` (POST batch, import-related).
- `RecurringTransactionsController` split into `RecurringTransactionsController` (CRUD) and `RecurringTransactionInstanceController` (instance management, skip, resume).
- `RecurringTransfersController` split into `RecurringTransfersController` (CRUD) and `RecurringTransferInstanceController` (instance management).
- `CategorySuggestionsController` migrated to a Minimal API endpoint group as a pilot pattern.
- All splits preserve identical HTTP routes and response contracts — zero breaking changes for clients.

---

## User Stories

### US-153-001: Split TransactionsController at Query/Batch Seam

**As a** developer reviewing or extending transaction endpoints  
**I want** query operations and batch operations in separate controllers  
**So that** each controller is small, focused, and independently testable

**Acceptance Criteria:**
- [ ] `TransactionQueryController` handles all GET operations (list, get by id, date range, paged)
- [ ] `TransactionBatchController` handles batch POST/DELETE operations
- [ ] All existing routes remain identical (no client-facing changes)
- [ ] API tests for each new controller pass
- [ ] No controller exceeds 300 lines

### US-153-002: Split RecurringTransactionsController

**As a** developer adding recurring transaction features  
**I want** CRUD and instance management in separate controllers  
**So that** each is under 300 lines and independently reviewable

**Acceptance Criteria:**
- [ ] `RecurringTransactionsController` handles CRUD (Create, Read, Update, Delete)
- [ ] `RecurringTransactionInstanceController` handles `SkipNext`, `ResumeNext`, `UpdateFromDate`, `GetInstances`
- [ ] All routes preserved
- [ ] Line counts under 300

### US-153-003: Split RecurringTransfersController

**As a** developer adding recurring transfer features  
**I want** CRUD and instance management in separate controllers  
**So that** each is under 300 lines

**Acceptance Criteria:**
- [ ] `RecurringTransfersController` handles CRUD
- [ ] `RecurringTransferInstanceController` handles instance management
- [ ] All routes preserved

### US-153-004: Migrate CategorySuggestionsController to Minimal API (Pilot)

**As a** developer evaluating Minimal API adoption  
**I want** `CategorySuggestionsController` migrated to a Minimal API endpoint group  
**So that** we have a reference pattern and validate the approach before broader adoption

**Acceptance Criteria:**
- [ ] Endpoint group registered under `/api/v1/category-suggestions`
- [ ] All 5+ endpoints (GET suggestions, POST review, POST batch-accept, DELETE, POST trigger) preserved
- [ ] OpenAPI spec generated correctly via `WithOpenApi()`
- [ ] API tests pass unchanged (same routes, same response shapes)
- [ ] Adoption decision documented in `decisions.md`

---

## Technical Design

### Controller Split Pattern

Each split follows this pattern:

1. **Identify the seam** — group endpoints by logical concern (query vs mutation, CRUD vs lifecycle).
2. **Create the new controller** — same `[ApiController]`, same `[Route("api/v{version:apiVersion}/...")]`, new class name.
3. **Move endpoint methods** — cut from original, paste into new. No logic changes.
4. **Update shared fields** — both controllers inject the same service interface(s).
5. **Verify routes** — confirm no route duplication or gaps.
6. **Verify tests** — existing API tests pass; add targeted tests for new controller.

### TransactionsController Split

```
TransactionQueryController    ← GET /transactions, GET /transactions/{id}, GET /transactions/date-range, GET /transactions/paged
TransactionBatchController    ← POST /transactions/batch, DELETE /transactions/batch, POST /transactions/import
```

Shared dependency: `ITransactionService` injected into both.

### RecurringTransactionsController Split

```
RecurringTransactionsController         ← GET, POST, PUT, DELETE /recurring-transactions
RecurringTransactionInstanceController  ← POST /recurring-transactions/{id}/skip-next
                                           POST /recurring-transactions/{id}/resume-next
                                           PUT  /recurring-transactions/{id}/update-from/{date}
                                           GET  /recurring-transactions/{id}/instances
```

### RecurringTransfersController Split

```
RecurringTransfersController         ← GET, POST, PUT, DELETE /recurring-transfers
RecurringTransferInstanceController  ← POST /recurring-transfers/{id}/skip-next
                                        POST /recurring-transfers/{id}/resume-next
                                        PUT  /recurring-transfers/{id}/update-from/{date}
                                        GET  /recurring-transfers/{id}/instances
```

### CategorySuggestionsController → Minimal API Pilot

```csharp
// In a new file: src/BudgetExperiment.Api/Endpoints/CategorySuggestionEndpoints.cs
public static class CategorySuggestionEndpoints
{
    public static IEndpointRouteBuilder MapCategorySuggestionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/category-suggestions")
            .WithTags("CategorySuggestions")
            .RequireAuthorization();

        group.MapGet("/", GetSuggestionsAsync).WithName("GetCategorySuggestions").WithOpenApi();
        group.MapPost("/{id}/review", ReviewSuggestionAsync).WithName("ReviewCategorySuggestion").WithOpenApi();
        // ... all other endpoints
        return app;
    }
}
```

Register in `Program.cs`: `app.MapCategorySuggestionEndpoints();`

**Rationale for pilot:** `CategorySuggestionsController` at 309 lines is the smallest of the four violators and has a natural grouping. It serves as a low-risk test of the Minimal API pattern before committing to broader migration.

---

## Implementation Plan

### Phase 1: TransactionsController Split (Standalone PR)

**Tasks:**
- [ ] Identify query vs batch endpoint boundary in `TransactionsController`
- [ ] Create `TransactionQueryController.cs` — move all GET endpoints
- [ ] Create `TransactionBatchController.cs` — move batch/import endpoints
- [ ] Delete original methods from `TransactionsController.cs` (or remove the file if empty after split)
- [ ] Ensure all routes identical to pre-split
- [ ] `dotnet build src/BudgetExperiment.Api/` — zero errors
- [ ] Run API tests: `dotnet test tests/BudgetExperiment.Api.Tests/ --filter "Category!=Performance"` — green
- [ ] Add `TransactionQueryControllerTests.cs` and `TransactionBatchControllerTests.cs` covering happy + error paths

**Commit:**
```
refactor(api): split TransactionsController into Query and Batch controllers

TransactionQueryController: all GET endpoints (list, by-id, date-range, paged)
TransactionBatchController: batch and import operations

All routes preserved. No client-facing changes.

Closes F-007 (TransactionsController — 2026-04-09 audit)
Refs: §24 Engineering Guide
```

---

### Phase 2: RecurringTransactionsController Split

**Tasks:**
- [ ] Separate CRUD endpoints from instance management endpoints
- [ ] Create `RecurringTransactionInstanceController.cs`
- [ ] Update `RecurringTransactionsController.cs` (CRUD only)
- [ ] Verify routes, run tests, add targeted tests

**Commit:**
```
refactor(api): split RecurringTransactionsController — extract instance management

RecurringTransactionInstanceController: skip-next, resume-next, update-from-date, get-instances
RecurringTransactionsController: CRUD only

All routes preserved.

Closes F-007 (RecurringTransactionsController — 2026-04-09 audit)
```

---

### Phase 3: RecurringTransfersController Split

**Tasks:**
- [ ] Same pattern as Phase 2 but for transfers
- [ ] Create `RecurringTransferInstanceController.cs`
- [ ] Update `RecurringTransfersController.cs`

**Commit:**
```
refactor(api): split RecurringTransfersController — extract instance management

All routes preserved.

Closes F-007 (RecurringTransfersController — 2026-04-09 audit)
```

---

### Phase 4: CategorySuggestionsController → Minimal API Pilot

**Tasks:**
- [ ] Create `src/BudgetExperiment.Api/Endpoints/CategorySuggestionEndpoints.cs`
- [ ] Implement all endpoints as `MapGet`, `MapPost`, `MapDelete` etc. with `.WithOpenApi()`
- [ ] Preserve response types, status codes, and ProblemDetails error handling
- [ ] Register in `Program.cs` via `app.MapCategorySuggestionEndpoints()`
- [ ] Remove `CategorySuggestionsController.cs`
- [ ] Run API tests — green
- [ ] Add endpoint tests via `HttpClient` (same approach as controller tests)
- [ ] Update `decisions.md`: document adoption verdict for Minimal API pattern

**Commit:**
```
refactor(api): migrate CategorySuggestionsController to Minimal API endpoint group (pilot)

CategorySuggestionEndpoints replaces CategorySuggestionsController.
Routes, contracts, and status codes unchanged.
Minimal API pilot establishes pattern for future endpoint migrations.

Closes F-007 (CategorySuggestionsController — 2026-04-09 audit)
Refs: §9 Engineering Guide (REST API design), §20 (Versioning)
```

---

### Phase 5: Post-Split Verification

**Tasks:**
- [ ] Confirm all four original controllers are either split or replaced
- [ ] Run full test suite: `dotnet test --filter "Category!=Performance"` — all green
- [ ] Run `dotnet format` — no violations
- [ ] Check all new controllers/endpoints have XML doc comments on public methods
- [ ] Verify OpenAPI/Scalar UI reflects all endpoints correctly (manual check)
- [ ] Update `decisions.md` with Minimal API adoption decision

---

## Testing Strategy

### Per-Split API Tests (WebApplicationFactory)

Each new controller or endpoint group needs:
- At least one happy-path test per endpoint (correct HTTP method, route, response shape)
- One 404 test (not found resource)
- One 400 test (invalid input)

### Route Preservation Verification

Before and after each split, dump all routes:
```powershell
# In test: Assert that routes are identical pre/post split
```

Or manually verify via Scalar UI that all expected endpoints are listed.

---

## Migration Notes

No database changes. No DTO changes. All HTTP routes preserved — zero breaking changes for the Blazor client or any external consumer.

---

## References

- [2026-04-09 Full Principle Audit — F-007](../docs/audit/2026-04-09-full-principle-audit.md#f-007-high--4-api-controllers-exceed-300-line-limit)
- Engineering Guide §9 (REST API Design)
- Engineering Guide §24 (Forbidden: God services > ~300 lines)
- Engineering Guide §10 (OpenAPI & Scalar — ensure spec correctness after migration)
- Engineering Guide §20 (Versioning & Deprecation)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit F-007 | Alfred (Lead) |
