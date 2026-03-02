# Feature 077: Domain Interface Organization
> **Status:** Done
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

## Vertical Slices

Each slice is independently deliverable: move interface(s), update namespaces, update all references, verify build + tests, commit. Slices are ordered lowest-risk first.

---

### Slice 1: Move `IAutoRealizeService` to `Domain/Services/`

**Risk:** Lowest — 3 source files + 1 test file reference it.

**As a** developer
**I want** `IAutoRealizeService` in a `Services/` folder rather than `Repositories/`
**So that** the domain folder structure accurately reflects each interface's role.

**Analysis:**
- `IAutoRealizeService` has async I/O and orchestrates repos — arguably an application concern.
- However, its *interface* defines a domain capability ("auto-realize past-due items") and the implementation already lives in `Application/Recurring/AutoRealizeService.cs`.
- Decision: Move the **interface** to `Domain/Services/`. The implementation stays in Application. This matches the DIP pattern already used by projectors.

**Namespace Change:**
| Interface | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| `IAutoRealizeService` | `BudgetExperiment.Domain.Repositories` | `BudgetExperiment.Domain.Services` |

**Affected Files (4):**
| File | Usage |
|------|-------|
| `Application/Recurring/AutoRealizeService.cs` | Implementation |
| `Application/Calendar/CalendarGridService.cs` | Constructor injection |
| `Application/DependencyInjection.cs` | DI registration |
| `Application.Tests/CalendarGridServiceTests.cs` | Mock |

**Tasks:**
- [ ] Create `src/BudgetExperiment.Domain/Services/` folder
- [ ] Move `IAutoRealizeService.cs` → `Domain/Services/`
- [ ] Update namespace in moved file to `BudgetExperiment.Domain.Services`
- [ ] Update `using` statements in 4 affected files
- [ ] Update `GlobalUsings.cs` if needed
- [ ] `dotnet build` succeeds
- [ ] All tests pass

**Commit:**
```
refactor(domain): move IAutoRealizeService to Domain/Services/

- Create Domain/Services/ folder for non-repository domain abstractions
- Move IAutoRealizeService from Repositories/ to Services/
- Update namespace to BudgetExperiment.Domain.Services
- Update references in Application and test projects

Refs: #077
```

---

### Slice 2: Move `ITransactionMatcher` to `Domain/Services/`

**Risk:** Low — 6 source files + 3 test files. Implementation (`TransactionMatcher`) already lives in `Domain/Reconciliation/`, confirming this is a pure domain service.

**As a** developer
**I want** `ITransactionMatcher` in `Domain/Services/` rather than `Repositories/`
**So that** pure domain logic interfaces are separated from data access contracts.

**Namespace Change:**
| Interface | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| `ITransactionMatcher` | `BudgetExperiment.Domain.Repositories` | `BudgetExperiment.Domain.Services` |

**Affected Files (8):**
| File | Usage |
|------|-------|
| `Domain/Reconciliation/TransactionMatcher.cs` | Implementation (in Domain) |
| `Application/Reconciliation/ReconciliationService.cs` | Constructor injection |
| `Application/Import/ImportService.cs` | Constructor injection |
| `Application/DependencyInjection.cs` | DI registration |
| `Domain.Tests/...` | Tests for TransactionMatcher |
| `Application.Tests/Services/ImportServiceLocationTests.cs` | Mock |
| `Application.Tests/Services/ImportServiceTests.cs` | Mock |
| `Application.Tests/ReconciliationServiceTests.cs` | Mock |

**Tasks:**
- [ ] Move `ITransactionMatcher.cs` → `Domain/Services/`
- [ ] Update namespace in moved file
- [ ] Update `using` statements in all 8 affected files
- [ ] `dotnet build` succeeds
- [ ] All tests pass

**Commit:**
```
refactor(domain): move ITransactionMatcher to Domain/Services/

- ITransactionMatcher is a pure domain service (sync, no I/O)
- Implementation already in Domain/Reconciliation/TransactionMatcher.cs
- Update namespace and references across Application and test projects

Refs: #077
```

---

### Slice 3: Move Projectors to `Domain/Services/`

**Risk:** Medium — combined ~12 source files + ~8 test files, but the two interfaces are always used together in the same consuming files, making them a natural pair.

**As a** developer
**I want** `IRecurringInstanceProjector` and `IRecurringTransferInstanceProjector` in `Domain/Services/`
**So that** projection/computation interfaces aren't confused with persistence abstractions.

**Namespace Changes:**
| Interface | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| `IRecurringInstanceProjector` | `BudgetExperiment.Domain.Repositories` | `BudgetExperiment.Domain.Services` |
| `IRecurringTransferInstanceProjector` | `BudgetExperiment.Domain.Repositories` | `BudgetExperiment.Domain.Services` |

**Affected Files (~13):**
| File | Usage |
|------|-------|
| `Application/Recurring/RecurringInstanceProjector.cs` | Implementation |
| `Application/Recurring/RecurringTransferInstanceProjector.cs` | Implementation |
| `Application/Calendar/CalendarGridService.cs` | Both injected |
| `Application/Calendar/DayDetailService.cs` | Both injected |
| `Application/Accounts/TransactionListService.cs` | Both injected |
| `Application/Reconciliation/ReconciliationService.cs` | `IRecurringInstanceProjector` injected |
| `Application/Import/ImportService.cs` | `IRecurringInstanceProjector` injected |
| `Application/DependencyInjection.cs` | DI registrations |
| `Application.Tests/CalendarGridServiceTests.cs` | Both mocked |
| `Application.Tests/DayDetailServiceTests.cs` | Both mocked |
| `Application.Tests/TransactionListServiceTests.cs` | Both mocked |
| `Application.Tests/ReconciliationServiceTests.cs` | `IRecurringInstanceProjector` mocked |
| `Application.Tests/Services/ImportServiceTests.cs` | `IRecurringInstanceProjector` mocked |

