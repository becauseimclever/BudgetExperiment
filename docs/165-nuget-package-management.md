# Feature 165: NuGet Package Management Simplification
> **Status:** Planning

## Overview

The repository currently has two custom NuGet workflows (`nuget-package-hygiene.yml` and `nuget-upgrade-cycle-audit.yml`) and three supporting PowerShell scripts that duplicate functionality already provided by the .NET SDK and Dependabot. This feature replaces that bespoke infrastructure with a lean, Dependabot-first model: the SDK's built-in audit properties enforce a hard build failure on vulnerable packages, Dependabot handles upgrade discovery and PR creation, and a single lightweight CI check validates that no vulnerabilities are present. The two existing workflows, supporting scripts, and generated feature doc automation are removed entirely.

## Problem Statement

### Current State

- `nuget-package-hygiene.yml` runs on a monthly schedule, on Dependabot PRs, and on manual dispatch. It runs `dotnet restore` with audit properties, `dotnet list --vulnerable`, and `dotnet list --outdated`, then generates a feature doc via `new-nuget-upgrade-feature-doc.ps1` and commits it. This is heavyweight, fragile, and produces doc noise.
- `nuget-upgrade-cycle-audit.yml` does much of the same thing separately: it calls `invoke-nuget-restore-vulnerability-gate.ps1`, `invoke-nuget-package-policy-gates.ps1`, and `new-nuget-upgrade-feature-doc.ps1`. Both workflows overlap significantly.
- `Directory.Build.props` does **not** currently set `NuGetAudit`, `NuGetAuditMode`, or `NuGetAuditLevel` globally, so vulnerability checking is not enforced on every `dotnet restore` in CI.
- The three PowerShell scripts (`invoke-nuget-restore-vulnerability-gate.ps1`, `invoke-nuget-package-policy-gates.ps1`, `new-nuget-upgrade-feature-doc.ps1`) exist only to serve the two workflows. They become orphaned when the workflows are removed.
- Dependabot is already configured (`dependabot.yml`) with grouped updates on a weekly Monday schedule. It produces PRs automatically. The custom workflows add no value on top of this.
- There is no agent, prompt, or instruction that guides a maintainer through reviewing and merging a Dependabot PR or triggering a manual upgrade when a vulnerability is found.

### Target State

- `Directory.Build.props` sets `NuGetAudit=true`, `NuGetAuditMode=all`, and `NuGetAuditLevel=low` globally so every `dotnet restore` — locally and in CI — fails immediately if any vulnerable package is present.
- `ci.yml` fails at the restore step on vulnerable packages with no extra workflow needed.
- `nuget-package-hygiene.yml` and `nuget-upgrade-cycle-audit.yml` are deleted.
- `invoke-nuget-restore-vulnerability-gate.ps1`, `invoke-nuget-package-policy-gates.ps1`, and `new-nuget-upgrade-feature-doc.ps1` are deleted.
- Dependabot handles all upgrade discovery and PR creation per the existing `dependabot.yml` configuration.
- A `dotnet-nuget-upgrade.prompt.md` prompt guides a maintainer through reviewing a Dependabot PR, running the upgrade locally if needed, and verifying build cleanliness.
- The `dotnet-devops-specialist.agent.md` is updated to reflect that NuGet hygiene is now owned by Dependabot + SDK audit properties, not custom workflows.
- The engineering guide and spec-driven gate instructions are updated to note that vulnerability failures are a build-time error, not a separate workflow concern.

---

## User Stories

### Vulnerability Gate

#### US-165-001: Hard Build Failure on Vulnerable Packages
**As a** developer  
**I want to** `dotnet restore` to fail immediately if any direct or transitive package has a known vulnerability  
**So that** vulnerable packages can never make it into a build, whether locally or in CI.

**Acceptance Criteria:**
- [ ] `Directory.Build.props` includes `<NuGetAudit>true</NuGetAudit>`, `<NuGetAuditMode>all</NuGetAuditMode>`, and `<NuGetAuditLevel>low</NuGetAuditLevel>` in the main `PropertyGroup`.
- [ ] `ci.yml` restore step fails and reports the vulnerable package when a known-vulnerable package is present (validated manually or via a test branch with a pinned vulnerable package version).
- [ ] Local `dotnet restore` produces a clear error message identifying the vulnerable package and advisory.
- [ ] `TreatWarningsAsErrors` (already set in `Directory.Build.props`) escalates NuGet audit warnings to errors, so `NU1901`–`NU1904` are build-breaking.

#### US-165-002: Remove Custom Vulnerability Workflow Infrastructure
**As a** maintainer  
**I want to** the two NuGet hygiene/audit workflows and their supporting scripts removed  
**So that** the pipeline is simpler and the audit enforcement moves to the build itself.

