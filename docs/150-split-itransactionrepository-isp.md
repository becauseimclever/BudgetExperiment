# Feature 150: Split ITransactionRepository (ISP Violation — 23 Methods)

> **Status:** Done  
> **Severity:** 🟠 High — F-004  
> **Audit Source:** `docs/audit/2026-04-09-full-principle-audit.md`

---

## Overview

`ITransactionRepository` has grown to 23 methods spanning date-range queries, import-specific operations, analytics, and reconciliation concerns. This violates the Interface Segregation Principle: any consumer (test fake, alternative implementation, new service) must implement all 23 methods regardless of which subset it needs. This feature splits the interface into three focused sub-interfaces, retains `ITransactionRepository` as a composition root for backward compatibility, and updates all consumers.

This is a pure structural refactor. No behavior changes, no new endpoints, no migrations.

---

## Problem Statement

### Current State

- `ITransactionRepository` declares 23 methods: 3 inherited from `IReadRepository<Transaction>`, 2 from `IWriteRepository<Transaction>`, and 18 additional domain-specific query/analytics methods.
- At least 4 distinct concerns coexist: read queries, import operations, analytics, and reconciliation.
- Every test fake must implement all 23 methods (even those testing only date-range queries need to stub analytics methods).
- `TransactionRepository.cs` is 495 lines — a direct consequence of the interface breadth (F-014).

### Target State

- **`ITransactionQueryRepository`** — date-range queries, daily totals, paged unified queries, transaction search.
- **`ITransactionImportRepository`** — duplicate detection, batch queries, import-specific lookups.
- **`ITransactionAnalyticsRepository`** — health analysis, spending by category, account balance queries.
- **`ITransactionRepository`** — composition root that inherits all three focused interfaces plus `IReadRepository<Transaction>` and `IWriteRepository<Transaction>`. Existing consumers that inject `ITransactionRepository` continue to work unchanged.
- `TransactionRepository` implements `ITransactionRepository` (and therefore all sub-interfaces).
- Services that only need query operations inject `ITransactionQueryRepository`; import services inject `ITransactionImportRepository`; analytics services inject `ITransactionAnalyticsRepository`.

---

## User Stories

### US-150-001: Focused Interface for Query Consumers

**As a** developer writing a report service  
**I want** to inject `ITransactionQueryRepository`  
**So that** my test fake only needs to implement ~6 methods instead of 23

**Acceptance Criteria:**
- [ ] `ITransactionQueryRepository` exists with date-range, paged, and search query methods
- [ ] Report and calendar services that only need queries are updated to inject `ITransactionQueryRepository`
- [ ] Test fakes for those services implement only `ITransactionQueryRepository`

### US-150-002: Focused Interface for Import Consumers

**As a** developer writing import pipeline tests  
**I want** to inject `ITransactionImportRepository`  
**So that** import test fakes only implement import-relevant methods

**Acceptance Criteria:**
- [ ] `ITransactionImportRepository` exists with duplicate detection and batch query methods
- [ ] Import services updated to inject `ITransactionImportRepository`

### US-150-003: Backward-Compatible Composition Root

**As a** developer maintaining existing code  
**I want** `ITransactionRepository` to remain valid  
**So that** services already injecting `ITransactionRepository` do not break

**Acceptance Criteria:**
- [ ] `ITransactionRepository` still exists and inherits all three focused interfaces
- [ ] `TransactionRepository` implements `ITransactionRepository`
- [ ] No existing service must change unless it benefits from a narrower interface

---

## Technical Design

### Interface Hierarchy

```
IReadRepository<Transaction>   IWriteRepository<Transaction>
         │                              │
         └──────────────┬───────────────┘
                        │
              ITransactionQueryRepository
              ITransactionImportRepository
              ITransactionAnalyticsRepository
                        │
              ITransactionRepository   ←── composition root (inherits all)
                        │
              TransactionRepository    ←── single concrete implementation
```

### Method Distribution (Indicative — confirm against actual interface)

**ITransactionQueryRepository** (target: ~6–8 methods):
- `GetByDateRangeAsync`
- `GetByAccountAndDateRangeAsync`
- `GetDailyTotalsAsync`
- `GetUnifiedPagedAsync`
- `GetUncategorizedPagedAsync`
- `GetByIdAsync` (from IReadRepository)

**ITransactionImportRepository** (target: ~4–6 methods):
- `GetDuplicateCandidatesAsync`
- `GetByImportBatchAsync`
- `GetRecentByAccountAsync`
- `ExistsAsync` (import duplicate check variant)

**ITransactionAnalyticsRepository** (target: ~4–6 methods):
- `GetSpendingByCategoryAsync`
- `GetAccountBalanceSnapshotAsync`
- `GetDataHealthMetricsAsync`
- `GetByReconciliationAsync` (if reconciliation-specific)

### Files to Create

- `src/BudgetExperiment.Domain/Repositories/ITransactionQueryRepository.cs`
- `src/BudgetExperiment.Domain/Repositories/ITransactionImportRepository.cs`
- `src/BudgetExperiment.Domain/Repositories/ITransactionAnalyticsRepository.cs`

