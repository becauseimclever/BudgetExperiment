# Vic — History & Learnings

## Project Context

- **Project:** BudgetExperiment — .NET 10 budgeting app with Blazor WebAssembly client
- **User:** Fortinbra
- **Stack:** .NET 10, ASP.NET Core Minimal API, Blazor WebAssembly, EF Core + Npgsql (PostgreSQL), xUnit + Shouldly, StyleCop, Scalar UI
- **Universe:** DC Universe (Batman)
- **Architecture:** Clean/Onion hybrid — Domain → Application → Infrastructure → API → Client → Contracts → Shared
- **Test count (baseline):** ~5,716 tests across 5 projects (Domain, Application, Infrastructure, API, Client)
- **Guiding documents:** `docs/ACCURACY-FRAMEWORK.md` (10 financial invariants), Copilot Instructions (Engineering Guide in copilot-instructions.md)

## Team

- **Alfred** — Lead. Architecture decisions, code quality standards, SOLID/Clean Code compliance
- **Lucius** — Backend Dev. .NET implementation, EF Core, repositories, domain models, API endpoints
- **Barbara** — Tester. Test strategy, accuracy tests, coverage gaps
- **Scribe** — Silent. Session logs, decision merges
- **Ralph** — Work Monitor. GitHub issue tracking and backlog management

## Key Decisions (from decisions.md)

Read `.squad/decisions.md` at spawn time for the full current decision ledger.

## Known Context

- The project has a formal **financial accuracy framework** (`docs/ACCURACY-FRAMEWORK.md`) with 10 invariants (INV-1 through INV-10). Three gaps (145–147) are identified and have feature specs.
- **Feature flags** are used to gate new behavior; features should be independently toggleable.
- **Kakeibo** is the core budgeting methodology: Essentials, Wants, Culture, Unexpected buckets.
- **Performance tests** are excluded from normal runs via `--filter "Category!=Performance"`.
- No FluentAssertions, no AutoFixture. Use Shouldly or xUnit Assert only.
- All monetary values use `decimal` — no float/double anywhere in money paths.
- Authentication uses Authentik (OIDC/PKCE). Connection strings are in user secrets, never committed.

## Learnings

### 2026-04-09 — Full Principle Audit

**Report:** `docs/audit/2026-04-09-full-principle-audit.md`
**Findings:** 18 (1 Critical, 6 High, 9 Medium, 2 Low)
**Team inbox:** `.squad/decisions/inbox/vic-audit-findings.md`

**Key observations:**
- Financial arithmetic (MoneyValue, decimal, rounding) is solid — no calculation accuracy issues
- Critical gap: 7 bare `.ToString("C")` calls in Statement Reconciliation bypass FormatCurrency() — financial display risk
- Decision #2 (DIP: extract interfaces for controllers) remains incomplete for CalendarController and AccountsController
- ITransactionRepository has grown to 23 methods — ISP violation, needs splitting
- 23 god classes (>300 lines) across Domain (5) and Application (18)
- 4 controllers exceed 300 lines
- UTC discipline is excellent (zero DateTime.Now in codebase)
- No banned libraries found (FluentAssertions, AutoFixture)
- Layer separation is clean — no EF Core leakage into Domain or Application
- Test assertion framework (Shouldly vs Assert) is inconsistently mixed across projects

### 2026-04-09 — Performance Code Review

**Report:** `docs/audit/2026-04-09-performance-review.md`
**Findings:** 17 (1 Critical, 6 High, 7 Medium, 3 Low)
**Team inbox:** `.squad/decisions/inbox/vic-performance-findings.md`

