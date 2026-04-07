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

## Core Context

### Architecture Quality Assessment (2026-03-22)

- **Overall: B+.** Domain: rich entities, no infrastructure leakage, 21 repo interfaces. Application: 87 services; 54 methods exceed 20-line target. Infrastructure: excellent EF fluent config, scope filtering prevents data leaks. API: textbook REST, DTOs-only, RFC 7807, ETags, OpenAPI+Scalar.
- **Fixes shipped:** ExceptionHandlingMiddleware now uses `DomainException.ExceptionType` enum (not string matching). `this._` prefix removed (~1,474 occurrences). 3 controllers switched to interface injection.
- **DIP verdict (3 controllers):** Interfaces (`ITransactionService`, `IRecurringTransactionService`, `IRecurringTransferService`) already existed and were registered; controllers just used wrong concrete types. Expanded interfaces (+8 methods). Duplicate concrete DI registrations removed.
- **Performance exclusion is correct:** 13 performance tests take 4–5 min. CI separation is optimal (standard ~70s, performance ~4 min, E2E ~10 min).
- **Test suite audit:** 5,413 tests; 2 critical service gaps (RecurringTransactionInstanceService, UserSettingsService); 68 low-value tests cleaned up; vanity enum tests removed; net: 5,449.

### Feature 127: Enhanced Charts & Visualizations — COMPLETE (2808 tests)

**Existing baseline:** 11 hand-rolled SVG chart components, 100% bUnit coverage, 9-theme CSS custom property integration. Active: BarChart + DonutChart. Unused/showcase: LineChart, SparkLine, RadialGauge, AreaChart (removed), GroupedBarChart, StackedBarChart.

**Hybrid approach (chosen):**
- Tier 1 & 2 self-implemented (SVG): Heatmap, Scatter, Stacked Area, Radial Bar, Candlestick, Waterfall, Box Plot
- Tier 3 library (Blazor-ApexCharts): Treemap, Radar — squarified/trig algorithms outsourced
- ApexCharts.js community license (free <$2M revenue) + wrapper MIT. Bundle: ~80 KB gzipped.

**Chart → report page wiring (Slice 8):**
- BudgetComparisonReport: WaterfallChart + RadialBarChart + BudgetRadar
- MonthlyCategoriesReport: BudgetTreemap (HeatmapChart/ScatterChart deferred — need raw transactions)
- MonthlyTrendsReport: StackedAreaChart (income vs spending totals only — no per-category data in DTO)
- CandlestickChart: ComponentShowcase only (daily balance data not in any current report)
- BarChart + DonutChart: untouched

**Slice 9:** AreaChart deleted (zero consumers confirmed by grep). GroupedBarChart/StackedBarChart/LineChart/SparkLine remain (no consumers found, deferred cleanup).

**Slice 10:** ReportsDashboard (`/reports/dashboard`) added; 2804 → 2808 tests.

### ApexCharts Technical Notes (Slice 1)

- v6.x: ES module lazy loading — no `<script>` tag needed; `AddApexCharts()` in `Program.cs` sufficient.
- `ThemeService` has no interface — inject concrete directly (consistent with DI registration).
- CSS vars not readable from C# — `ChartColorProvider` hardcodes design-token values; update on `tokens.css` changes.
- Dark themes: only `dark` and `vscode-dark`; `system` defaults to light (no async JS sync available).
- `@namespace` directive required for `.razor` files in a subdirectory (e.g., `ApexCharts/`).
- `[Inject]` nullable workaround (Blazor/.NET 10): inject `IServiceProvider` and call `GetService<T>()`.
- `GetHashCode()` for category colour cycling is session-stable (acceptable for in-session visual assignment).

### Documentation Fixes (README/CONTRIBUTING)

- Added `BudgetExperiment.Shared` to project list (was missing).
- Test run command requires `Category!=Performance` filter — updated README and CONTRIBUTING.
- Docker prerequisite for Testcontainers tests (Infrastructure.Tests, Api.Tests) now documented.
- Typo: `cd BudgetExpirement` → `cd BudgetExperiment` fixed.

