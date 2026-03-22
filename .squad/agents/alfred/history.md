# Project Context

- **Owner:** Fortinbra
- **Project:** BudgetExperiment — .NET 10 budgeting application
- **Stack:** .NET 10, ASP.NET Core Minimal API, Blazor WebAssembly (plain, no FluentUI), EF Core + Npgsql (PostgreSQL), xUnit + Shouldly, NSubstitute/Moq (one, consistent), StyleCop.Analyzers
- **Created:** 2026-03-22

## Architecture

Clean/Onion hybrid. Layers (outer → inner, dependencies flow inward only):
- `BudgetExperiment.Client` — Blazor WebAssembly UI
- `BudgetExperiment.Api` — REST API, DI wiring, OpenAPI + Scalar, versioning, error handling
- `BudgetExperiment.Application` — use cases, services, validators, mapping, domain event handlers
- `BudgetExperiment.Domain` — entities, value objects, enums, domain events, interfaces (NO EF types)
- `BudgetExperiment.Contracts` — shared DTOs/request/response types
- `BudgetExperiment.Shared` — shared enums (BudgetScope, CategorySource, DescriptionMatchMode)
- `BudgetExperiment.Infrastructure` — EF Core DbContext, repository implementations, migrations

Tests mirror structure under `tests/`.

## Key Conventions

- TDD: RED → GREEN → REFACTOR, always
- Warnings as errors, StyleCop enforced
- One top-level type per file, filename matches type name
- REST endpoints: `/api/v{version}/{resource}` (URL segment versioning, start at v1)
- All DateTime UTC, use `DateTime.UtcNow`
- No FluentAssertions, no AutoFixture
- Exclude `Category=Performance` tests by default
- Private fields: `_camelCase`

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-06-XX — Code Quality Review Feature Docs (Docs 121–124)

**Review Scope:** Full code quality review findings from branch `feature/code-quality-review`, grouped into four actionable feature docs.

**Key Findings & Decisions:**

1. **Both test database strategies are wrong.** Infrastructure tests use EF Core's in-memory provider; API tests use `UseInMemoryDatabase()`. Both miss PostgreSQL-specific behaviour. Feature 121 (Testcontainers) is the correct fix and is a prerequisite for meaningful repository integration tests.

2. **Test coverage gaps are concentrated in newer features.** `RecurringChargeSuggestionsController` and `RecurringController` (two endpoints) have zero test coverage. Four repositories (`AppSettingsRepository`, `CustomReportLayoutRepository`, `RecurringChargeSuggestionRepository`, `UserSettingsRepository`) are also untested. These should land together with the Testcontainers migration to avoid testing against in-memory from day one.

3. **Vanity enum tests inflate coverage metrics without providing regression value.** ~20 tests assert `(int)Enum.Member == N`. These should be removed; only replace with a serialisation-contract test if the integer value is part of a stored or transmitted contract.

4. **`ExceptionHandlingMiddleware` string matching is a latent bug.** The domain already has `DomainException.ExceptionType`. Switching on the enum is strictly superior. This was the highest-risk backend quality finding.

5. **DIP is applied judiciously per Fortinbra's directive.** Feature 124 requires explicit assessment per controller before extracting interfaces — not blanket extraction. Controllers with no realistic substitution scenario and no unit-test isolation need retain their concrete dependencies; the decision is recorded in `decisions.md`.

6. **`this._field` style conflicts with `_camelCase` convention.** StyleCop `SA1101` (if enabled) directly conflicts with `IDE0003`. Must check `stylecop.json` before applying the `.editorconfig` fix to avoid a rule collision that produces contradictory warnings.

### Cross-Agent Note: DI Findings (2026-03-22T10-04-29)

**From Lucius (Backend):** Investigation of three "backward compatibility" concrete DI registrations revealed they are all **legitimately needed**. Controllers inject the concrete types directly:
- `TransactionsController` → `TransactionService`
- `RecurringTransactionsController` → `RecurringTransactionService`  
- `RecurringTransfersController` → `RecurringTransferService`

Feature Doc 124 (Controller Abstractions Assessment) should note this finding when assessing DIP for each controller. Current concrete injection is load-bearing; refactoring requires controller changes, not just interface extraction.

