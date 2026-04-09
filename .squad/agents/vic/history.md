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

<!-- Vic appends audit findings and project observations here over time -->
