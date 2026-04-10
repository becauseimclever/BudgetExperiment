# Feature 151: Extract TransactionFactory (Transaction Entity God Class)

> **Status:** Done  
> **Severity:** 🟠 High — F-005  
> **Audit Source:** `docs/audit/2026-04-09-full-principle-audit.md`

---

## Overview

The `Transaction` entity in `BudgetExperiment.Domain` has grown to 545 lines and contains at least five distinct factory methods (`Create`, `CreateFromRecurringTransaction`, `CreateFromRecurringTransfer`, `CreateTransferPair`, etc.) alongside update logic, import-batch assignment, reconciliation linking, and location management. This violates SRP: a domain entity should own its state and invariants, not also be the factory for every creation scenario.

This feature extracts all static factory methods into a `TransactionFactory` domain service. The `Transaction` entity retains all non-factory behavior: property definitions, state transitions, domain invariants, and update methods. No API surface changes, no database changes.

---

## Problem Statement

### Current State

- `Transaction.cs` is 545 lines.
- Contains 5+ static factory methods, each encoding a different creation scenario:
  - `Create(...)` — standard transaction from user input
  - `CreateFromRecurringTransaction(...)` — instantiates a recurring transaction
  - `CreateFromRecurringTransfer(...)` — instantiates a recurring transfer leg
  - `CreateTransferPair(...)` — creates both legs of a transfer
  - Possibly additional import or batch creation variants
- Factory methods contain guard clauses, parameter validation, and wiring logic that is duplicated or nearly duplicated across scenarios.
- Any test that needs to construct a `Transaction` must either call these static methods (coupling to factory logic) or use test builders that duplicate the construction logic.

### Target State

- `TransactionFactory` class in `BudgetExperiment.Domain/Accounts/` containing all extracted factory methods.
- `Transaction` entity reduced to its core responsibility: state definition, invariant enforcement, update methods, domain event raising.
- All call sites that called `Transaction.Create(...)` or similar static methods are updated to call `TransactionFactory.Create(...)`.
- No behavior changes — same guard clauses, same output, same domain invariants.

---

## User Stories

### US-151-001: Clear Separation Between Transaction State and Construction

**As a** developer working on transaction creation scenarios  
**I want** factory logic in `TransactionFactory`, not on the entity  
**So that** I can add a new creation scenario by adding to `TransactionFactory` without touching the `Transaction` entity

**Acceptance Criteria:**
- [ ] `TransactionFactory` exists in `BudgetExperiment.Domain/Accounts/`
- [ ] `Transaction.cs` no longer contains static factory methods
- [ ] All existing call sites compile and behave identically
- [ ] `Transaction.cs` is meaningfully shorter (target: ≤ 300 lines)

### US-151-002: Simpler Test Construction

**As a** developer writing unit tests  
**I want** a single place to find all `Transaction` construction logic  
**So that** test builders and test helpers have one authoritative source

**Acceptance Criteria:**
- [ ] Test helpers / builders reference `TransactionFactory` not `Transaction` static methods
- [ ] Existing tests updated to use `TransactionFactory` and still pass

---

## Technical Design

### Responsibilities After Split

**`TransactionFactory` (new — domain service in Domain layer):**
- `Create(...)` — standard creation with guard clauses
- `CreateFromRecurringTransaction(recurringTransaction, instanceDate, ...)` — instantiate recurring
- `CreateFromRecurringTransfer(recurringTransfer, direction, instanceDate, ...)` — instantiate transfer leg
- `CreateTransferPair(sourceAccount, destinationAccount, amount, date, ...)` — creates both legs; returns a value tuple or a dedicated `TransferPair` value type
- Any additional import/batch factory methods currently on `Transaction`

**`Transaction` entity (retains):**
- All property definitions and backing fields
- Domain invariants enforced via private setters
- `Update(...)` method(s)
- `AssignToImportBatch(...)`, `LinkReconciliation(...)`, `UpdateLocation(...)` — state mutation methods that act on an existing instance
- `KakeiboOverride` management
- Domain event raising (`AddDomainEvent`, `_domainEvents`)

### File Layout

```
src/BudgetExperiment.Domain/Accounts/
  Transaction.cs          ← retains state + behavior
  TransactionFactory.cs   ← new: all static factory methods (extracted)
```

### Call Site Pattern

```csharp
// Before (Application service or test)
var tx = Transaction.Create(accountId, amount, date, description, categoryId);

// After
var tx = TransactionFactory.Create(accountId, amount, date, description, categoryId);
```

The method signatures do not change — only the type they live on.

### Visibility

`TransactionFactory` is a `public static class` (if all factory methods are stateless/pure). If any factory method requires domain services (e.g., rule evaluation), make it a `public sealed class` with constructor injection. Prefer static where methods are pure guard-clause + property initialization.

---

## Implementation Plan

### Phase 1: Audit Transaction.cs

