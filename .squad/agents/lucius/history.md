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

### Feature 116 Slice 2 — Strategy 3: Regex Alternation (2026)

**What was added:**
- `FindRegexAlternations` method in `RuleConsolidationAnalyzer` — Strategy 3.
- `AddAlternationSuggestionsForGroup` — batching logic with 500-char limit per merged pattern.
- `BuildAlternationSuggestion` — constructs `ConsolidationSuggestion` with `MergedMatchType = Regex`.

**Algorithm:**
1. Collect all `SourceRuleIds` from Strategy 1 (exact duplicates) and Strategy 2 (substring) into `allClaimedIds`.
2. Filter active Contains rules, excluding `allClaimedIds`.
3. Group by `CategoryId`; skip groups with `< 2` rules.
4. For each group, escape each pattern via `Regex.Escape(pattern)` and join with `|`.
5. If joined string ≤ 500 chars → 1 suggestion for the whole group.
6. If > 500 chars → greedy batching: accumulate rules until adding the next would exceed 500, then emit a suggestion and start a fresh batch. Final batch always emitted (covers all IDs).

**Key design decisions:**
- `System.Text.RegularExpressions.Regex.Escape` ensures dots, plus signs, and other metacharacters are properly escaped.
- The 500-char threshold is per `MergedPattern` string length (not per rule count).
- Strategy 3 only promotes `Contains` → `Regex`; existing `Regex`-typed rules are ignored.
- Rules already claimed by Strategies 1 or 2 are excluded to prevent double-reporting.

**Result:** 1003 tests pass (previously 13 tests in this file; 8 new Slice 2 tests all green).

### Feature 116 Slice 1 — RuleConsolidationAnalyzer (2026)

**Files created:**
- `src/BudgetExperiment.Application/Categorization/ConsolidationSuggestion.cs` — sealed record with `SourceRuleIds`, `MergedPattern`, `MergedMatchType`, `Confidence`.
- `src/BudgetExperiment.Application/Categorization/RuleConsolidationAnalyzer.cs` — pure logic analyzer, no DI dependencies.

**Key design decisions:**
- Two strategies run sequentially: exact duplicates first, substring containment second. Exact-duplicate IDs are excluded from the substring pass to avoid double-reporting.
- Exact duplicate key: `(CategoryId, MatchType, Pattern.ToUpperInvariant())`. Case-insensitive by normalizing to uppercase before grouping.
- Substring containment only for `RuleMatchType.Contains` within the same `CategoryId`. Uses ordered pair iteration (i≠j) so only the broader (shorter) pattern becomes `MergedPattern`.
- `AnalyzeAsync` is a thin orchestrator; all logic is in private static helpers to keep methods ≤20 lines.
- `Task.FromResult` for the async signature — no I/O, no awaits needed.
- 13 tests all pass, 0 build warnings.

### Feature 116 Slice 3 — RuleConsolidationPreviewService (2026)

**File created:**
- `src/BudgetExperiment.Application/Categorization/RuleConsolidationPreviewService.cs` — sealed class implementing `IRuleConsolidationPreviewService`. Pure logic, no constructor dependencies.

**Key design decisions:**
- Single private static `Matches(RuleMatchType, pattern, description)` method using a switch expression — one responsibility, easy to extend.
- `Contains`, `Exact`, `StartsWith`, `EndsWith`: use built-in `StringComparison.OrdinalIgnoreCase` overloads.
- `Regex`: construct `new Regex(pattern, RegexOptions.IgnoreCase)` inline per call (acceptable for preview use, not a hot path).
- Unmatched list built with a second `Where(!Matches)` pass (avoids allocating a set; descriptions list is small for preview scenarios).
- `CoveragePercentage = total == 0 ? 0.0 : (double)matched.Count / total * 100.0`.
- Registered in `DependencyInjection.AddApplication()` as `AddScoped<IRuleConsolidationPreviewService, RuleConsolidationPreviewService>()`.

**Result:** Build green (0 warnings, 0 errors). 1012 Application tests pass (9 new Slice 3 tests + all prior tests).

### Feature 116 Slice 4 — RuleConsolidationService & Endpoint (2026)

**Files created:**
- `src/BudgetExperiment.Application/Categorization/RuleConsolidationService.cs` — sealed class implementing `IRuleConsolidationService`.

**Files modified:**
- `src/BudgetExperiment.Application/DependencyInjection.cs` — registered `RuleConsolidationAnalyzer` (scoped) and `IRuleConsolidationService → RuleConsolidationService` (scoped).
- `src/BudgetExperiment.Api/Controllers/CategorizationRulesController.cs` — injected `IRuleConsolidationService` and `IRuleSuggestionService`; added `POST analyze-consolidation` action.

