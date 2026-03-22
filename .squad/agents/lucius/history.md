# Project Context

- **Owner:** Fortinbra
- **Project:** BudgetExperiment — .NET 10 budgeting application
- **Stack:** .NET 10, ASP.NET Core Minimal API, Blazor WebAssembly (plain, no FluentUI), EF Core + Npgsql (PostgreSQL), xUnit + Shouldly, NSubstitute/Moq (one, consistent), StyleCop.Analyzers
- **Created:** 2026-03-22

## Architecture

Clean/Onion hybrid. Projects: Domain, Application, Infrastructure, Api, Client, Contracts, Shared.
Tests under `tests/` mirror the src structure.

## Key Conventions

- TDD: RED → GREEN → REFACTOR
- `dotnet add <csproj> package <name> --version <ver>` — never hand-edit PackageReference blocks
- Warnings as errors, StyleCop enforced, nullable reference types enabled
- One top-level type per file
- Private fields: `_camelCase`, async methods end with `Async`
- REST: `/api/v{version}/{resource}`, URL segment versioning, v1 start
- All DateTime UTC; DateOnly for date-only fields
- No FluentAssertions, no AutoFixture, no AutoMapper
- Migrations live in Infrastructure; EF types never leave Infrastructure

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-03-22: Backend Code Quality Deep-Dive Review

Conducted comprehensive backend code review (Domain, Application, Infrastructure, API). Key findings:

**Architecture Quality: B+ (Strong fundamentals with targeted improvements needed)**

- **Domain Layer**: Rich, behavior-focused entities with proper value objects. No infrastructure leakage detected. 21 repository interfaces, 4 domain services, comprehensive scope management (Shared/Personal). Zero anemic models found.
- **Application Layer**: 87 services properly orchestrating domain objects. Explicit mapping (no AutoMapper). Main issue: 54 methods exceed 20-line guideline (6 with critical 3+ level nesting).
- **Infrastructure Layer**: Excellent EF Core fluent configuration isolates persistence concerns. Repository scope filtering prevents cross-tenant data leaks. Optimistic concurrency via PostgreSQL xmin properly configured.
- **API Layer**: Textbook REST with proper HTTP verbs/status codes, DTOs-only exposure, RFC 7807 Problem Details, ETag concurrency support. OpenAPI + Scalar configured correctly.

**Critical Issues (Address First):**
1. Six methods with 3+ nesting levels need immediate refactoring (TransactionListService recurring instance methods, ImportExecuteRequestValidator, RuleSuggestionResponseParser, etc.)
2. ExceptionHandlingMiddleware uses brittle string matching instead of DomainException.ExceptionType enum
3. Three services registered as both interface + concrete ("backward compatibility" with no justification found)

**Positive Highlights:**
- Zero infrastructure types in Domain (perfect abstraction)
- Comprehensive scope filtering at repository level (security win)
- No primitive obsession (proper value object usage throughout)
- No commented-out code found
- StyleCop warnings-as-errors enforced with zero global suppressions

**Method Length Statistics:** 54 violations (40 in Application, 19 in Infrastructure configs, ~10 spread across layers). Configuration files are acceptable (EF fluent API verbosity). Priority: Refactor 6 critically nested methods, then tackle 26 methods in 21-40 line range.

**Technical Debt:** Manageable. Main issue is method extraction discipline, not architectural problems. No refactoring needed at layer boundaries. Incremental cleanup viable without breaking changes.
