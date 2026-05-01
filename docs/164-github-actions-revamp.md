# Feature 164: GitHub Actions Revamp

> **Status:** Implementation Complete, Pending GitHub Runtime Verification

## Overview

Feature 164 removes duplicate pipeline work and aligns release automation to the reusable workflow chain that exists today:

1. A tag push matching `v*` triggers `release.yml`.
2. `release.yml` calls `docker-build-publish.yml` through `workflow_call`.
3. `docker-build-publish.yml` calls `ci.yml` through `workflow_call` as its first job.
4. If CI or Docker fails, release creation is blocked automatically.

This design keeps CI and CodeQL enforcement centered on `main` branch protection and keeps Docker workflow jobs artifact-only.

## Source of Truth Reviewed

- `.github/workflows/ci.yml`
- `.github/workflows/docker-build-publish.yml`
- `.github/workflows/release.yml`
- `.github/workflows/performance.yml`
- `.github/prompts/setup-release.prompt.md`
- `.github/agents/dotnet-devops-specialist.agent.md`
- `.github/instructions/engineering-guide.instructions.md`
- `.github/instructions/spec-driven-feature-gate.instructions.md`
- `.github/instructions/workflow-test-validation.instructions.md`
- `.github/instructions/agent-handoff-coordination.instructions.md`
- User-provided fact (May 1, 2026): CodeQL is now configured as a required check on `main` branch protection.

---

## User Stories and Acceptance Criteria

### US-164-001: Eliminate Duplicate Build-and-Test on Tag Push

**As a** maintainer  
**I want** Docker publishing to depend on CI and reuse CI output  
**So that** release images are built only from validated artifacts.

**Acceptance Criteria:**

- [x] `docker-build-publish.yml` is reusable-only (`on: workflow_call`) and does not run independently on tag push.
- [x] `docker-build-publish.yml` contains no `dotnet restore`, `dotnet build`, `dotnet test`, or `dotnet publish` steps.
- [x] Docker jobs download and use the `app-publish` artifact produced by `ci.yml`.
- [ ] CI minutes per release are compared before and after the change.
  - **Blocker (May 1, 2026):** A valid post-Feature-164 safe tag run is not yet available, so an honest before/after release-path CI duration comparison cannot be completed.
  - **Manual Step Required:** After one successful post-change safe tag run, compare CI elapsed duration for that run versus a pre-change release-path CI sample and record both run links and elapsed times.

### US-164-002: Path Filtering on CI Workflow

**As a** developer  
**I want** docs-only/config-only changes to skip CI build/test work  
**So that** non-code changes do not consume full CI time.

**Acceptance Criteria:**

- [x] `ci.yml` `push` and `pull_request` use `paths-ignore` for `docs/**`, `**/*.md`, `nginx/**`, and `sample data/**`.
- [ ] A docs-only commit is verified in GitHub to skip CI.
  - **Blocker (May 1, 2026):** No post-Feature-164 docs-only push commit exists yet to validate `paths-ignore` behavior in the new runtime chain. The available docs-only candidate commit (`9bc442ac4b5fed634696408d992dcbabce8d45af`) is also a release-tag commit (`v3.33.0`) and did start CI through the tag-driven release chain, so it is not valid evidence for docs-only push skip behavior.
  - **Manual Step Required:** Push a new commit that changes only ignored paths (`docs/**`, `**/*.md`, `nginx/**`, or `sample data/**`) on a normal branch push, then confirm no `CI` run exists for that commit SHA in Actions run history.
- [x] A source-code commit is verified in GitHub to trigger CI.
  - **Evidence (May 1, 2026):** Source/workflow change commit `ebbbc4b8181549e3a7b91e36beb94416cccdae03` triggered CI push run `25148643513` (success): <https://github.com/becauseimclever/BudgetExperiment/actions/runs/25148643513>

### US-164-003: Standardize and Pin Action Versions

**As a** maintainer  
**I want** consistent action versions in delivery workflows  
**So that** drift and surprise behavior are reduced.

**Acceptance Criteria:**

- [x] `ci.yml`, `docker-build-publish.yml`, `release.yml`, and `performance.yml` use `actions/checkout@v4`.
- [x] `ci.yml` and `performance.yml` use `actions/setup-dotnet@v4`.
- [x] `ci.yml` and `performance.yml` use `actions/cache@v4`.
- [x] Delivery workflows use `actions/upload-artifact@v4` and `actions/download-artifact@v4` where needed.
- [x] Top-of-file action-version comment blocks exist in these workflows.

