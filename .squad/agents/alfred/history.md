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

### Branch Strategy Operationalization — APPROVED (2026-04-14)

- **Decision:** Operationalize trunk-based development with `develop` stabilization layer.
- **Implementation:** Lucius completed all changes; CI passes.
  - Feature branches now branch from and PR against `develop` (not `main`).
  - CI extended to run on both `main` and `develop`.
  - Release and Docker semantics unchanged (tag-driven from `main`).
- **Status:** ✅ APPROVED and operationalized. Team is ready for feature work.
- **Related:** `.squad/decisions.md` entry 1; orchestration logs dated 2026-04-12T22:52:28Z.

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

### 2026-04-09 — Feature Specs 148–153 Created

**Agent Task:** Create feature specification documents for all Critical and High findings from Vic's 2026-04-09 full principle audit.

**Deliverables:**
- 6 feature docs (148–153) addressing findings F-001 through F-007
- Commit: `bde4d03` — `docs: add feature specs 148-153 for Vic audit findings`

**Docs Created:**
| # | Slug | Finding | Severity |
|---|------|---------|----------|
| 148 | `statement-reconciliation-locale-fix` | F-001 | 🔴 Critical |
| 149 | `extract-icalendarservice-iaccountservice` | F-002 + F-003 | 🟠 High |
| 150 | `split-itransactionrepository-isp` | F-004 | 🟠 High |
| 151 | `extract-transactionfactory` | F-005 | 🟠 High |
| 152 | `god-application-services-split-plan` | F-006 | 🟠 High |
| 153 | `god-controllers-split-strategy` | F-007 | 🟠 High |

**Key Notes:**
- Doc 148 is critical and low-effort (7 lines across 4 Razor files) — recommend Lucius prioritize this first
- Doc 149 formally closes Decision #2 (2026-03-22) for the remaining two controllers (CalendarController, AccountsController)
- Docs 152–153 establish opportunistic split policy: long tail during feature work, top offenders in standalone PRs
- All docs in Proposed status, ready for Lucius to implement

**Merged to decisions.md** on 2026-04-09 by Scribe.

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

### Feature Docs 154–159: Vic Performance Audit Response — Complete (2026-04-09)

**Scope:** Created six feature specification documents addressing all Critical and High findings from Vic's 2026-04-09 performance audit (P-001 through P-007).

- Feature docs 154-159 created for Vic's performance audit findings
- P-001 (DataHealthService) is the Critical finding — triple memory load + O(n²) near-duplicate detection; gated behind `feature-data-health-optimized-analysis` feature flag
- Deployment target is Raspberry Pi ARM64 — performance severity is elevated throughout all findings
- Doc 157 (P-004 + P-005) is the infrastructure foundation that Doc 154 depends on; implement 157 first
- Doc 159 (P-007) requires a Product Owner decision: Option A (deprecate endpoint) vs Option B (add pagination); Alfred recommends Option A

**All docs committed as:** `bbd1c42`

---

### Feature 160: Pluggable AI Backend — Architecture Design (2026-04-XX)

**Requested by:** Fortinbra (via request to design pluggable AI backend feature)

**Research findings:**
- **Current stack:** 
  - `OllamaAiService` (Infrastructure) implements `IAiService` (Application layer interface)
  - Hard-coded Ollama-only backend; endpoint (`OllamaEndpoint`) is Ollama-specific
  - Configuration: `AiSettingsData`, `AiSettingsDto`, `AiDefaults` all Ollama-centric
  - DI registration (Infrastructure.DependencyInjection): `services.AddHttpClient<IAiService, OllamaAiService>();`
  - Both Ollama and llama.cpp expose OpenAI-compatible HTTP APIs (`/v1/chat/completions`, `/v1/models`, health checks)
  - ~150 lines of HTTP protocol logic in OllamaAiService can be shared

- **Architecture approach chosen: Strategy Pattern with shared base class**
  - Add `AiBackendType` enum to `BudgetExperiment.Shared` (Ollama, LlamaCpp)
  - Create abstract `OpenAiCompatibleAiService` base class in Infrastructure encapsulating shared HTTP logic
  - Refactor `OllamaAiService` to extend base class; implement `LlamaCppAiService` for llama.cpp
  - `IAiService` (public abstraction) unchanged — stays in Application, all consumers continue to work
  - Configuration gains `BackendType` property; `OllamaEndpoint` renamed to generic `EndpointUrl`
  - DI registration becomes conditional: based on `AiSettings:BackendType`, register appropriate implementation
  - Default: Ollama (zero-breaking-change for existing users)

