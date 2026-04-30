# Feature 169: Dependabot Auto-Merge Security Signal Hardening

> **Status:** Planning

## Overview

Feature 165 re-audit confirmed that the NuGet package management simplification is now merge-ready. The remaining follow-up is a hardening task for Dependabot NuGet auto-merge: the current workflow excludes security-related pull requests by checking for a `security` label and the word `security` in the PR title.

That heuristic is reasonable, but it is not a first-class advisory signal. If GitHub label or title conventions drift, a security-related Dependabot NuGet PR could become eligible for auto-merge when the repository intends human review.

## Problem Statement

### Current State

- [dependabot-automerge.yml](.github/workflows/dependabot-automerge.yml#L14) enables auto-merge only for patch-level NuGet Dependabot PRs.
- The workflow excludes PRs when a `security` label is present or when the PR title contains `security`.
- The repository does not currently document or enforce a stronger advisory-specific signal for this decision.

### Target State

- Auto-merge security exclusions rely on the strongest signal GitHub and Dependabot make available for security updates.
- The workflow and CI/CD documentation describe the exact signal used.
- Maintainers can verify why a Dependabot PR was or was not eligible for auto-merge.

## Acceptance Criteria

- [ ] The auto-merge workflow uses an advisory-specific or Dependabot metadata-backed security signal when one is available.
- [ ] If GitHub does not expose a stronger signal in the workflow context, the repository documentation states that the current title and label heuristic is an accepted limitation.
- [ ] [docs/ci-cd-deployment.md](docs/ci-cd-deployment.md) documents the final security-review exclusion rule for Dependabot NuGet PRs.
- [ ] Validation notes confirm the workflow still auto-merges eligible patch-only NuGet PRs and still blocks security-related PRs from auto-merge.

## Implementation Tasks

- [ ] Inspect Dependabot and GitHub Actions metadata available to [dependabot-automerge.yml](.github/workflows/dependabot-automerge.yml).
- [ ] Replace the current heuristic with a stronger security-update signal if one exists.
- [ ] Otherwise, document the heuristic limitation and manual review expectation in [docs/ci-cd-deployment.md](docs/ci-cd-deployment.md).
- [ ] Capture validation evidence for both an eligible patch update path and a blocked security-update path.

## Open Questions

1. Does `dependabot/fetch-metadata` expose a reliable security-update indicator for NuGet PRs in this repository context?
2. If not, should the repository prefer a GitHub rule-based control outside the workflow instead of title and label heuristics?