### US-164-004: NuGet Workflow Removal

> **Moved to Feature 165.** NuGet vulnerability enforcement and workflow removal are tracked in `docs/165-nuget-package-management.md`.

### US-164-005: Separate Publish from Test in CI

**As a** maintainer  
**I want** publish to run only after tests pass  
**So that** artifacts are produced only from green CI runs.

**Acceptance Criteria:**

- [x] `ci.yml` has a distinct publish step after successful build/test steps.
- [x] `app-publish` artifact uploads only the publish output directory.
- [x] Docker workflow downloads `app-publish` for image build.

### US-164-006: Version Calculation Owned by CI and Consumed by Docker

**As a** maintainer  
**I want** CI to own version calculation once  
**So that** Docker tags stay deterministic for the same commit.

**Acceptance Criteria:**

- [x] `ci.yml` calculates and outputs version via MinVer.
- [x] `docker-build-publish.yml` does not install or run MinVer.
- [x] Docker image tags use `needs.ci.outputs.version` from the CI reusable job.

### US-164-007: CodeQL Gate Before Docker Publish

**As a** maintainer  
**I want** CodeQL enforced before release tagging proceeds from `main`  
**So that** known security findings are blocked before release activity.

**Acceptance Criteria:**

- [x] GitHub Default Setup for CodeQL is confirmed enabled.
  - **Evidence (May 1, 2026):** GitHub API `repos/becauseimclever/BudgetExperiment/code-scanning/default-setup` returned `state: configured` with languages including `csharp`.
- [x] CodeQL status check is configured as required on `main` branch protection.
  - **Evidence (May 1, 2026):** GitHub API `repos/becauseimclever/BudgetExperiment/branches/main/protection` returned required checks including `CodeQL`.
- [x] CI status check on `main` branch protection is reconfirmed in the same evidence capture.
  - **Evidence (May 1, 2026):** Same branch-protection API response lists required checks `CodeQL` and `Build & Test`.
- [x] `docker-build-publish.yml` does not add workflow-local CodeQL gating logic.

### US-164-008: Enforce Correct Gate Order for Release

**As a** maintainer  
**I want** release creation to depend on Docker publication  
**So that** no release is created without container artifacts.

**Acceptance Criteria:**

- [x] `release.yml` calls `docker-build-publish.yml` as a reusable workflow job.
- [x] `release` job depends on `docker-build`; if Docker fails, release creation does not run.
- [ ] End-to-end behavior is verified with a safe non-production tag.
  - **Blocker (May 1, 2026):** No release tag has been pushed after Feature 164 workflow changes were merged, so chain runtime proof for the new design is not yet obtainable.
  - **Manual Step Required:** Push a safe non-production tag from `main` (for example, `vX.Y.Z-rc1`) and capture Actions links for CI, Docker, and Release runs for the same tag SHA.

### US-164-009: Enforce Feature Branch Workflow in Agent Instructions

**As a** developer using Copilot agents  
**I want** branch-first policy in instructions  
**So that** implementation changes are not made directly on `main`.

**Acceptance Criteria:**

- [x] `engineering-guide.instructions.md` requires branch-based implementation work.
- [x] `spec-driven-feature-gate.instructions.md` requires pre-work branch validation.
- [x] `agent-handoff-coordination.instructions.md` includes active-branch handoff context.
- [x] `workflow-test-validation.instructions.md` requires branch-context final validation.

### US-164-010: Update Setup-Release Prompt for New Pipeline

**As a** maintainer  
**I want** the setup-release prompt to describe the real release chain  
**So that** tagged releases follow current automation behavior.

**Acceptance Criteria:**

- [x] Prompt states release preparation is from `main` only.
- [x] Prompt describes post-tag chain: `release.yml` -> `docker-build-publish.yml` -> `ci.yml` via reusable calls.
- [x] Prompt does not use workflow-local `verify-ci` messaging.
- [x] Prompt explains what to monitor after tagging.

### US-164-011: Update DevOps Agent Scope

**As a** maintainer  
**I want** agent scope to enforce the reusable chain and artifact-only Docker workflow  
**So that** future edits do not reintroduce duplicate build/test jobs.

**Acceptance Criteria:**

