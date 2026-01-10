# Copilot Instructions / Engineering Guide

Authoritative guidance for GitHub Copilot and contributors. Keep this concise, up‑to‑date, and PR‑reviewed before changing architectural decisions.

## 1. Solution Purpose
Budget Experiment: .NET 10 solution with a clean, test-first, modular architecture for a budgeting domain. Emphasis on maintainability, SOLID, Clean Code, and proper RESTful API design.

## 2. High-Level Architecture (Clean / Onion Hybrid)
Layers (outer → inner depends inward only):
- Client (Blazor WebAssembly UI) – presentation layer using FluentUI-Blazor.
- API (ASP.NET Core Minimal or Controllers) – REST interface, validation, auth, OpenAPI + Scalar UI.
- Application / Services – use cases (business workflows), orchestration, domain-centric service interfaces.
- Domain / Models – entities, value objects, enums, domain events, interfaces (abstractions only, NO EF types).
- Infrastructure (Repository / Data Access / External Integrations) – EF Core (Npgsql), repositories, migrations, external service adapters.

## 3. Projects (All under `src/`)
- `BudgetExperiment.Domain` (pure C# domain models, value objects, interfaces, domain exceptions)
- `BudgetExperiment.Application` (services, DTOs, validators, mapping, domain event handlers)
- `BudgetExperiment.Infrastructure` (EF Core DbContext, repository implementations, migrations, third-party adapters)
- `BudgetExperiment.Api` (REST API, DI wiring, OpenAPI, Scalar page, versioning, error handling)
- `BudgetExperiment.Client` (Blazor WebAssembly app + FluentUI-Blazor components)

Tests under `tests/` mirroring structure:
- `BudgetExperiment.Domain.Tests`
- `BudgetExperiment.Application.Tests`
- `BudgetExperiment.Infrastructure.Tests`
- `BudgetExperiment.Api.Tests`
- `BudgetExperiment.Client.Tests` (bUnit for component tests if needed)

## 4. Technology Stack
- .NET 10
- Blazor WebAssembly + FluentUI-Blazor (UI)
- ASP.NET Core API
- EF Core + Npgsql (PostgreSQL)
- xUnit (unit tests) + Shouldly OR built-in Assert (NO FluentAssertions, NO AutoFixture)
- Mapping: manual or source generators (avoid AutoMapper until justified)

## 5. Naming & Conventions
- C# Style: PascalCase types/members (except local variables & private fields). Private fields: `_camelCase`.
- Project folders: `src/ProjectName`, `tests/ProjectName.Tests`.
- REST endpoints: `/api/v{version}/{resource}`. Version with **URL segment** (start at v1). Resource names plural (`/budgets`, `/transactions`).
- Domain value objects end with `Value` (e.g., `CurrencyValue`).
- Async methods end with `Async`.
- Interfaces: prefix `I` (except domain events if using record marker types).

## 6. TDD Workflow (ALWAYS)
1. Write a failing unit test (RED) – smallest vertical slice (domain first, then application, then API edge).
2. Implement minimal code to pass (GREEN).
3. Refactor with SOLID & Clean Code (REFACTOR) – ensure tests still pass.
4. Add integration tests for repository / API boundary when behavior stable.
5. For new API endpoint: contract (OpenAPI spec auto-generated) validated before implementation details.

## 7. SOLID Enforcement Quick Cues
- SRP: One reason to change – extract cohesive services.
- OCP: Prefer extension via new types, sealed where appropriate for safety.
- LSP: Substitutable abstractions (avoid throwing NotImplementedException in derived classes).
- ISP: Lean interfaces (split broad repository behaviors as needed – e.g., `IReadRepository<T>`, `IWriteRepository<T>`).
- DIP: Higher layers depend on abstractions in Domain/Application; Infrastructure supplies implementations.

## 8. Clean Code Guidelines
- Short methods (< ~20 lines target; justify exceptions).
- Guard clauses > nested conditionals.
- No commented-out code; remove or justify with TODO (dated, owner tagged).
- One assertion intent per test (logical grouping allowed).
- Avoid primitive obsession: introduce value objects (e.g., Money, CategoryName).
- Centralize constants; avoid magic strings/numbers.

## 9. REST API Design
- Use proper HTTP methods: GET (read), POST (create), PUT (full update), PATCH (partial), DELETE (remove), HEAD/OPTIONS as needed.
- Status Codes: 200/201/204, 400 (validation), 404 (not found), 409 (conflict), 422 (semantic validation), 500 (unexpected). No 200 for errors.
- Idempotency: PUT and DELETE must be idempotent; create idempotency keys if needed for external calls.
- Pagination: Use `?page=1&pageSize=20` (document defaults & max). Return `X-Pagination-TotalCount` header.
- Filtering: `?filter.field=value` or simple `?field=value` (choose one approach consistently).
- Sorting: `?sort=field` or `?sort=-field` for descending.
- Error Shape (Problem Details): RFC 7807 (`application/problem+json`). Include `traceId`.
- ETags / Concurrency: Support optimistic concurrency for mutable aggregates (header `If-Match`).
- HATEOAS optional – avoid until value clear; keep design open.

## 10. OpenAPI & Scalar Page
- Enable OpenAPI generation in all environments except production can restrict UI.
- Scalar UI served at `/scalar` (or `/api/scalar`).
- Ensure spec describes schemas for all DTOs, uses tags per resource, includes version.
- Provide examples in OpenAPI via attributes or XML comments.

## 11. Data & Persistence
- EF Core DbContext in Infrastructure only. Migrations live in Infrastructure.
- Repositories expose aggregate root operations. Avoid generic repository abuse; tailor interfaces.
- Transactions: Use `IDbContextTransaction` or ambient transaction in Application service where needed.

## 12. Domain Model Rules
- Entities: Identity + behavior. Value objects: Immutable, equality by components.
- Domain events optional; introduce when cross-aggregate side effects appear.
- No direct references to EF Core annotations inside Domain; use Fluent configuration in Infrastructure.

## 13. Services Layer
- Orchestrate domain objects; no direct controllers -> repositories without application service mediation (except trivial pass-through can still wrap for future evolution).

## 14. Dependency Injection
- Composition root: `BudgetExperiment.Api` `Program.cs`.
- Configure per layer registration extension methods (e.g., `AddDomain()`, `AddApplication()`, `AddInfrastructure(configuration)`).

## 15. Testing Strategy
- Unit: Domain logic (pure, fast). Application services with repositories mocked (custom hand-written fakes or minimal mocking library like NSubstitute / Moq – choose one and stay consistent).
- Integration: EF Core with test database (Use PostgreSQL test container OR SQLite in-memory only if behavior parity assured; prefer Testcontainers for fidelity).
- API: Use `WebApplicationFactory` for endpoint tests (happy + failure paths).
- UI: Optional bUnit for component logic; keep thin components.
- Performance / Load: Add later if needed.
- NO: FluentAssertions, AutoFixture.

## 16. Logging & Observability
- Structured logging (Serilog or built-in). Log correlation IDs (traceId). Minimal logging in hot paths.
- Metrics/Health: `/health` endpoint (liveness/readiness). Add basic counters if needed.

## 17. Security & Configuration
- Store connection strings & secrets outside code (user-secrets/local env). NEVER commit secrets.
- **Database Connection**: The database connection string (`AppDb`) is stored in user secrets for the API project (`BudgetExperiment.Api`). Use `dotnet user-secrets set "ConnectionStrings:AppDb" "<connection-string>"` to configure locally.
- Validate all external inputs (DTO validation). Avoid over-posting.
- Use HTTPS redirection & security headers middleware.

## 18. Style / Analyzers
- Enable nullable reference types.
- Treat warnings as errors (except explicitly documented suppressions).
- StyleCop enforced via `StyleCop.Analyzers` NuGet (added centrally in `Directory.Build.props`).
- Root `stylecop.json` + `.editorconfig` define rule set (ordering, documentation, spacing, usings placement outside namespace).
- All analyzer warnings (including StyleCop) escalate to errors; no blanket suppressions. If a suppression is truly required: add scoped `#pragma warning disable <ID>` with a dated TODO + issue link directly above the line and re-enable immediately after.
- Do NOT globally disable rules without architectural review.
- Use `dotnet format` (with analyzers) before committing.
- Add missing XML docs only for public API surface (internal/private XML docs optional unless rule escalated for specific cases).

## 19. DTO & Mapping Rules
- Controllers expose DTOs, never domain entities.
- Mapping central: manual static mappers or extension methods. Keep explicit and test critical mappings.

## 20. Versioning & Deprecation
- Start at v1. Provide `api-supported-versions` header.
- Deprecate endpoints with `Deprecation` and `Sunset` headers when needed.

## 21. Build & Folder Layout (Example)
```
root
 ├─ src
 │   ├─ BudgetExperiment.Domain
 │   ├─ BudgetExperiment.Application
 │   ├─ BudgetExperiment.Infrastructure
 │   ├─ BudgetExperiment.Api
 │   └─ BudgetExperiment.Client
 └─ tests
     ├─ BudgetExperiment.Domain.Tests
     ├─ BudgetExperiment.Application.Tests
     ├─ BudgetExperiment.Infrastructure.Tests
     ├─ BudgetExperiment.Api.Tests
     └─ BudgetExperiment.Client.Tests
```

## 22. Git / Branching
- Main: stable.
- Feature branches: `feature/<short-desc>`.
- Always include tests in same PR; failing tests block merge.

## 23. PR Checklist (Enforced Culturally)
- [ ] Follows TDD (tests precede or included logically)
- [ ] No banned libraries (FluentAssertions, AutoFixture)
- [ ] Naming consistent
- [ ] OpenAPI docs updated (if API changes)
- [ ] No unnecessary comments / dead code
- [ ] Unit + integration tests green

## 24. Forbidden / Discouraged
- FluentAssertions
- AutoFixture
- Leaking EF Core types outside Infrastructure
- God services ( > ~300 lines or too many responsibilities )

## 25. Copilot Prompting Patterns
Good:
- "Write a xUnit test (Arrange/Act/Assert) for MoneyValue addition overflow rule (expect DomainException)."
- "Generate repository interface for Budget aggregate with GetByIdAsync and AddAsync only."
Avoid vague prompts ("write code for budgets"). Always specify layer & intent.

## 26. Incremental Delivery Order (Suggested)
1. Domain primitives & core entities + unit tests.
2. Infrastructure: DbContext + migrations + repository skeletons.
3. Application services + tests (faking repos).
4. API project (controllers/endpoints + OpenAPI + Scalar).
5. Blazor Client shell + FluentUI integration.
6. Integration tests (data + API).
7. Additional features/optimizations.

## 27. Scalar Integration (Reference Notes)
- Add Scalar NuGet package (e.g., `Scalar.AspNetCore` if available at implementation time).
- Serve page at `/scalar` referencing generated OpenAPI doc `/swagger/v1/swagger.json` (adjust naming if needed).
- Link from root `/` or provide 302 redirect in development.

## 28. Error Handling Pattern
Central middleware converting exceptions → ProblemDetails. Domain validation → 400 or 422; concurrency → 409; not found → 404.

## 29. Concurrency & IDs
- Prefer GUIDs (comb/ulid acceptable) for aggregate keys; ensure DB indexing strategy documented.

## 30. Date/Time Handling
- **Backend & Database:** All `DateTime` values must be stored and transmitted in **UTC**. Use `DateTime.UtcNow` (not `DateTime.Now`). Domain entities use `DateTime` with UTC assumption; document with `CreatedAtUtc` naming if clarity needed.
- **DateOnly:** For date-only fields (e.g., transaction date), use `DateOnly` which has no time zone implications.
- **API:** Return UTC timestamps in ISO 8601 format (`2026-01-09T14:30:00Z`). Accept UTC or convert incoming local times to UTC immediately.
- **Client (Blazor):** Detect user's local time zone via JavaScript interop (`Intl.DateTimeFormat().resolvedOptions().timeZone`) and convert UTC to local for display. Store the detected time zone in a service for consistent formatting throughout the UI.
- **Never** store local times in the database or pass non-UTC `DateTime` values across the API boundary.

## 31. Continuous Improvement
Refactor safely behind tests. Each new concept: add a unit test first.

---
Keep this file lean—prune when obsolete. Update when architectural decisions shift.

## 32. Shell / Scripting Guidelines
- PowerShell commands must use fully qualified paths (no reliance on `~`, `$HOME`, or implicit working directory). Example: `dotnet sln c:\ws\BudgetExperiment\BudgetExperiment.sln add c:\ws\BudgetExperiment\src\BudgetExperiment.Domain\BudgetExperiment.Domain.csproj`.
- Avoid stateful assumptions about current directory; each command should be executable in isolation.
- Prefer explicit paths when creating, editing, or referencing solution/project files to ensure reproducibility across developer environments.

## 33. NuGet Package Management
- Always add or update NuGet dependencies using the `dotnet` CLI, not by manually editing `.csproj` files.
- Preferred pattern (explicit paths + version pin):
    - `dotnet add c:\ws\BudgetExperiment\tests\BudgetExperiment.Api.Tests\BudgetExperiment.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing --version 10.0.0`
- Do NOT hand-edit `<ItemGroup><PackageReference .../></ItemGroup>` blocks unless performing a mechanical conflict resolution that cannot be expressed via CLI (rare—document justification if needed).
- When removing a package: `dotnet remove <csprojPath> package <PackageName>`.
- Keep versions explicit (no floating ranges) to preserve reproducibility; update via intentional CLI commands.

## 34. Client-Server Development Workflow
- **CRITICAL**: The Blazor WebAssembly client (`BudgetExperiment.Client`) is hosted by the API (`BudgetExperiment.Api`) and should NEVER be run standalone.
- **Only run the API**: `dotnet run --project c:\ws\BudgetExperiment\src\BudgetExperiment.Api\BudgetExperiment.Api.csproj`
- The API serves both the REST endpoints AND hosts the Blazor WebAssembly client.
- The client is served from the API's wwwroot and makes HTTP calls to the same server's API endpoints.
- For development, only one terminal is needed:
  - Terminal: API server (usually http://localhost:5099) - this serves both API and client
- When testing or debugging client features, ensure only the API is running - the client will be automatically served by the API.

## 35. Docker & Deployment (CI/CD Only)
- **Local Development**: NEVER use Docker locally. Use standard `dotnet run` workflow (see section 33).
- **Docker is for deployment only**: Raspberry Pi and production servers pull pre-built images.
- **CI/CD Pipeline**: GitHub Actions (`.github/workflows/docker-build-publish.yml`) automatically builds multi-architecture (amd64, arm64) Docker images on push to `main` or version tags.
- **Image Registry**: Images published to `ghcr.io/becauseimclever/budgetexperiment` (GitHub Container Registry).
- **Dockerfile**: Multi-stage build (`Dockerfile`) - builds from source, no pre-build required.
- **Deployment**: Raspberry Pi uses `docker-compose.pi.yml` to pull and run images from ghcr.io.
- **Database Connection**: Passed via environment variable `ConnectionStrings__AppDb` from `.env` file (never committed).
- **Legacy Scripts**: All legacy local build/deploy scripts (e.g., `build-docker-windows.ps1`, `deploy-to-pi.ps1`, `deploy.sh`) have been removed from the repository. Do not add or reference local Docker scripts. Use CI/CD-built images and deploy by pulling with `docker compose` on the target device.
- **Documentation**: See `README.Docker.md` for deployment guide, `DEPLOY-QUICKSTART.md` for quick Pi setup.
- **Important**: Do NOT create or suggest local Docker build workflows. Direct users to CI/CD pipeline or standard .NET local development.
