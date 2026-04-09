# Feature 146: Transfer Deletion with Orphan Detection

> **Status:** Planning

## Overview

This feature implements safe deletion of transfer transactions, preventing the orphaning of transfer legs. When a user deletes a transaction that is one half of a linked transfer pair (identified by `TransferId`), both legs are deleted atomically. This ensures **INV-2 (Transfer Net-Zero)** is never violated and closes the accuracy gap where deleting one leg leaves the other leg with a `TransferId` pointing to nothing.

A transfer is a linked pair of transactions: a source transaction (negative amount) in one account and a destination transaction (positive amount) in another account, identified by a shared `TransferId` GUID. Currently, there is no guard to prevent deleting just one leg, creating an orphan state that violates INV-2.

## Problem Statement

### Current State

- `Transaction` has a nullable `TransferId` field linking two transactions into a transfer.
- `BudgetCategory` may have `CategorySource.Transfer` type (denoting category used for transfer transactions).
- No service currently handles deletion of a transfer as a unit.
- If a user deletes a transaction with `TransferId != null`, the other leg is orphaned (still has `TransferId` pointing to a non-existent transaction).
- No API endpoint explicitly deletes transfers; deletion likely goes through generic transaction delete.

### Target State

- New `DeleteTransferAsync(transferId)` method on `ITransactionRepository` or new `ITransferRepository` interface.
- When deleting a transfer:
  - Fetch both legs by `TransferId`.
  - Delete both in a single database transaction (atomic).
  - If only one leg exists (already orphaned), allow deletion without error.
- API endpoint `DELETE /api/v1/transfers/{transferId}` to delete transfer by ID.
- Feature flag `feature-transfer-atomic-deletion` gates the endpoint.
- Accuracy tests prove:
  - After deleting a transfer, neither leg exists.
  - Account balances reflect removal of both amounts (INV-1 and INV-2 both hold).
  - Orphaned legs (if they somehow exist) are handled gracefully.

---

## User Stories

### Transfer Management

#### US-146-001: Delete Transfer Without Orphaning Legs
**As a** user who made a transfer mistake  
**I want to** delete a transfer (both source and destination) with a single action  
**So that** my accounts stay balanced and no orphaned transactions remain

**Acceptance Criteria:**
- [ ] `DELETE /api/v1/transfers/{transferId}` removes both legs of the transfer
- [ ] Both account balances are correctly updated (money returned to source, removed from destination)
- [ ] No orphaned transaction with dangling `TransferId` remains
- [ ] If transfer is already partially deleted, gracefully handle the remaining leg
- [ ] Feature flag gates the endpoint

#### US-146-002: Prevent Transfer Imbalance
**As a** system guardian  
**I want to** ensure no transfer can exist with only one leg  
**So that** INV-2 (Transfer Net-Zero) invariant is never violated

**Acceptance Criteria:**
- [ ] Atomic deletion prevents orphans caused by failed deletes (one leg deleted, one committed)
- [ ] Deletion is transactional (all-or-nothing: both legs deleted or neither)
- [ ] Test verifies: after deletion, sum of all account balances unchanged from before transfer existed

---

## Technical Design

### Architecture Changes

- Option: Add `DeleteTransferAsync(Guid transferId, CancellationToken)` to `ITransactionRepository`
- Alternative: Create new `ITransferRepository` interface (cleaner separation if transfer operations expand later)
- **Decision: Extend `ITransactionRepository`** — simpler for now, one responsibility (both legs are transactions)
- Service layer (Application): `ITransferService` or extend existing transaction service with `DeleteTransferAsync`
- Uses `IDbContextTransaction` for atomic delete (EF Core database transaction)

### Domain Model

No domain model changes. Uses existing:
- `Transaction.TransferId` (nullable `Guid?`)
- `TransferDirection` enum (Source/Destination) — may be used in selection logic
- No new value objects or entities

