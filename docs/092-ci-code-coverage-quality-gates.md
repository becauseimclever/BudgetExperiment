# Feature 092: CI Code Coverage & Quality Gates
> **Status:** Planning  
> **Priority:** High  
> **Dependencies:** Coverlet already present in all test projects

## Overview

Add code coverage collection and reporting to the GitHub Actions CI pipeline, and establish quality gates that prevent pull requests from merging when test coverage is inadequate. Coverage results will be collected via Coverlet during the test step, merged into a unified Cobertura report, reported as a PR comment and workflow summary, and enforced with configurable minimum thresholds.

This follows a **vertical slice** approach: each phase delivers a fully working, independently valuable increment—from basic collection through reporting, then enforcement, and finally developer experience improvements.

## Problem Statement

### Current State

- All test projects already reference `coverlet.collector` (v6.0.4).
- The CI workflow (`ci.yml`) runs tests but does **not** collect or report coverage.
- There is no mechanism to block a PR that reduces or lacks adequate test coverage.
- Developers have no visibility into coverage trends without running local tools manually.

### Target State

- Every CI run collects code coverage from all non-E2E test projects.
- A merged Cobertura report is produced as a build artifact.
- PR comments display a coverage summary (per-project and total).
- The workflow **fails** if total line coverage drops below a configurable threshold (e.g., 80%).
- Coverage badges or summary links are available in the repository README.

---

## User Stories

### Coverage Collection

#### US-092-001: Collect Coverage in CI
**As a** contributor  
**I want** code coverage data collected automatically on every CI run  
**So that** I have objective metrics on how well the codebase is tested

**Acceptance Criteria:**
- [ ] `dotnet test` in CI passes `--collect:"XPlat Code Coverage"` to Coverlet
- [ ] Individual Cobertura XML files are produced per test project
- [ ] Coverage files are uploaded as build artifacts

### Coverage Reporting

#### US-092-002: PR Coverage Summary
**As a** PR reviewer  
**I want** a coverage summary posted as a PR comment and/or check  
**So that** I can quickly assess whether the change maintains adequate test coverage

**Acceptance Criteria:**
- [ ] A coverage report action (e.g., `danielpalme/ReportGenerator` or `irongut/CodeCoverageSummary`) merges per-project results
- [ ] Coverage summary appears in the GitHub Actions job summary
- [ ] On pull requests, a comment shows overall and per-project line/branch coverage

### Quality Gates

#### US-092-003: Enforce Minimum Coverage Threshold
**As a** project maintainer  
**I want** PRs to fail CI when total line coverage is below a configured minimum  
**So that** code quality does not degrade over time

**Acceptance Criteria:**
- [ ] A configurable threshold (default 80% line coverage) is defined in the workflow
- [ ] The CI job fails with a clear message when coverage is below the threshold
- [ ] The threshold value is easy to adjust (single YAML variable or workflow input)

#### US-092-004: Branch Protection Integration
**As a** repository admin  
**I want** the coverage check to be a required status check on `main`  
**So that** PRs cannot be merged without passing coverage gates

**Acceptance Criteria:**
- [ ] The coverage gate is a distinct job or step with a clear pass/fail status
- [ ] GitHub branch protection rules can reference this check by name

### Developer Experience

### Coverage Accuracy

#### US-092-005: Exclude Boilerplate from Coverage
**As a** project maintainer  
**I want** framework boilerplate and configuration classes excluded from coverage metrics  
**So that** coverage percentages reflect the actual quality of business logic tests

**Acceptance Criteria:**
- [ ] `[ExcludeFromCodeCoverage]` added to all identified Api boilerplate classes (~17 files)
- [ ] `[ExcludeFromCodeCoverage]` added to all identified Infrastructure boilerplate classes (~25 files)
- [ ] `[ExcludeFromCodeCoverage]` added to Application `DependencyInjection`
- [ ] EF Migrations excluded via assembly-level attribute
- [ ] Coverage re-run confirms improved percentages reflect actual tested code