**Tasks:**
- [ ] Read `Transaction.cs` in full (545 lines)
- [ ] Categorize each method: factory / state-mutation / property / domain-event
- [ ] List all call sites for each static factory method (grep `Transaction.Create`, `Transaction.CreateFrom`, `Transaction.CreateTransferPair`)
- [ ] Identify if any factory method calls instance methods (would require factory to construct then call — still valid)

**No code changes. Output: documented method inventory.**

---

### Phase 2: Create TransactionFactory

**Tasks:**
- [ ] Create `src/BudgetExperiment.Domain/Accounts/TransactionFactory.cs`
- [ ] Copy each factory method signature and body verbatim from `Transaction.cs`
- [ ] Update method bodies: any `new Transaction(...)` constructor calls remain unchanged
- [ ] Add XML doc comments to each factory method
- [ ] Keep original factory methods on `Transaction.cs` temporarily (two copies) — ensures build stays green during migration
- [ ] `dotnet build src/BudgetExperiment.Domain/` — zero errors

---

### Phase 3: Update All Call Sites

**Tasks:**
- [ ] In `BudgetExperiment.Application` — update all `Transaction.Create*(...)` calls to `TransactionFactory.Create*(...)`
- [ ] In `BudgetExperiment.Infrastructure` — update any factory calls (e.g., import batch processing)
- [ ] In test projects — update all test construction helpers to use `TransactionFactory`
- [ ] `dotnet build` (full solution) — zero errors

---

### Phase 4: Remove Factory Methods from Transaction Entity

**Tasks:**
- [ ] Delete all static factory method bodies from `Transaction.cs`
- [ ] Verify `Transaction.cs` no longer has any `public static` factory methods
- [ ] `dotnet build` — zero errors
- [ ] `dotnet test --filter "Category!=Performance"` — all green
- [ ] Confirm `Transaction.cs` line count is significantly reduced (target: ≤ 300 lines)

**Commit:**
```
refactor(domain): extract TransactionFactory from Transaction entity

Transaction entity (545 lines) reduced by moving all static factory
methods (Create, CreateFromRecurringTransaction, CreateFromRecurringTransfer,
CreateTransferPair, + any import variants) to TransactionFactory.

Transaction retains state management, invariants, and update methods.
All call sites updated. No behavior changes.

Closes F-005 (2026-04-09 audit)
Refs: §8, §12 Engineering Guide (SRP, Entities: Identity + behavior)
```

---

### Phase 5: Update Domain Tests

**Tasks:**
- [ ] Search `BudgetExperiment.Domain.Tests` for tests that call `Transaction.Create*` static methods
- [ ] Update those tests to call `TransactionFactory.Create*`
- [ ] Add `TransactionFactory_Create_WithNullAccountId_ThrowsDomainException` (guard clause test)
- [ ] Add `TransactionFactory_CreateTransferPair_ProducesMatchedLegs` (validates both legs have correct directions)
- [ ] `dotnet test tests/BudgetExperiment.Domain.Tests/ --filter "Category!=Performance"` — all green

**Commit:**
```
test(domain): update Transaction construction tests to use TransactionFactory

All existing tests updated. New factory-specific guard clause tests added.

Refs: F-005
```

---

## Testing Strategy

### Unit Tests (Domain)

- `TransactionFactory_Create_ValidParameters_ReturnsTransaction`
- `TransactionFactory_Create_NullAccountId_ThrowsDomainException`
- `TransactionFactory_Create_NullAmount_ThrowsDomainException`
- `TransactionFactory_CreateFromRecurringTransaction_SetsCorrectFields`
- `TransactionFactory_CreateFromRecurringTransfer_SetsDirectionCorrectly`
- `TransactionFactory_CreateTransferPair_BothLegsHaveMatchingPairId`
- `TransactionFactory_CreateTransferPair_SourceLegIsDebit_DestinationLegIsCredit`

### Regression: Existing Tests

All existing `Transaction` entity tests (state changes, update methods, Kakeibo resolution) must remain green unchanged.

---

## Security Considerations

None — factory methods are pure domain logic with no external I/O.

---

## Migration Notes

No database changes. No API changes. No DTO changes. This refactor is Domain-layer only.

---

## Future Enhancements

F-013 (god domain classes) identifies `RuleSuggestion.cs` (434 lines) as a candidate for the same treatment. Apply this same `EntityFactory` extraction pattern there.

---

## References

- [2026-04-09 Full Principle Audit — F-005](../docs/audit/2026-04-09-full-principle-audit.md#f-005-high--god-class-transaction-entity-545-lines)
- Engineering Guide §8 (Clean Code — SRP, short methods)
- Engineering Guide §12 (Domain Model Rules — "Entities: Identity + behavior")
- Engineering Guide §24 (Forbidden: God services > ~300 lines)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit F-005 | Alfred (Lead) |
