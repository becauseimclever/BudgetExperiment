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

**Technical Debt:** Manageable. Main issue is method extraction discipline, not architectural problems. No refactoring at layer boundaries needed. Incremental cleanup viable without breaking changes.

### 2026-03-22: Three Quick-Win Code Quality Fixes Applied

**Fix 1: ExceptionHandlingMiddleware — enum-based routing (done)**

- Created `DomainExceptionType` enum (`Validation = 0`, `NotFound = 1`) in `BudgetExperiment.Domain.Common`.
- Updated `DomainException` to accept an optional `DomainExceptionType` parameter (defaults to `Validation` so all existing callers without a type remain valid).
- Updated `ExceptionHandlingMiddleware` to `switch (domainEx.ExceptionType)` — no more string matching.
- Updated all 17 "not found" throw sites across Domain, Application (Recurring, Accounts, Import, Categorization) to pass `DomainExceptionType.NotFound`.
- Updated existing middleware test to use typed constructor.

**Fix 2: Redundant DI registrations — comments corrected (done)**

- Investigated all three "backward compat" concrete registrations: `TransactionService`, `RecurringTransactionService`, `RecurringTransferService`.
- Found concrete consumers in the API controllers (`TransactionsController`, `RecurringTransactionsController`, `RecurringTransfersController` each inject the concrete type directly).
- Updated comments in `DependencyInjection.cs` to name the actual consumer rather than vague "backward compatibility".
- No registrations removed — all three are legitimately needed.

**Fix 3: DateTime.Now → DateTime.UtcNow in Reconciliation.razor (done)**

- Replaced `DateTime.Now.Month/.Year` (3 occurrences) with `DateTime.UtcNow` for field initializers and year-range loop.

**Also fixed (pre-existing build error):**
- `PostgreSqlFixture.cs`: Updated `new PostgreSqlBuilder()` → `new PostgreSqlBuilder("postgres:16")` (obsolete parameterless constructor).

**Result:** Build clean (0 warnings, 0 errors), 5415 tests pass, 1 pre-existing skip.

### Cross-Agent Note: DI Validation & Architectural Clarity (2026-03-22T10-04-29)

**Finding:** All three concrete service registrations (`TransactionService`, `RecurringTransactionService`, `RecurringTransferService`) are load-bearing — controllers directly inject the concrete types. Comments clarified to name actual consumers.

**Relevance to Feature Doc 124:** When Alfred assesses DIP for `TransactionsController`, `RecurringTransactionsController`, and other controller abstractions, these findings provide the DI implementation context. Extracting interfaces requires changes to both service registration and controller injection sites. Assessment should use pragmatic directive: interface only if realistic substitution scenario exists.
