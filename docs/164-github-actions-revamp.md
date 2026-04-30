# Feature 164: GitHub Actions Revamp

> **Status:** In Progress

## Overview

The current GitHub Actions setup has accumulated redundancy and inefficiency across the CI, Docker build, and release pipelines. `docker-build-publish.yml` historically duplicated the full build-and-test path already handled by `ci.yml`, release gating logic was split across workflows instead of relying on branch protection, and action versions had drifted across delivery workflows. This feature consolidates the delivery pipeline into a clear three-stage flow: **CI -> Docker -> Release**, with Docker explicitly triggering and waiting for a CI run before image publishing.

## Problem Statement

### Current State

- `docker-build-publish.yml` re-runs restore, build, test, and publish independently of `ci.yml`.
- Releases were gated inside workflows instead of relying on branch protection on `main`.
- No path filtering existed on `ci.yml`, so documentation-only changes still triggered the full build matrix.
- The Docker workflow recalculated the version instead of reusing CI-owned version information and the CI publish artifact.
- Delivery workflow action versions drifted across `ci.yml`, `docker-build-publish.yml`, `release.yml`, and `performance.yml`.
- CodeQL is managed through GitHub Default Setup rather than a repository workflow file, so enforcement depends on branch protection configuration.

### Target State

- A single `ci.yml` handles build, test, coverage, and publish steps and produces the reusable app artifact.
- A lean `docker-build-publish.yml` is triggered by successful CI completion, then consumes the CI publish artifact for image builds.
- `release.yml` runs only after successful Docker publication for the tagged commit.
- Branch protection on `main` enforces CI and CodeQL rather than workflow-local `verify-ci` jobs.
- `ci.yml` uses path filters so docs-only pushes skip the build matrix.
- Delivery workflows document and pin consistent action versions.
- NuGet audit workflows are handled by Feature 165 and are not modified here.
- Squad automation workflows are out of scope and may be removed by a future feature.

---

## User Stories

### Pipeline Efficiency

#### US-164-001: Eliminate Duplicate Build-and-Test on Tag Push

**As a** maintainer  
**I want** the Docker build workflow to run only after CI succeeds and consume the resulting publish artifact  
**So that** release images are built only after a successful, up-to-date CI validation.

**Acceptance Criteria:**

- [x] `docker-build-publish.yml` only runs for successful CI workflow runs on tagged commits.
- [x] `docker-build-publish.yml` has no `dotnet restore`, `dotnet build`, `dotnet test`, or `dotnet publish` steps.
- [x] Docker consumes the `app-publish` artifact from the gated CI run.
- [ ] CI minutes per release are compared before and after the change and noted in the PR.

#### US-164-002: Path Filtering on CI Workflow

**As a** developer  
**I want** docs-only and config-only commits to skip the full build matrix  
**So that** documentation PRs do not consume CI runner time needlessly.

**Acceptance Criteria:**

- [x] `ci.yml` `push` and `pull_request` triggers use path filters excluding `docs/**`, `*.md`, `nginx/**`, and `sample data/**`.
- [ ] A commit touching only `docs/` does not trigger the build job.
- [ ] Source code changes still trigger CI as expected in GitHub.

### Action Version Consistency

#### US-164-003: Standardize and Pin Action Versions

**As a** maintainer  
**I want** the delivery workflows (`ci.yml`, `docker-build-publish.yml`, `release.yml`, and `performance.yml`) to use the same versions of shared actions  
**So that** the delivery pipeline dependency surface is smaller and version drift does not introduce silent behavior differences.

**Acceptance Criteria:**

- [x] `ci.yml`, `docker-build-publish.yml`, `release.yml`, and `performance.yml` use `actions/checkout@v4`.
- [x] `ci.yml` and `performance.yml` use `actions/setup-dotnet@v4`.
- [x] `ci.yml` and `performance.yml` use `actions/cache@v4`.
- [x] Delivery workflows use `actions/upload-artifact@v4` and `actions/download-artifact@v4` where needed.
- [x] A comment block at the top of each delivery workflow lists the pinned versions it uses.

#### US-164-004: NuGet Workflow Removal

> **Moved to Feature 165.** NuGet vulnerability enforcement and workflow removal are fully scoped in `docs/165-nuget-package-management.md`.

### Docker Best Practices

#### US-164-005: Separate Publish from Test in CI

**As a** maintainer  
**I want** `dotnet publish` to be its own step that runs only after tests pass  
**So that** publish artifacts are only produced on green builds.

**Acceptance Criteria:**

- [x] `ci.yml` has a separate publish step after test execution succeeds.
- [x] The `app-publish` artifact includes only the published output directory.
- [x] `docker-build-publish.yml` downloads the `app-publish` artifact from the CI run it triggered and gated on.

#### US-164-006: Version Calculation Owned by CI, Consumed by Docker

**As a** maintainer  
**I want** version ownership to stay in the CI/release path without redundant version drift  
**So that** both workflows agree on the version string for the same commit while Docker remains artifact-only.

**Acceptance Criteria:**

