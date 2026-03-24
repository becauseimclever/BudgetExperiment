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

### 2026-03-24 — Feature 127: Hybrid Chart Implementation Strategy Chosen

**Task:** User decided: **go hybrid**. Update Feature 127 doc to make hybrid approach the PRIMARY recommendation. Create decision record.

**Decision:** **Hybrid approach is the primary strategy** for Feature 127 (Enhanced Charts & Visualizations).

**Breakdown:**
- **Tier 1 & 2 (Self-Implement):** Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick, Waterfall, Box Plot — all using existing SVG + Blazor pattern, zero new JS dependencies.
- **Tier 3 (Library):** Treemap, Radar — use Blazor-ApexCharts for complex algorithms (squarified rectangle packing, trigonometry).

**Changes Made:**

1. **`docs/127-enhanced-charts-and-visualizations.md` — Library Recommendation Section:**
   - Revised to lead with hybrid approach as PRIMARY
   - Explained why Tier 1/2 self-implement (team skills, zero bundle impact, natural evolution of existing charts)
   - Explained why Tier 3 uses ApexCharts (algorithms too error-prone to maintain in-house)
   - Bundle impact: zero for Tier 1/2, ~80 KB for Tier 3 (within 200 KB budget)

2. **`docs/127-enhanced-charts-and-visualizations.md` — Implementation Approach Section:**
   - Updated to state "Hybrid is the chosen approach" as conclusion
   - Preserved chart-by-chart assessment (all technical analysis remains)
   - Clarified Tier classification and trade-offs

3. **`docs/127-enhanced-charts-and-visualizations.md` — Implementation Slices:**
   - Already updated to reflect hybrid build order:
     - Slice 1: ApexCharts spike (Tier 3 only) + bundle validation
     - Slice 2: Shared data service
     - Slices 3–5: Self-implement Tier 1/2
     - Slice 6: ApexCharts Tier 3 final integration

4. **`docs/127-enhanced-charts-and-visualizations.md` — Architecture Section:**
   - Added new subsection "Theming Strategy: Unified Visual Language Across All Chart Types"
   - Documented how Tier 1/2 (SVG) and Tier 3 (ApexCharts) share CSS custom property theming
   - Clarified component structure separating `SelfImplemented/` and `ApexCharts/` folders

5. **New Decision Record:** `.squad/decisions/inbox/alfred-127-hybrid-decision.md`
   - Formal record of hybrid decision
   - Rationale, trade-offs, risk mitigation
   - References to feature doc and implementation plan

**Rationale:**
- Preserves zero-JS-dependency advantage for 7 of 9 chart types (aligns with project philosophy)
- Pragmatically outsources genuinely complex algorithms (Treemap, Radar) to library
- Leverages team's proven SVG competency (11 existing charts, full bUnit coverage)
- Unified visual theming across all chart types via shared CSS custom properties
- No-regression decision point: if ApexCharts bundle proves problematic, team can pivot to all-self-implement (Slices 1 validated this)

**Outcomes:**
- Feature 127 doc now treats hybrid as PRIMARY recommendation (not secondary or optional)
- Team has clear guidance: Tier 1/2 follow existing SVG pattern, Tier 3 use library
- Decision recorded formally in team space
- Implementation slices already reflect hybrid build order (Tier 1/2 first, Tier 3 after bundle validation)

### 2026-XX-XX — Feature 127: Self-Implement vs. Library Assessment for Chart Types

**Task:** The user asked whether the new chart types proposed in Feature 127 (Enhanced Charts & Visualizations) could be implemented self-rolled in SVG + Blazor, instead of adopting Blazor-ApexCharts. Provide an honest technical assessment for each proposed chart type.

**Context:** The app already has 11 hand-rolled SVG + Blazor chart components (BarChart, DonutChart, etc.) with full bUnit coverage, 9-theme CSS support, and zero JS dependencies. The user is proud of this approach and asked if the 9 new chart types (Treemap, Heatmap, Waterfall, Scatter, Radar, Stacked Area, Candlestick, Radial Bar, Box Plot) could continue this pattern.