**Tasks:**
- [ ] Move `IRecurringInstanceProjector.cs` → `Domain/Services/`
- [ ] Move `IRecurringTransferInstanceProjector.cs` → `Domain/Services/`
- [ ] Update namespaces in both moved files
- [ ] Update `using` statements in all ~13 affected files
- [ ] `dotnet build` succeeds
- [ ] All tests pass

**Commit:**
```
refactor(domain): move projector interfaces to Domain/Services/

- Move IRecurringInstanceProjector and IRecurringTransferInstanceProjector
- Projectors compute/project instances, not persist data
- Update namespace and references across Application and test projects

Refs: #077
```

---

### Slice 4: Move `IUserContext` to `Domain/Identity/`

**Risk:** Highest — referenced in **25+ files** across all 4 layers (Domain, API, Application, Infrastructure) plus 7+ test files. This is the widest-reaching change.

**As a** developer
**I want** `IUserContext` in a dedicated `Domain/Identity/` folder
**So that** identity/cross-cutting concerns have their own namespace, separate from both repositories and domain services.

**Namespace Change:**
| Interface | Old Namespace | New Namespace |
|-----------|--------------|---------------|
| `IUserContext` | `BudgetExperiment.Domain.Repositories` | `BudgetExperiment.Domain.Identity` |

**Affected Files (~20+):**
| Layer | Files |
|-------|-------|
| **API** | `UserContext.cs` (impl), `Program.cs` (DI), `BudgetScopeMiddleware.cs`, `ChatController.cs`, `MerchantMappingsController.cs` |
| **Infrastructure** | `AccountRepository.cs`, `BudgetCategoryRepository.cs`, `BudgetGoalRepository.cs`, `CustomReportLayoutRepository.cs`, `ReconciliationMatchRepository.cs`, `RecurringTransactionRepository.cs`, `RecurringTransferRepository.cs`, `TransactionRepository.cs` |
| **Application** | `ImportService.cs`, `ImportMappingService.cs`, `UserSettingsService.cs`, `UserSettingsCurrencyProvider.cs`, `CustomReportLayoutService.cs`, `CategorySuggestionService.cs` |
| **Tests** | 7+ files across `Api.Tests`, `Application.Tests`, `Infrastructure.Tests` |

**Tasks:**
- [ ] Create `src/BudgetExperiment.Domain/Identity/` folder
- [ ] Move `IUserContext.cs` → `Domain/Identity/`
- [ ] Update namespace in moved file
- [ ] Update `using` statements in all ~20+ affected files
- [ ] Add `using BudgetExperiment.Domain.Identity;` to `GlobalUsings.cs` in Api, Application, Infrastructure, and test projects (reduces per-file churn)
- [ ] `dotnet build` succeeds
- [ ] All tests pass

**Commit:**
```
refactor(domain): move IUserContext to Domain/Identity/

- IUserContext is a cross-cutting identity abstraction, not a repository
- Create Domain/Identity/ namespace for user identity concerns
- Update references across all layers (API, Application, Infrastructure, Tests)

Refs: #077
```

---

## Technical Notes

### Global Using Strategy

After completing all slices, evaluate adding these to `GlobalUsings.cs` files to minimize future import churn:

```csharp
// In projects that consume domain services
global using BudgetExperiment.Domain.Services;

// In projects that consume IUserContext
global using BudgetExperiment.Domain.Identity;
```

### DI Registrations

No DI registration changes are needed — implementations stay in their current projects. Only namespace imports on the registration lines need updating.

---

## Testing Strategy

### Per-Slice Verification (after each slice)
- [ ] `dotnet build` succeeds for entire solution
- [ ] All unit tests pass (`dotnet test`)
- [ ] No runtime namespace resolution issues

### Post-Completion Verification (after all 4 slices)
- [ ] `Domain/Repositories/` contains only repository interfaces + `IUnitOfWork` (22 files)
- [ ] `Domain/Services/` contains 4 interfaces: `IAutoRealizeService`, `ITransactionMatcher`, `IRecurringInstanceProjector`, `IRecurringTransferInstanceProjector`
- [ ] `Domain/Identity/` contains 1 interface: `IUserContext`
- [ ] Integration tests pass
- [ ] No orphaned `using BudgetExperiment.Domain.Repositories` imports referencing moved types

---

## Risk Assessment

| Slice | Risk | Blast Radius | Mitigation |
|-------|------|-------------|------------|
| 1 – `IAutoRealizeService` | Low | 4 files | Smallest change; creates the `Services/` folder |
| 2 – `ITransactionMatcher` | Low | 8 files | Pure domain service; impl already in Domain |
| 3 – Projectors | Medium | ~13 files | Always co-located; move as a pair |
| 4 – `IUserContext` | Higher | ~25+ files | Use `GlobalUsings.cs` to reduce churn |

Overall: **Low risk** — namespace-only changes with no behavior modification. Each slice is independently revertible.

---

## References

- Coding standard §7 (ISP): "Lean interfaces (split broad repository behaviors as needed)."
- Coding standard §2: Architecture layers.
- Coding standard §5 (DIP): "Higher layers depend on abstractions in Domain/Application."

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-03-01 | Restructured as vertical slices ordered by risk | Copilot |
| 2026-02-26 | Initial draft from codebase audit | @copilot |