### Files to Modify

- `src/BudgetExperiment.Domain/Repositories/ITransactionRepository.cs` — reduce own declarations, add interface inheritance
- `src/BudgetExperiment.Infrastructure/Persistence/Repositories/TransactionRepository.cs` — no logic changes; ensure it still satisfies `ITransactionRepository` (which composes all three)
- Application services: update constructor injection to narrowest applicable interface
- DI registrations: add `services.AddScoped<ITransactionQueryRepository, TransactionRepository>()` etc. alongside existing `ITransactionRepository` registration

---

## Implementation Plan

### Phase 1: Audit and Classify Methods

**Tasks:**
- [ ] Read `ITransactionRepository.cs` in full — list all 23 method signatures with their concern labels (query / import / analytics / write)
- [ ] Read each Application service that injects `ITransactionRepository` — record which subset of methods each service uses
- [ ] Produce method-to-interface mapping (used in Phase 2)

**No code changes in this phase. Output: documented method map.**

---

### Phase 2: Create Focused Interfaces

**Tasks:**
- [ ] Create `ITransactionQueryRepository.cs` — move query methods; XML doc each method
- [ ] Create `ITransactionImportRepository.cs` — move import methods; XML doc each method
- [ ] Create `ITransactionAnalyticsRepository.cs` — move analytics methods; XML doc each method
- [ ] Update `ITransactionRepository.cs`:
  - Remove moved method declarations
  - Add `ITransactionQueryRepository, ITransactionImportRepository, ITransactionAnalyticsRepository` to inheritance list
- [ ] `dotnet build src/BudgetExperiment.Domain/` — zero errors

**Commit:**
```
refactor(domain): split ITransactionRepository into focused sub-interfaces

Add ITransactionQueryRepository, ITransactionImportRepository,
ITransactionAnalyticsRepository. ITransactionRepository retains all
via composition for backward compatibility.

Closes F-004 (2026-04-09 audit)
Refs: §7 Engineering Guide (ISP)
```

---

### Phase 3: Update Infrastructure

**Tasks:**
- [ ] Verify `TransactionRepository.cs` compiles — it already implements all methods via `ITransactionRepository`; no logic changes needed
- [ ] Register new interface mappings in DI:
  ```csharp
  services.AddScoped<ITransactionQueryRepository, TransactionRepository>();
  services.AddScoped<ITransactionImportRepository, TransactionRepository>();
  services.AddScoped<ITransactionAnalyticsRepository, TransactionRepository>();
  // ITransactionRepository registration remains unchanged
  ```
- [ ] `dotnet build src/BudgetExperiment.Infrastructure/` — zero errors

---

### Phase 4: Update Application Service Consumers

**Tasks:**
- [ ] For each Application service that injects `ITransactionRepository`:
  - Check which sub-interface is sufficient (from Phase 1 method map)
  - If only query methods are used → change injection to `ITransactionQueryRepository`
  - If mixed → leave as `ITransactionRepository` (acceptable)
- [ ] Update corresponding test fakes to implement narrower interfaces
- [ ] `dotnet build` (full solution) — zero errors
- [ ] `dotnet test --filter "Category!=Performance"` — all green

**Commit:**
```
refactor(app): narrow ITransactionRepository injection to sub-interfaces where possible

Services that only need query operations now inject ITransactionQueryRepository.
Import services inject ITransactionImportRepository.
Test fakes simplified accordingly.

Refs: F-004, §7 Engineering Guide (ISP)
```

---

### Phase 5: Verification

**Tasks:**
- [ ] Run full suite: `dotnet test --filter "Category!=Performance"` — all green
- [ ] Run `dotnet format` — no violations
- [ ] Confirm no `ITransactionRepository` injections remain in services that only needed a sub-interface

---

## Testing Strategy

### Existing Tests

All existing tests pass unchanged — `TransactionRepository` still satisfies `ITransactionRepository`. No behavior changes.

### New Test Fakes

- Each focused interface should have a corresponding minimal fake used in Application unit tests (e.g., `FakeTransactionQueryRepository` implementing only `ITransactionQueryRepository`).
- Reduces test setup verbosity: fakes now implement ~6 methods instead of 23.

---

## Migration Notes

No database changes. No API contract changes. No consumer-visible behavior changes. The refactor is purely structural.

---

## References

- [2026-04-09 Full Principle Audit — F-004](../docs/audit/2026-04-09-full-principle-audit.md#f-004-high--isp-violation-itransactionrepository-has-23-methods)
- Engineering Guide §7 (ISP — "Lean interfaces (split broad repository behaviors as needed)")
- Engineering Guide §7 (example: `IReadRepository<T>`, `IWriteRepository<T>`)
- F-014 (TransactionRepository god class — splitting the interface naturally splits the implementation)

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-04-09 | Initial spec from Vic audit F-004 | Alfred (Lead) |