**Methodology:** For each chart, evaluated:
1. SVG feasibility (can it be done?)
2. Complexity relative to BarChart/DonutChart (Low/Medium/High/Very High)
3. Key challenges (algorithm, geometry, interaction)
4. Time estimate (developer weeks)
5. Maintenance burden
6. Recommendation (self-implement or library)

**Chart Assessments (Summary):**

| Chart | Complexity | Feasibility | Key Challenge | Recommendation | Tier |
|-------|-----------|-------------|---------------|----|------|
| **Heatmap** | Low | ✅ Yes | Simple grid rendering | Self-implement | 1 |
| **Scatter Plot** | Low | ✅ Yes | Reuse LineChart infra | Self-implement | 1 |
| **Stacked Area** | Medium | ✅ Yes | Cumulative Y tracking | Self-implement | 1 |
| **Radial Bar/Gauge** | Low-Medium | ✅ Yes | Extend RadialGauge | Self-implement | 1 |
| **Candlestick** | Low | ✅ Yes | Bar + wick rendering | Self-implement | 1 |
| **Waterfall** | Medium-High | ✅ Yes | Floating bar coordinates | Self-implement possible | 2 |
| **Box Plot** | Medium | ✅ Yes | Quartile calculations (math-heavy) | Self-implement possible | 2 |
| **Treemap** | High | ⚠️ Possible | Squarified algorithm + drill-down | **Library** | 3 |
| **Radar/Spider** | Very High | ⚠️ Possible | Trigonometry (sin/cos per point) | **Library** | 3 |
| **Sunburst** | Very High | ⚠️ Possible | Combine treemap + sunburst layout | **Library** | 3 |

**Key Findings:**

1. **Tier 1 (Self-Implement, Low Risk):** Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick
   - These are natural evolutions of existing chart types or trivial SVG work
   - Total effort: 4–7 weeks for all five
   - Zero additional bundle size

2. **Tier 2 (Self-Implement with Caveats):** Waterfall, Box Plot
   - Feasible but require careful testing (coordinate math, statistical calculations)
   - Self-implementation adds ~2–3 weeks of development
   - Viable if rigor is maintained; otherwise deferred to library

3. **Tier 3 (Library Recommended):** Treemap, Radar/Spider, Sunburst
   - **Treemap:** Squarified algorithm is not domain-specific; outsource.
   - **Radar:** Trigonometry is error-prone and unmaintainable without domain expertise.
   - **Sunburst:** Complex layout algorithm; no clear benefit over treemap + drill-down.
   - Using library avoids custodial burden and accumulation of subtle geometric bugs.

**Recommendation: Hybrid Approach**

- **Use ApexCharts (Blazor-ApexCharts NuGet) for Tier 3 charts only** → Treemap, Radar, Sunburst.
- **Self-implement Tier 1 and Tier 2 charts** → Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick, Waterfall, Box Plot.
- **Decision point after Slice 1:** Measure actual bundle impact. If <200 KB and acceptable, migrate to ApexCharts for all types (simplifies scope). If critical, keep hybrid.

**Rationale:**
- The team has proven SVG/Blazor competency (11 existing charts, all tested).
- Simple charts (Heatmap, Scatter, Radial Bar) are faster to self-implement than writing library integration code.
- Complex algorithms (Treemap, Radar) are not worth maintaining in-house; library amortizes complexity.
- ApexCharts adds ~80 KB gzipped; acceptable within 200 KB budget, but hybrid saves ~50 KB if needed.
- **Fastest path to value:** Commit to ApexCharts for all types (Slice 1 planned). Reduces risk, faster delivery. Hybrid only if bundle size proves critical post-measurement.

**Feature Doc Update:**
Added new section "## Implementation Approach: Self vs. Library" to Feature 127 doc (`docs/127-enhanced-charts-and-visualizations.md`) with full chart-by-chart assessment, tier classification, and hybrid recommendation.

**Outcome:** User has both options documented. If they choose to proceed with ApexCharts (current recommendation), work begins immediately. If they insist on self-implementation, Tier 1 and 2 are low-risk; Tier 3 requires escalation/discussion.