- [x] Agent scope describes the three-stage reusable workflow chain.
- [x] Agent scope forbids adding `dotnet` build/test/publish back into `docker-build-publish.yml`.
- [x] Agent scope states tags originate from `main` only.

---

## Technical Design (Current Implementation)

### Workflow Architecture

```text
Tag push (v*)
  -> release.yml
      -> job: docker-build (uses docker-build-publish.yml via workflow_call)
          -> job: ci (uses ci.yml via workflow_call)
          -> job: docker-build (matrix per platform)
          -> job: docker-merge
      -> job: release (needs docker-build)
```

### CI (`ci.yml`)

- Handles restore, build, test, coverage, publish, and artifact upload.
- Applies `paths-ignore` for docs and non-code locations.
- Exposes `version` output consumed by Docker workflow.

### Docker (`docker-build-publish.yml`)

- Reusable-only workflow.
- First job is CI reusable call.
- No `dotnet` build/test/publish commands.
- Builds amd64 and arm64 images from `Dockerfile.prebuilt` using downloaded publish artifact.
- Merges per-arch images into a multi-arch manifest.

### Release (`release.yml`)

- Trigger: `push` tags `v*`.
- Calls Docker reusable workflow first.
- Creates GitHub Release only after Docker workflow success.

### Gate Model

- CI and CodeQL are enforcement checks on `main` branch protection.
- Tag creation is expected from `main` after protected checks pass.
- No workflow-local `verify-ci` job is used.

---

## Implementation Tasks

- [x] **T-164-01** Confirm CodeQL Default Setup enabled and capture exact required-check names from branch protection.
  - **Evidence (May 1, 2026):**
    - CodeQL Default Setup API: `repos/becauseimclever/BudgetExperiment/code-scanning/default-setup` -> `state: configured`.
    - Branch protection API: `repos/becauseimclever/BudgetExperiment/branches/main/protection` -> required checks `CodeQL` and `Build & Test`.
- [x] **T-164-02** Add `paths-ignore` filters to CI push/pull_request triggers.
- [x] **T-164-03** Keep publish as a separate CI step and upload `app-publish` artifact.
- [x] **T-164-04** Keep Docker workflow artifact-only and CI-dependent via reusable workflow call.
- [x] **T-164-05** Keep MinVer out of Docker workflow and consume CI version output.
- [x] **T-164-06** Standardize action major versions across delivery workflows.
- [x] **T-164-07** Maintain top-level pinned-action comment blocks in delivery workflows.
- [x] **T-164-10** Enforce branch policy in engineering guide instructions.
- [x] **T-164-11** Enforce pre-work branch check in spec-driven gate instructions.
- [x] **T-164-12** Include active branch context in handoff instructions.
- [x] **T-164-13** Require branch-context final validation in workflow test validation instructions.
- [x] **T-164-14** Update setup-release prompt to current reusable chain.
- [x] **T-164-15** Update Dotnet DevOps Specialist agent scope to current reusable chain.
- [ ] **T-164-16** Run and capture a safe tag-based end-to-end verification.

---

## Pending Verification Checklist

These items remain open because they require new GitHub-hosted runtime events that are not yet present in available run history:

1. Docs-only commit verification showing CI skipped.
  - **Why blocked now:** No qualifying post-Feature-164 docs-only branch push commit is available.
  - **Manual step:** Push a docs-only commit and confirm zero CI runs for that commit SHA.
2. Safe tag run verification showing the full chain and gate order.
  - **Why blocked now:** No release tag has been pushed after Feature 164 workflow changes were merged.
  - **Manual step:** Push a safe non-production tag from `main` (for example, `vX.Y.Z-rc1`) and capture Actions links for CI run, Docker workflow run, and Release run for the same tag SHA.
3. Before/after CI minute comparison for release path efficiency.
  - **Why blocked now:** A valid post-change tag-run sample for the new chain is missing, so an honest before/after comparison cannot be completed.
  - **Manual step:** After one successful post-change safe tag run, compare its CI elapsed duration against a pre-change release-path CI sample and record both run links and elapsed times.

---

## Completion Statement

Feature 164 implementation and documentation are aligned with current repository workflows and instruction files. Runtime governance evidence for CodeQL Default Setup, branch protection required checks, and source-change CI trigger is now captured. However, this feature **cannot be honestly marked fully Complete yet** because required post-change runtime event evidence is still pending.

Once the pending verification checklist above is captured and linked, the feature can move to Complete with sign-off.
