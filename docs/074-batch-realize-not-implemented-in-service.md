# Feature 074: Batch Realize — NotImplementedException in Application Service
> **Status:** Planning  
> **Priority:** Medium (code hygiene / architecture)  
> **Estimated Effort:** Small (< 1 day)  
> **Dependencies:** None

## Overview

The `PastDueService.RealizeBatchAsync` method throws `NotImplementedException`, but the batch realize feature already works end-to-end because the `RecurringController` implements the logic directly. This violates clean architecture by placing business logic in the controller layer instead of the application service layer. The fix is to move the existing controller logic into `PastDueService.RealizeBatchAsync` and have the controller delegate to it.

## Problem Statement

### Current State

- **Controller** (`RecurringController.RealizeBatchAsync`): Contains full batch realize logic — iterates `BatchRealizeRequest.Items`, calls `ITransactionRealizationService` or `ITransferRealizationService` per item type, builds `BatchRealizeResultDto` with success/failure tracking.
- **Application service** (`PastDueService.RealizeBatchAsync`): Throws `NotImplementedException` with comment `// This will be implemented when we wire up the batch endpoint`.
- **Client** (`BudgetApiService.RealizeBatchAsync`): Correctly calls `POST api/v1/recurring/realize-batch`.

The feature works, but the orchestration logic lives in the wrong layer.

### Target State

- `PastDueService.RealizeBatchAsync` contains the batch realize orchestration logic.
- `RecurringController.RealizeBatchAsync` delegates to `IPastDueService.RealizeBatchAsync`.
- No `NotImplementedException` remains in the codebase.

---

## User Stories

### Refactor Batch Realize to Application Layer

#### US-074-001: Move Batch Realize Logic to Application Service
**As a** developer  
**I want to** have batch realize business logic in the application service layer  
**So that** the architecture follows the clean architecture pattern and the controller remains thin.

**Acceptance Criteria:**
- [ ] `PastDueService.RealizeBatchAsync` implements batch realize logic (iterate items, call realization services, collect failures)
- [ ] `RecurringController.RealizeBatchAsync` delegates to `IPastDueService.RealizeBatchAsync`
- [ ] `NotImplementedException` is removed
- [ ] Existing API behavior is unchanged (same request/response shape, same error handling)
- [ ] Unit tests cover `PastDueService.RealizeBatchAsync` (success, partial failure, unknown type)
- [ ] Existing E2E/integration tests still pass

---

## Technical Design

### Architecture Changes

Move the batch realize orchestration from `RecurringController` into `PastDueService`. The controller should only validate the request and return the result.

### Files to Modify

| File | Change |
|------|--------|
| `src/BudgetExperiment.Application/Calendar/PastDueService.cs` | Implement `RealizeBatchAsync` with logic currently in controller |
| `src/BudgetExperiment.Api/Controllers/RecurringController.cs` | Simplify to delegate to `IPastDueService.RealizeBatchAsync` |

### Current Controller Logic to Relocate

```csharp
// Currently in RecurringController — should move to PastDueService
foreach (var item in request.Items)
{
    try
    {
        if (item.Type == "recurring-transaction")
        {
            await _transactionRealizationService.RealizeInstanceAsync(item.Id, ...);
            successCount++;
        }
        else if (item.Type == "recurring-transfer")
        {
            await _transferRealizationService.RealizeInstanceAsync(item.Id, ...);
            successCount++;
        }
        else
        {
            failures.Add(new BatchRealizeFailure { ... });
        }
    }
    catch (Exception ex)
    {
        failures.Add(new BatchRealizeFailure { ... });
    }
}
```

---

## Testing Strategy

### Unit Tests (TDD)

1. **RED**: Write test for `PastDueService.RealizeBatchAsync` — happy path with mixed transaction/transfer items
2. **RED**: Write test for unknown item type → failure entry
3. **RED**: Write test for partial failure (one item throws) → success count + failure list
4. **GREEN**: Implement `RealizeBatchAsync` in `PastDueService`
5. **REFACTOR**: Simplify controller, verify existing integration/E2E tests pass

---

## Definition of Done

- [ ] No `NotImplementedException` in `PastDueService`
- [ ] Controller delegates to application service
- [ ] Unit tests cover all batch realize paths
- [ ] All existing tests pass
- [ ] No new StyleCop warnings