### Developer Experience

#### US-092-006: Coverage Badge in README
**As a** visitor or contributor  
**I want** a coverage badge displayed in the README  
**So that** I can see the project's current test coverage at a glance

**Acceptance Criteria:**
- [ ] A coverage badge (shields.io or similar) is added to README.md
- [ ] Badge value updates automatically from CI results

---

## Technical Design

### Architecture Changes

No application architecture changes. This feature modifies CI/CD configuration files, adds `[ExcludeFromCodeCoverage]` attributes to boilerplate classes, and updates documentation.

### Workflow Changes (`ci.yml`)

**Vertical slice 1 — Collection:**

```yaml
- name: Run tests with coverage
  run: |
    dotnet test BudgetExperiment.sln \
      --configuration Release \
      --no-build \
      --filter "FullyQualifiedName!~E2E&Category!=ExternalDependency" \
      --collect:"XPlat Code Coverage" \
      --results-directory ./TestResults \
      --logger "trx"
```

**Vertical slice 2 — Merge & Report:**

Use `danielpalme/ReportGenerator-GitHub-Action` to merge Cobertura files:

```yaml
- name: Merge coverage reports
  uses: danielpalme/ReportGenerator-GitHub-Action@5
  with:
    reports: ./TestResults/**/coverage.cobertura.xml
    targetdir: ./CoverageReport
    reporttypes: Cobertura;MarkdownSummaryGithub

- name: Publish coverage summary
  run: cat ./CoverageReport/SummaryGithub.md >> $GITHUB_STEP_SUMMARY
```

**Vertical slice 3 — Quality Gate:**

```yaml
- name: Enforce coverage threshold
  uses: irongut/CodeCoverageSummary@v1.3.0
  with:
    filename: ./CoverageReport/Cobertura.xml
    fail_below_min: true
    thresholds: '80 90'
    format: markdown
    output: both
```

**Vertical slice 4 — PR Comment:**

```yaml
- name: Add coverage PR comment
  uses: marocchino/sticky-pull-request-comment@v2
  if: github.event_name == 'pull_request'
  with:
    recreate: true
    path: code-coverage-results.md
```

### Excluded from Coverage

Exclude boilerplate and framework-dependent setup code that is explicitly not unit tested, using `[ExcludeFromCodeCoverage]` attributes on classes and assembly-level suppressions for generated code.

#### Assembly-Level Exclusions (via `GlobalSuppressions.cs` or `coverlet.runsettings`)

- `BudgetExperiment.E2E.Tests` — end-to-end tests, not a coverage target
- Generated migration code (`BudgetExperiment.Infrastructure.Persistence.Migrations.*`)

#### Class-Level Exclusions (`[ExcludeFromCodeCoverage]`)

**BudgetExperiment.Api:**
- `Program.cs` — DI wiring, middleware registration (top-level statements)
- Auth option models: `AuthenticationOptions`, `AuthModeConstants`, `AuthProviderConstants`, `AuthentikOptions`, `AuthentikProviderOptions`, `GenericOidcProviderOptions`, `GoogleProviderOptions`, `MicrosoftProviderOptions`, `ClientConfigOptions`
- Authentication handlers: `FamilyUserContext`, `GenericOidcClaimMapper`, `GoogleClaimMapper`, `MicrosoftClaimMapper`, `NoAuthHandler`
- Middleware: `ExceptionHandlingMiddleware`, `BudgetScopeMiddleware`
- Health checks: `MigrationHealthCheck`
- Markers: `ApiMarker`
- `VersionController` — static version info only

**BudgetExperiment.Infrastructure:**
- `DependencyInjection` — DI registration
- `BudgetDbContext` — EF Core DbContext wrapper
- `DesignTimeBudgetDbContextFactory` — migration tooling factory
- `DatabaseOptions` — connection config model
- All 22 EF Fluent Configuration classes (`Persistence/Configurations/*`)
- All migration files (assembly-level exclusion)

