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

### 2026-XX-XX — Comprehensive Test Suite Audit (Fortinbra Request)

**Task:** Audit the entire test suite to verify all features are tested and determine feasibility of removing `Category=Performance` exclusion.

**Scope:** 7 test projects, 523 test files, 5,404 passing tests, 13 performance tests, 1 skipped test, 22 test categories.

**Key Findings:**

1. **Performance tests are well-designed and necessary:**
   - 13 performance tests + 9 E2E performance tests (23 total)
   - Load tests: sustained 15–50 req/s with p99 < 1000ms thresholds
   - Stress tests: ramp to 100 req/s with p99 < 5000–10000ms thresholds
   - E2E Core Web Vitals: FCP < 2500ms, LCP < 4000ms, CLS < 0.1
   - All have explicit latency assertions (not just error rate checks)

2. **Standard CI duration blocker:**
   - Performance test suite adds 4–5 minutes per run
   - Running on every PR degrades developer feedback loop
   - Current separation (dedicated performance.yml job) is optimal

3. **Coverage is comprehensive:**
   - All 28 API controllers have test files ✓
   - All domain models covered ✓
   - All application services covered ✓
   - 2,698 Client component tests ✓
   - Zero major feature gaps

4. **Exclusions are correct:**
   - ExternalDependency (OllamaAiServiceTests) — requires running Ollama service, properly isolated
   - Performance (13 tests) — requires 4+ minutes, already in dedicated CI workflow
   - E2E (42 tests) — requires Playwright + browser automation, separate optional workflow

5. **Single permanently skipped test:**
   - `EmptyState_RendersIcon()` — Icon rendering with ThemeService IAsyncDisposable complexity
   - Icon testing still covered by complementary test
   - Acceptable technical debt

**Decision:** **Do NOT remove Category=Performance exclusion.** The current CI/test separation is optimal:
- Standard CI: ~70 seconds, 5,404 tests, no external dependencies
- Performance CI: ~4 minutes, 13 tests, Docker required
- E2E CI: ~10 minutes, 42 tests, Playwright required

Attempted removal would degrade PR feedback time by 5–7 minutes with no benefit (performance tests already gated by dedicated workflow).

**Recommendation:** Document the multi-filter approach in CONTRIBUTING.md to help developers understand why performance tests are excluded by default.

### 2026-06-XX — Documentation Review (README.md & CONTRIBUTING.md)

**Task:** Thorough accuracy review of README.md and CONTRIBUTING.md against actual repo state.

**Findings & Changes:**

1. **Missing project in README** — `BudgetExperiment.Shared` was absent from the source project list. There are 7 src/ projects, not 6. Added with accurate description (shared enums: BudgetScope, CategorySource, DescriptionMatchMode, etc.).

2. **Test projects incomplete** — README only said "corresponding test projects for each layer" with no names or caveats. Expanded to list all 7 test projects (`Domain`, `Application`, `Infrastructure`, `Api`, `Client`, `Performance`, `E2E`) with Docker/Playwright prerequisites noted where applicable.

3. **Typo in clone command** — `cd BudgetExpirement` → `cd BudgetExperiment`. Long-standing user-facing bug.

4. **Test run commands missing required filters** — Both README and CONTRIBUTING showed bare `dotnet test`. Updated to the correct filter `"FullyQualifiedName!~E2E&Category!=ExternalDependency&Category!=Performance"` matching what CI uses. Also added notes for running Performance and E2E tests separately.

5. **Docker prerequisite undocumented** — Infrastructure.Tests, Api.Tests, and Performance.Tests all use Testcontainers. This was nowhere in the docs. Added explicit callouts.

**What was NOT changed:**
- Feature descriptions, API overview, AI features, observability, deployment — all verified accurate
- Port numbers (5099) — correct
- Auth setup (Authentik OIDC) — correct
- Hosted client model (API hosts Blazor WASM) — already correct



### 2026-03-22 — Comprehensive Project Evolution Audit

**Task:** Audit all archived features (001-124) for blog post material requested by Fortinbra.

**Scope:** Read 12 archive group documents, 2 individual archived features, active features (111-123), README, and all team decisions.

