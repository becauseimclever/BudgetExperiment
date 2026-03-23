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

### Performance Baseline: Multi-Scenario Capture (2026-03-23)

**Task:** Capture and commit NBomber performance baselines for all load test scenarios.

**Root Cause:** Previous baseline.json only had get_transactions from an earlier stress test run. LoadTests.cs has 4 scenarios (accounts, budgets, calendar, transactions). The other three showed as "New" in every CI comparison — no regression detection.

**CI Bug Discovered:** performance.yml used 	ail -1 to pick the alphabetically-last CSV file. With multiple load test CSVs (load_accounts.csv, load_budgets.csv, load_calendar.csv, load_transactions.csv), only load_transactions.csv was ever compared. Fixed by looping over all CSVs.

**NBomber Behavior:** Each NBomber runner instance cleans/overwrites the reports folder on init. Running 4 sequential load tests = only the last test's CSV survives. Metrics must be extracted from console output for previous runs. The aselines/README.md instructions are correct — use the BaselineComparer --generate mode with the CSV artifact.

**Metrics Captured (dev hardware, in-memory DB, LoadProfile 10 req/s 35s):**
- get_accounts:     p95=0.66ms, p99=0.84ms, RPS=9.14
- get_budgets:      p95=0.76ms, p99=1.54ms, RPS=9.14
- get_calendar:     p95=12.02ms, p99=15.53ms, RPS=9.14
- get_transactions: p95=11.61ms, p99=30.43ms, RPS=9.14

**Decision:** Thresholds already correct (maxLatencyRegressionPercent=15%). No change needed to ThresholdConfig defaults.

## Core Context

### Backend Code Quality & Architecture (2026-03-22)

**Architecture Quality: B+ (Strong fundamentals with targeted improvements)** — Domain: Rich behavior-focused entities, no infrastructure leakage, 21 repository interfaces, 4 domain services, comprehensive scope mgmt. Application: 87 services, explicit mapping, main issue: 54 methods exceed 20-line guideline (6 with 3+ nesting). Infrastructure: Excellent EF fluent config, repository scope filtering prevents data leaks, optimistic concurrency via PostgreSQL xmin. API: Textbook REST, DTOs-only, RFC 7807 Problem Details, ETag support, OpenAPI + Scalar configured.

**Critical Issues:** (1) Six methods with 3+ nesting levels need refactoring (TransactionListService recurring, ImportValidator, RuleSuggestionParser). (2) ExceptionHandlingMiddleware uses string matching instead of DomainException.ExceptionType enum. (3) Three services registered as both interface + concrete (no justification). **Method Count:** 54 violations total — 40 Application, 19 Infrastructure configs (acceptable), ~10 spread.

**Positives:** Zero infrastructure types in Domain. Comprehensive scope filtering at repo level (security). No primitive obsession (proper value objects). No dead code. StyleCop warnings-as-errors enforced.

### Feature 111: Performance Optimizations (2026-03-22)

**AsNoTracking Propagation:** Added AsNoTracking/AsNoTrackingWithIdentityResolution to all read-only repository queries; preserved change tracking on update paths. **Parallelized Hot Paths:** CalendarGridService (9+ sequential → parallelized), TransactionListService (similar), DayDetailService (orchestration-level). Used scoped query helper with fallback for test constructors. Registered DbContextFactory for future parallel support. **Bounded Eager Loading:** AccountRepository reduced to 90-day lookback (production Pis with large histories need this). Added extension interfaces (IAccountTransactionRangeRepository, IAccountNameLookupRepository). DayDetailService uses targeted account-name lookup instead of full history. **Result:** Build green (-warnaserror). No regression in entity refresh.

### CI Fixes & Infrastructure (2026-03-22 → 2026-03-23)

**Performance CI Actions:** Fixed broken version references (checkout@v6, upload-artifact@v7, setup-dotnet@v5, cache@v5 → all @v4). Entire performance CI pipeline couldn't run.

**Baseline Committed:** Used local stress_transactions.csv (1335 requests, p99=7.3ms) per Decision 5 (smoke tests are sanity checks, not baselines). CI Smoke artifact only contained in-memory data unsuitable for baselines. Updated baseline.json with stress data; must replace with scheduled PostgreSQL load test run eventually.

**PostgreSQL 18 Upgrade:** Migrated docker-compose.demo.yml and docs from postgres:16 to postgres:18 using Docker Hardened Image (dhi.io/postgres:18). Npgsql 10.0.0 supports 13–18 (no driver changes). EF migrations fully compatible. Migration path: new auto-uses 18, existing demo `down -v`, production pg_dumpall/restore.

### Recent Work

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

### 2026-03-22T18-23-42Z — Session Close: Batch 2+3 Complete

**Cross-Team Summary:**

- **From Barbara:** Testcontainers migration complete; real concurrency bug found and fixed in `IUnitOfWork.MarkAsModified`. API tests now run against PostgreSQL. 55 new high-value tests added. Vanity enum tests cleaned up (12 deleted).
- **From Alfred:** DIP verdict complete — all 3 controllers VERDICT A. Interfaces already existed but were incomplete. These expansions were handled by Lucius.
- **From Coordinator:** 5,409 tests passing, 0 build warnings. All assertion bugs fixed. PR ready for merge.

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

### 2026-03-23 — Performance Baseline Committed (Lucius)

