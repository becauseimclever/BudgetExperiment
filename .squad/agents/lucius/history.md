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

## Core Context

### Initial Code Quality Review (2026-03-22)

Conducted comprehensive backend code review finding B+ architecture. **Critical Issues:** 6 methods with 3+ nesting levels, ExceptionHandlingMiddleware using brittle string matching instead of enum, 3 services with redundant concrete registrations. **Positives:** Zero infrastructure types in Domain, comprehensive scope filtering, no primitive obsession, no commented code, StyleCop enforced with zero global suppressions.

**Method Length Statistics:** 54 violations (40 in Application, 19 in Infrastructure configs). Priority: Refactor 6 critically nested methods, then tackle 26 methods in 21-40 line range.

### Quick-Win Fixes Applied (2026-03-22)

1. **ExceptionHandlingMiddleware Enum-Based Routing:** Created `DomainExceptionType` enum (Validation, NotFound) in `Domain.Common`. Updated `DomainException` to accept optional type parameter (defaults to Validation). Updated all 17 "not found" throw sites to pass `DomainExceptionType.NotFound`. Removed string matching from middleware.

2. **Redundant DI Registrations:** Investigated all three "backward compat" concrete registrations (`TransactionService`, `RecurringTransactionService`, `RecurringTransferService`). Found concrete consumers in API controllers. Updated comments to name actual consumers rather than vague "backward compatibility".

3. **DateTime.Now → DateTime.UtcNow:** Replaced 3 occurrences in `Reconciliation.razor` (field initializers, year-range loop).

4. **PostgreSqlFixture Constructor:** Updated `new PostgreSqlBuilder()` → `new PostgreSqlBuilder("postgres:16")` (parameterless constructor obsolete).

**Result:** Build clean (0 warnings, 0 errors), 5415 tests pass, 1 pre-existing skip.

### Performance Optimizations (2026-03-22)

**Feature 111: Pragmatic Performance Optimizations** implemented across three areas:
- **Area 1 (AsNoTracking):** Added AsNoTracking/AsNoTrackingWithIdentityResolution to all read-only repository queries while preserving tracking on update paths.
- **Area 2 (Parallelized Hot Paths):** CalendarGridService (9+ sequential queries), TransactionListService, DayDetailService. Registered `IDbContextFactory<BudgetDbContext>` for future parallel support. Fallback behavior for test constructors.
- **Area 3 (Bounded Eager Loading):** AccountRepository reduced eager loading to 90-day lookback window; added extension interfaces `IAccountTransactionRangeRepository`, `IAccountNameLookupRepository`; DayDetailService uses targeted name lookup.

**Bug Fixed:** Feature 111 DI bug — removed unused `AddDbContextFactory` Singleton registration that broke DI validation (commit `599483a`).

**Result:** Build green (-warnaserror enabled). Feature 111 documentation updated to Done.

### GitHub Actions Version Fixes (2026-03-22)

**Performance CI Workflow (`performance.yml`):** Fixed non-existent action versions blocking entire performance pipeline:
- `actions/checkout@v6` → `actions/checkout@v4`
- `actions/setup-dotnet@v5` → `actions/setup-dotnet@v4`
- `actions/cache@v5` → `actions/cache@v4`
- `actions/upload-artifact@v7` → `actions/upload-artifact@v4` (2 occurrences)

**Impact:** Performance CI workflow can now execute successfully on GitHub Actions.

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

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

### 2026-03-22T18-23-42Z — Session Close: Batch 2+3 Complete

**Cross-Team Summary:**

- **From Barbara:** Testcontainers migration complete; real concurrency bug found and fixed in `IUnitOfWork.MarkAsModified`. API tests now run against PostgreSQL. 55 new high-value tests added. Vanity enum tests cleaned up (12 deleted).
- **From Alfred:** DIP verdict complete — all 3 controllers VERDICT A. Interfaces already existed but were incomplete. These expansions were handled by Lucius.
- **From Coordinator:** 5,409 tests passing, 0 build warnings. All assertion bugs fixed. PR ready for merge.


