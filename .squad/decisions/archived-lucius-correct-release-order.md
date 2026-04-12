# Release Correction: v3.27.0 Created from Merged Squad Branch

**Executed by:** Lucius  
**Date:** 2026-04-12  
**Status:** Complete

## Decision
Create release tag v3.27.0 on origin/main after safely merging origin/squad into main.

## Rationale
The squad branch (which was tagged v3.26.0) contained audit-approved work (audit report publication, performance optimizations, code quality fixes) that needed to be merged to main and released as v3.27.0. The merge was verified as a clean fast-forward, ensuring no conflicts or unexpected behavior.

## Implementation
1. Created a clean git worktree on origin/main to avoid disturbing the current dirty squad working tree
2. Fetched latest from origin
3. Verified origin/squad is a descendant of origin/main (clean fast-forward)
4. Merged origin/squad → main using `git merge --ff-only`
5. Created annotated tag v3.27.0 with message: "Release v3.27.0: Merge squad branch (audit report publication, performance optimizations, code quality fixes)"
6. Pushed main and v3.27.0 tag to origin
7. Verified:
   - origin/main HEAD: 04e5ea5 (squad: merge audit report publication decisions)
   - v3.27.0 tag: points to 04e5ea5
   - v3.26.0 tag: unchanged, also points to 04e5ea5 (correct — same release commit)
   - GitHub Actions: Docker build workflow triggered for v3.27.0 tag (status: in_progress)
8. Cleaned up temporary worktree

## Result
- ✅ v3.27.0 created and pushed to origin
- ✅ origin/main updated to squad commit (04e5ea5)
- ✅ v3.26.0 left untouched
- ✅ No source files modified
- ✅ GitHub Actions release workflow started

The release is now live. Docker images (amd64, arm64) are building for both tags on the same commit.