**Key Narrative Themes:**

1. **Foundation → Production Journey (120+ features):** From greenfield reset (001-002) to a fully-featured multi-user budgeting application with AI, authentication, observability, and production deployment.

2. **AI as a Core Competency:** Not a gimmick — AI rule suggestions, category suggestions, chat assistant, and recurring charge detection all built on local models (Ollama). Privacy-first, no cloud dependencies.

3. **Architecture Discipline Under Pressure:** Clean Architecture enforced through all 120+ features. Multiple refactoring waves (020, 080, 097-104) systematically paid down tech debt. StyleCop re-enabled after being disabled. DIP violations fixed. God services decomposed.

4. **Quality as Non-Negotiable:** TDD throughout. Coverage gates added (092). Testcontainers migration (121) chose fidelity over convenience. Vanity tests removed. Performance baselines on real PostgreSQL, not in-memory fakes.

5. **Deployment Evolution:** From local-only → Docker Compose demo mode → multi-arch CI/CD → Raspberry Pi production → hardened images (108) → flexible auth (Authentik/Google/Microsoft/Generic OIDC).

6. **UX Maturity:** Started with basic CRUD. Added calendar-first navigation (003), themes (044), mobile experience (047), localization (096), zero-flash auth (052), skeleton screens. Eventual Blazor ViewModel extraction (097-104) for testability.

7. **Honest Retrospective Lessons:**
   - **StyleCop disabled then re-enabled (078):** Warnings were initially too noisy. Should have tuned ruleset from day one instead of disabling entirely.
   - **In-memory test databases (121 pending):** SQLite/in-memory tests gave false confidence for 80+ features. Testcontainers should have been the baseline from Feature 001.
   - **Interface sprawl then pragmatic reversion (086, 124):** Early DIP was dogmatic. Later assessments (Feature 124) adopted "interfaces must earn their weight" — better late than never.
   - **CSV import security (063):** File upload processed server-side for 30+ features before being moved to client-side parsing. Should have been client-first from Feature 027.
   - **ViewModel extraction late (097-104):** Razor code-behind patterns degraded coverage reporting for 90+ features. ViewModels should have been the pattern from Client layer introduction (002).

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

### 2026-03-22 — Feature 112 Performance Testing Architectural Review (Alfred)

Completed comprehensive review of Feature 112 (API Performance Testing) scope and design requirements. Key finding: **performance test baselines captured against EF Core in-memory provider are unreliable for detecting real regressions in production.**

**Issue:** In-memory baselines mask I/O latency, query planning overhead, and concurrency behavior. Example: CalendarGridService makes 9+ sequential queries. In-memory shows ~10ms total; real PostgreSQL on a Raspberry Pi shows ~50-100ms. A baseline built on in-memory data won't catch a 2× latency regression because the starting point was artificially low.

**Decision:** Baselines MUST be captured against real PostgreSQL via `PERF_USE_REAL_DB=true` flag (already implemented in `PerformanceWebApplicationFactory`). This requires Docker/Testcontainers in CI. Local baseline capture must use a real PostgreSQL instance. PR smoke tests can continue using in-memory for speed (they're sanity checks, not baselines).

**Documentation:** Decision merged to `decisions.md`; findings integrated into engineering guide's performance testing section.

### 2026-03-23 — v3.25.0 Changelog Entry (Alfred)

**Task:** Compose v3.25.0 CHANGELOG entry summarizing 11 commits since v3.24.3 release.

**Commits Grouped:**
1. **Features:** PostgreSQL 18 upgrade across docker-compose files (hardened DHI image)
2. **Refactoring:** Exception middleware switch, DI registration cleanup, service method nesting depth reduction (4 service classes)
3. **Testing:** 54 new unit tests (coverage gaps), performance baselines (NBomber, all 4 load scenarios), Testcontainers + PostgreSQL 18, zero skipped tests
4. **Documentation:** Archive features 112, 114, 115, 119; close 121, 122, 123

**Format:** Followed existing CHANGELOG.md style (Feature/Refactoring/Testing/Documentation sections). Date: 2026-03-23. Prepended to top of CHANGELOG after intro line, before [3.23.0] entry.