- [x] `ci.yml` calculates and exports a version as a job output.
- [x] `docker-build-publish.yml` does not install MinVer CLI independently.
- [x] The Docker image tag is derived deterministically from the tagged commit.

### Gate Enforcement

#### US-164-007: CodeQL Analysis Gate Before Docker Publish

**As a** maintainer  
**I want** CodeQL static analysis to pass on the tagged commit before Docker images are built and published  
**So that** images with known security findings are never shipped to the container registry.

**Acceptance Criteria:**

- [ ] GitHub Default Setup for CodeQL is confirmed enabled on the repository.
- [ ] The CodeQL status check name is identified and added to `main` branch protection as a required status check.
- [ ] Branch protection on `main` includes CodeQL as a required status check alongside CI.
- [x] `docker-build-publish.yml` does not explicitly wait for CodeQL; branch protection remains the enforcement point.

#### US-164-008: Enforce Correct Gate Order for Release

**As a** maintainer  
**I want** the release workflow to gate on Docker image publication before creating the GitHub Release  
**So that** a GitHub Release never exists without a corresponding published Docker image.

**Acceptance Criteria:**

- [x] `release.yml` waits on successful completion of `docker-build-publish.yml` via `workflow_run`.
- [x] If Docker publish fails, the GitHub Release is not created.
- [ ] The acceptance criteria is validated end to end with a non-production tag run or captured as a manual validation step.

### Branch Workflow Policy

#### US-164-009: Enforce Feature Branch Workflow in Agent Instructions

**As a** developer using Copilot agents  
**I want** all agents and instructions to require a feature branch before starting work  
**So that** no implementation changes, feature docs, or refactors are committed directly to `main`.

**Acceptance Criteria:**

- [x] `engineering-guide.instructions.md` states that all work must happen on a branch and direct commits to `main` are forbidden.
- [x] `spec-driven-feature-gate.instructions.md` includes a pre-work branch check.
- [x] `agent-handoff-coordination.instructions.md` includes active branch context in handoffs.
- [x] `workflow-test-validation.instructions.md` states final validation must occur from a branch workflow context.

#### US-164-010: Update Setup-Release Prompt for New Pipeline

**As a** maintainer  
**I want** `setup-release.prompt.md` to reflect the new workflow architecture  
**So that** running the prompt produces a release correctly under the branch-protection and tag-time rebuild model.

**Acceptance Criteria:**

- [x] The prompt makes clear that `main` is the only valid release branch.
- [x] The prompt documents the automated post-tag flow.
- [x] The prompt no longer references a workflow-local `verify-ci` gate.
- [x] The prompt describes what to monitor after tagging.
- [x] The prompt notes that Docker runs after successful CI and reuses CI artifacts.

#### US-164-011: Update DevOps Agent Scope

**As a** maintainer  
**I want** `dotnet-devops-specialist.agent.md` to reflect the new workflow structure  
**So that** the agent understands the three-workflow pipeline and does not regenerate the removed `build-and-test` job in `docker-build-publish.yml`.

**Acceptance Criteria:**

- [x] The agent scope references the three-workflow pipeline and branch-protection model.
- [x] The agent instructions state that `docker-build-publish.yml` must never contain `dotnet` build or test steps.
- [x] The agent instructions state that tags originate from `main` only.

---

## Technical Design

### Architecture Changes

#### Revised `ci.yml` Job Graph

```text
checkout -> setup-dotnet -> restore -> build -> test -> coverage -> publish -> upload-artifact
```

- Path filters on push and pull request triggers.
- Publish runs only after successful build and test execution.
- Exports `version` and `artifact-name` as job outputs.

#### Revised `docker-build-publish.yml` Job Graph

```text
ci-completed(success, tagged) -> docker-build(matrix: artifact-only) -> docker-merge
```

- Triggered by successful completion of `ci.yml`.
- Resolves the release tag from `workflow_run.head_sha`.
- Downloads `app-publish` from that gated CI run.
- Builds multi-arch images from the CI publish output.

#### Revised `release.yml` Job Graph

```text
workflow_run(docker-build-publish) -> resolve-tag -> create-release
```

- No `verify-ci` job.
- Triggered by successful completion of `docker-build-publish.yml`.
- Resolves the `v*` tag associated with `workflow_run.head_sha` before creating the GitHub Release.

### Branch Workflow Policy Enforcement

The following files are updated as part of this feature:

| File | Change |
| --- | --- |
| `.github/instructions/engineering-guide.instructions.md` | Add branch policy language forbidding direct commits to `main`. |
| `.github/instructions/spec-driven-feature-gate.instructions.md` | Add a pre-work branch check. |
| `.github/instructions/agent-handoff-coordination.instructions.md` | Add `active-branch` context to the handoff package. |
| `.github/instructions/workflow-test-validation.instructions.md` | Add a branch-validation note. |
| `.github/prompts/setup-release.prompt.md` | Rewrite release steps for the CI -> Docker -> Release pipeline. |
| `.github/agents/dotnet-devops-specialist.agent.md` | Update scope to reference the three-workflow delivery pipeline. |

