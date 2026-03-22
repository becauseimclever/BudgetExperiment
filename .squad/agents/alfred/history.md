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