### Feature 128: Kakeibo + Kaizen Calendar-First Philosophy — Documentation Update

**What Changed:**
- **README.md opening**: Replaced feature-list tagline with Kakeibo + Kaizen philosophy statement. Calendar described as "the household ledger" and the centerpiece of interaction.
- **Purpose section**: Reframed entirely — leads with WHY (Kakeibo + Kaizen), then lists capabilities as tools serving the mindful philosophy. Calendar described as primary surface, not just a view.
- **Key Domain Concepts**: Added three new Kakeibo entities: `KakeiboCategory` (four spending buckets), `MonthlyReflection` (monthly journal + savings intention), `KaizenGoal` (weekly micro-improvement). Updated `BudgetCategory` and `Transaction` descriptions to mention Kakeibo routing and override.
- **Development Guidelines**: Added "Philosophy First" bullet — every feature must deepen the Kakeibo + Kaizen rhythm. Points to `docs/128-kakeibo-kaizen-calendar-first.md` for full spec.
- **CONTRIBUTING.md**: Added entire "Design Philosophy — Kakeibo + Kaizen" section after intro, before Development Guidelines. Explains how philosophy affects contributions: calendar as centerpiece, reflection over data display, no gamification, categorization carries intention, small/consistent over large/occasional.

**Why:**
The application's identity has fundamentally pivoted. It is no longer described as "a capable transaction tracker" — it is a mindful budgeting application built around Kakeibo + Kaizen philosophy. The calendar is the centerpiece. All future features and enhancements must support this philosophy. README and CONTRIBUTING must communicate this to contributors immediately, not bury it in feature specs.

**What Stayed the Same:**
All technical content (architecture diagram, setup commands, test commands, Docker info, API list, release process, localization section) remains unchanged. Surgical edits only — no file rewrites.

## Learnings

### Feature 120: Plugin System Planning (2026-03-22)

**Domain Event Scaffolding Discovery:**
- `Transaction._domainEvents` exists at line 17 (`List<object>`) but is never dispatched or consumed.
- No `IDomainEvent` interface exists — must be created in Domain layer.
- No Events folder exists in Domain — needs creation.
- `BudgetDbContext` does not override `SaveChangesAsync` to dispatch events — wiring required.

**Plugin Architecture Boundary Issue:**
- `IDomainEventHandler<TEvent>` requires constraint `where TEvent : IDomainEvent`.
- Plugin.Abstractions must have zero core deps per spec — but needs `IDomainEvent`.
- **Resolution:** Recommend moving `IDomainEvent` marker to Plugin.Abstractions; Domain references Abstractions for the marker only. This inverts typical layering but keeps plugin authoring simple (single SDK reference).

**Blazor WASM Limitation:**
- WASM cannot dynamically load assemblies at runtime.
- Plugin Blazor pages cannot be hot-pluggable in current architecture.
- **MVP Scope:** Plugin UI limited to navigation links and management page. Full page support deferred.

**Existing Patterns Observed:**
- Report builders use `ITrendReportBuilder`, `ILocationReportBuilder` — not a unified `IReportBuilder` interface yet.
- Import service uses composition of sub-services (`IImportRowProcessor`, etc.) — no `IImportParser` abstraction exists.
- NavMenu renders static nav items — no dynamic plugin section yet.

**Test Infrastructure:**
- All API/Infrastructure tests use PostgreSQL Testcontainers.
- Integration tests for plugin loading will need sample plugin DLL fixture.

### Feature 129: Kakeibo Alignment Audit — Complete (2026-04-07)

**Scope:** Audited all existing features (27 distinct features) against the Kakeibo + Kaizen calendar-first philosophy established in Feature 128. Scored alignment (🟢/🟡/🔴/⚪), identified required changes, prioritized implementation order.