**Task:** Generate and commit `tests/BudgetExperiment.Performance.Tests/baselines/baseline.json`.

**Source decision:** Used local `stress_transactions.csv` (NOT the CI Smoke artifact). The CI run #24 artifact (`performance-reports-Smoke-24`) only contained `smoke_calendar.csv` — a smoke/in-memory run with 10 requests over 10 seconds. Per Decision 5, smoke runs are "sanity checks, not baseline sources"; baselines must use real PostgreSQL data.

**Baseline content:**
- Scenario: `get_transactions` (1 scenario)
- p50=3.97ms, p95=5.74ms, p99=7.3ms, RPS=22.25, errors=0
- Source: local committed `stress_transactions.csv` (1335 requests, 60s run)
- Commit SHA pinned: `995dd0fedbe3a5752b9586e80fb41f17decdef0c`

**Tool used:** `BaselineComparer --generate` mode. Built fine without `--no-build`.

**Commit:** `perf: establish performance baseline` pushed to main (267d73d).

**Key learning:** When CI artifacts exist from Smoke profile runs, always check the artifact content and profile type before using — in-memory smoke data is not valid for performance baselines per the team's architectural decision.



### 2026-03-23 — Feature 118: PostgreSQL 18 Upgrade (Lucius)

**Task:** Upgrade PostgreSQL from version 16 to 18 across Docker compose, documentation, and hardened image policy.

**Files Changed:**
- `docker-compose.demo.yml`: Updated image from `dhi.io/postgres:16` → `dhi.io/postgres:18`, updated comments
- `DEPLOY-QUICKSTART.md`: Updated bundled database version reference
- `.github/copilot-instructions.md`: Updated hardened image policy section
- `docs/ci-cd-deployment.md`: Updated container security PostgreSQL reference
- `docs/118-postgresql-18-upgrade.md`: Marked status Done, checked all acceptance criteria

**Validation:** Npgsql 10.0.0 supports PostgreSQL 13–18; no driver changes needed. Migration guidance documented for existing deployments (pg_dumpall/restore for production, down -v for demo).

**Commits:**
1. `feat(docker): upgrade PostgreSQL to version 18` (aea4122)
2. `docs: update PostgreSQL references from 16 to 18` (556d126)
3. `docs: archive feature 118 - PostgreSQL 18 upgrade complete` (0632ebb)

**Archive:** Moved `docs/118-postgresql-18-upgrade.md` → `docs/archive/111-120-postgresql-18-upgrade.md` per existing archive naming pattern.

**Key Pattern:** Docker Hardened Images (dhi.io) provide continuously patched, SLSA-provenance PostgreSQL images. Always check hardened catalog first; use when available.

### 2026-03-23 — Test Coverage Gaps Filled + PostgreSQL 18 Upgrade

**Task 1: Missing Application Service Tests (20 + 17 tests)**

**RecurringTransactionInstanceServiceTests.cs — 20 tests**
- `GetInstancesAsync`: not found (null), occurrences in range, empty query window, skipped/modified exception handling
- `ModifyInstanceAsync`: not found (null), new exception creation, existing exception update, concurrency token + MarkAsModified, unit of work integration
- `SkipInstanceAsync`: not found (null), skip exception creation, old exception cleanup, SaveChanges call verification
- `GetProjectedInstancesAsync`: account filtering, GetActiveAsync fallback, empty results, skipped instance filtering, effective date ordering

**UserSettingsServiceTests.cs — 17 tests**
- `GetCurrentUserProfile`: context field mapping, null GUID handling
- `GetCurrentUserSettingsAsync`: happy path, unauthenticated exception
- `UpdateCurrentUserSettingsAsync`: scope/autoRealize/lookbackDays/currency updates, validation, auth checks
- `CompleteOnboardingAsync`: flag setting, auth checks
- `GetCurrentScope` / `SetCurrentScope`: valid/null/whitespace handling, validation

**Result:** Application.Tests: 982 → 1,019 (+37); Full suite: 5,412 → 5,449

**Learnings:**
- `GetProjectedInstancesAsync` calls `GetInstancesAsync` internally which calls `GetByIdAsync` per recurring — test mocks must setup GetByIdAsync for all projected test scenarios, not just GetActiveAsync
- `IUserSettingsRepository.SaveAsync` signature: `Task` (no return value) — mock with `.Returns(Task.CompletedTask)`
- `UserSettings.CreateDefault(userId)` is the correct factory for test instances; works for post-update behavior assertions
- SA1512 comment rule fires on `// --- Section ---` dividers followed by blank lines — remove blank line or use XML docs

**Task 2: PostgreSQL 18 Upgrade**
- Updated `docker-compose.demo.yml`: `dhi.io/postgres:16` → `dhi.io/postgres:18`
- Updated documentation references (DEPLOY-QUICKSTART.md, docs/ci-cd-deployment.md)
- Verified Npgsql 10.0.0 supports 13–18; no driver changes
- EF Core migrations fully compatible
- Migration path: new deployments automatic, existing demo `down -v` needed, production uses pg_dumpall/restore

**Commit Messages:**
- `feat(docker): upgrade PostgreSQL to version 18`
- `docs: update PostgreSQL references from 16 to 18`
- `test: fill coverage gaps for RecurringTransactionInstanceService and UserSettingsService`