---

### 2026-03-24 — Feature 127: Hybrid Approach Finalized and Documented

**Task:** User requested confirmation of ApexCharts licensing (both ApexCharts.js library and Blazor-ApexCharts wrapper) and wanted the Feature 127 doc updated to make the **hybrid approach the primary recommendation** (not library-only).

**Licensing Verified:**
- **ApexCharts.js:** Dual-license model. Community License (free) for organizations with annual revenue <$2M USD — includes personal and small-business commercial use without restrictions. All chart types (treemap, radar, etc.) available in free tier. Attribution required.
- **Blazor-ApexCharts wrapper:** MIT License (Copyright 2020 Joakim Dangården). Permits commercial and personal use; attribution required.
- **Verdict:** ✅ Safe for self-hosted personal budgeting app. No CLA triggered. No "Pro tier" restrictions on specific chart types.

**Feature Doc Updates Completed:**
1. ✅ Updated status from "Planning" to "Approach Decided (Hybrid Primary)"
2. ✅ Revised Library Recommendation section to lead with hybrid as primary strategy
3. ✅ Added "License Notes" subsection documenting both libraries' terms
4. ✅ Reorganized Implementation Slices to reflect hybrid build order:
   - Slices 1–2: Foundation (ApexCharts spike, ChartDataService)
   - Slices 3–5: Self-implement Tier 1 & 2 charts (7 types)
   - Slice 6: ApexCharts Tier 3 integration (Treemap, Radar only)
   - Slice 7: Visual polish (all 9 charts)
   - Slice 8: Integrate into reports
   - Slice 9: Optional legacy cleanup
5. ✅ Updated Acceptance Criteria to clarify which ACs apply to self-implemented vs. ApexCharts charts
6. ✅ Created decision record: `.squad/decisions/inbox/alfred-charts-hybrid-decision.md`

**Outcome:** Feature 127 now has a clear, documented hybrid recommendation as primary strategy. User can proceed with implementation planning knowing:
- 7 charts are self-implemented (Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick, Waterfall, Box Plot)
- 2 charts use ApexCharts (Treemap, Radar) for algorithmic complexity
- Zero JS-dependency risk for 7 of 9 types; acceptable 80 KB cost for 2 genuinely complex types
- Full licensing compliance and zero CLA/attribution complications

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

### 2026-03-24 — Multi-Vendor Bank Connector Support (Feature 126 Scope Elevation)

**Task:** Update Feature 126 to promote multi-vendor bank connector support from a future consideration to a first-class feature.

**Rationale:** User explicitly requested support for multiple vendors (Plaid for US, Nordigen for EU) allowing users to choose their provider at account-link time. The existing `IBankConnector` abstraction already supports this architectural pattern; this change is a scope elevation, not a redesign.

**Key Changes:**

1. **Removed Non-Goal NG3** (one active connector per deployment) — now a first-class goal.
2. **Added Goal G8** explicitly stating multi-vendor support with simultaneous active connectors and user-selectable providers.
3. **Introduced ConnectorRegistry pattern** in the Architecture section:
   - `IBankConnectorRegistry` interface manages registered connectors.
   - `ConnectorRegistryEntry` records hold metadata (name, regions, enabled state).
   - Registry resolution enables routing to the correct `IBankConnector` impl at sync time.
4. **Added `ConnectorType` field to `BankConnection` entity** — tracks which adapter is used for each linked account.
5. **New Connector Configuration section** showing how to enable/disable vendors via `appsettings.json` (Plaid, Nordigen, etc. with Enabled/SupportedRegions/credentials).
6. **Revised Vendor Recommendation** from "Plaid primary, Nordigen future" to "Plaid recommended for US, Nordigen for EU, both can be active simultaneously."
7. **Added 10 new acceptance criteria (AC-126-36 through AC-126-45)** covering:
   - Registry region filtering
   - Connector resolution and fallback
   - `BankConnection.ConnectorType` storage/retrieval
   - Multi-connector sync independence
   - UI region/provider selection