- **OpenAI-compatible shared logic:** Request/response DTOs, timeout handling, JSON options, health check pattern, token counting
- **Backend-specific overrides:** Health check endpoint path, model list parsing (Ollama: `OllamaTagsResponse` struct; llama.cpp: `models` array)

**Feature doc created:** `docs/160-pluggable-ai-backend.md` — 7 phases, Strategy Pattern + base class, user stories, testing strategy, migration notes (zero-breaking-change), configuration examples.

---

### Feature Docs 148–153: Vic Principle Audit Response — Complete (2026-04-09)

**Scope:** Created six feature specification documents addressing all Critical and High findings from Vic's first full principle audit.

- **F-001 (Critical, Doc 148):** 7 bare `.ToString("C")` calls in 4 Statement Reconciliation Razor components. Replace with `FormatCurrency(CultureService.CurrentCulture)`. Low effort, high user-trust impact. Only Critical finding in the audit.
- **F-002 + F-003 (High, Doc 149):** `CalendarController` and `AccountsController` still inject concrete service types. Extract `ICalendarService` and `IAccountService`. **This closes Decision #2 from 2026-03-22** — the last two concrete-injecting controllers not addressed in the original DIP fix.
- **F-004 (High, Doc 150):** `ITransactionRepository` has 23 methods (ISP violation). Split into `ITransactionQueryRepository`, `ITransactionImportRepository`, `ITransactionAnalyticsRepository`. Retain `ITransactionRepository` as composition root for backward compat.
- **F-005 (High, Doc 151):** `Transaction` entity is 545 lines. Extract all 5+ static factory methods to `TransactionFactory` domain service. Entity retains state and behavior.
- **F-006 (High, Doc 152):** 18 Application services exceed 300 lines. Top 5 get standalone split PRs; remaining 13 split opportunistically during feature work. Feature-work-coupled policy established.
- **F-007 (High, Doc 153):** 4 API controllers exceed 300 lines. Split strategy: `TransactionsController` → Query + Batch, `RecurringTransactionsController` → CRUD + Instance, `RecurringTransfersController` → CRUD + Instance, `CategorySuggestionsController` → Minimal API pilot.

**All docs committed as:** `bde4d03`

---

### Features 131–136: Kakeibo Foundation Implementation Specs — Complete (2026-04-10)

**Scope:** Created six coordinated feature specification documents defining the foundational Kakeibo alignment work. Each document follows Feature 128's format with Status, Prerequisites, Feature Flags, Domain/API/UI Changes, and Acceptance Criteria.

**Documents Created:**

1. **Feature 131: Budget Categories — Kakeibo Category Routing**
   - Core foundation: `BudgetCategory.KakeiboCategory` enum field (Essentials/Wants/Culture/Unexpected).
   - Smart-defaulted migration (Groceries→Essentials, Dining→Wants, Education→Culture).
   - One-time Kakeibo Setup Wizard on first login post-migration.
   - No feature flag (always on).
   - **Dependency:** All downstream features depend on this.

2. **Feature 132: Transaction Entry — Kakeibo Selector**
   - Add `Transaction.KakeiboOverride` nullable field for per-transaction overrides.
   - KakeiboSelector component (4 icons: Essentials/Wants/Culture/Unexpected) in transaction modal.
   - Default to category routing; allow override for one-off exceptions.
   - Feature flag: `Features:Kakeibo:TransactionOverride` (default: true).
   - **Depends on:** Feature 131.

3. **Feature 133: Onboarding — Kakeibo Setup Step**
   - Extend onboarding from 4 to 5 steps; Step 5 explains four Kakeibo categories.
   - Show all Expense categories, ask user to confirm/correct Kakeibo routing.
   - Trigger on first login post-migration for existing users (`HasCompletedKakeiboSetup` flag).
   - No feature flag (always on).
   - **Depends on:** Feature 131.