**BudgetExperiment.Application:**
- `DependencyInjection` — DI registration

#### What Stays IN Coverage

- All domain controllers (26 controllers with REST endpoint logic)
- All repository implementations (19 repositories)
- All domain entities and value objects
- All application services
- API Models/DTOs
- External service adapters (`OllamaAiService`, `NominatimGeocodingService`)
- `DatabaseSeeder`

### API Endpoints

None — this is a CI-only feature.

### Database Changes

None.

### UI Components

None.

---

## Implementation Plan

### Phase 0: Exclude Boilerplate from Coverage (Vertical Slice 0)

**Objective:** Add `[ExcludeFromCodeCoverage]` to framework setup and configuration classes so coverage metrics reflect actual business logic quality.

**Tasks:**
- [ ] Add `[ExcludeFromCodeCoverage]` to Api boilerplate: auth options, middleware, auth handlers, health checks, `ApiMarker`, `VersionController`
- [ ] Add `[ExcludeFromCodeCoverage]` to Infrastructure boilerplate: `DependencyInjection`, `BudgetDbContext`, `DesignTimeBudgetDbContextFactory`, `DatabaseOptions`, all EF Configurations
- [ ] Add `[ExcludeFromCodeCoverage]` to Application `DependencyInjection`
- [ ] Add assembly-level `[ExcludeFromCodeCoverage]` for Migrations namespace in `GlobalSuppressions.cs`
- [ ] Run coverage locally and verify improved baseline

**Commit:**
```bash
git add .
git commit -m "chore: exclude boilerplate from code coverage metrics

- Add [ExcludeFromCodeCoverage] to framework setup classes
- Api: auth options, middleware, auth handlers, health checks
- Infrastructure: DI, DbContext, EF configurations, migrations
- Application: DI registration

Refs: #092"
```

---

### Phase 1: Collect Coverage in CI (Vertical Slice 1)

**Objective:** Add `--collect:"XPlat Code Coverage"` to the existing test step and upload raw coverage artifacts.

**Tasks:**
- [ ] Modify `dotnet test` command in `.github/workflows/ci.yml` to include `--collect:"XPlat Code Coverage"`
- [ ] Upload `TestResults/**/coverage.cobertura.xml` as a build artifact
- [ ] Verify coverage XML is produced for each test project
- [ ] Apply same change to `docker-build-publish.yml` test step

**Commit:**
```bash
git add .
git commit -m "ci: collect code coverage during CI test runs

- Add --collect:'XPlat Code Coverage' to dotnet test
- Upload Cobertura XML as build artifact
- Applied to ci.yml and docker-build-publish.yml

Refs: #092"
```

---

### Phase 2: Merge Reports & Job Summary (Vertical Slice 2)

**Objective:** Merge per-project Cobertura files and display a coverage summary in the GitHub Actions job summary.

**Tasks:**
- [ ] Add `danielpalme/ReportGenerator-GitHub-Action@5` step to merge coverage XMLs
- [ ] Generate `MarkdownSummaryGithub` report type
- [ ] Append summary markdown to `$GITHUB_STEP_SUMMARY`
- [ ] Upload merged Cobertura report as artifact
- [ ] Create `coverlet.runsettings` to exclude E2E, Client assembly, and migrations

**Commit:**
```bash
git add .
git commit -m "ci: merge coverage reports and publish job summary

- Use ReportGenerator to merge Cobertura XMLs
- Display coverage summary in GitHub Actions summary
- Exclude E2E, Client, and migration assemblies
- Upload merged report as artifact

Refs: #092"
```

---

### Phase 3: Enforce Coverage Threshold (Vertical Slice 3)

**Objective:** Fail the CI job when line coverage drops below a configurable minimum.

**Tasks:**
- [ ] Add `irongut/CodeCoverageSummary@v1.3.0` step with `fail_below_min: true`
- [ ] Set initial threshold at 80% line / 90% as warning
- [ ] Define threshold as a workflow-level env variable for easy adjustment
- [ ] Test that the job fails correctly when coverage is below threshold
- [ ] Document threshold rationale and how to adjust