**Outcome:** Feature 126 now fully specifies a production-ready multi-vendor architecture. Implementation is vendor-agnostic; adding Nordigen (or any future vendor) requires only a new Infrastructure adapter + configuration entry, zero domain/application changes.

**Architecture Strength:** The original design already supported this (hence "future consideration" was overly conservative). The `IBankConnector` abstraction and ConnectorRegistry pattern ensure clean separation of vendor-specific logic from domain and application layers.

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

### 2026-XX-XX — Feature 125: Data Health & Statement Reconciliation (Planning)

**Task:** Write feature document `docs/125-data-health-and-statement-reconciliation.md` for two sub-features requested by Fortinbra: Data Health Dashboard (125a) and Statement Reconciliation Workflow (125b).

**Context:** User reported "a mix of all" data quality issues — duplicates, wrong amounts, missing transactions, wrong dates. Existing reconciliation only covers recurring transactions (~30%). No cleared/uncleared status, no statement balance input, no reconciliation completion state.

**Audit Findings Incorporated:**
- Transaction entity has no `IsCleared`, `ClearedDate`, or reconciliation lock
- No `ReconciliationRecord` aggregate for completion history
- No `StatementBalance` entity for statement input
- Existing `ReconciliationMatch` + `ReconciliationService` are solid but scoped to recurring only
- Account has `InitialBalance` and `InitialBalanceDate` — used as foundation for cleared balance computation

**Document Scope:**
- **125a (Data Health Dashboard):** Duplicate detection (exact + near-duplicate with description similarity), amount outlier detection (3σ from merchant mean), date gap detection per account, uncategorized/orphaned summary, inline fix actions (merge, edit, delete)
- **125b (Statement Reconciliation):** `IsCleared`/`ClearedDate`/`ReconciliationRecordId` on Transaction, `ReconciliationRecord` aggregate, `StatementBalance` entity, cleared balance computation, completion workflow with locking, reconciliation history
- 12 vertical implementation slices (domain → infrastructure → application → API → UI per sub-feature)
- 30 testable acceptance criteria (13 for 125a, 17 for 125b) spanning unit, integration, and bUnit test types
- 11 new API endpoints for data health, 11 for statement reconciliation
- 6 open questions flagged for team discussion

**Format:** Matched existing feature doc format (116, 120) — User Stories with acceptance criteria, Technical Design with code sketches, API tables, UI layout diagrams, sliced implementation plan with commit messages.

### 2026-XX-XX — Feature 126: Bank Connectivity & Automatic Transaction Sync (Planning)

**Task:** Write feature document `docs/126-bank-connectivity-and-transaction-sync.md` for direct bank connectivity through a financial data aggregation service, replacing manual CSV exports.

**Context:** User (Fortinbra) wants to stop requiring manual CSV exports for transaction imports. Requested evaluation of financial data aggregation vendors (Plaid, MX, Finicity, Tink, Mono, TrueLayer, Akoya, Nordigen/GoCardless) for a self-hosted personal budgeting app deployed on Raspberry Pi.

**Codebase Audit Findings:**
- Existing import pipeline is comprehensive: `ImportService` orchestrates preview → execute flow with 6 sub-services (row processor, duplicate detector, transaction creator, batch manager, mapping service, preview enricher)
- `ImportController` provides 11 endpoints for CSV import (mappings CRUD, preview, execute, history, batch management)
- `Transaction` entity already has `ImportBatchId` and `ExternalReference` — the `ExternalReference` field is perfect for storing bank-provided transaction IDs for deterministic deduplication
- `ImportBatch` tracks import metadata but has no concept of import source (CSV vs bank sync)
- CSV parsing happens client-side in Blazor, with pre-parsed rows sent to the API — bank connectivity will bypass this entirely
- Duplicate detection is heuristic (date + amount + description similarity) — bank transaction IDs eliminate this for synced transactions