### 2026-03-22 — Full Architecture Review

**Review Scope:** Complete solution architecture and code quality assessment.

**Key Findings:**

1. **Architecture is solid.** Layer boundaries are correctly enforced. Domain has no EF Core types. Dependencies flow inward only. IUserContext interface in Domain with implementation in API layer — proper DIP.

2. **DIP violations in 3 controllers.** `TransactionsController`, `AccountsController`, and `RecurringTransactionsController` inject concrete service classes instead of interfaces. Low impact but should be corrected for consistency and testability.

3. **Mixed `this._field` style.** Some services use `this._fieldName`, others use just `_fieldName`. Both valid but inconsistent. Recommend enforcing one style via `.editorconfig`.

4. **ImportService has 14 dependencies.** Borderline SRP concern, but the service properly delegates to focused sub-services. Acceptable orchestration pattern.

5. **Build is clean.** Zero warnings, StyleCop enforced, nullable reference types enabled, warnings-as-errors active.

6. **No forbidden libraries detected.** No FluentAssertions, AutoFixture, AutoMapper, or FluentUI-Blazor.

**Overall Assessment:** The codebase demonstrates mature Clean Architecture adherence. Issues found are minor and easily fixable. No critical violations.

### 2026-03-22 — DIP Assessment for Three Concrete-Injecting Controllers

**Task:** Pragmatic DIP assessment per Feature Doc 124 for `TransactionsController`, `RecurringTransactionsController`, and `RecurringTransfersController`.

**Finding:** All three controllers received **VERDICT A: Add interface**. However, the "cost" is effectively zero because:

1. **The interfaces already exist** (`ITransactionService`, `IRecurringTransactionService`, `IRecurringTransferService`)
2. **They're already registered in DI** with interface→concrete mappings
3. **The controllers just use the wrong type** (concrete instead of interface)

**Root Cause:** The interfaces were not kept in sync with the concrete classes. As new methods were added to the services (e.g., `SkipNextAsync`, `UpdateFromDateAsync`, `PauseAsync`, `ResumeAsync`), they weren't added to the interfaces. The controllers then had to inject concrete types to access those methods, and duplicate concrete DI registrations were added as a workaround.

**Required Fixes:**
1. Expand `IRecurringTransactionService` with `SkipNextAsync`, `UpdateFromDateAsync`
2. Expand `IRecurringTransferService` with `UpdateAsync`, `DeleteAsync`, `PauseAsync`, `ResumeAsync`, `SkipNextAsync`, `UpdateFromDateAsync`
3. Update controller constructors to use interface types
4. Remove duplicate concrete DI registrations
5. Update test mocks to implement new interface methods

**Pragmatic SOLID Application:** The directive says interfaces should "earn their cost." Here, the interfaces already exist — using them costs nothing. Leaving concrete injection in place when interfaces exist and are registered is technical debt, not pragmatism.

**Pre-existing Issues Noted:** Repository has multiple unrelated build errors (IUnitOfWork.MarkAsModified not implemented, SA1117/SA1127/SA1210 StyleCop violations) that block `-warnaserror` builds.

### 2026-03-22T18-23-42Z — Session Close: Batch 2+3 Complete

**Cross-Team Summary:**

- **From Lucius:** Flattened 9 deeply nested methods (improved readability), removed 1,474 `this._` usages (reduced verbosity), expanded 2 service interfaces (+8 methods total), switched 3 controllers to interface injection. Service interfaces now complete for DIP assessment.
- **From Barbara:** Testcontainers migration complete; real concurrency bug found and fixed in `IUnitOfWork.MarkAsModified`. API tests now run against PostgreSQL. 55 new high-value tests added. Vanity enum tests cleaned up (12 deleted).
- **From Coordinator:** 5,409 tests passing, 0 build warnings. All assertion bugs fixed. PR ready for merge.

**Assessment Outcome:** DIP verdicts delivered (Verdict A for all 3 controllers). Interfaces already existed but were incomplete — work prioritized to Lucius for expansion. All 3 controllers now ready for interface injection transition.