**Acceptance Criteria:**
- [ ] `.github/workflows/nuget-package-hygiene.yml` is deleted.
- [ ] `.github/workflows/nuget-upgrade-cycle-audit.yml` is deleted.
- [ ] `scripts/operations/invoke-nuget-restore-vulnerability-gate.ps1` is deleted.
- [ ] `scripts/operations/invoke-nuget-package-policy-gates.ps1` is deleted.
- [ ] `scripts/operations/new-nuget-upgrade-feature-doc.ps1` is deleted.
- [ ] No other workflow or script references the deleted files.
- [ ] CI remains green after removal.

### Dependabot-First Upgrades

#### US-165-003: Dependabot as Sole Upgrade Discovery Mechanism
**As a** maintainer  
**I want to** rely on Dependabot exclusively for outdated package discovery and PR creation  
**So that** I do not need to run or maintain any workflow that lists outdated packages.

**Acceptance Criteria:**
- [ ] `dependabot.yml` retains and, if needed, improves the existing NuGet grouping configuration (AspNetCore, Extensions, EF Core, testing).
- [ ] Dependabot PRs trigger `ci.yml` automatically and the build gate (including vulnerability audit) runs on every Dependabot PR.
- [ ] No custom workflow replaces the removed `dotnet list --outdated` step.

#### US-165-004: Dependabot Auto-Merge for Patch-Level Non-Vulnerable Updates
**As a** maintainer  
**I want to** patch-level Dependabot NuGet PRs that pass CI to be eligible for auto-merge  
**So that** low-risk routine upgrades do not require manual review.

**Acceptance Criteria:**
- [ ] A `dependabot-automerge.yml` workflow (or equivalent GitHub repository rule) is introduced that auto-merges Dependabot NuGet PRs when: the update is patch-level only, CI passes, and the PR is not marked as a security advisory upgrade requiring human review.
- [ ] Minor and major version bumps are never auto-merged.
- [ ] Auto-merge behavior is documented in `docs/ci-cd-deployment.md`.

### Maintainer Tooling

#### US-165-005: NuGet Upgrade Prompt for Maintainers
**As a** maintainer  
**I want to** a prompt that guides me through reviewing a Dependabot NuGet PR or responding to a vulnerability failure  
**So that** I can resolve package issues quickly without remembering the exact commands.

**Acceptance Criteria:**
- [ ] A new `.github/prompts/nuget-upgrade.prompt.md` prompt exists.
- [ ] The prompt covers three scenarios: (a) a Dependabot PR is open and needs review, (b) CI failed due to a vulnerability, (c) a manual upgrade of a specific package is needed.
- [ ] The prompt includes the commands to run locally to verify upgrade cleanliness: `dotnet restore`, `dotnet build`, `dotnet test`.
- [ ] The prompt describes how to trigger a manual Dependabot update via the GitHub UI if the weekly schedule has not yet fired.
- [ ] The prompt references `dependabot.yml` group names so the maintainer knows which PR group to look for.

#### US-165-006: Update Agent and Instruction Files
**As a** developer using Copilot agents  
**I want to** agent instructions to reflect the Dependabot-first NuGet model  
**So that** agents do not suggest recreating the deleted workflow infrastructure.

**Acceptance Criteria:**
- [ ] `dotnet-devops-specialist.agent.md` scope section notes: "NuGet vulnerability enforcement is owned by `Directory.Build.props` audit properties and Dependabot. Do not create custom NuGet audit workflows."
- [ ] `engineering-guide.instructions.md` includes a NuGet policy note: vulnerability properties are set globally in `Directory.Build.props`; adding `NuGetAudit` properties per-project is redundant.
- [ ] `workflow-test-validation.instructions.md` notes that NuGet vulnerability failures surface at the `dotnet restore` step in `ci.yml`, not in a separate workflow.

---

## Technical Design

### `Directory.Build.props` Changes

Add the following to the existing `PropertyGroup`:

```xml
<!-- NuGet Security Audit: fail restore on any vulnerable package (direct or transitive) -->
<NuGetAudit>true</NuGetAudit>
<NuGetAuditMode>all</NuGetAuditMode>
<NuGetAuditLevel>low</NuGetAuditLevel>
```

`TreatWarningsAsErrors` is already set to `true`, so `NU1901`–`NU1904` (vulnerability severity warnings) are automatically elevated to build errors. No additional properties are needed.

### Workflows to Delete

| File | Reason |
|---|---|
| `.github/workflows/nuget-package-hygiene.yml` | Replaced by SDK audit in `Directory.Build.props` |
| `.github/workflows/nuget-upgrade-cycle-audit.yml` | Replaced by Dependabot + SDK audit |

### Scripts to Delete

| File | Reason |
|---|---|
| `scripts/operations/invoke-nuget-restore-vulnerability-gate.ps1` | Logic moved to SDK properties |
| `scripts/operations/invoke-nuget-package-policy-gates.ps1` | No longer needed |
| `scripts/operations/new-nuget-upgrade-feature-doc.ps1` | Feature doc auto-generation is removed |

### New Workflow: `dependabot-automerge.yml`