4. **Feature 134: Calendar — Kakeibo Enhancements**
   - 6 enhancements: spending heatmap (green/amber/red), month savings progress bar, month-start intention prompt, week summary Kakeibo breakdown bars, day cell Kakeibo badges, month header reflection link.
   - Feature flags: `Features:Calendar:SpendingHeatmap` (true), `Features:Kakeibo:CalendarOverlay` (false), `Features:Kakeibo:MonthlyReflectionPrompts` (true).
   - Aggregation service: `IKakeiboCalendarService`.
   - **Depends on:** Features 131, 132; coordinates with Feature 135 (savings progress bar).

5. **Feature 135: Monthly Reflection Panel**
   - New entity: `MonthlyReflection` (year, month, SavingsGoal, ActualSavings, IntentionText, GratitudeText, ImprovementText).
   - UI: panel showing income/spending/savings, Kakeibo breakdown, fields for gratitude/improvement journaling.
   - Accessible from calendar month header; also standalone reflection history page.
   - Feature flag: `Features:Kakeibo:MonthlyReflectionPrompts` (shared with Feature 134, true).
   - **Depends on:** Features 131, 132, 134 (coordinates with calendar savings progress).

6. **Feature 136: Kaizen Micro-Goals**
   - New entity: `KaizenGoal` (WeekStartDate, Description, TargetAmount, KakeiboCategory, IsAchieved).
   - UI: week summary card showing goal + status (✓/✗), goal-setting modal, week-end achievement reminder.
   - Kaizen Dashboard report: 12-week rolling view with goal outcomes overlaid, trend line for improvable spend.
   - Feature flag: `Features:Kaizen:MicroGoals` (true).
   - **Depends on:** Features 131, 134.

**Architectural Decisions Embedded:**

- **KakeiboCategory placement:** Enum in Domain; field on `BudgetCategory`, optional override on `Transaction`. Routing is computed at read time (no per-transaction data mutations on category change).
- **Feature flags:** 3 new flags defined; Feature 131 has no flag (foundation, always on); Features 134 and 135 share `Features:Kakeibo:MonthlyReflectionPrompts`.
- **Migration defaults:** Smart name-based routing applied via startup seeder (not `HasData()`), allowing users to customize without losing changes on subsequent `dotnet ef database update`.
- **API Endpoints:** RESTful CRUD for reflections and goals; calendar endpoints enhanced with Kakeibo breakdown aggregations; monthly summary endpoint for reflection panel.
- **UI Patterns:** Non-blocking prompts (modals/inline), permissive defaults (can skip setup), no gamification, non-judgmental language.
- **Implementation precedence:** Feature 131 must complete first; 132–136 can follow in phases.

**All six documents follow Feature 128 spec style and are ready for implementation.**

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

## Feature 131–136: Kakeibo Foundation & Kaizen Integration — COMPLETE (2026-04-10)

Created 6 comprehensive feature specification documents:

1. **Feature 131: Budget Categories — Kakeibo Routing** — KakeiboCategory enum on BudgetCategory, optional KakeiboOverride on Transaction, non-destructive seeding strategy.
2. **Feature 132: Transaction Entry — Kakeibo Selector** — Per-transaction override UI, feature flag Features:Kakeibo:TransactionOverride (default: true).
3. **Feature 133: Onboarding — Kakeibo Setup** — Kakeibo intro as Step 5 of onboarding flow, reusable component.
4. **Feature 134: Calendar — Kakeibo Enhancements** — Spending heatmap, breakdown bars, badges overlay. Feature flags: Features:Calendar:SpendingHeatmap (true), Features:Kakeibo:CalendarOverlay (false), Features:Kakeibo:MonthlyReflectionPrompts (true).
5. **Feature 135: Monthly Reflection Panel** — MonthlyReflection entity (one per user per month), editable fields vary by month age. Shared flag with 134.
6. **Feature 136: Kaizen Micro-Goals** — KaizenGoal entity with ISO 8601 week starts, non-gamified achievement tracking (✓/✗), week-end reflection prompt. Feature flag Features:Kaizen:MicroGoals (true).

**Key Architectural Decisions:**
- KakeiboCategory as enum routing point (primary on category, optional override per transaction).
- Smart-defaulted migration via startup seeder (non-destructive, no HasData()).
- MonthlyReflection as separate domain entity (not nested in UserSettings).
- No gamification in micro-goals—intrinsic motivation via reflection.
- Week-end prompt is non-judgmental, dismissable.
- Reflection editability: current all-editable, past savings read-only, future preview-only.