#### Branch Naming Convention

- Feature work: `feature/NNN-short-description`
- Bug fixes: `fix/NNN-short-description`
- Chore and docs: `chore/short-description`
- Releases are tagged from `main` after branch work is merged.

#### Updated `setup-release.prompt.md` Outline

1. Validate branch is `main` and working tree is clean.
2. Confirm CI and CodeQL are green on `HEAD`.
3. Resolve and confirm the target version.
4. Run `git cliff` to update `CHANGELOG.md`.
5. Commit `CHANGELOG.md` with `chore(release): vX.Y.Z`.
6. Create annotated tag on `main`.
7. Push commit and tag.
8. Explain that tag push triggers `ci.yml`; successful CI then triggers `docker-build-publish.yml`, which reuses the CI publish artifact.
9. Explain that `release.yml` runs after successful Docker publication.
10. Report outcome and provide links to monitor workflow runs.

#### NuGet Workflow Delineation

> **Delegated to Feature 165.** Both `nuget-package-hygiene.yml` and `nuget-upgrade-cycle-audit.yml` are deleted by Feature 165. This feature takes no action on those workflows.

### Release Flow: End to End

**Trigger:** Tag pushed to `main` matching `v*`.

#### Step 1: CI and CodeQL Run on `main`

- `ci.yml` runs on pushes to `main` and must pass before a commit lands.
- CodeQL is enforced via branch protection rather than a repository workflow file.
- By the time a release tag is pushed from `main`, CI and CodeQL should already be green.

#### Step 2: Docker Build and Publish

- `ci.yml` is triggered by the tag push.
- `docker-build-publish.yml` is triggered by successful completion of that CI run.
- It downloads the publish artifact from that CI run before Docker image build.
- It builds amd64 and arm64 images and merges them into a multi-arch manifest in `ghcr.io`.

#### Step 3: GitHub Release

- `release.yml` is triggered by successful completion of `docker-build-publish.yml`.
- The workflow resolves the release tag pointing at the completed commit SHA.
- `git-cliff` extracts the changelog body for that tag.
- The workflow creates the GitHub Release only after Docker publication succeeds.

### Action Version Standard

| Action | Version |
| --- | --- |
| `actions/checkout` | `v4` |
| `actions/setup-dotnet` | `v4` |
| `actions/cache` | `v4` |
| `actions/upload-artifact` | `v4` |
| `actions/download-artifact` | `v4` |
| `actions/github-script` | `v9` |
| `docker/setup-buildx-action` | `v3` |
| `docker/login-action` | `v3` |
| `docker/build-push-action` | `v6` |
| `docker/metadata-action` | `v5` |

Versions are reviewed quarterly alongside Dependabot action PRs. The deleted NuGet workflows and future squad-removal work are out of scope for this standardization pass.

---

## Implementation Tasks

- [ ] **T-164-01** Confirm CodeQL Default Setup is enabled and identify the exact required status check name on `main`.
- [x] **T-164-02** Add path filters to `ci.yml` push and pull request triggers.
- [x] **T-164-03** Add a separate publish step to `ci.yml` and expose `app-publish` for CI visibility.
- [x] **T-164-04** Replace the duplicate Docker pipeline with a CI-dependent artifact-consumption flow.
- [x] **T-164-05** Remove MinVer installation from `docker-build-publish.yml` and derive the image version from the tag.
- [x] **T-164-06** Standardize action versions across `ci.yml`, `docker-build-publish.yml`, `release.yml`, and `performance.yml`.
- [x] **T-164-07** Add a pinned-action comment block at the top of each delivery workflow.
- [x] **T-164-10** Update `engineering-guide.instructions.md` with branch policy guidance.
- [x] **T-164-11** Update `spec-driven-feature-gate.instructions.md` with a pre-work branch check.
- [x] **T-164-12** Update `agent-handoff-coordination.instructions.md` to include active branch context.
- [x] **T-164-13** Update `workflow-test-validation.instructions.md` with branch-validation guidance.
- [x] **T-164-14** Rewrite `setup-release.prompt.md` for the new delivery pipeline.
- [x] **T-164-15** Update `dotnet-devops-specialist.agent.md` scope for the new pipeline.
- [ ] **T-164-16** Validate the pipeline end to end with a safe tag or equivalent GitHub-side verification.

---

## Open Questions

1. **Artifact retention strategy:** Is three days sufficient for `app-publish`, or should manual recovery always require a fresh CI run?
2. **CodeQL required check name:** What exact status check label should be enforced on `main` branch protection?
3. **Action SHA pinning:** Should a later hardening feature pin actions by full commit SHA rather than version tags?

---

## Assumptions

- All implementation work occurs on a feature branch and is merged before release.
- Releases originate from `main` only.
- The `app-publish` artifact produced by `ci.yml` is architecture-neutral for the Docker build flow used here.
- CodeQL remains managed by GitHub Default Setup rather than a repository workflow file.
- GitHub Container Registry remains the image registry.
- Squad automation workflows are intentionally out of scope for this feature because they are planned for future removal.
