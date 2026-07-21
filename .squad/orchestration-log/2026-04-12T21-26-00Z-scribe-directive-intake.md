# Orchestration Log: Scribe — Release-Order Directive Intake & Process Documentation

**Timestamp:** 2026-04-12T21:26:00Z  
**Agent:** Scribe (Documentation)  
**Role:** Squad Memory & Decision Keeper  
**Status:** Completed  

---

## Request Summary

Capture user directive on releases and document the release-order correction workflow in squad decision ledger.

## Work Completed

### 1. Directive Merged into Decisions Registry

- **Source:** `.squad/decisions/inbox/copilot-directive-2026-04-12T21-26-00Z-release-from-main.md`
- **Destination:** `.squad/decisions.md` (appended as decision #6)
- **Content:** User directive requiring all releases to be cut from `main` branch, with context from the v3.26.0 correction.
- **Status:** ✅ Merged

### 2. Process Rule Established

**New Squad Rule:** All release tags must point to the canonical `main` branch.

- Releases are cut only after merging working branches (e.g., `squad`) into `main`
- Tags are created on `main` HEAD
- CI/CD workflows trigger from main-sourced tags
- Ensures consistency: GitHub Release artifacts, git tags, and Docker images all reference the same commit ancestry

### 3. Inbox File Cleared

- **File:** `.squad/decisions/inbox/copilot-directive-2026-04-12T21-26-00Z-release-from-main.md`
- **Action:** Blanked (content moved to decisions.md)
- **Status:** ✅ Cleared

---

## Decisions Written

- Decision #6 added to `.squad/decisions.md` with full directive text and implications
- New orchestration log entry created for this task (this file)

---

## Release-Order Correction Context

The v3.26.0 release was corrected by:
1. Merging `squad` branch into `main` (preserving commit history)
2. Force re-tagging v3.26.0 to point to the merge commit on `main`
3. Documenting the process rule to prevent recurrence

This task completes the **documentation step** of that correction. The release is now correctly sourced from `main`.

---
