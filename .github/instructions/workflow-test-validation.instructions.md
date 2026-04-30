---
name: "Workflow Test Validation"
description: "Use when fixing bugs, regressions, broken workflows, release failures, dependency issues, or any change claimed as a fix. Requires final validation against the workflow acceptance scope enforced by this repository's CI and release-related workflows by inspecting all relevant workflow files before completion."
---

# Workflow Test Validation

## When It Applies
- Apply this instruction to any change claimed as a fix.
- Includes bug fixes, regressions, hotfixes, remediations, stabilization work, flaky-test fixes, deployment fixes, workflow/CI failures, release failures, dependency update fixes, and requests to "make workflows green".

## Workflow Acceptance Scope Rule
- Before claiming a fix is complete, inspect every relevant workflow under `.github/workflows/` that can gate merge, tag, release, or deploy success for the requested change.
- Treat workflow definitions as the source of truth for acceptance. Do not guess, rely on habit, or stop at narrow local checks.
- Define the workflow acceptance scope as the union of enforced checks across relevant workflows, including build, test execution, coverage gates, publish/package checks, and release checks when those workflows enforce them.
- Narrow checks are allowed during iteration, but final validation must match the workflow acceptance scope as closely as the local environment permits.
- All checks in that scope that are runnable locally must pass before a fix can be claimed complete.
- Unless the user explicitly narrows scope, validate against the full union of relevant workflow checks.
- Final validation must be performed from a branch workflow context, not by treating direct commits to `main` as an acceptable development path. Merge-ready work is validated on a branch and then promoted to `main` via PR unless the workflow explicitly documents a release-only exception.

## Required Workflow Inspection
- Identify and inspect all workflows relevant to the fix request. For this repository, this commonly includes:
	- `.github/workflows/ci.yml`
	- `.github/workflows/docker-build-publish.yml`
	- `.github/workflows/release.yml`
- Include any additional workflow that can gate the requested fix path.

## Handling Blockers
- Use a blocker path only for checks that genuinely cannot be run locally.
- For each blocked check, state exactly what is blocked, why it cannot be run locally, and the residual risk.
- Do not imply full success when any required check is failed, pending, or unvalidated.

## Completion Rule
- A fix is complete only when one of the following is true:
	- Workflow acceptance scope validated: The agent ran workflow-equivalent final validation for the full relevant workflow acceptance scope, and every required check that is runnable locally passed.
	- Blocked with explicit risk report: The agent reported checks that are genuinely non-runnable locally, listed each unvalidated workflow acceptance check, and stated the residual risk.
- Running checks and reporting results is not sufficient for completion when any required runnable check failed.