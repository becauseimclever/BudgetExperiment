# Audit: Feature 164 — workflow_call Chained Pipeline Redesign

**Date:** 2026-04-29  
**Auditor:** Dotnet Auditor Reviewer (automated)  
**Scope:** `.github/workflows/ci.yml`, `.github/workflows/docker-build-publish.yml`, `.github/workflows/release.yml`  
**Reference:** `docs/164-github-actions-revamp.md`

## Context

Feature 164 redesigned the three delivery workflows from a `workflow_run`-triggered, fragile chain into a clean `workflow_call` composition:

- `ci.yml` is a reusable workflow that also accepts direct `push`/`pull_request` triggers.
- `docker-build-publish.yml` is a pure `workflow_call` workflow that chains `ci` → `docker-build` → `docker-merge`.
- `release.yml` triggers on tag push, calls `docker-build-publish.yml`, and then creates the GitHub Release.

This audit verifies all acceptance criteria from the redesign spec against the live workflow files.

---

## Verification Matrix

### ci.yml

| # | Criterion | Status |
|---|-----------|--------|
| CI-1 | No `tags:` key in `push:` trigger | PASS |
| CI-2 | `workflow_call:` present in `on:` block | PASS |
| CI-3 | `workflow_call.outputs.version` declared and maps to `jobs.build-and-test.outputs.version` | PASS |
| CI-4 | `build-and-test` has `outputs.version` pointing to `steps.version.outputs.version` | PASS |
| CI-5 | No `workflow_run:` trigger anywhere | PASS |

### docker-build-publish.yml

| # | Criterion | Status |
|---|-----------|--------|
| DB-1 | Only `workflow_call:` trigger | PASS |
| DB-2 | First job is `ci:` with `uses: ./.github/workflows/ci.yml` and `secrets: inherit` | PASS |
| DB-3 | `docker-build` job has `needs: ci` | PASS |
| DB-4 | No `github.event.workflow_run.*` references | PASS |
| DB-5 | No `Set version` step reading from `github.event.workflow_run.head_branch` | PASS |
| DB-6 | Version references use `needs.ci.outputs.version` | PASS |
| DB-7 | Artifact download step has no `run-id:` or `github-token:` parameters | PASS |
| DB-8 | `docker-merge` `needs` includes both `ci` and `docker-build` | PASS |
| DB-9 | No `if:` conditions checking `startsWith(github.event.workflow_run.head_branch, 'v')` | PASS |

### release.yml

| # | Criterion | Status |
|---|-----------|--------|
| R-1 | Trigger is `push: tags: ['v*']`; no `workflow_run:` | PASS |
| R-2 | First job calls `docker-build-publish.yml` via `uses:` with `secrets: inherit` | PASS |
| R-3 | `release` job has `needs: docker-build` | PASS |
| R-4 | No `github.event.workflow_run.*` references | PASS |
| R-5 | Changelog step uses `github.ref_name` for the tag argument | PASS |
| R-6 | GitHub Release step uses `github.ref_name` for `tag_name` | PASS |
| R-7 | `prerelease:` condition references `github.ref_name` | PASS |

### Additional Checks

| Check | Status |
|-------|--------|
| YAML syntactically valid (indentation, colons, list markers) | PASS |
| No workflow_run-era artifacts (`head_sha`, `head_branch`, `Checkout release commit` with old SHA) | PASS |
| `release` job has `contents: write` declared at job level | PASS |

---

## Findings

None. All 24 criteria passed.

---

## Open Questions / Observations

1. **`app-publish` artifact availability across nested reusable workflow calls:** The `app-publish` artifact is uploaded inside `ci.yml`'s `build-and-test` job. When `docker-build-publish.yml` calls `ci.yml` as a reusable workflow (and is itself called from `release.yml`), GitHub Actions runs all jobs within the same workflow run context, making the artifact available to `docker-build`. This is correct behaviour as of the current GitHub Actions runtime. If GitHub changes artifact scoping for nested reusable workflows, this will need revisiting.
2. **`artifact-name: app-publish` output on `build-and-test`:** The job declares `artifact-name: app-publish` as an output but this is never consumed by any downstream `needs.build-and-test.outputs.artifact-name` reference. The `docker-build` job hardcodes `name: app-publish` in its download step. The output is harmless but unused. This is a minor housekeeping item, not a defect.

---

## Overall Verdict

**PASS — No findings.**

The `workflow_call` chained pipeline redesign is correctly implemented across all three files. All acceptance criteria from the spec are satisfied. No `workflow_run`-era references remain. The three-stage composition (`ci` → `docker-build-publish` → `release`) is wired correctly, version propagation flows through `needs.ci.outputs.version`, and job-level permissions are properly scoped.