**Key Findings:**
1. **5 Immediate-Priority Features:** Calendar (homepage + views), Transaction Management, Budget Categories & Goals, Onboarding Flow — these touch the core Kakeibo workflow (mindful recording, category routing, philosophy introduction). Must be modified before Feature 128 ships.
2. **12 Soon-Priority Features:** Reports, AI Chat Assistant, AI Rule Suggestions, Transaction List, Uncategorized Transactions, Settings, Weekly Kaizen Goals — these augment Kakeibo but aren't blocking. Can follow in Phase 2-4.
3. **8 Low-Priority Features:** Paycheck Planner, Location Report, Export enhancements, Custom Reports Builder — nice-to-have Kakeibo awareness but not critical to philosophy.
4. **Custom Reports Builder is a 🔴 Tension:** Encourages endless data exploration — opposite of Kakeibo's "simple, consistent reflection" philosophy. Recommend feature-flagging (default off) for power users only.
5. **17 Feature Flag Candidates Identified:** Split between user-simplification flags (hide features you don't use) and experimental rollout flags (phased release of new Kakeibo features). Custom Reports Builder, Advanced Charts, Geocoding stub are default-off. Core Kakeibo features (heatmap, reflection prompts, micro-goals) are default-on but user-controllable.

**Feature Flag Architecture Designed:**
- **Configuration:** Hierarchical `FeatureFlags` section in `appsettings.json`, overrideable via env vars (Docker/production)
- **Scope:** Instance-level flags (per-deployment), not per-user. User preferences (e.g., "hide Paycheck Planner in my menu") handled separately via `UserSettings` entity.
- **API Surface:** `GET /api/v1/config/feature-flags` endpoint returns JSON tree of flags. Client-side `FeatureFlagClientService` fetches once on load, caches in memory.
- **Naming Convention:** `{Area}:{SubArea?}:{Feature}` in PascalCase (e.g., `Kakeibo:MonthlyReflectionPrompts`). Maps to `__` double-underscore in env vars.
- **Clean Architecture Placement:** `IFeatureFlagService` in Application layer. Domain remains flag-agnostic (business logic is not conditional on deployment config). Controllers check flags to conditionally expose endpoints. Client components inject `IFeatureFlagClientService` and conditionally render UI.
- **Default Strategy:** Core Kakeibo/Kaizen features default ON. Experimental/incomplete features (Custom Reports, Advanced Charts, Geocoding stub, Candlestick chart with no data source) default OFF.
- **Blazor Pattern:** Scoped service fetches flags from API in `Program.cs` before root component render. Components use `@if (FeatureFlags.IsEnabled("Feature.Name"))` to conditionally render sections. Nav menu items conditionally shown based on flags + user settings.

**Implementation Order (Recommended):**
1. **Phase 1 (Immediate):** Budget categories Kakeibo routing, onboarding Kakeibo setup, transaction entry Kakeibo selector — foundational domain changes
2. **Phase 2 (Immediate):** Calendar heatmap, month intention prompt, week summary Kakeibo breakdown, day cell badges — visual philosophy layer
3. **Phase 3 (Soon):** Monthly reflection panel, Kaizen micro-goals, Kaizen dashboard — close the ritual loop
4. **Phase 4 (Soon):** Transaction list Kakeibo filter, AI Kakeibo awareness, settings preferences — supporting features
5. **Phase 5 (Low Priority):** Reports Kakeibo grouping, paycheck planner breakdown, location report filtering, export Kakeibo column — nice-to-have enhancements
6. **Phase 6 (Low Priority):** Feature flag system implementation, feature-flag Custom Reports Builder, user settings for feature toggles — infrastructure and simplification

**Deliverable:** `docs/129-feature-audit-kakeibo-alignment.md` — 27 features audited, 17 feature flags proposed, architecture fully specified (configuration shape, API surface, client pattern, naming conventions, default strategy). Ready for Lucius to implement.

### Feature Flag Runtime Toggleability Decision (2026-04-09)

**Request:** User wants feature flags toggleable at runtime without performance impact or process restart.

**Options Evaluated:**
1. **Option A** (`IOptionsMonitor<T>` file hot-reload): Free, zero new dependencies, hot-reload from JSON. **Blocker:** Env vars do not hot-reload in .NET; incompatible with Docker + Raspberry Pi (production deployment) where flags are set via `.env` file.
2. **Option B** (Database-backed + in-memory cache): Cache-first reads (zero per-request overhead), DB write on toggle, works everywhere. **Cost:** One new table, cache invalidation pattern.
3. **Option C** (File rewrite via API): Rewrite `appsettings.json` on toggle. **Blockers:** Container filesystem is ephemeral (changes lost on restart), non-idiomatic practice.

**Decision: Option B** — Database-backed with in-memory cache (5-minute TTL per-server, 1-hour client cache TTL).

**Rationale:**
Option B is the only approach compatible with all three deployment contexts (local dev with file config, Docker via env vars, and persistent Raspberry Pi setups). Zero per-request overhead via cache. Admin toggle endpoint (`PUT /api/v1/features/{name}`) is simple and secure. Database as source of truth is enterprise-standard practice.

**Key Design Decisions:**
- **Server cache TTL:** 5 minutes (eventually consistent, tolerates DB transient failures).
- **Client cache TTL:** Keep 1 hour as proposed in 129b; eventual consistency acceptable for rare admin toggles.
- **Admin endpoint:** `PUT /api/v1/features/{flagName}` with `{enabled: bool}` payload (admin-protected, invalidates cache immediately).
- **Seeding:** Migration seeds default flags from Feature 129 audit (17 flags, per-flag default on/off).

**Implications:**
- `docs/129-feature-audit-kakeibo-alignment.md`: Feature Flag Architecture section needs "Runtime Toggleability" subsection explaining cache strategy. Replace "file-based config only" with "database + optional file seeding".
- `docs/129b-feature-flag-implementation.md`: Add FeatureFlagsDbContext migration sketch, update FeaturesController to use cache + invalidation pattern, explain seed strategy.

**Deliverable:** `.squad/decisions/inbox/alfred-runtime-feature-flags.md` with full architecture, implementation checklist, and admin UI sketch.

### Feature 130: Serialization Alternatives Investigation — Research Complete (2026-04-10)

**Scope:** Evaluated 7 serialization candidates for network-level optimization targeting Raspberry Pi deployment (ARM64, bandwidth-constrained).

**Key Findings:**
1. **HTTP compression (Brotli) is the immediate win.** Already configured in `Program.cs`; just needs Pi testing. Provides 40-45% bandwidth reduction with zero breaking changes.
2. **System.Text.Json + source generation is the baseline.** No action needed; already optimal for uncompressed JSON.
3. **Binary formats (MessagePack, CBOR, Avro, FlatBuffers) are deferred.** Each adds 50-200 KB to Blazor bundle and custom formatter complexity. OpenAPI tooling incompatibility for all.
4. **Protocol Buffers + gRPC is rejected.** Architectural mismatch (polyglot RPC framework for single-tier app), breaking API changes, gRPC-Web kludge, OpenAPI incompatibility.

**Recommendation Hierarchy:**
1. Deploy Brotli (already configured) and test on Pi. Target: 40-45% bandwidth reduction.
2. Monitor post-deployment metrics. Only pursue binary formats if bandwidth remains a constraint.
3. If binary format needed, prioritize MessagePack (best ecosystem + encoding speed balance).
4. Never pursue Protocol Buffers + gRPC or Avro for this app.

**Bandwidth Reduction Estimates:**
- Brotli: -40-45% (immediate, zero cost).
- MessagePack over Brotli: +5-10% (marginal, high complexity cost).
- FlatBuffers over Brotli: +10-15% (only valuable for 10,000+ object exports, zero-copy semantics).

**Deliverable:** `docs/130-serialization-alternatives-investigation.md` — comprehensive 8-section analysis (What it is, Wire format, WASM compatibility, ASP.NET Core support, Pros, Cons, Bandwidth, CPU, OpenAPI, Verdict) + Appendix with performance benchmarks.
