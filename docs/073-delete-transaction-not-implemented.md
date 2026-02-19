# Feature 073: Delete Transaction — Not Implemented Across Stack
> **Status:** Planning  
> **Priority:** High (bug)  
> **Estimated Effort:** Medium (1–2 days)  
> **Dependencies:** None

## Overview

The delete transaction button on the account transaction list page appears to work (shows a confirmation dialog), but clicking confirm does not actually delete the transaction. The transaction reappears after the page reloads. The root cause is that the delete operation was never implemented: the API call in the client is commented out, the client service method does not exist, no DELETE endpoint exists on the API, and the application service has no delete method.

## Problem Statement

### Bug: Delete Transaction Button Does Nothing

When a user clicks the delete button on a transaction row in the `AccountTransactions` page:

1. A confirmation dialog correctly appears.
2. Clicking "Confirm" closes the dialog and triggers `LoadData()` to refresh.
3. The transaction is **still present** because no API call was made — the actual delete call is commented out with a TODO.

**Root cause:** The entire delete flow is stubbed. The UI scaffolding exists (button, confirmation dialog, handler), but every layer below it is missing or disabled:

```csharp
// AccountTransactions.razor — ConfirmDelete method
try
{
    // TODO: Implement delete when API supports it
    // await ApiService.DeleteTransactionAsync(deletingItem.Id);
    showDeleteConfirm = false;
    deletingItem = null;
    await LoadData();   // reloads list, but nothing was deleted
}
```

### Current State

| Layer | Status |
|-------|--------|
| **UI (button + dialog)** | Exists and works |
| **Client service** (`IBudgetApiService` / `BudgetApiService`) | `DeleteTransactionAsync` method does not exist |
| **API controller** (`TransactionsController`) | No `[HttpDelete]` endpoint |
| **Application service** (`ITransactionService` / `TransactionService`) | No `DeleteAsync` method |
| **Repository** (`ITransactionRepository` via `IWriteRepository<Transaction>`) | `RemoveAsync` already available — no changes needed |

### Target State

- Clicking delete and confirming actually removes the transaction from the database.
- The transaction list refreshes and no longer shows the deleted transaction.
- The current balance and running balances update correctly after deletion.
- Proper error handling if the transaction is not found (404) or deletion fails.
- The delete operation follows REST conventions: `DELETE /api/v1/transactions/{id}` returning `204 No Content`.

---

## User Stories

### Delete Transaction

#### US-073-001: Delete a Transaction
**As a** budget user  
**I want to** delete a transaction from my account  
**So that** I can correct mistakes or remove duplicate entries.

**Acceptance Criteria:**
- [ ] Clicking confirm on the delete dialog sends a DELETE request to the API.
- [ ] The API returns `204 No Content` on successful deletion.
- [ ] The API returns `404 Not Found` if the transaction does not exist.
- [ ] The transaction list refreshes after successful deletion without the deleted item.
- [ ] The current balance and running balances recalculate correctly after deletion.
- [ ] An error message is shown to the user if the delete fails.

---

## Technical Design

### Architecture Changes

No architectural changes. This implements a standard CRUD operation across existing layers.

### Domain Model

No domain changes. `IWriteRepository<Transaction>` already provides `RemoveAsync`.

### API Endpoints

| Method | Endpoint | Description | Response |
|--------|----------|-------------|----------|
| DELETE | `/api/v1/transactions/{id:guid}` | Delete a transaction by ID | `204 No Content` / `404 Not Found` |

### Database Changes

None.

### UI Components

No new components. Existing `AccountTransactions.razor` confirmation dialog and handler will be wired up to make the real API call.

---

## Implementation Plan

### Phase 1: Application Service — Add DeleteAsync

**Objective:** Add the delete method to the transaction service interface and implementation.

**Tasks:**
- [ ] Write a failing unit test for `TransactionService.DeleteAsync` (happy path: deletes successfully)
- [ ] Write a failing unit test for `DeleteAsync` when transaction not found (throws or returns false)
- [ ] Add `DeleteAsync(Guid id, CancellationToken)` to `ITransactionService`
- [ ] Implement `DeleteAsync` in `TransactionService` — fetch by ID, call `RemoveAsync`, save
- [ ] Verify tests pass

**Commit:**
```bash
git add .
git commit -m "feat(application): add DeleteAsync to TransactionService

- Add DeleteAsync to ITransactionService interface
- Implement in TransactionService with not-found validation
- Unit tests for happy path and not-found scenario

Refs: #073"
```

---

### Phase 2: API Controller — Add DELETE Endpoint

**Objective:** Expose the delete operation as a REST endpoint.

**Tasks:**
- [ ] Write an API integration test for `DELETE /api/v1/transactions/{id}` (204 response)
- [ ] Write an API test for 404 when transaction does not exist
- [ ] Add `[HttpDelete("{id:guid}")]` action to `TransactionsController`
- [ ] Return `NoContent()` on success, `NotFound()` on missing transaction
- [ ] Verify OpenAPI spec includes the new endpoint

**Commit:**
```bash
git add .
git commit -m "feat(api): add DELETE endpoint for transactions

- DELETE /api/v1/transactions/{id} returns 204 or 404
- Integration tests for success and not-found paths

Refs: #073"
```

---

### Phase 3: Client Service — Add DeleteTransactionAsync

**Objective:** Add the HTTP client method and wire up the UI.

**Tasks:**
- [ ] Add `DeleteTransactionAsync(Guid id)` to `IBudgetApiService`
- [ ] Implement in `BudgetApiService` — `HttpClient.DeleteAsync($"api/v1/transactions/{id}")`
- [ ] Uncomment and update the call in `AccountTransactions.razor` `ConfirmDelete` method
- [ ] Add error handling / user feedback for failed deletes
- [ ] Manual testing against local API

**Commit:**
```bash
git add .
git commit -m "feat(client): wire up delete transaction to API

- Add DeleteTransactionAsync to IBudgetApiService
- Implement HTTP DELETE call in BudgetApiService
- Enable delete call in AccountTransactions.razor ConfirmDelete

Refs: #073"
```

---

### Phase 4: Cleanup & Documentation

**Objective:** Remove TODO comment, verify full flow, update docs.

**Tasks:**
- [ ] Remove the TODO comment from `AccountTransactions.razor`
- [ ] Verify current balance recalculates correctly after deletion
- [ ] Update feature doc status to Complete

**Commit:**
```bash
git add .
git commit -m "docs: mark feature 073 complete

- Remove TODO stub comment
- Verified delete flow end-to-end

Refs: #073"
```

---

## Conventional Commit Reference

| Type | When to Use | SemVer Impact | Example |
|------|-------------|---------------|---------|
| `feat` | New feature or capability | Minor | `feat(api): add DELETE endpoint for transactions` |
| `fix` | Bug fix | Patch | `fix(client): enable delete transaction API call` |
| `test` | Adding or fixing tests | None | `test(application): add transaction delete tests` |
| `docs` | Documentation only | None | `docs: mark feature 073 complete` |
