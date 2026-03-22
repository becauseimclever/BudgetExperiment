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


## Learnings

### Nesting Flattening Session (2025)
- **Guard clauses are the primary tool**: Inverting conditions to return/throw early eliminates one nesting level per guard without adding abstraction overhead.
- **LINQ FirstOrDefault > foreach+if**: ules.FirstOrDefault(r => r.Matches(description)) is cleaner and more idiomatic than a foreach with an inner if returning early.
- **Extract tiny named methods**: IsRealizedAsTransaction, TryBuildLocationFromMatch, ParseEntityId — each does one thing. The calling code reads like prose.
- **StyleCop ordering rules matter at refactor time**: SA1204 (static before non-static) and SA1202 (internal before private) must be respected when inserting new methods. Place static helpers in the right access group up front.
- **Python for disk file manipulation**: The iew/dit tool in the Copilot CLI operates on a virtual layer — actual disk file writes require PowerShell or Python with open(path, 'w'). Use Python when string replacements involve special characters (em-dash, dollar signs in C# interpolation, backticks).
- **CRLF awareness**: C# files in this repo use CRLF. Always normalize with .replace('\r\n', '\n') before string matching in Python scripts.
- **Nested ternaries count as nesting depth**: Guid.TryParse(...) ? eid : null embedded inside another ternary hits 3 levels — extract to ParseEntityId(opt).

### Style Enforcement Session — this._ Removal (2026)
- **SA1101 is disabled in .editorconfig (`SA1101.severity = none`)**: This project chose `_camelCase` fields over the `this.` qualifier. Any `this._field` usage is inconsistency, not compliance.
- **dotnet format --severity warn is dangerous**: It applies not just code-style fixes but also whitespace reformatting (expanding single-line `new { }` to multi-line), changes concrete type annotations to interfaces, and can cascade into SA1413/SA1500 violations. Use `--diagnostics IDE0003` or avoid it entirely for style-only tasks.
- **dotnet format --verify-no-changes has pre-existing whitespace conflicts**: The codebase has single-line `new { id = x }` anonymous objects that `dotnet format` wants to expand. These cannot be applied without triggering SA1413 (trailing commas required). This is a known tension; don't chase this rabbit unless the whole SA1413 + SA1413 fix cycle is intended.
- **Concurrent agent awareness**: Alfred (frontend/architecture agent) was working on controller interface changes (doc 124) simultaneously. When stashing/popping, check for other agents' unstaged changes in the working tree before making assumptions about baseline state.
- **Interfaces must be complete before switching controllers**: When changing `RecurringTransfersController` from `RecurringTransferService` to `IRecurringTransferService`, ensure the interface has ALL methods the controller uses. Missing methods cause CS1061. Always cross-check concrete class public API against the interface before the switch.
- **PowerShell -replace with -NoNewline on CRLF files**: Using `Set-Content -NoNewline` on CRLF files preserves the content correctly. The `this\._` regex in PowerShell does not escape the dot specially (it's already literal in a character sequence context). The replacement `this\._` → `_` is safe for this codebase.
