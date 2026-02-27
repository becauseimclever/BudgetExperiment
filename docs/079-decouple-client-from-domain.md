# Feature 079: Decouple Client from Domain Layer
> **Status:** Planning
> **Priority:** High (architecture / separation of concerns)
> **Estimated Effort:** Large (3-5 days)
> **Dependencies:** Feature 075 (one-type-per-file may ease merge conflicts)

## Overview

The coding standard (§2, §19) requires the presentation layer to use DTOs, never domain entities. An audit found that the Blazor WebAssembly client (`BudgetExperiment.Client`) directly references `BudgetExperiment.Domain` and imports **11 domain namespaces** globally. At least **27 Razor files** and **2 service files** use domain types directly, creating tight coupling between the UI and the domain model.

This means changes to domain entities (renaming, restructuring, adding validation) directly impact the Client project, violating the dependency inversion principle and making the domain harder to evolve independently.

## Problem Statement

### Current State

**Project reference** in `BudgetExperiment.Client.csproj`:
```xml
<ProjectReference Include="..\BudgetExperiment.Domain\BudgetExperiment.Domain.csproj" />
```

**Global imports** in `Client/GlobalUsings.cs` (11 domain namespaces):
- `BudgetExperiment.Domain.Accounts`
- `BudgetExperiment.Domain.Budgeting`
- `BudgetExperiment.Domain.Categorization`
- `BudgetExperiment.Domain.Chat`
- `BudgetExperiment.Domain.Common`
- `BudgetExperiment.Domain.Import`
- `BudgetExperiment.Domain.Paycheck`
- `BudgetExperiment.Domain.Reconciliation`
- `BudgetExperiment.Domain.Recurring`
- `BudgetExperiment.Domain.Repositories`
- `BudgetExperiment.Domain.Settings`

Common types used from Domain in Client:
- **Enums**: `AccountType`, `CategoryType`, `RecurrenceFrequency`, `ChatRole`, `ImportField`, `AmountParseMode`, `BudgetScope`, `SuggestionStatus`, etc.
- **Value Objects**: `MoneyValue`, `RecurrencePattern`, etc.
- **Entities**: Properties/types from `Account`, `Transaction`, etc. (indirect through models)

### Target State

- `BudgetExperiment.Client` references ONLY `BudgetExperiment.Contracts` (no Domain reference)
- All domain enums used by the client are either:
  - Mirrored in Contracts as shared enums, OR
  - Already defined in Contracts DTOs
- Client components display DTO properties, not domain entity properties
- Domain project can evolve without impacting Client compilation

---

## User Stories

### US-079-001: Move Shared Enums to Contracts
**As a** developer
**I want to** shared enums (used by both API and Client) defined in Contracts
**So that** the Client doesn't need to reference Domain for enum types.

**Acceptance Criteria:**
- [ ] All domain enums used by Client are available via Contracts
- [ ] Domain can either re-export or define its own copy (evaluate duplication vs. shared project trade-off)
- [ ] No behavior change

### US-079-002: Replace Domain Type Usage in Client
**As a** developer
**I want to** all Client Razor files and services to use Contracts DTOs instead of Domain types
**So that** the Client has no compile-time dependency on Domain.

**Acceptance Criteria:**
- [ ] All 27+ Razor files updated to use DTO/Contracts types
- [ ] All Client service files updated
- [ ] `_Imports.razor` has no `@using BudgetExperiment.Domain` lines
- [ ] `GlobalUsings.cs` has no Domain namespace imports
- [ ] `BudgetExperiment.Client.csproj` has no `ProjectReference` to Domain
- [ ] Build succeeds
- [ ] UI behavior unchanged

---

## Technical Design

### Strategy Options

**Option A: Move shared enums to Contracts** (Recommended)
- Move enums like `AccountType`, `CategoryType` etc. from Domain to Contracts
- Domain references Contracts (already does via Application → Contracts)
- Minimal duplication, single source of truth

**Option B: Duplicate enums in Contracts**
- Create copies of domain enums in Contracts namespace
- Map between them in Application mappers
- More separation but duplication overhead

**Option C: Create a shared Enums project**
- New `BudgetExperiment.Shared` or similar
- Overkill for current scope

**Recommendation: Option A** — The Contracts project already exists and is referenced by both Domain and Client. Moving shared enums there preserves single source of truth.

### Audit of Domain Types Used in Client

A detailed per-file audit is needed to catalog every domain type reference. Categories:

1. **Enums** (~15-20 enum types) — most common usage, easiest to migrate
2. **Value objects** (e.g., `MoneyValue`) — may need DTO equivalents in Contracts
3. **Entity types** — should already be represented by DTOs
4. **Interface types** (e.g., `IUserContext`) — should not be used in Client at all

---

## Implementation Plan

### Phase 1: Audit All Domain References in Client

**Objective:** Create exhaustive list of every domain type used in Client.

**Tasks:**
- [ ] Grep all `.razor`, `.cs` files in Client for domain type usage
- [ ] Categorize each type (enum, value object, entity, interface)
- [ ] Determine if Contracts already has an equivalent DTO
- [ ] Plan migration for each type

### Phase 2: Move Shared Enums to Contracts

**Objective:** Make shared enums available without Domain reference.

**Tasks:**
- [ ] Move or re-export enum types to Contracts
- [ ] Update Domain to reference Contracts enums (or keep originals with aliasing)
- [ ] Update Client imports to use Contracts namespace
- [ ] Verify build

### Phase 3: Replace Remaining Domain References

**Objective:** Replace all remaining domain type usage with Contracts types.

**Tasks:**
- [ ] Update Razor components to use DTO types
- [ ] Update Client services
- [ ] Add missing DTO types to Contracts if needed
- [ ] Verify build

### Phase 4: Remove Domain Reference

**Objective:** Cut the dependency.

**Tasks:**
- [ ] Remove `ProjectReference` to Domain from Client `.csproj`
- [ ] Remove domain `using` statements from `GlobalUsings.cs` and `_Imports.razor`
- [ ] Final build verification
- [ ] Full test suite green
- [ ] Manual UI smoke test

**Commit:**
```bash
git commit -m "refactor(client): decouple Client from Domain layer

- Move shared enums to Contracts project
- Replace all Domain type references with Contracts DTOs
- Remove BudgetExperiment.Domain project reference from Client
- Client now depends only on Contracts for shared types

BREAKING CHANGE: Enum namespaces changed from Domain to Contracts

Refs: #079"
```

---

## Testing Strategy

### Unit Tests
- [ ] Verify any moved enums have same values (prevent accidental reordering)
- [ ] Client service tests (if any) still pass

### Integration Tests
- [ ] API tests pass (enum serialization unchanged)

### Manual Testing
- [ ] All Client pages render correctly
- [ ] Dropdowns with enum values display correctly
- [ ] Form submissions work

---

## Risk Assessment

- **High risk**: Large number of files affected. Enum namespace changes touch both server and client code.
- **Breaking change**: If enum namespaces change, serialization/deserialization must be tested carefully.
- **Mitigation**: Move enums without renaming them; use same type names in new namespace.

---

## References

- Coding standard §2: "Client (Blazor WebAssembly UI) – presentation layer."
- Coding standard §19: "Controllers expose DTOs, never domain entities."
- Coding standard §5 (DIP): "Higher layers depend on abstractions in Domain/Application."

---

## Changelog

| Date | Change | Author |
|------|--------|--------|
| 2026-02-26 | Initial draft from codebase audit | @copilot |