**Dependencies:** All depend on Feature 129b (Feature Flag Infrastructure). Feature 131 is blocking for 132–136.

**Implementation Order:** 131 → 132 → 133 → 134–136 (3–4 weeks estimated).

**Decision Document:** .squad/decisions/inbox/alfred-feature-docs-131-136.md (12 decisions, testing strategy, open questions).


---

## 2026-04-09: Financial Accuracy Audit Framework (Alfred)

**Date:** 2026-04-09T02:38:31Z
**Status:** Framework Designed & Documented

### Deliverables

1. **docs/ACCURACY-FRAMEWORK.md** — Comprehensive specification of 10 financial invariants:
   - INV-1 through INV-10 covering accounts, transfers, budgets, paycheck allocation, recurring transactions, category assignment, reporting, and reconciliation
   - Precision standard: decimal-only arithmetic with MidpointRounding.AwayFromZero
   - Test project ownership matrix (Domain.Tests, Application.Tests, Api.Tests, Infrastructure.Tests)
   - Five identified test gaps (P1–P3) for implementation by Barbara
   - Accurate Test Location convention (Accuracy/ folder or AccuracyTests suffix)

2. **.squad/decisions/inbox/alfred-accuracy-framework.md** — Formal decision document
   - Ready for team review and acceptance
   - Merged to decisions.md by Scribe

### Key Architecture Decision

All monetary calculations must use decimal type exclusively. MoneyValue value object enforces MidpointRounding.AwayFromZero on construction. No float or double permitted in any financial computation path.

### Next Steps

Barbara (in parallel session) implemented 49 new accuracy tests across Domain.Tests and Application.Tests, targeting identified gaps. All 49 tests pass; no production bugs found.

---

### Feature 161: BudgetScope Removal — Specification Written (2026-04-10)

**Requested by:** Fortinbra (user directive from 2026-04-06)

**Scope & Context:**
- BudgetScope (Personal vs Shared) enum contradicts Kakeibo household-ledger philosophy (家計簿 = single household ledger, not personal ↔ shared duality)
- Enum lives in `BudgetExperiment.Shared/Budgeting/BudgetScope.cs` and referenced in ~80+ files across all layers
- High-impact, multi-phase removal required; phased delivery chosen to minimize risk

**Architecture approach: 4-phase elimination**
1. **Phase 1 (UI hide, low risk):** Remove ScopeSwitcher component; default to Shared/household; backend logic unchanged. 2–3 days. Ship safely.
2. **Phase 2 (API simplification, medium risk):** Remove BudgetScopeMiddleware, UserContext.BudgetScope, scope from all DTOs. 3–4 days. Breaking change; clean contracts.
3. **Phase 3 (Domain/Application purge, high risk):** Remove scope property from all entities, IUserContext, service method signatures, repository filters. 4–5 days. Extensive refactoring + test updates.
4. **Phase 4 (Database migration, data-critical):** Drop scope columns; validate staging; production rollout. 1–2 days. Requires backup + rollback plan.

**Deliverable:** `docs/161-budget-scope-removal.md` — comprehensive feature spec (23 KB) covering:
- Problem statement (scope duality breaks Kakeibo model)
- 5 user stories (one per phase)
- Technical design (architecture changes, files to delete/edit by layer)
- Phased delivery plan with risk mitigation
- Success metrics (test coverage, zero regressions, schema clean, UI clarity)
- TDD implementation strategy
- Breaking changes and non-breaking aspects

**Key insights:**
- Scope is not per-household; it's per-user preference (Personal/Shared toggle). This undermines the single-household-ledger model — all members should see and reflect on the same ledger.
- UI-centric change (Phase 1) can ship independently; backend remains scope-aware but hidden. Low risk, high confidence.
- Database migration (Phase 4) is the bottleneck; requires staging validation + production coordination.
- ~80+ file impact is manageable via phased delivery + TDD; each phase independently deployable.

**Alignment:**
- User directive captured in `.squad/decisions.md` (2026-04-06-235342)
- Aligns with Kakeibo philosophy reinforcement (Feature 128+)
- Antithetical to multi-user household model (households are collective, not individual scopes)
- No dependency on other features; can proceed in parallel with Feature 131–136 (Kakeibo refinement)