**Key design decisions:**
- Constructor: `(ICategorizationRuleRepository, IRuleSuggestionRepository, RuleConsolidationAnalyzer, IUnitOfWork)` — exactly per Barbara's contract.
- Early-exit guard: if `analyzer.AnalyzeAsync` returns 0 suggestions, return `Array.Empty<RuleSuggestion>()` and skip repo + unit-of-work calls.
- `BuildSuggestion` is a private static helper that calls `RuleSuggestion.CreateConsolidationSuggestion(...)` with a human-readable title, description, and reasoning.
- One `AddRangeAsync` call (batch insert) and one `SaveChangesAsync` call per invocation — no per-item round trips.
- Controller delegates DTO mapping entirely to `IRuleSuggestionService.MapSuggestionsToDtosAsync` — no mapping logic in the controller.
- `RuleConsolidationAnalyzer` registered as `AddScoped<RuleConsolidationAnalyzer>()` (concrete, no interface needed — pure logic with no I/O dependencies).

**Result:** Build green (0 warnings, 0 errors). 1016 Application tests pass (4 new Slice 4 + all prior). 651 API tests pass (3 new Slice 4 + all prior).

### Feature 116 Slice 5 — Accept & Dismiss Workflow (2026)

**Files modified:**
- `src/BudgetExperiment.Application/Categorization/RuleConsolidationService.cs` — added `AcceptConsolidationAsync` and `DismissConsolidationAsync` methods.
- `src/BudgetExperiment.Application/Categorization/SuggestionAcceptanceHandler.cs` — added `IRuleConsolidationService` as 4th constructor parameter; replaced `RuleConsolidation => throw new DomainException(...)` with `await _consolidationService.AcceptConsolidationAsync(suggestionId, ct)`.
- `src/BudgetExperiment.Api/Controllers/CategorizationRulesController.cs` — added `AcceptConsolidationAsync` and `DismissConsolidationAsync` endpoints.
- `src/BudgetExperiment.Domain/Repositories/ICategorizationRuleRepository.cs` — added explicit `new Task AddAsync(...)` re-declaration to satisfy `<see cref="ICategorizationRuleRepository.AddAsync"/>` in Barbara's test XML docs (cref resolution requires the member be declared directly on the interface, not just inherited).
- `tests/BudgetExperiment.Application.Tests/Services/SuggestionAcceptanceHandlerTests.cs` — added `Mock<IRuleConsolidationService>` field, updated constructor to pass 4th arg, removed `AcceptSuggestionAsync_Consolidation_ThrowsDomainException` test (behavior changed: now delegates instead of throws).
- `tests/BudgetExperiment.Application.Tests/RuleSuggestionServiceTests.cs` — updated one inline `SuggestionAcceptanceHandler` construction to pass 4th arg.

**Key design decisions:**
- `AcceptConsolidationAsync`: Creates merged rule with `RuleMatchType.Regex` and the suggestion's `OptimizedPattern` as the pattern. Name is `"Consolidated: {pattern}"` truncated to `MaxNameLength`. Deactivates all source rules, then `AddAsync` + `SaveChangesAsync`. Does NOT call `suggestion.Accept()` — the handler does that after the service returns.
- `DismissConsolidationAsync`: Loads suggestion, calls `suggestion.Dismiss()` (no reason), `SaveChangesAsync`. Simple 3-step flow.
- New controller endpoints map to `consolidation/{id}/accept` and `consolidation/{id}/dismiss`. Accept returns 200 with DTO via `_service.GetByIdAsync(mergedRule.Id, ct)`. Dismiss returns 204. Middleware handles NotFound → 404 automatically.
- No DI changes needed — `IRuleConsolidationService` was already registered in `AddApplication()`.
- `new Task AddAsync(...)` on `ICategorizationRuleRepository` is intentional: C# interfaces allow re-declaring inherited members with `new` to make them resolvable in `cref` attributes. This avoids modifying Barbara's test files.

**Result:** Build green (0 warnings, 0 errors). 1022 Application tests pass (6 new Slice 5 + all prior). 654 API tests pass (3 new Slice 5 + all prior).

### Feature 116 Slice 6 — Undo Consolidation (2026)

**Files modified:**
- `src/BudgetExperiment.Application/Categorization/RuleConsolidationService.cs` — two changes:
  1. **Retrofix**: Added `suggestion.RecordMergedRuleId(mergedRule.Id)` in `AcceptConsolidationAsync` before `SaveChangesAsync`, so the undo operation can find the merged rule.
  2. **New method**: `UndoConsolidationAsync(Guid suggestionId, CancellationToken)` — loads suggestion (throws NotFound if absent), guards that status is Accepted (throws InvalidState if not), loads source rules via `GetByIdsAsync`, calls `Activate()` on each, loads merged rule via `GetByIdAsync(MergedRuleId.Value)`, calls `Deactivate()` on merged rule, calls `suggestion.Reopen()`, then `SaveChangesAsync`.
