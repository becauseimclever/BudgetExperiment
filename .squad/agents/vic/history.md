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
