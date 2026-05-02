# Audit: Release-Triggered Workflow CI Gate

**Date:** 2026-04-29  
**Auditor:** Dotnet Auditor Reviewer (automated)  
**Scope:** `.github/workflows/release.yml`, `.github/workflows/docker-build-publish.yml`  
**Reference:** `.github/workflows/ci.yml`

## Context

A prior release run failed because tests were failing at the time the tag was pushed. Changes were made to both `release.yml` and `docker-build-publish.yml` to add a `verify-ci` gate job that checks for a successful CI run on the tagged commit SHA before allowing the release or Docker build to proceed.

---

## Findings

### Finding 1 — LOW: Artifact action version mismatch

**File:** `.github/workflows/docker-build-publish.yml`

`build-and-test` uploads artifacts using `actions/upload-artifact@v7`. Both `docker-build` and `docker-merge` download using `actions/download-artifact@v8`. The upload and download major versions are mismatched across jobs.

**Risk:** Both v7 and v8 post-date the v4 backend migration and are expected to be wire-compatible. No runtime failures are anticipated, but the inconsistency could surface if GitHub introduces a breaking format change between major versions and could cause confusion during maintenance.

**Recommendation:** Align all artifact action references to the same major version.

---

### Finding 2 — LOW: Test filter in docker-build-publish.yml excludes `ExternalDependency`; CI does not

**Files:** `.github/workflows/docker-build-publish.yml`, `.github/workflows/ci.yml`

- `ci.yml` filter: `"FullyQualifiedName!~E2E&Category!=Performance"`
- `docker-build-publish.yml` filter: `"FullyQualifiedName!~E2E&Category!=ExternalDependency&Category!=Performance"`

The Docker build workflow silently drops the `ExternalDependency` category from its test run while `ci.yml` runs those tests. This is likely intentional (no external service available during the Docker build), but the intent is not documented with a comment in the workflow.

**Risk:** Mitigated by the CI gate — `verify-ci` ensures CI (which includes `ExternalDependency` tests) passed before `build-and-test` is allowed to run. A miscategorized test could be silently skipped without notice.

**Recommendation:** Add a comment in the workflow explaining why `ExternalDependency` is excluded. Consider whether the category should also be documented in `ci-cd-deployment.md`.

---

### Finding 3 — LOW: `--settings coverlet.runsettings` absent from docker-build-publish.yml test run

**File:** `.github/workflows/docker-build-publish.yml`

`ci.yml` passes `--settings coverlet.runsettings` to `dotnet test`. `docker-build-publish.yml` does not include this flag. The runsettings file governs coverage collection behavior (include/exclude patterns). The Docker workflow still runs ReportGenerator and publishes a summary, but the coverage data may differ from CI output.

**Risk:** Not a gate risk. Coverage summary artifacts from Docker builds may be misleading or incomplete compared to CI-generated coverage. No test failures result from this omission.

**Recommendation:** Add `--settings coverlet.runsettings` to the `dotnet test` call in `docker-build-publish.yml` for consistency, or explicitly document that coverage enforcement is a CI-only concern.

---

## Verification Matrix

| # | Requirement | Status | Notes |
|---|-------------|--------|-------|
| 1 | `release.yml`: `verify-ci` runs first and blocks `release` if CI hasn't passed | **PASS** | `release` has `needs: verify-ci` with no bypass. Gate calls `listWorkflowRuns` with `workflow_id: 'ci.yml'`, `head_sha: context.sha`, `status: 'success'`. API usage is correct. |
| 2 | `docker-build-publish.yml`: `verify-ci` blocks `build-and-test` and all downstream jobs if CI hasn't passed | **PASS** | `build-and-test` uses `if: always() && (needs.verify-ci.result == 'success' \|\| needs.verify-ci.result == 'skipped')`. When `verify-ci` fails, this evaluates to `false`, `build-and-test` is skipped, and `docker-build` / `docker-merge` are skipped by default dependency propagation. |
| 3 | `workflow_dispatch` bypasses the gate correctly | **PASS** | `verify-ci` has `if: github.event_name != 'workflow_dispatch'`, producing a `skipped` result. `build-and-test` condition explicitly handles `\|\| needs.verify-ci.result == 'skipped'`. Logic is sound. |
| 4 | `ENCRYPTION_MASTER_KEY` present in `build-and-test` env, matching `ci.yml` | **PASS** | Both set `ENCRYPTION_MASTER_KEY: "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="`. Values match exactly. |
| 5 | No YAML structure issues, incorrect dependency wiring, or API call problems | **CONDITIONAL PASS** | No structural defects. `listWorkflowRuns` API parameters are valid. See Findings 1–3 for housekeeping issues. |
| 6 | `needs:` chain extends correctly to `docker-build` and `docker-merge` | **PASS** | Chain: `verify-ci` → `build-and-test` → `docker-build` → `docker-merge`. No `always()` override needed on `docker-build` / `docker-merge`; GitHub's default skips them when their predecessor is skipped. |

---

## Open Questions

1. **Forced re-tagging:** If a developer does `git tag -f vX.Y.Z <new-sha>` after a failed CI run, then pushes the tag, `context.sha` in the gate resolves to the new SHA. If CI hasn't run on the new SHA, the gate blocks correctly. If CI passed on the new SHA, the gate passes correctly. No risk identified, but this edge case should be mentioned in developer documentation.
2. **`verify-ci` permissions model:** Both workflows declare `permissions: actions: read` only on the `verify-ci` job. The `github.rest.actions.listWorkflowRuns` call does require `actions: read`. Confirmed correct scoping.
3. **`ExternalDependency` exclusion documentation:** The intent should be captured in `ci-cd-deployment.md` to avoid future confusion.

---

## Overall Verdict

**PASS with minor recommendations.**

The CI gate requirement is correctly implemented in both workflows. The gate logic is structurally sound and handles all three trigger scenarios (tag push with passing CI, tag push with failing CI, and `workflow_dispatch`) correctly. `ENCRYPTION_MASTER_KEY` is present and matches CI. The `needs:` chain propagates the gate block through all downstream jobs without requiring explicit `if:` overrides on `docker-build` or `docker-merge`.

No changes are required for the gate to function as intended. The three LOW findings are housekeeping items that should be addressed in a follow-up but do not compromise gate correctness or release safety.

---

## Acceptance Criteria for Remediation (LOW findings)

If the LOW findings are addressed in a follow-up PR, the following acceptance criteria apply:

1. **Finding 1 (artifact version mismatch):** All `upload-artifact` and `download-artifact` action references in `docker-build-publish.yml` use the same major version. CI passes.
2. **Finding 2 (ExternalDependency comment):** A YAML comment in `docker-build-publish.yml` documents why `Category!=ExternalDependency` is included in the test filter. Optionally, `ci-cd-deployment.md` references the category convention.
3. **Finding 3 (runsettings):** Either `--settings coverlet.runsettings` is added to the `dotnet test` invocation in `docker-build-publish.yml`, or a comment explicitly documents that coverage enforcement is CI-only. CI and Docker build pipelines both pass.
