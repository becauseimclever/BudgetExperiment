---
description: "Prepare and publish a release from main: refresh changelog, create tag, and push to trigger release workflow"
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
- Changelog must be fully up to date before tagging.
- Tag push must trigger the release workflow automatically.

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

3. Update `CHANGELOG.md` for this exact release tag:
- Use `cliff.toml` and regenerate the release entry for the target tag.
- Prefer command: `git cliff --config cliff.toml --tag <tag> --prepend CHANGELOG.md`.
- If `git cliff` is unavailable, explain what is missing and stop (do not continue with stale changelog).

4. Verify changelog quality:
- Confirm `CHANGELOG.md` now includes a new top release section for the target version.
- Ensure no obvious placeholder or malformed release block is introduced.
- Show a concise preview of the new section in the response.

5. Commit release metadata:
- Stage release-related files (for example `CHANGELOG.md` and release documentation updates).
- Commit with message: `chore(release): <tag>`.

6. Create and verify tag on main:
- Prefer signed tag when signing is configured: `git tag -s <tag> -m "Release <tag>"`.
- If signing is not configured or signing fails due configuration, fallback to annotated tag: `git tag -a <tag> -m "Release <tag>"`.
- Verify tag points to the new release commit on `main`.

7. Push commit and tag:
- Push `main` to `origin`.
- Push the tag to `origin`.
- Confirm the repository has both the release commit and tag on remote.

8. Report outcome:
- Return a short summary including:
  - release tag
  - release commit SHA
  - whether changelog was updated
  - confirmation that tag push should trigger `.github/workflows/release.yml`
- If any step fails, stop immediately and report exact command/output needed to recover.

Output style:
- Keep response concise and operational.
- Use explicit pass/fail checkpoints for each step.
