# Feature 124: Controller Abstractions Assessment and Style Consistency
> **Status:** Pending

## Overview

Two related housekeeping items surfaced during the code quality review: three API controllers depend directly on concrete service classes rather than interfaces, and private field naming is inconsistent across Application services (`this._field` in some, `_field` in others). This document assesses the DIP concern pragmatically and defines a plan to enforce consistent field-access style via `.editorconfig`.

## Problem Statement

### Current State

**DIP: Concrete dependencies in controllers**

`TransactionsController`, `AccountsController`, and `RecurringTransactionsController` inject concrete service classes (e.g., `TransactionService`, `AccountService`) directly via constructor parameters rather than interfaces. This was noted in the March 2026 architecture review.

The project convention (per engineering guidelines §7, DIP) is that higher layers depend on abstractions. However, Fortinbra's stated directive for this review is: *"Apply SOLID principles judiciously. Add interfaces/abstractions when they earn their weight; skip when the added complexity doesn't justify the benefit. A single concrete service with no realistic substitution scenario doesn't need an interface just to satisfy DIP."*

Assessment is therefore required before work begins: do these services have realistic substitution scenarios that justify extraction?

**Style: `this._field` vs `_field`**

Some Application service classes access private fields with the explicit `this.` qualifier (`this._repository`, `this._logger`). Others use the bare form (`_repository`, `_logger`). Both compile and StyleCop does not flag one over the other by default, but the inconsistency makes the codebase feel unowned and slightly complicates future code generation / refactoring.

### Target State

- A documented decision exists for each of the three controllers: either introduce an interface (with justification) or record the explicit decision not to (with rationale), keeping the concrete dependency.
- Private field access is consistent across all Application services. The project convention (`_camelCase` per §5) is enforced by `.editorconfig`; the `this.` qualifier is not used for field access.

---

## Acceptance Criteria

### DIP Assessment

- [ ] Each of the three controllers (`TransactionsController`, `AccountsController`, `RecurringTransactionsController`) is assessed: does a realistic substitution scenario exist?
  - A scenario is "realistic" if it is needed for testability (e.g., mocking in unit tests), pluggability (e.g., swapping implementations based on config), or is already anticipated by another feature doc.
- [ ] If yes for a controller: an interface is extracted, the controller depends on the interface, and the DI registration is updated to map interface → concrete.
- [ ] If no for a controller: the concrete dependency is kept and the rationale is recorded in `.squad/decisions.md`.
- [ ] For any interface extracted: unit tests for the controller are written or updated using a test double against the interface.

### Style Consistency

- [ ] `.editorconfig` is updated to add the `dotnet_style_qualification_for_field = false:warning` rule (or equivalent) so that `this._field` style is flagged.
- [ ] All existing `this._field` usages in Application services are updated to `_field`.
- [ ] `dotnet format --verify-no-changes` passes after the style update.
- [ ] No StyleCop or analyzer warnings introduced.

---

## Technical Design

### DIP Pragmatic Framework

Before extracting an interface, answer these questions for each service:

| Question | If Yes → | If No → |
|----------|----------|---------|
| Is the controller unit-tested or planned to be? | Interface aids mocking | No benefit |
| Does any other feature doc plan a second implementation? | Interface required | No benefit |
| Is the service a domain boundary that Infrastructure could implement differently? | Interface appropriate | Concrete is fine |

For application services that are simple orchestrators with no realistic alternative implementation and no current unit test isolation need, the concrete dependency is the pragmatic choice. The engineering guidelines already acknowledge this in §25: *"A single concrete service with no realistic substitution scenario doesn't need an interface just to satisfy DIP."*

### Style Fix

Add to `.editorconfig` in the repository root (under `[*.cs]`):

```ini
# Do not qualify field access with 'this.'
dotnet_style_qualification_for_field = false:warning
dotnet_style_qualification_for_property = false:warning
dotnet_style_qualification_for_method = false:warning
dotnet_style_qualification_for_event = false:warning
```

Then run:

```powershell
dotnet format c:\ws\BudgetExperiment\BudgetExperiment.sln --diagnostics IDE0003
```

This will automatically remove `this.` qualifiers where the rule fires.

---

## Implementation Plan

### Phase 1: DIP Assessment and Decision

**Objective:** Evaluate each controller and record the decision; implement interface extraction only where justified.

**Tasks:**
- [ ] Review `TransactionsController` — does it have (or need) unit tests? Is a second `ITransactionService` implementation plausible?
- [ ] Review `AccountsController` — same assessment
- [ ] Review `RecurringTransactionsController` — same assessment
- [ ] For each controller where an interface is justified:
  - [ ] Extract `IXxxService` interface in the Application layer
  - [ ] Update DI registration: `services.AddScoped<IXxxService, XxxService>()`
  - [ ] Update controller constructor to inject the interface
  - [ ] Write or update controller unit/integration tests using a test double
- [ ] For each controller where the concrete dependency is retained:
  - [ ] Record the decision in `.squad/decisions.md` with rationale
- [ ] Run full test suite — confirm green

**Commit (if interface extracted):**
```bash
git commit -m "refactor(api): extract service interfaces for controllers where substitution is justified

- ITransactionService / IAccountService / IRecurringTransactionService extracted as applicable
- Controllers depend on interfaces; DI wiring updated
- Concrete-dependency decisions recorded in decisions.md

Refs: #124"
```

**Commit (if no interface extracted for a given controller):**
```bash
git commit -m "docs: record DIP assessment decisions for controller service dependencies

- Assessed TransactionsController, AccountsController, RecurringTransactionsController
- Concrete dependencies retained where no realistic substitution scenario exists
- Rationale documented in .squad/decisions.md

Refs: #124"
```

---

### Phase 2: Style Consistency — Remove `this.` Qualifiers

**Objective:** Enforce consistent `_field` (no `this.`) access across Application services via `.editorconfig` and `dotnet format`.

**Tasks:**
- [ ] Add `dotnet_style_qualification_for_field = false:warning` (and related rules) to `.editorconfig`
- [ ] Run `dotnet format --diagnostics IDE0003` to auto-fix existing violations
- [ ] Review the diff — confirm only `this.` removal, no logic changes
- [ ] Run `dotnet build` — confirm zero new warnings
- [ ] Run tests — confirm green

**Commit:**
```bash
git commit -m "style: enforce no this. qualifier for field access

- .editorconfig: dotnet_style_qualification_for_field = false:warning
- dotnet format applied to remove this._field usages in Application services
- No logic changes

Refs: #124"
```

---

## Notes

- The `this.` qualifier issue spans Application services primarily; check Infrastructure services as a secondary sweep.
- If StyleCop's `SA1101` rule (`PrefixLocalCallsWithThis`) is currently enabled in `stylecop.json`, it must be disabled — it conflicts with the `IDE0003` rule enforcing the opposite style. Review `stylecop.json` before applying the `.editorconfig` change.
- Do not introduce interfaces speculatively. The goal is consistent, justifiable architecture — not maximum indirection.