Triggered on `pull_request` when `github.actor == 'dependabot[bot]'`. Calls `gh pr merge --auto --squash` after CI passes for patch-level NuGet updates only. Uses the GitHub CLI with `GITHUB_TOKEN`. Does not auto-merge grouped PRs that include minor or major bumps.

```
on:
  pull_request
  (filter: actor == dependabot[bot], ecosystem == nuget, semver-update == patch)

jobs:
  auto-merge:
    if: patch-only NuGet PR
    steps:
      - enable auto-merge via GitHub CLI
```

### New Prompt: `nuget-upgrade.prompt.md`

Three-scenario prompt:

1. **Dependabot PR open**: Check PR title/group, pull branch locally, run `dotnet restore && dotnet build && dotnet test`, approve and merge if green.
2. **CI failed on vulnerability**: Identify the package from the restore error, check Dependabot for an open PR or trigger one manually, apply the fix on a feature branch.
3. **Manual upgrade**: `dotnet add package <PackageName>` on a feature branch, verify build, open PR.

### `dependabot.yml` Improvements

- Add `allow` filter to restrict NuGet updates to `direct` dependencies only if transitive noise becomes a problem (optional, marked as open question).
- Consider adding `ignore` entries for packages where major upgrades require explicit developer review (e.g., EF Core major versions).

### Files to Update

| File | Change |
|---|---|
| `Directory.Build.props` | Add three NuGet audit properties |
| `dependabot.yml` | Minor tuning (see open questions) |
| `.github/agents/dotnet-devops-specialist.agent.md` | Add NuGet ownership note |
| `.github/instructions/engineering-guide.instructions.md` | Add NuGet policy note |
| `.github/instructions/workflow-test-validation.instructions.md` | Note vulnerability failure location |
| `docs/ci-cd-deployment.md` | Update NuGet section to reflect new model; document auto-merge behavior |

---

## Implementation Tasks

- [ ] **T-165-01** Add `NuGetAudit`, `NuGetAuditMode`, and `NuGetAuditLevel` properties to `Directory.Build.props`.
- [ ] **T-165-02** Delete `nuget-package-hygiene.yml`.
- [ ] **T-165-03** Delete `nuget-upgrade-cycle-audit.yml`.
- [ ] **T-165-04** Delete `scripts/operations/invoke-nuget-restore-vulnerability-gate.ps1`.
- [ ] **T-165-05** Delete `scripts/operations/invoke-nuget-package-policy-gates.ps1`.
- [ ] **T-165-06** Delete `scripts/operations/new-nuget-upgrade-feature-doc.ps1`.
- [ ] **T-165-07** Create `.github/workflows/dependabot-automerge.yml` for patch-level NuGet auto-merge.
- [ ] **T-165-08** Create `.github/prompts/nuget-upgrade.prompt.md` covering the three upgrade scenarios.
- [ ] **T-165-09** Update `dotnet-devops-specialist.agent.md` with NuGet ownership note.
- [ ] **T-165-10** Update `engineering-guide.instructions.md` with NuGet policy note.
- [ ] **T-165-11** Update `workflow-test-validation.instructions.md` with vulnerability failure location.
- [ ] **T-165-12** Update `docs/ci-cd-deployment.md` NuGet section.
- [ ] **T-165-13** Validate: run `dotnet restore` against a branch with a pinned vulnerable package version; confirm build fails with `NU190x` error.
- [ ] **T-165-14** Validate: confirm CI passes cleanly after all deletions.

---

## Open Questions

1. **Auto-merge scope**: Should auto-merge apply only to patch-level NuGet updates, or also to minor updates for test-only packages (e.g., xUnit, coverlet)? Test packages cannot introduce runtime vulnerabilities.
2. **Grouped PRs**: Dependabot groups (AspNetCore, EF Core, etc.) may bundle patch + minor updates together. Should the auto-merge workflow skip grouped PRs entirely, or parse the update type per package?
3. **Transitive-only vulnerability PRs**: If a transitive dependency has a vulnerability and Dependabot cannot update it directly, how should the maintainer respond? Should the prompt include guidance on adding a direct reference to force the version?
4. **`artifacts/nuget-audit/` directory**: The existing `artifacts/` folder appears to contain generated hygiene artifacts committed to the repo. Should this directory and its contents be removed, or kept for historical reference?

---

## Assumptions

- `TreatWarningsAsErrors=true` is already set in `Directory.Build.props` and will remain. No per-project override of this setting exists.
- Dependabot's existing weekly Monday schedule and grouping configuration is sufficient for routine upgrade discovery. No acceleration of the schedule is needed.
- The NuGet vulnerability advisory database used by `dotnet restore` is the same database Dependabot uses; there is no gap in coverage between the two.
- Auto-merge uses the repository's existing branch protection rules; CI must pass before auto-merge fires.
- Removing the two workflows does not break any required status check configuration on `main` branch protection (neither workflow currently appears as a required check).