**Commit:**
```bash
git add .
git commit -m "ci: enforce minimum code coverage threshold

- Add CodeCoverageSummary with fail_below_min
- Set threshold to 80% line coverage (90% warning)
- Threshold configurable via workflow env variable

Refs: #092"
```

---

### Phase 4: PR Comment with Coverage Details (Vertical Slice 4)

**Objective:** Post a sticky PR comment showing coverage per project and overall totals.

**Tasks:**
- [ ] Add `marocchino/sticky-pull-request-comment@v2` step (conditional on `pull_request` event)
- [ ] Format coverage markdown for PR comment
- [ ] Ensure comment updates on force-push (sticky/recreate)
- [ ] Verify bot has `pull-requests: write` permission

**Commit:**
```bash
git add .
git commit -m "ci: add coverage summary as PR comment

- Post sticky coverage comment on pull requests
- Show per-project and total line/branch coverage
- Comment updates on subsequent pushes

Refs: #092"
```

---

### Phase 5: Badge, Branch Protection & Documentation

**Objective:** Add a coverage badge to the README and document how to configure branch protection.

**Tasks:**
- [ ] Generate coverage badge (via ReportGenerator `Badges` report type or shields.io endpoint)
- [ ] Add badge to README.md
- [ ] Document branch protection setup: add the coverage job as a required status check
- [ ] Update `copilot-instructions.md` §23 PR Checklist with coverage gate reference
- [ ] Archive feature doc to `docs/archive/` when complete

**Commit:**
```bash
git add .
git commit -m "docs: add coverage badge and branch protection guide

- Coverage badge in README.md
- Document branch protection configuration
- Update PR checklist with coverage gate

Refs: #092"
```

---

## Conventional Commit Reference

| Type | When to Use | SemVer Impact | Example |
|------|-------------|---------------|---------|
| `ci` | CI/CD pipeline changes | None | `ci: collect code coverage during CI test runs` |
| `docs` | Documentation only | None | `docs: add coverage badge and branch protection guide` |
| `chore` | Build, dependencies | None | `chore: add coverlet runsettings` |

---

## Notes

### Vertical Slice Rationale

Each phase is independently deployable and valuable:

0. **Phase 0** — Boilerplate excluded; coverage metrics reflect real code quality.
1. **Phase 1** — Raw coverage data available as artifact (useful even without reporting).
2. **Phase 2** — Developers see coverage in job summary (useful even without enforcement).
3. **Phase 3** — Quality gate prevents regressions (the core value).
4. **Phase 4** — Reviewer experience improved with inline PR comments.
5. **Phase 5** — Public visibility (badge) and process compliance (branch protection).

### Baseline Coverage (pre-exclusion, measured 2026-03-07)

| Assembly | Line Coverage | Branch Coverage |
|---|---|---|
| BudgetExperiment.Domain | 96.2% | 88.5% |
| BudgetExperiment.Application | 94.2% | 79.9% |
| BudgetExperiment.Contracts | 98.6% | 75.0% |
| BudgetExperiment.Client | 42.5% | 46.7% |
| BudgetExperiment.Api | 23.3% | 58.6% |
| BudgetExperiment.Infrastructure | 7.7% | 61.9% |
| **Overall** | **32.8%** | **59.5%** |

3,535 tests (3,534 passed, 1 skipped).

### Threshold Calibration

Start with 80% line coverage as the minimum. After the first run, review actual coverage and adjust:
- If current coverage is significantly above 80%, raise the threshold to prevent regression.
- If current coverage is below 80%, set the threshold at the current level and incrementally raise it.
- After Phase 0 exclusions, Domain + Application + Contracts should be well above 80%.

### Action Versions

Pin all third-party actions to specific versions (SHA or tag) and review Dependabot PRs for updates. Prefer well-maintained, widely-adopted actions to minimize supply-chain risk.