**Vendor Evaluation Summary:**
- **8 vendors compared** across US/EU coverage, pricing, .NET SDK availability, self-hosted compatibility, and personal-use suitability
- **Plaid recommended as primary** — free Launch tier (100 Items), best US coverage, `/transactions/sync` polling (no public URL needed), bank-provided transaction IDs
- **Nordigen/GoCardless recommended as future secondary** — free tier, EU-only, ideal if EU coverage needed later
- **5 vendors eliminated:** MX (enterprise-only), Finicity (per-call pricing), Tink (EU-only + enterprise), TrueLayer (EU-only + commercial), Akoya (commercial agreement required), Mono (Africa-only)

**Architecture Decisions:**
- `IBankConnector` interface in Domain layer — vendor-agnostic abstraction
- Infrastructure adapters per vendor (Plaid first), swappable without domain/app changes
- Polling-based sync via `BackgroundService` (default 6h interval) — no webhook/public URL required
- OAuth token encryption via ASP.NET Core Data Protection API
- Sync conflict resolution: bank data updates amount/date/description but NEVER overwrites user-applied categories
- New entities: `BankConnection` (institution link), `LinkedAccount` (per-account mapping)
- Existing `ExternalReference` on Transaction reused for bank transaction IDs

**Document Scope:**
- Vendor comparison table with 8 providers
- Vendor recommendation with detailed rationale
- Domain model: `BankConnection`, `LinkedAccount`, `IBankConnector` + 7 value objects/records
- Application services: `IBankConnectionService` (link/manage), `IBankSyncService` (sync)
- 12 new API endpoints for bank connectivity
- 11 vertical implementation slices
- 35 testable acceptance criteria (9 domain, 11 application, 7 API, 3 infrastructure, 5 UI)
- 7 open questions flagged for team discussion
- Detailed sync conflict resolution strategy
- Token security considerations for Raspberry Pi deployment

### 2026-XX-XX — Feature 127: Enhanced Charts & Visualizations (Planning)

**Task:** Audit current charting system, evaluate chart libraries, and write comprehensive feature document for enhanced charts.

**Codebase Audit Findings:**
- **No external chart library** — all 11 chart components are hand-rolled SVG rendered by Blazor
- Chart types: BarChart, DonutChart, LineChart, AreaChart, GroupedBarChart, StackedBarChart, SparkLine, RadialGauge, ProgressBar, ChartLegend, ChoroplethMap (map, not chart)
- Shared infrastructure: ChartAxis, ChartGrid, ChartTooltip, ChartTick in `Components/Charts/Shared/`
- Only 3 chart types actively used in reports: BarChart (MonthlyTrends, BudgetComparison), DonutChart (MonthlyCategoriesReport, CalendarInsightsPanel)
- 4 chart types exist in code but are only used in ComponentShowcase (LineChart, SparkLine, RadialGauge) or not used at all (AreaChart, GroupedBarChart, StackedBarChart)
- 100% bUnit test coverage across all chart components
- Full CSS custom property integration with 9-theme design system (light, dark, accessible, crayons, geocities, macOS, monopoly, vscode-dark, win95)
- Strong accessibility: ARIA labels, keyboard navigation, tabindex on interactive elements

**Library Recommendation:** Blazor-ApexCharts — 20+ chart types, ~80KB gzipped, actively maintained Blazor wrapper, programmatic theming, MIT license. Rejected: ChartJs.Blazor (stale wrapper), Plotly.NET (3MB, overkill), D3.js (too much custom JS), Radzen (tied to component lib).

**Migration Strategy:** Parallel introduction → gradual replacement. New charts built with ApexCharts; existing SVG charts migrated one-at-a-time in dedicated PRs. Legacy components retained until all consumers migrated. Go/no-go decision after Slice 1 validates .NET 10 compat and bundle size.

**New Chart Types Proposed (10):** Treemap, Heatmap, Waterfall, Scatter, Radar/Spider, Stacked Area, Candlestick, Radial Bar, Box Plot, Sunburst (via treemap drill-down).

**Document:** `docs/127-enhanced-charts-and-visualizations.md` — 36 acceptance criteria, 10 implementation slices, 5 open questions.
**Decision Record:** `.squad/decisions/inbox/alfred-charts-feature.md`