- `src/BudgetExperiment.Application/Categorization/IRuleConsolidationService.cs` — fixed SA1514 spacing (blank line before new `UndoConsolidationAsync` doc header).
- `src/BudgetExperiment.Api/Controllers/CategorizationRulesController.cs` — added `POST consolidation/{id:guid}/undo` endpoint returning 204 No Content; 404/422 handled by `ExceptionHandlingMiddleware`.
- `src/BudgetExperiment.Infrastructure/Persistence/Configurations/RuleSuggestionConfiguration.cs` — added `builder.Property(s => s.MergedRuleId)` so EF maps the new nullable UUID column.

**Migration created:**
- `src/BudgetExperiment.Infrastructure/Persistence/Migrations/20260322221032_Feature116_AddMergedRuleId.cs` — adds nullable `MergedRuleId uuid` column to `RuleSuggestions` table (no FK constraint per spec).

**Key pitfall — build-before-test after migration:**
EF Core 10 throws `PendingModelChangesWarning` at runtime if the compiled assembly's snapshot doesn't include a newly added migration. After `dotnet ef migrations add`, always run `dotnet build` before `dotnet test --no-build`, otherwise the test host's `MigrateAsync()` will fail with "pending model changes."

**Pre-existing state Barbara introduced:**
Barbara added `MergedRuleId` to `RuleSuggestion.cs` and `Reopen()` domain method, plus `DomainExceptionType.InvalidState` enum value, and the `ExceptionHandlingMiddleware` mapping for 422 — all before this slice. Without the migration, all API tests were failing due to the `PendingModelChangesWarning`. The Feature116 migration resolved this.

**Result:** Build green (0 warnings, 0 errors). 1027 Application tests pass (5 new Slice 6 + all prior). 657 API tests pass (3 new Slice 6 + all prior).

### 2026-03-22: Feature 111 performance optimizations

- Added AsNoTracking/AsNoTrackingWithIdentityResolution to read-only repository queries while preserving tracking for update paths.
- Parallelized CalendarGridService, TransactionListService, and DayDetailService reads via scoped parallel query helper with fallback for test constructors.
- Bounded account transaction eager loading to a 90-day lookback and added range/name lookup repository extensions for targeted account name retrieval.
- Registered DbContextFactory for future parallel query support.

### 2026-03-22 — Feature 111: Complete Implementation (Lucius)

**Feature 111: Pragmatic Performance Optimizations** fully implemented across three areas:

#### Area 1: AsNoTracking Propagation
- Added AsNoTracking/AsNoTrackingWithIdentityResolution to all read-only repository queries
- Preserved change tracking on update paths (critical for concurrency)
- No regression in entity refresh behavior

#### Area 2: Parallelized Hot Paths
- CalendarGridService: 9+ sequential queries → parallelized via scoped helper
- TransactionListService: Similar parallelization for transaction fetching
- DayDetailService: Orchestration-level parallelization
- Registered `IDbContextFactory<BudgetDbContext>` for future parallel context usage
- Fallback behavior for test constructors when scope factory unavailable

#### Area 3: Bounded Eager Loading
- AccountRepository: Reduced eager loading to 90-day lookback window (production Pis with large histories need this bound)
- Added non-breaking extension interfaces: `IAccountTransactionRangeRepository`, `IAccountNameLookupRepository`
- DayDetailService now uses targeted account-name lookup instead of loading full history

**Architectural Notes:**
- `IDbContextFactory` could not be injected directly into Application services without layering conflicts; scoped query helpers + fallback providers preserve scope filtering and test constructors
- Extension interfaces avoid breaking changes to existing `IAccountRepository` implementers and tests
- No areas skipped

**Result:** Build green (-warnaserror enabled). Feature 111 documentation status updated to Done.

### 2026-03-22 — CI Fix: performance.yml Action Versions (Lucius)

**Task:** Fix non-existent GitHub Actions version references in `.github/workflows/performance.yml`.

**Root Cause:** Four action references used versions that do not exist on GitHub Actions:
- `actions/checkout@v6` (latest major: v4)
- `actions/upload-artifact@v7` (2 occurrences; latest major: v4)
- `actions/setup-dotnet@v5` (latest major: v4)
- `actions/cache@v5` (latest major: v4)

The task only flagged checkout and upload-artifact, but setup-dotnet@v5 and cache@v5 were caught during the audit and corrected in the same pass.

**Fix Applied:** All five occurrences updated to v4. No workflow logic, job structure, or environment variables changed.

**Validation:** Python script confirmed all `uses:` references are now `@v4` (except `marocchino/sticky-pull-request-comment@v3` which is correct). YAML structure verified by visual review — no indentation errors.

**Commit:** `ci: fix GitHub Actions version references in performance.yml` on branch `feature/code-quality-review`.

**Impact:** Performance workflow has never successfully executed on GitHub Actions due to this bug. With these corrections, scheduled, PR, and manual workflow_dispatch runs should now reach the test execution step.

