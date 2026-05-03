---
description: "Prepare and publish a release tag from main; tag push triggers workflow automation for changelog and release notes"
name: "Setup Release"
argument-hint: "Optional release version (example: 3.31.0 or v3.31.0); leave blank to get a suggested next version"
agent: "agent"
---
Prepare a release in this repository from main.

Version handling:
- If a version argument is provided, use it.
- If no version argument is provided, suggest the next semantic version and ask for confirmation before making any changes.
- Use semantic versioning guidance for suggestion:
  - Major: breaking changes
  - Minor: backward-compatible features
  - Patch: minor changes and bug fixes
- Base suggestion on commit history and changelog context since the latest release tag.

Requirements:
- Release branch must be `main` only.
- CI and CodeQL must already be green on the target `main` commit before tagging.
- Tag push triggers `release.yml`, which calls `docker-build-publish.yml` (which calls `ci.yml` internally as a reusable workflow). The release workflow updates `CHANGELOG.md` and release notes automatically. If any stage fails, the release is blocked automatically.

Steps:
1. Resolve target version and normalize tag format:
- If argument is `3.31.0`, use `v3.31.0`.
- If argument is already `v3.31.0`, keep it.
- If argument is omitted, propose a next version with brief rationale and wait for user confirmation.

2. Validate repository state:
- Ensure local branch is `main`.
- Fetch and fast-forward local `main` to `origin/main`.
- Stop and report if working tree is not clean.
- Stop and report if the target tag already exists locally or remotely.
- Confirm CI and CodeQL are green for `HEAD`. If they cannot be checked automatically, stop and tell the user to verify them before continuing.

3. Create and verify tag on main:
- Prefer signed tag when signing is configured: `git tag -s <tag> -m "Release <tag>"`.
- If signing is not configured or signing fails due configuration, fallback to annotated tag: `git tag -a <tag> -m "Release <tag>"`.
- Verify tag points to the intended `main` commit.

4. Push tag:
- Push the tag to `origin`.
- Confirm the repository has the release tag on remote.

5. Report outcome:
- Return a short summary including:
  - release tag
  - tagged commit SHA
  - confirmation that changelog and release notes automation was triggered
  - confirmation that tag push triggers `release.yml`, which calls `docker-build-publish.yml` via `workflow_call`, which in turn calls `ci.yml` via `workflow_call`
- The chain fails automatically if CI fails — no separate gating needed. If any stage fails, inspect the workflow run logs, patch on a feature branch, merge to `main`, and retag from the updated commit.
- If Docker or release fails after CI success, tell the user to inspect workflow logs, patch on a feature branch, merge, and retag from updated `main`.
- If any step fails, stop immediately and report exact command/output needed to recover.

Output style:
- Keep response concise and operational.
- Use explicit pass/fail checkpoints for each step.
