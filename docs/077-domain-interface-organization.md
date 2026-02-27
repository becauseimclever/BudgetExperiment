# Feature 077: Domain Interface Organization
> **Status:** Planning
> **Priority:** Medium (architecture clarity)
> **Estimated Effort:** Small (< 1 day)
> **Dependencies:** None

## Overview

The `Domain/Repositories/` folder has become a catch-all for all domain abstractions, not just repository interfaces. An audit found **5 interfaces** that are not repositories but are misplaced alongside `IAccountRepository`, `ITransactionRepository`, etc. This makes the domain model harder to navigate and blurs the distinction between data access contracts and service/cross-cutting concerns.

## Problem Statement

### Current State

The `Domain/Repositories/` folder contains 27 files, of which 5 are not repository abstractions:

| Interface | Nature | Why It's Misplaced |
|-----------|--------|-------------------|
| `IAutoRealizeService` | Application service | Name says "Service"; orchestrates auto-realization workflow. Should be in Application or a `Domain/Services/` folder. |
| `ITransactionMatcher` | Domain service | Pure matching logic (no I/O, no async). Not a repository. |
| `IRecurringInstanceProjector` | Projection/computation service | Projects recurring instances for date ranges. No persistence. |
| `IRecurringTransferInstanceProjector` | Projection/computation service | Projects transfer instances. No persistence. |
| `IUserContext` | Cross-cutting concern | Provides user identity (UserId, Username, Email). Not data access. |

The remaining 22 interfaces (`IAccountRepository`, `ITransactionRepository`, `IReadRepository<T>`, `IWriteRepository<T>`, `IUnitOfWork`, etc.) are legitimate repository/data-access abstractions.

### Target State

```
Domain/
├── Repositories/          ← Only repository interfaces + IUnitOfWork
│   ├── IAccountRepository.cs
│   ├── ITransactionRepository.cs
│   ├── IReadRepository.cs
│   ├── IWriteRepository.cs
│   ├── IUnitOfWork.cs
│   └── ... (17 other repository interfaces)
├── Services/              ← Domain service abstractions
│   ├── ITransactionMatcher.cs
│   ├── IRecurringInstanceProjector.cs
│   └── IRecurringTransferInstanceProjector.cs
└── Identity/              ← Cross-cutting identity abstraction
    └── IUserContext.cs
```

`IAutoRealizeService` should be evaluated for whether it belongs in `Domain/Services/` or `Application/` — if it orchestrates multiple domain objects with I/O, it's an application concern.

---

## User Stories

### US-077-001: Separate Non-Repository Interfaces
**As a** developer
**I want to** find domain service interfaces in a `Services/` folder, not mixed with repositories
**So that** the domain model structure is clear and self-documenting.

**Acceptance Criteria:**
- [ ] `ITransactionMatcher` moved to `Domain/Services/`
- [ ] `IRecurringInstanceProjector` moved to `Domain/Services/`
- [ ] `IRecurringTransferInstanceProjector` moved to `Domain/Services/`
- [ ] `IUserContext` moved to `Domain/Identity/` (or `Domain/Services/`)
- [ ] `IAutoRealizeService` evaluated and moved appropriately
- [ ] Namespaces updated to match new folder locations
- [ ] All references across all projects updated
- [ ] All tests pass

---

## Technical Design

### Namespace Changes

| Interface | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| `ITransactionMatcher` | `BudgetExperiment.Domain.Repositories` | `BudgetExperiment.Domain.Services` |
| `IRecurringInstanceProjector` | `BudgetExperiment.Domain.Repositories` | `BudgetExperiment.Domain.Services` |
| `IRecurringTransferInstanceProjector` | `BudgetExperiment.Domain.Repositories` | `BudgetExperiment.Domain.Services` |
| `IUserContext` | `BudgetExperiment.Domain.Repositories` | `BudgetExperiment.Domain.Identity` |
| `IAutoRealizeService` | `BudgetExperiment.Domain.Repositories` | `BudgetExperiment.Domain.Services` or `Application` |

### Impact

- `GlobalUsings.cs` files may need updated `using` statements
- DI registrations unchanged (implementations stay in their current projects)
- Infrastructure implementations reference the interface — namespace imports need updating
- Application services reference the interface — namespace imports need updating

---

## Implementation Plan

### Phase 1: Create Folders and Move Interfaces

**Objective:** Create `Domain/Services/` and `Domain/Identity/` folders and move interfaces.

**Tasks:**
- [ ] Create `src/BudgetExperiment.Domain/Services/` folder
- [ ] Create `src/BudgetExperiment.Domain/Identity/` folder
- [ ] Move `ITransactionMatcher.cs` → `Domain/Services/`
- [ ] Move `IRecurringInstanceProjector.cs` → `Domain/Services/`
- [ ] Move `IRecurringTransferInstanceProjector.cs` → `Domain/Services/`
- [ ] Move `IUserContext.cs` → `Domain/Identity/`
- [ ] Evaluate `IAutoRealizeService` placement and move
- [ ] Update namespaces in moved files
- [ ] Update all references across solution
- [ ] Update `GlobalUsings.cs` files if needed
- [ ] Verify build and tests

**Commit:**
```bash
git commit -m "refactor(domain): organize interfaces into Services/ and Identity/ folders

- Move ITransactionMatcher, IRecurringInstanceProjector, IRecurringTransferInstanceProjector to Domain/Services/
- Move IUserContext to Domain/Identity/
- Repositories/ now contains only data access abstractions
- Namespaces updated to match folder structure

Refs: #077"
```

---

## Testing Strategy

### Verification
- [ ] `dotnet build` succeeds for entire solution
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No runtime namespace resolution issues

---

## Risk Assessment

- **Low risk**: Namespace-only change. No behavior modification.
- **Merge conflicts**: Minimal — only touching `using` statements and file locations.

---

## References

- Coding standard §7 (ISP): "Lean interfaces (split broad repository behaviors as needed)."
- Coding standard §2: Architecture layers.

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