```csharp
// Example: What we're working with
public sealed class Transaction
{
    public Guid? TransferId { get; private set; }
    public bool IsTransfer => this.TransferId.HasValue;
    // ... rest of model
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| DELETE | `/api/v1/transfers/{transferId}` | Delete transfer (both legs) by TransferId |

**Path Parameters:**
- `transferId` (required): GUID of the transfer (the shared `TransferId` value from both transactions)

**Response:**
- `204 No Content`: Transfer deleted successfully
- `404 Not Found`: No transfer with that ID exists, or no complete legs found
- `409 Conflict`: Inconsistent transfer state (warning; should not happen if deletion is atomic)

**Error Response (if conflict):**
```json
{
  "type": "about:blank",
  "title": "Conflict",
  "status": 409,
  "detail": "Transfer is in an inconsistent state (one leg found, not both). Manual cleanup required.",
  "traceId": "0HN..."
}
```

### Database Changes

No new tables or columns. Uses existing `Transactions.TransferId` column.

### UI Components

*Out of scope for this feature.* UI will consume the endpoint to provide "Delete Transfer" button on transfer details.

---

## Implementation Plan

### Phase 1: Repository & Service Layer

**Objective:** Implement atomic delete logic in repository and service.

**Tasks:**
- [ ] Add method to `ITransactionRepository`:
  ```csharp
  Task DeleteTransferAsync(Guid transferId, CancellationToken cancellationToken);
  ```
- [ ] Implement `TransactionRepository.DeleteTransferAsync`:
  - Fetch both transactions with `TransferId == transferId`
  - If neither exists, return (no-op; 404 at controller level)
  - If only one exists, log warning and delete it (orphan cleanup)
  - If both exist, delete both in a single database transaction:
    ```csharp
    using var transaction = _context.Database.BeginTransaction();
    try 
    {
        _context.Transactions.Remove(source);
        _context.Transactions.Remove(dest);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    catch 
    { 
        await transaction.RollbackAsync(cancellationToken);
        throw; 
    }
    ```
- [ ] Create `ITransferService` in Application layer (or extend transaction service):
  ```csharp
  public interface ITransferService
  {
      Task DeleteTransferAsync(Guid transferId, CancellationToken cancellationToken);
  }
  ```
- [ ] Implement service: calls `_transactionRepository.DeleteTransferAsync`, wraps exceptions
- [ ] Unit tests (mocked repository):
  - `TransferService_DeleteTransfer_CallsRepository`
  - `TransferService_RepositoryThrows_PropagatesException`
- [ ] Register service in DependencyInjection

**Commit:**
```bash
git add .
git commit -m "feat(infra,app): add atomic transfer deletion to repository and service

- Add ITransactionRepository.DeleteTransferAsync method
- Implement atomic deletion in TransactionRepository
- Use IDbContextTransaction for all-or-nothing semantics
- Add ITransferService wrapper in Application
- Handle orphaned legs gracefully

Refs: INV-2"
```

---

### Phase 2: API Endpoint & Feature Flag

**Objective:** Expose transfer deletion via REST.

**Tasks:**
- [ ] Create or extend `TransfersController` with `DeleteTransferAsync` endpoint
- [ ] Route: `DELETE /api/v1/transfers/{transferId}`
- [ ] Call `_transferService.DeleteTransferAsync(transferId, ct)`
- [ ] Return `204 NoContent` on success
- [ ] Return `404 NotFound` if transfer not found
- [ ] Add `[FeatureGate("feature-transfer-atomic-deletion")]` attribute
- [ ] Add OpenAPI documentation
- [ ] Write API tests (WebApplicationFactory):
  - `TransfersController_DeleteTransfer_FeatureFlagDisabled_Returns403`
  - `TransfersController_DeleteTransfer_ValidId_Returns204`
  - `TransfersController_DeleteTransfer_NotFound_Returns404`
  - `TransfersController_DeleteTransfer_InvalidGuid_Returns400`
- [ ] Update Scalar documentation

**Commit:**
```bash
git add .
git commit -m "feat(api): expose DELETE /api/v1/transfers/{transferId} endpoint

- Add TransfersController.DeleteTransferAsync
- Feature flag: feature-transfer-atomic-deletion
- Validate GUID format, return 204 on success, 404 if not found
- OpenAPI/Scalar documentation

Refs: INV-2"
```

---

### Phase 3: Integration & Accuracy Tests

**Objective:** Prove atomic deletion holds end-to-end with PostgreSQL.

**Tasks:**
- [ ] Create `TransferDeletionAccuracyTests.cs` in `BudgetExperiment.Infrastructure.Tests/Accuracy/`
- [ ] Integration test with Testcontainers (PostgreSQL):
  - Set up two accounts (A and B)
  - Create a transfer: -$100 in A, +$100 in B
  - Record initial balances: `balanceA = X`, `balanceB = Y`
  - Delete the transfer
  - Verify:
    - Both transaction legs are gone
    - `balanceA == X + 100` (money returned)
    - `balanceB == Y - 100` (money removed)
    - `A.Balance + B.Balance == initial_combined` (total unchanged)
  - Test case: `TransferDeletionAccuracy_DeletedTransfer_BothLegsGone_BalancesExact`
- [ ] Orphan handling test:
  - Manually create an orphaned transaction (simulate database corruption)
  - Call `DeleteTransferAsync` with its `TransferId`
  - Verify it is deleted without error
- [ ] Concurrency test (optional):
  - Two concurrent delete requests for the same transfer
  - Only one succeeds; second gets 404

**Commit:**
```bash
git add .
git commit -m "test(infra): add transfer deletion integration and accuracy tests

- Integration test with Testcontainers PostgreSQL
- Prove both transfer legs deleted atomically (INV-2)
- Verify account balances correct after deletion (INV-1)
- Test orphan cleanup
- Verify total sum of balances unchanged

Refs: INV-2"
```

---

### Phase 4: Documentation & Cleanup

**Objective:** Final documentation and polish.

**Tasks:**
- [ ] Update `docs/ACCURACY-FRAMEWORK.md` section 6 to mark INV-2 mitigation as complete
- [ ] Add XML comments to public APIs (service interface + controller)
- [ ] Add feature flag documentation
- [ ] Verify all tests pass: `dotnet test --filter "Category!=Performance"`
- [ ] Code style check: `dotnet format`

**Commit:**
```bash
git add .
git commit -m "docs(transfers): complete transfer deletion documentation

- XML comments for public APIs
- Update accuracy framework (INV-2 orphan protection)
- Document feature flag and endpoint behavior

Refs: INV-2"
```

---

## Testing Strategy

### Unit Tests

- **Service initialization:**
  - `TransferService_NullRepository_ThrowsArgumentNull`

- **Repository behavior (mocked):**
  - `TransferService_DeleteExistingTransfer_CallsRepository`
  - `TransferService_TransferNotFound_ThrowsNotFoundException`

- **Error handling:**
  - `TransferService_RepositoryThrows_PropagatesException`

### Integration Tests

- **End-to-end with PostgreSQL (Testcontainers):**
  - `TransferDeletionAccuracy_DeletedTransfer_BothLegsGone_BalancesExact`
  - `TransferDeletionAccuracy_OrphanedLeg_DeletedGracefully`
  - `TransferDeletionAccuracy_ConcurrentDeletes_OnlyOneSucceeds`

### Manual Testing Checklist

- [ ] Feature flag disabled: endpoint returns 403 Forbidden
- [ ] Feature flag enabled: endpoint returns 204 on delete
- [ ] Verify with UI: after deleting transfer, both amounts removed from accounts
- [ ] Verify with UI: account balances update correctly
- [ ] Check database: no orphaned transactions with stray `TransferId` remain

---

## Migration Notes

No database migration required. This feature uses existing `TransferId` column.

---

## Security Considerations

- **Authorization:** Only users with write access to both accounts in the transfer can delete it.
- **Audit trail:** Ensure deletion is logged (domain events or audit table) for compliance/debugging.
- **Idempotency:** If a transfer is already deleted, `DELETE /api/v1/transfers/{transferId}` should return 404 (not 204), making it safe to retry.

---

## Performance Considerations

- **Query:** Single query to fetch both transaction legs by `TransferId`.
- **Delete:** Two DELETE operations in a single transaction; negligible cost.
- **Acceptable latency:** < 100ms for typical transfer deletion.

---

## Future Enhancements

- **Soft delete / Audit trail:** Store deleted transfers in an audit table instead of hard-deleting, for compliance/recovery.
- **Undo/Redo:** Allow users to undo deletion within a time window.
- **Cascade delete:** Auto-delete recurring transfers that reference a deleted transfer (requires design review).

---

## References

- [INV-2: Transfer Net-Zero](./ACCURACY-FRAMEWORK.md#inv-2-transfer-net-zero)
- [Transaction domain model](../src/BudgetExperiment.Domain/Accounts/Transaction.cs)
- [ITransactionRepository](../src/BudgetExperiment.Domain/Repositories/ITransactionRepository.cs)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-01-09 | Initial planning draft | Alfred (Lead) |