**Key observations:**
- Critical: DataHealthService loads all transactions into memory 3 times per AnalyzeAsync call — OOM risk on Pi
- High: BudgetProgressService and ReportService both exhibit N+1 query patterns in loops
- High: 4 repository methods return unbounded result sets (GetUncategorizedAsync, GetAllForHealthAnalysisAsync, GetAllDescriptionsAsync, GetAllWithLocationAsync)
- High: GET /transactions endpoint has no pagination
- Medium: Zero `<Virtualize>` usage in Blazor client; missing `@key` on list loops
- Medium: Correlated subquery for account-name sorting in GetUnifiedPagedAsync
- Strengths: Consistent AsNoTracking, Task.WhenAll parallel loading, server-side pagination on primary endpoints, no lazy loading, projection queries in several key spots

### 2026-04-09 — Full Principle Audit & Performance Review Complete

**Two complete audits delivered:**

1. **Full Principle Audit** (report: `docs/audit/2026-04-09-full-principle-audit.md`)
   - Assessed against engineering guide, SOLID principles, Clean Code, REST API standards
   - 18 findings: 1 Critical (F-001 financial display), 6 High (DIP, ISP, god classes), 9 Medium, 2 Low
   - Merged to decisions.md on 2026-04-09

2. **Performance Code Review** (report: `docs/audit/2026-04-09-performance-review.md`)
   - Assessed application services, repositories, and Blazor UI for scalability and efficiency
   - 17 findings: 1 Critical (P-001 memory efficiency), 6 High (N+1 queries, unbounded results), 7 Medium, 3 Low
   - Merged to decisions.md on 2026-04-09

**Critical Path Identified:**
- **F-001** (statement reconciliation locale fix) — 7 bare `.ToString("C")` calls bypass FormatCurrency() requirement (§38). Financial accuracy risk. Low effort, high impact.
- **P-001** (DataHealthService memory leak) — Loads all transactions 3× per call on Pi with 5K+ transactions. OOM risk.
- **P-002** (BudgetProgressService N+1) — 20 sequential DB round-trips per call (one per category).

**Team Decision Needed:**
Performance findings P-001 and P-002 — should team prioritize immediate fixes or batch all High findings (6 total) into a performance sprint?

**Feature specs 148–153 created by Alfred** to address all Critical+High principle findings (F-001 through F-007). Merged to decisions.md on 2026-04-09.

### 2026-04-10 — Principle Re-Audit (Post F-151 through F-153)

**Report:** `docs/audit/2026-04-10-principle-reaudit-post-151-153.md`
**Findings:** 3 new (0 Critical, 0 High, 1 Medium, 2 Low)
**Prior findings status:** 4 Resolved (F-001, F-002, F-003, F-004, F-007), 2 Partially Resolved (F-005, F-006)

**Key observations:**
- **F-001 fully resolved:** All 7 bare `.ToString("C")` calls in Statement Reconciliation replaced with `.FormatCurrency(Culture.CurrentCulture)`
- **DIP violations resolved:** CalendarController and AccountsController now use interfaces (`ICalendarService`, `IAccountService`)
- **ISP violation resolved:** ITransactionRepository split into `ITransactionQueryRepository`, `ITransactionImportRepository`, `ITransactionAnalyticsRepository`
- **Controller splits successful:** TransactionsController (401 lines) → TransactionQueryController (198) + TransactionBatchController (235)
- **Minimal API pilot introduced:** CategorySuggestionsController replaced with CategorySuggestionEndpoints (Minimal API pattern)
- **ChatActionParser refactored:** 482 → 174 lines via extraction of per-action parsers
- **ImportRowProcessor reduced:** 508 → 323 lines via field extractor/parser extraction

**Remaining god class debt:**
- 17 Application services still exceed 300 lines (ChatService 487, RuleSuggestionResponseParser 472 at top)
- 9 Domain entities exceed 300 lines (Transaction 532, RuleSuggestion 486 at top)
- 4 controllers at 300-line boundary (302-323 lines)

**New patterns introduced:**
- `TransactionFactory` pattern for entity factory extraction
- Minimal API endpoint groups as controller alternative
- Per-action-type parser extraction pattern (ChatActionParser → TransactionActionParser, etc.)

**Architectural question for team:** Should Minimal API endpoints use inline mappers or Application-layer mappers?