**Spec status:** Ready for team review. Feature 161 assigned (backlog) pending Vic's architecture audit feedback on phased approach.



## Session Update: Scribe Orchestration (2026-04-12T20:32:43Z)

**Merged from inbox to team decisions ledger:**

1. **Feature 160 (Alfred):** Architecture Decision — Pluggable AI Backend via Strategy Pattern + OpenAiCompatibleAiService base class. Approved. Implementation ready.
2. **Feature 161 (Alfred):** Specification complete (docs/161-budget-scope-removal.md). 4-phase elimination of BudgetScope enum to enforce Kakeibo single-household model. Ready for team review & scheduling.
3. **Controllers Standard (Fortinbra):** All API endpoints must use ASP.NET Core controllers. No Minimal API. CategorySuggestionEndpoints pilot reverted.
4. **Features 151–153 (Lucius):** TransactionFactory, Parsers (RuleSuggestionResponseParser, ImportRowProcessor, ChatActionParser), CategorySuggestionService, Controller splits. All tests green (Domain: 919, Application: 1125, Client: 2824).
5. **FeatureFlagClientService (Lucius):** Fixed singleton/scoped captive dependency by injecting IHttpClientFactory instead of HttpClient. Established pattern for new API controller tests.
6. **Perf Batch 156/159 (Lucius):** F-156 N+1 fix (ReportService), F-159 v2 pagination endpoint + v1 deprecation.
7. **KakeiboSetupBanner (Lucius):** Modal implementation (ModalSize.Small, overlay dismiss, footer buttons).
8. **Principle Re-Audit (Vic):** Findings post-151–153. Critical/High findings resolved. Decisions needed: Minimal API mapper pattern, god class reduction priority, controller growth monitoring.

**Outcome:** Lucius audit-ready. Two backend regressions fixed (TransactionRepository projections, AccountRepository default overload). Full test suite green (Application, API, Infrastructure; excluding Performance). Solution ready for merge.

**Post-Agent Tasks Complete:**
- ✅ Orchestration log: .squad/orchestration-log/2026-04-12T20-32-43Z-lucius.md
- ✅ Session log: .squad/log/2026-04-12T20-32-43Z-audit-ready.md
- ✅ Decisions merged to decisions.md; inbox cleared
- ✅ This history updated

## Session Update: Feature 160 Closeout (2026-04-13T03:43:13Z)

**Feature 160 (Pluggable AI Backend) — COMPLETE & APPROVED FOR MERGE**

**Alfred (Lead):**
- Reviewed Feature 160 architecture: all code, tests, API contracts correct and production-ready
- Initially rejected due to incomplete client UI (no BackendType selector)
- Approved final state after Lucius + Barbara completed client form and test coverage

**Barbara (Tester):**
- Validated Feature 160 via Docker-backed integration tests
- Infrastructure tests: 257 passed (Testcontainers PostgreSQL)
- API tests: 687 passed (full stack)
- Approved client test coverage: 2826 passed, 1 skipped (Category!=Performance)
- Feature approved for merge

**Lucius (Backend Dev):**
- Completed client UI: AiSettingsForm.razor with BackendType dropdown
- Implemented generic EndpointUrl binding with smart default swapping
- Updated docs/AI.md; added AI-BACKEND-EXTENSION.md guide
- All tests passing (Domain 924, Application 1136, Infrastructure 257, API 687, Client 2826)
- Feature approved for production

**Decisions Merged to .squad/decisions.md:**
- Decision #31: Feature 160 architecture & acceptance criteria (all met)
- Decision #32: User directive (main always releasable; use develop branch)
- Decision #33: User directive (Docker reminder for feature starts)

**Outcome:** Feature 160 eligible for merge. Architecture is sound, extensible, SOLID-compliant. All team members sign off.

---

**Post-Scribe Tasks Complete:**
- ✅ Orchestration logs: 2026-04-13T03-43-13Z-{alfred,barbara,lucius}.md
- ✅ Session log: 2026-04-13T03-43-13Z-feature-160-closeout.md
- ✅ Decisions merged to decisions.md; inbox cleared
- ✅ Agent histories updated (this entry)
